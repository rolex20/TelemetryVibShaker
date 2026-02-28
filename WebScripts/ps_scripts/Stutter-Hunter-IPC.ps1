<#!
MANUAL TESTS (Windows PowerShell 5.1)

1) Start server manually:
   powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Stutter-Hunter-IPC.ps1 -Mode Server

2) Add PID manually:
   powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Stutter-Hunter-IPC.ps1 -Mode Client -Action Add -ProcessId 1234 -GameProcessName ForzaHorizon5.exe

3) Remove PID manually:
   powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Stutter-Hunter-IPC.ps1 -Mode Client -Action Remove -ProcessId 1234

4) Verify only one long-lived server exists:
   - Run multiple ADD commands above.
   - Observe only one minimized powershell.exe running with "-Mode Server".
   - Client invocations should exit quickly.

Server behavior notes:
   - Server mode uses one long-lived NamedPipeServerStream and blocks on WaitForConnection().
   - One command is processed per connection, then the server replies once and disconnects.
   - SHUTDOWN is an internal command used by idle self-poke logic; clients do not need to call it.
#>

param(
    [ValidateSet('Client','Server')]
    [string]$Mode = 'Client',

    [ValidateSet('Add','Remove')]
    [string]$Action = 'Add',

    [int]$ProcessId,
    [string]$GameProcessName,
    [int]$SampleIntervalMs = 100,
    [double]$CpuSpikeThreshold = 25.0,
    [int]$HardFaultThreshold = 10,
    [int]$IdleExitSeconds = 15
)

. "$PSScriptRoot\Import-OptimizedCSharp.ps1"

