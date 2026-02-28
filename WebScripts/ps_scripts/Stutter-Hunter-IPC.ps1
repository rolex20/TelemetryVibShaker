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

if (-not ('StutterHunterIpcRunner' -as [type])) {
$code = @'
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    $client = $null
    $reader = $null
    $writer = $null

    try {
        $client = New-Object System.IO.Pipes.NamedPipeClientStream('.', $pipeName, [System.IO.Pipes.PipeDirection]::InOut)
        $client.Connect($ConnectTimeoutMs)

        $reader = New-Object System.IO.StreamReader($client, [System.Text.Encoding]::UTF8)
        $writer = New-Object System.IO.StreamWriter($client, [System.Text.Encoding]::UTF8)
        $writer.AutoFlush = $true

        $writer.WriteLine($CommandLine)
        $reply = $reader.ReadLine()

        if ([string]::IsNullOrWhiteSpace($reply)) {
            return @{ Ok = $false; Reply = 'ERR empty reply' }
        }

        return @{ Ok = $reply.StartsWith('OK'); Reply = $reply }
    }
    catch {
        return @{ Ok = $false; Reply = "ERR $($_.Exception.Message)" }
    }
    finally {
        if ($writer) { $writer.Dispose() }
        if ($reader) { $reader.Dispose() }
        if ($client) { $client.Dispose() }
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
    $createdNew = $false
    $mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$createdNew)

    if (-not $createdNew) {
        Write-Host 'Stutter-Hunter-IPC server already running.' -ForegroundColor DarkYellow
        if ($mutex) { $mutex.Dispose() }
        return
    }

    Write-Host 'Starting Stutter-Hunter-IPC server...' -ForegroundColor Cyan

    [StutterHunterIpcRunner]::Start($SampleIntervalMs, $CpuSpikeThreshold, $HardFaultThreshold)

    $lastEmptySince = [datetime]::UtcNow

    try {
        while ($true) {
            $pipe = New-Object System.IO.Pipes.NamedPipeServerStream(
                $pipeName,
                [System.IO.Pipes.PipeDirection]::InOut,
                1,
                [System.IO.Pipes.PipeTransmissionMode]::Byte,
                [System.IO.Pipes.PipeOptions]::Asynchronous
            )

            $connected = $false
            $async = $pipe.BeginWaitForConnection($null, $null)
            try {
                if ($async.AsyncWaitHandle.WaitOne(1000)) {
                    $pipe.EndWaitForConnection($async)
                    $connected = $true
                }
            }
            catch {
                $connected = $false
            }

            if ($connected) {
                $reader = $null
                $writer = $null
                try {
                    $reader = New-Object System.IO.StreamReader($pipe, [System.Text.Encoding]::UTF8)
                    $writer = New-Object System.IO.StreamWriter($pipe, [System.Text.Encoding]::UTF8)
                    $writer.AutoFlush = $true

                    $line = $reader.ReadLine()
                    if ([string]::IsNullOrWhiteSpace($line)) {
                        $writer.WriteLine('ERR empty command')
                    }
                    else {
                        $parts = $line.Trim().Split(' ')
                        $verb = $parts[0].ToUpperInvariant()

                        switch ($verb) {
                            'PING' {
                                $writer.WriteLine('OK PONG')
                            }
                            'LIST' {
                                $writer.WriteLine("OK $([StutterHunterIpcRunner]::ListTargets())")
                            }
                            'ADD' {
                                if ($parts.Length -lt 3) {
                                    $writer.WriteLine('ERR usage: ADD <pid> <processName>')
                                }
                                else {
                                    $pid = 0
                                    if (-not [int]::TryParse($parts[1], [ref]$pid)) {
                                        $writer.WriteLine('ERR invalid pid')
                                    }
                                    else {
                                        $name = ($line.Substring($line.IndexOf($parts[2]))).Trim()
                                        [void][StutterHunterIpcRunner]::AddTarget($pid, $name)
                                        $writer.WriteLine("OK ADDED $pid")
                                    }
                                }
                            }
                            'REMOVE' {
                                if ($parts.Length -lt 2) {
                                    $writer.WriteLine('ERR usage: REMOVE <pid>')
                                }
                                else {
                                    $pid = 0
                                    if (-not [int]::TryParse($parts[1], [ref]$pid)) {
                                        $writer.WriteLine('ERR invalid pid')
                                    }
                                    else {
                                        [void][StutterHunterIpcRunner]::RemoveTarget($pid)
                                        $writer.WriteLine("OK REMOVED $pid")
                                    }
                                }
                            }
                            default {
                                $writer.WriteLine('ERR unknown command')
                            }
                        }
                    }
                }
                catch {
                    if ($writer) {
                        try { $writer.WriteLine("ERR $($_.Exception.Message)") } catch {}
                    }
                }
                finally {
                    if ($writer) { $writer.Dispose() }
                    if ($reader) { $reader.Dispose() }
                }
            }

            if ($pipe) {
                try { $pipe.Dispose() } catch {}
            }

            if ([StutterHunterIpcRunner]::TargetCount() -eq 0) {
                $idleSeconds = ([datetime]::UtcNow - $lastEmptySince).TotalSeconds
                if ($idleSeconds -ge $IdleExitSeconds) {
                    Write-Host "No monitored PIDs for $IdleExitSeconds seconds. Exiting server." -ForegroundColor DarkGray
                    break
                }
            }
            else {
                $lastEmptySince = [datetime]::UtcNow
            }
        }
    }
    finally {
        [StutterHunterIpcRunner]::Stop()
        if ($mutex) {
            try { $mutex.ReleaseMutex() } catch {}
            $mutex.Dispose()
        }
    }
}

if ($Mode -eq 'Client') {
    Invoke-StutterHunterClient
}
else {
    Start-StutterHunterServer
}