$mutexName = 'Global\TelemetryVibShaker.StutterHunter.Server'
$pipeName = 'TelemetryVibShaker.StutterHunter'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (-not ('StutterHunterIpcRunner' -as [type])) {
$code = @'
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

public class StutterHunterIpcRunner
{
    private class Snapshot
    {
        public double TotalMs;
        public double KernelMs;
        public long WorkingSetBytes;
        public long Tick;
    }

    private class ThreadSnap
    {
        public double TotalMs;
        public double KernelMs;
    }

    private class ThreadHistory
    {
        public Dictionary<int, ThreadSnap> Threads;
        public long LastTick;
    }

    private static readonly ConcurrentDictionary<int, string> _targets = new ConcurrentDictionary<int, string>();
    private static readonly object _startStopLock = new object();
    private static volatile bool _stopRequested = false;
    private static volatile bool _running = false;
    private static Thread _thread;

    private static int _sampleIntervalMs = 100;
    private static double _cpuSpikeThreshold = 25.0;
    private static int _wsDeltaAnnotateThresholdMb = 10;
    private static int _logicalCpuCount = Environment.ProcessorCount;

    public static void Start(int sampleIntervalMs, double cpuSpikeThreshold, int wsDeltaAnnotateThresholdMb)
    {
        lock (_startStopLock)
        {
            _sampleIntervalMs = sampleIntervalMs;
            _cpuSpikeThreshold = cpuSpikeThreshold;
            _wsDeltaAnnotateThresholdMb = wsDeltaAnnotateThresholdMb;

            if (_running)
                return;

            _stopRequested = false;
            _thread = new Thread(new ThreadStart(MonitorLoop));
            _thread.IsBackground = true;
            _thread.Name = "StutterHunterIpcRunner";
            _thread.Start();
            _running = true;
        }
    }

    public static void Stop()
    {
        lock (_startStopLock)
        {
            _stopRequested = true;
            if (_thread != null)
            {
                try { _thread.Join(1500); } catch { }
                _thread = null;
            }
            _running = false;
        }
    }

    public static bool AddTarget(int pid, string processName)
    {
        if (pid <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(processName))
            processName = "<unknown>";

        _targets.AddOrUpdate(pid, processName, (k, v) => processName);
        return true;
    }

    public static bool RemoveTarget(int pid)
    {
        string oldName;
        return _targets.TryRemove(pid, out oldName);
    }

    public static int TargetCount()
    {
        return _targets.Count;
    }

    public static string ListTargets()
    {
        var arr = _targets.OrderBy(kv => kv.Key)
                          .Select(kv => kv.Key.ToString() + ":" + kv.Value)
                          .ToArray();
        return string.Join(",", arr);
    }

    private static void MonitorLoop()
    {
        Stopwatch sw = Stopwatch.StartNew();
        Dictionary<int, Snapshot> prev = new Dictionary<int, Snapshot>(128);
        Dictionary<int, ThreadHistory> threadHist = new Dictionary<int, ThreadHistory>(128);
        long threadRetentionTicks = (long)(Stopwatch.Frequency * 2.0);

        while (!_stopRequested)
        {
            long loopTick = sw.ElapsedTicks;
            double loopStartMs = sw.Elapsed.TotalMilliseconds;

            int[] targetPids = _targets.Keys.ToArray();
            HashSet<int> currentIds = new HashSet<int>();

            for (int i = 0; i < targetPids.Length; i++)
            {
                int pid = targetPids[i];
                Process p = null;

                try
                {
                    p = Process.GetProcessById(pid);
                }
                catch
                {
                    string oldName;
                    _targets.TryRemove(pid, out oldName);
                    prev.Remove(pid);
                    threadHist.Remove(pid);
                    continue;
                }

                if (p == null)
                    continue;

                currentIds.Add(pid);

                double totalMs;
                double kernelMs = 0.0;
                long wsBytes = 0;
                string prioLabel = "Unknown";
                string name = SafeName(p);
                if (string.IsNullOrWhiteSpace(name))
                    _targets.TryGetValue(pid, out name);

                try { totalMs = p.TotalProcessorTime.TotalMilliseconds; }
                catch { SafeDispose(p); continue; }

                try { kernelMs = p.PrivilegedProcessorTime.TotalMilliseconds; } catch { }
                try { wsBytes = p.WorkingSet64; } catch { }
                try { prioLabel = p.PriorityClass.ToString(); } catch { }

                Snapshot old;
                bool hadOld = prev.TryGetValue(pid, out old);

                ThreadHistory th;
                threadHist.TryGetValue(pid, out th);
                bool trackedRecently = (th != null && (loopTick - th.LastTick) <= threadRetentionTicks);
                bool shouldUpdateThreadsThisSample = trackedRecently;

                if (hadOld)
                {
                    double dtMs = TicksToMs(loopTick - old.Tick, Stopwatch.Frequency);
                    if (dtMs > 0.0)
                    {
                        double cpuDeltaMs = totalMs - old.TotalMs;
                        double cpuPercent = (cpuDeltaMs / dtMs) * 100.0;

                        double kernelDeltaMs = kernelMs - old.KernelMs;
                        double kernelRatio = (cpuDeltaMs > 0.0) ? (kernelDeltaMs / cpuDeltaMs) : 0.0;

                        double wsDeltaMb = Math.Abs(wsBytes - old.WorkingSetBytes) / (1024.0 * 1024.0);
                        bool isLag = (cpuPercent > _cpuSpikeThreshold);
                        bool wantThreadsNow = shouldUpdateThreadsThisSample || isLag;

                        Dictionary<int, ThreadSnap> oldThreads = (th != null) ? th.Threads : null;
                        Dictionary<int, ThreadSnap> newThreads = null;

                        if (wantThreadsNow)
                        {
                            newThreads = TrySnapshotThreads(p);

                            if (newThreads != null)
                            {
                                if (th == null)
                                {
                                    th = new ThreadHistory();
                                    threadHist[pid] = th;
                                }
                                th.Threads = newThreads;
                                th.LastTick = loopTick;
                            }
                            else if (th != null && shouldUpdateThreadsThisSample)
                            {
                                th.LastTick = loopTick;
                            }
                        }

                        if (isLag)
                        {
                            string danger = "CPU Spike";
                            ConsoleColor color = ConsoleColor.Gray;

                            if (prioLabel == "High" || prioLabel == "RealTime")
                            {
                                danger = "CRITICAL (P-Core Theft)";
                                color = ConsoleColor.Red;
                            }
                            else if (prioLabel == "AboveNormal")
                            {
                                danger = "Moderate";
                                color = ConsoleColor.Yellow;
                            }
                            else if (kernelRatio > 0.8)
                            {
                                danger = "Driver/AV Spike";
                                color = ConsoleColor.Magenta;
                            }

                            string ts = DateTime.Now.ToString("HH:mm:ss.fff");
                            double totalCpuPercent = cpuPercent / (double)_logicalCpuCount;

                            string details = string.Format(
                                " | CPUΔ: {0,6:N1}ms | K: {1,3:N0}% | TotalCPU: {2,5:N1}% (of {3})",
                                cpuDeltaMs,
                                kernelRatio * 100.0,
                                totalCpuPercent,
                                _logicalCpuCount
                            );

                            if (_wsDeltaAnnotateThresholdMb > 0 && wsDeltaMb >= _wsDeltaAnnotateThresholdMb)
                            {
                                details += string.Format(" | WSΔ: {0,6:N1}MB", wsDeltaMb);
                            }

                            details += BuildThreadDetails(oldThreads, newThreads);

                            string shortName = name;
                            if (string.IsNullOrWhiteSpace(shortName)) shortName = "<unknown>";
                            if (shortName.Length > 10) shortName = shortName.Substring(0, 10);

                            string msg = string.Format(
                                "[{0}] {1,-10} | PID: {2} | CPU: {3,5:N1}% | Prio: {4,-10} | Type: {5}{6}",
                                ts, shortName, pid, cpuPercent, prioLabel, danger, details
                            );

                            WriteColoredLine(msg, color);
                            if (color == ConsoleColor.Red)
                            {
                                try { Console.Beep(800, 100); } catch { }
                            }

                            if (th != null)
                                th.LastTick = loopTick;
                        }
                    }
                }
                else
                {
                    Dictionary<int, ThreadSnap> snap = TrySnapshotThreads(p);
                    if (snap != null)
                    {
                        if (th == null)
                        {
                            th = new ThreadHistory();
                            threadHist[pid] = th;
                        }
                        th.Threads = snap;
                        th.LastTick = loopTick;
                    }
                }

                prev[pid] = new Snapshot
                {
                    TotalMs = totalMs,
                    KernelMs = kernelMs,
                    WorkingSetBytes = wsBytes,
                    Tick = loopTick
                };

                SafeDispose(p);
            }

            if (prev.Count > 0)
            {
                List<int> keys = new List<int>(prev.Keys);
                for (int k = 0; k < keys.Count; k++)
                {
                    int id = keys[k];
                    if (!currentIds.Contains(id) || !_targets.ContainsKey(id))
                        prev.Remove(id);
                }
            }

            if (threadHist.Count > 0)
            {
                List<int> keys = new List<int>(threadHist.Keys);
                for (int k = 0; k < keys.Count; k++)
                {
                    int id = keys[k];
                    ThreadHistory h;
                    if (!threadHist.TryGetValue(id, out h))
                        continue;

                    bool removed = !_targets.ContainsKey(id) || !currentIds.Contains(id);
                    bool expired = (h != null) && ((loopTick - h.LastTick) > threadRetentionTicks);
                    if (removed || expired)
                        threadHist.Remove(id);
                }
            }

            double elapsedMs = sw.Elapsed.TotalMilliseconds - loopStartMs;
            int sleepMs = (int)Math.Round(_sampleIntervalMs - elapsedMs);
            if (sleepMs > 0)
                Thread.Sleep(sleepMs);
            else
                Thread.Sleep(10);
        }

        WriteColoredLine("Stopping Stutter-Hunter-IPC monitor...", ConsoleColor.DarkGray);
    }

    private static string BuildThreadDetails(Dictionary<int, ThreadSnap> oldThreads, Dictionary<int, ThreadSnap> newThreads)
    {
        if (newThreads == null)
            return " | Threads: n/a";

        if (oldThreads == null)
            return " | TopTID: n/a";

        int topTid = -1;
        double topDeltaMs = 0.0;
        double topKernelRatio = 0.0;
        int hotCount = 0;
        const double hotThresholdMs = 5.0;

        foreach (KeyValuePair<int, ThreadSnap> kv in newThreads)
        {
            int tid = kv.Key;
            ThreadSnap cur = kv.Value;
            ThreadSnap old;
            if (!oldThreads.TryGetValue(tid, out old))
                continue;

            double dTotal = cur.TotalMs - old.TotalMs;
            if (dTotal <= 0.0)
                continue;

            if (dTotal >= hotThresholdMs)
                hotCount++;

            if (dTotal > topDeltaMs)
            {
                double dKernel = cur.KernelMs - old.KernelMs;
                topDeltaMs = dTotal;
                topTid = tid;
                topKernelRatio = (dTotal > 0.0) ? (dKernel / dTotal) : 0.0;
            }
        }

        if (topTid == -1)
            return " | TopTID: n/a";

        return string.Format(
            " | TopTID: {0} TΔ: {1,5:N1}ms (K {2,3:N0}%) | HotThr>=5ms: {3}",
            topTid,
            topDeltaMs,
            topKernelRatio * 100.0,
            hotCount
        );
    }

    private static Dictionary<int, ThreadSnap> TrySnapshotThreads(Process p)
    {
        try
        {
            ProcessThreadCollection threads = p.Threads;
            if (threads == null)
                return null;

            Dictionary<int, ThreadSnap> snap = new Dictionary<int, ThreadSnap>(threads.Count);
            for (int i = 0; i < threads.Count; i++)
            {
                ProcessThread t = threads[i];
                int tid;
                try { tid = t.Id; } catch { continue; }

                double totalMs;
                double kernelMs = 0.0;
                try { totalMs = t.TotalProcessorTime.TotalMilliseconds; } catch { continue; }
                try { kernelMs = t.PrivilegedProcessorTime.TotalMilliseconds; } catch { }
                snap[tid] = new ThreadSnap { TotalMs = totalMs, KernelMs = kernelMs };
            }
            return snap;
        }
        catch
        {
            return null;
        }
    }

    private static double TicksToMs(long ticks, long freq)
    {
        return (ticks * 1000.0) / (double)freq;
    }

    private static string SafeName(Process p)
    {
        try { return p.ProcessName; } catch { return "<unknown>"; }
    }

    private static void SafeDispose(Process p)
    {
        try { p.Dispose(); } catch { }
    }

    private static void WriteColoredLine(string msg, ConsoleColor color)
    {
        ConsoleColor old = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
        }
        finally
        {
            Console.ForegroundColor = old;
        }
    }
}

public sealed class StutterHunterIdleWatcher : IDisposable
{
    private readonly string _pipeName;
    private readonly int _idleExitSeconds;
    private readonly Timer _timer;
    private readonly object _sync = new object();

    private DateTime _emptySinceUtc;
    private bool _isArmed;
    private bool _pokeSent;
    private bool _disposed;

    public StutterHunterIdleWatcher(string pipeName, int idleExitSeconds)
    {
        _pipeName = pipeName;
        _idleExitSeconds = Math.Max(1, idleExitSeconds);
        _emptySinceUtc = DateTime.UtcNow;
        _timer = new Timer(OnTick, null, 1000, 1000);
    }

    private void OnTick(object state)
    {
        lock (_sync)
        {
            if (_disposed || _pokeSent)
                return;

            int count = StutterHunterIpcRunner.TargetCount();

            if (count > 0)
            {
                _isArmed = false;
                _emptySinceUtc = DateTime.UtcNow;
                return;
            }

            if (!_isArmed)
            {
                _isArmed = true;
                _emptySinceUtc = DateTime.UtcNow;
                return;
            }

            double idleSeconds = (DateTime.UtcNow - _emptySinceUtc).TotalSeconds;
            if (idleSeconds < _idleExitSeconds)
                return;

            try
            {
                using (var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut))
                {
                    client.Connect(500);
                    using (var writer = new StreamWriter(client, Encoding.UTF8, 1024, true))
                    using (var reader = new StreamReader(client, Encoding.UTF8, false, 1024, true))
                    {                       
                        writer.WriteLine("SHUTDOWN");
						writer.Flush();
                        reader.ReadLine();
                    }
                }

                _pokeSent = true;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch
            {
                // Retry on next tick if server is transiently unavailable.
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _timer.Dispose();
        }
    }
}
'@

Import-OptimizedCSharp `
  -Source $code `
  -ExpectedTypeName 'StutterHunterIpcRunner' `
  -Platform 'AnyCPU' `
  -CallerScriptPath $PSCommandPath
}

function Invoke-StutterHunterPipeCommand {
    param(
        [Parameter(Mandatory)]
        [string]$CommandLine,
        [int]$ConnectTimeoutMs = 600
    )

    $ipcDebug = ($env:STUTTER_HUNTER_IPC_DEBUG -eq '1')
    function _D([string]$msg) {
        if ($ipcDebug) { Write-Host ("[IPC-CLIENT] " + $msg) -ForegroundColor DarkCyan }
    }

    function Read-LineWithTimeout {
        param(
            [Parameter(Mandatory)] $Reader,
            [int] $TimeoutMs = 5000,
            [string] $Label = 'line'
        )
        $task = $Reader.ReadLineAsync()
        if (-not $task.Wait($TimeoutMs)) {
            throw "Timeout waiting for $Label after ${TimeoutMs}ms"
        }
        return $task.Result
    }

    $client = $null
    $reader = $null
    $writer = $null
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)

    try {
        _D "creating NamedPipeClientStream pipeName='$pipeName'"
        $client = New-Object System.IO.Pipes.NamedPipeClientStream('.', $pipeName, [System.IO.Pipes.PipeDirection]::InOut)

        _D "CONNECT begin timeout=${ConnectTimeoutMs}ms"
        $client.Connect($ConnectTimeoutMs)
        _D ("CONNECT ok IsConnected={0}" -f $client.IsConnected)

        _D "constructing reader"
        $reader = New-Object System.IO.StreamReader($client, $utf8NoBom, $false, 1024, $true)

        _D "constructing writer (no AutoFlush; explicit Flush())"
        $writer = New-Object System.IO.StreamWriter($client, $utf8NoBom, 1024, $true)

        _D ("WRITE begin: '{0}'" -f $CommandLine)
        $writer.WriteLine($CommandLine)
        $writer.Flush()
        _D "WRITE ok (flushed)"

        _D "READ begin (reply)"
        $reply = Read-LineWithTimeout -Reader $reader -TimeoutMs 5000 -Label 'server reply'
        _D ("READ ok reply='{0}'" -f $reply)

        if ([string]::IsNullOrWhiteSpace($reply)) {
            return @{ Ok = $false; Reply = 'ERR empty reply' }
        }

        return @{ Ok = $reply.StartsWith('OK'); Reply = $reply }
    }
    catch {
        _D ("EXCEPTION: {0}" -f $_.Exception.Message)
        return @{ Ok = $false; Reply = "ERR $($_.Exception.Message)" }
    }
    finally {
        if ($writer) { try { $writer.Dispose() } catch {} }
        if ($reader) { try { $reader.Dispose() } catch {} }
        if ($client) { try { $client.Dispose() } catch {} }
        _D "cleanup complete"
    }
}

function Start-StutterHunterServerIfNeeded {
    $scriptPath = Join-Path $PSScriptRoot 'Stutter-Hunter-IPC.ps1'
    Start-Process powershell.exe -WindowStyle Minimized -ArgumentList @(
        '-NoLogo','-NoProfile','-ExecutionPolicy','Bypass',
        '-File', $scriptPath,
        '-Mode', 'Server'
    ) | Out-Null
}

function Invoke-StutterHunterClient {
    if ($Action -eq 'Add' -and [string]::IsNullOrWhiteSpace($GameProcessName)) {
        throw 'GameProcessName is required when Action is Add.'
    }
    if ($ProcessId -le 0) {
        throw 'ProcessId must be greater than zero.'
    }

    $commandLine = if ($Action -eq 'Add') {
        "ADD $ProcessId $GameProcessName"
    }
    else {
        "REMOVE $ProcessId"
    }

    $result = Invoke-StutterHunterPipeCommand -CommandLine $commandLine -ConnectTimeoutMs 500
    if (-not $result.Ok) {
        Start-StutterHunterServerIfNeeded

        for ($i = 0; $i -lt 10; $i++) {
            Start-Sleep -Milliseconds 200
            $result = Invoke-StutterHunterPipeCommand -CommandLine $commandLine -ConnectTimeoutMs 700
            if ($result.Ok) {
                break
            }
        }
    }

    if ($result.Ok) {
        Write-Host $result.Reply -ForegroundColor Green
        exit 0
    }

    Write-Host $result.Reply -ForegroundColor Red
    exit 2
}

function Start-StutterHunterServer {
    $ipcDebug = ($env:STUTTER_HUNTER_IPC_DEBUG -eq '1')
    function _D([string]$msg) {
        if ($ipcDebug) { Write-Host ("[IPC-SERVER] " + $msg) -ForegroundColor DarkCyan }
    }

    function Read-LineWithTimeout {
        param(
            [Parameter(Mandatory)] $Reader,
            [int] $TimeoutMs = 5000,
            [string] $Label = 'line'
        )
        $task = $Reader.ReadLineAsync()
        if (-not $task.Wait($TimeoutMs)) {
            throw "Timeout waiting for $Label after ${TimeoutMs}ms"
        }
        return $task.Result
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)

    _D "acquiring mutex name='$mutexName'"
    $createdNew = $false
    $mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$createdNew)

    if (-not $createdNew) {
        Write-Host 'Stutter-Hunter-IPC server already running.' -ForegroundColor DarkYellow
        if ($mutex) { $mutex.Dispose() }
        return
    }

    Write-Host 'Starting Stutter-Hunter-IPC server...' -ForegroundColor Cyan
    _D "starting StutterHunterIpcRunner SampleIntervalMs=$SampleIntervalMs CpuSpikeThreshold=$CpuSpikeThreshold HardFaultThreshold=$HardFaultThreshold"
    [StutterHunterIpcRunner]::Start($SampleIntervalMs, $CpuSpikeThreshold, $HardFaultThreshold)

    _D "starting idle watcher IdleExitSeconds=$IdleExitSeconds"
    $idleWatcher = New-Object StutterHunterIdleWatcher($pipeName, $IdleExitSeconds)

    $stopRequested = $false
    $pipe = $null

    try {
        _D "creating NamedPipeServerStream pipeName='$pipeName' maxInstances=1"
        $pipe = New-Object System.IO.Pipes.NamedPipeServerStream(
            $pipeName,
            [System.IO.Pipes.PipeDirection]::InOut,
            1,
            [System.IO.Pipes.PipeTransmissionMode]::Byte,
            [System.IO.Pipes.PipeOptions]::None
        )

        while (-not $stopRequested) {
            Write-Host "PIPE: waiting for client..." -ForegroundColor DarkCyan
            _D "WAIT begin (WaitForConnection)"
            $pipe.WaitForConnection()
            _D "WAIT end (client connected)"
            Write-Host "PIPE: client connected" -ForegroundColor DarkCyan

            if (-not $pipe.IsConnected) {
                _D "pipe not connected after WaitForConnection() (unexpected). continuing."
                continue
            }

            $reader = $null
            $writer = $null

            try {
                _D ("pipe state IsConnected={0} CanRead={1} CanWrite={2}" -f $pipe.IsConnected, $pipe.CanRead, $pipe.CanWrite)

                _D "STEP A: constructing reader (UTF8 no BOM, leaveOpen=$true)"
                $reader = New-Object System.IO.StreamReader($pipe, $utf8NoBom, $false, 1024, $true)

                _D "STEP B: constructing writer (UTF8 no BOM, leaveOpen=$true, no AutoFlush)"
                $writer = New-Object System.IO.StreamWriter($pipe, $utf8NoBom, 1024, $true)

                _D "STEP C: before ReadLine() (request)"
                Write-Host "... before ReadLine()..." -ForegroundColor DarkGray
                $line = Read-LineWithTimeout -Reader $reader -TimeoutMs 5000 -Label 'request line'
                Write-Host "... after ReadLine()..." -ForegroundColor DarkGray
                _D ("STEP D: after ReadLine() line='{0}'" -f $line)

                $reply = $null

                if ([string]::IsNullOrWhiteSpace($line)) {
                    $reply = 'ERR empty command'
                }
                else {
                    $parts = $line.Trim().Split(' ')
                    $verb = $parts[0].ToUpperInvariant()

                    switch ($verb) {
                        'PING' {
                            $reply = 'OK PONG'
                        }
                        'LIST' {
                            $reply = "OK $([StutterHunterIpcRunner]::ListTargets())"
                        }
                        'ADD' {
                            if ($parts.Length -lt 3) {
                                $reply = 'ERR usage: ADD <pid> <processName>'
                            }
                            else {
                                $procid = 0
                                if (-not [int]::TryParse($parts[1], [ref]$procid)) {
                                    $reply = 'ERR invalid pid'
                                }
                                else {
                                    $name = ($line.Substring($line.IndexOf($parts[2]))).Trim()
                                    [void][StutterHunterIpcRunner]::AddTarget($procid, $name)
                                    $reply = "OK ADDED $procid"
                                }
                            }
                        }
                        'REMOVE' {
                            if ($parts.Length -lt 2) {
                                $reply = 'ERR usage: REMOVE <pid>'
                            }
                            else {
                                $procid = 0
                                if (-not [int]::TryParse($parts[1], [ref]$procid)) {
                                    $reply = 'ERR invalid pid'
                                }
                                else {
                                    [void][StutterHunterIpcRunner]::RemoveTarget($procid)
                                    $reply = "OK REMOVED $procid"
                                }
                            }
                        }
                        'SHUTDOWN' {
                            $reply = 'OK SHUTDOWN'
                            $stopRequested = $true
                        }
                        default {
                            $reply = 'ERR unknown command'
                        }
                    }
                }

                _D ("WRITE reply begin: '{0}'" -f $reply)
                $writer.WriteLine($reply)
                $writer.Flush()
                _D "WRITE reply ok (flushed)"
            }
            catch {
                _D ("handler EXCEPTION: {0}" -f $_.Exception.Message)
                try {
                    if ($writer) {
                        $writer.WriteLine("ERR $($_.Exception.Message)")
                        $writer.Flush()
                    }
                } catch {}
            }
            finally {
                if ($writer) { try { $writer.Dispose() } catch {} }
                if ($reader) { try { $reader.Dispose() } catch {} }
            }

            if ($pipe -and $pipe.IsConnected) {
                _D "disconnecting pipe"
                try { $pipe.Disconnect() } catch {}
            }

            if ($stopRequested) {
                Write-Host 'Received internal shutdown signal. Exiting server.' -ForegroundColor DarkGray
                _D "stopRequested true -> exiting loop"
            }
        }
    }
    finally {
        _D "server cleanup begin"
        if ($idleWatcher) { try { $idleWatcher.Dispose() } catch {} }
        [StutterHunterIpcRunner]::Stop()
        if ($pipe) {
            if ($pipe.IsConnected) {
                try { $pipe.Disconnect() } catch {}
            }
            try { $pipe.Dispose() } catch {}
        }
        if ($mutex) {
            try { $mutex.ReleaseMutex() } catch {}
            $mutex.Dispose()
        }
        _D "server cleanup complete"
    }
}

if ($Mode -eq 'Client') {
    Invoke-StutterHunterClient
}
else {
    Start-StutterHunterServer
}
