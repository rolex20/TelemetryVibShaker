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
   - Server mode uses one long-lived NamedPipeServerStream and blocks until a client connects or the one-shot idle timer expires.
   - One command is processed per connection, then the server replies once and disconnects.
   - Server exits after five minutes with no monitored targets by default.
   - Target changes and process-exit events arm/cancel the idle timer; no idle polling.
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
    [int]$IdleExitSeconds = 300
)


function Restore-PowerShellWindow {
    if (-not ('PSWindowRestore' -as [type])) {
        Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class PSWindowRestore {
    public delegate bool EnumProc(IntPtr hWnd, IntPtr parameter);
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int command);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumProc callback, IntPtr parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int count);
    public static IntPtr FindVisibleWindow(string marker) {
        IntPtr result = IntPtr.Zero; IntPtr consoleFallback = IntPtr.Zero;
        EnumWindows(delegate(IntPtr hWnd, IntPtr parameter) {
            var text = new StringBuilder(1024);
            GetWindowText(hWnd, text, text.Capacity);
            if (IsWindowVisible(hWnd) && text.ToString().Contains(marker)) {
                var className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                if (className.ToString() == "ConsoleWindowClass") { consoleFallback = hWnd; return true; }
                result = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return result != IntPtr.Zero ? result : consoleFallback;
    }
}
"@
    }

    $handle = [PSWindowRestore]::GetConsoleWindow()

    # Windows Terminal uses a hidden console handle. Find its visible window
    # using a temporary, unique title instead.
    if ($env:WT_SESSION -or -not [PSWindowRestore]::IsWindowVisible($handle)) {
        $oldTitle = $Host.UI.RawUI.WindowTitle
        $marker = 'PS-Restore-' + [guid]::NewGuid().ToString('N')
        try {
            $Host.UI.RawUI.WindowTitle = $marker
            $handle = [IntPtr]::Zero
            for ($attempt = 0; $attempt -lt 30; $attempt++) {
                Start-Sleep -Milliseconds 300
                $handle = [PSWindowRestore]::FindVisibleWindow($marker)
                if ($handle -ne [IntPtr]::Zero) { break }
            }
        }
        finally {
            $Host.UI.RawUI.WindowTitle = $oldTitle
        }
    }

    if ($handle -eq [IntPtr]::Zero) {
        throw 'Cannot find the visible host window. Windows Terminal may suppress application titles, or this script may have no visible host.'
    }

    [void][PSWindowRestore]::ShowWindow($handle, 9) # Restore
    [void][PSWindowRestore]::ShowWindow($handle, 1) # Normal
    [void][PSWindowRestore]::SetForegroundWindow($handle)

    if ([PSWindowRestore]::IsIconic($handle)) {
        throw 'The host window is still minimized.'
    }
}



if ($Mode -eq 'Client') { # Server logs later
    Write-Host "Requesting Stutter-Hunter [Action $Action] for [$GameProcessName] [PID $ProcessId] ..." -ForegroundColor Cyan
}

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
    private class TargetRegistration
    {
        public Process Process;
        public EventHandler Exited;
    }

    private static readonly object _targetLock = new object();
    private static readonly Dictionary<int, TargetRegistration> _registrations = new Dictionary<int, TargetRegistration>();
    private static readonly ManualResetEvent _idleExpired = new ManualResetEvent(false);
    private static readonly AutoResetEvent _targetsChanged = new AutoResetEvent(false);
    private static Timer _idleTimer;
    private static object _idleGeneration;
    private static int _idleExitSeconds;
    private static bool _acceptingTargets;
    private static readonly object _startStopLock = new object();

    public static WaitHandle IdleExpired { get { return _idleExpired; } }

    // Call only under _targetLock. A generation rejects callbacks already queued
    // when a target was added or a newer idle period began.
    private static void CancelIdleTimer()
    {
        _idleGeneration = null;
        if (_idleTimer != null) { _idleTimer.Dispose(); _idleTimer = null; }
    }

    private static void ArmIdleTimer()
    {
        CancelIdleTimer();
        object generation = new object();
        _idleGeneration = generation;
        _idleTimer = new Timer(delegate(object state) {
            lock (_targetLock)
            {
                if (!_acceptingTargets || !object.ReferenceEquals(state, _idleGeneration) || !_targets.IsEmpty)
                    return;

                _acceptingTargets = false;
                _idleExpired.Set();
            }
        }, generation, TimeSpan.FromSeconds(_idleExitSeconds), Timeout.InfiniteTimeSpan);
    }
    private static volatile bool _stopRequested = false;
    private static volatile bool _running = false;
    private static Thread _thread;

    private static int _sampleIntervalMs = 100;
    private static double _cpuSpikeThreshold = 25.0;
    private static int _wsDeltaAnnotateThresholdMb = 10;
    private static int _logicalCpuCount = Environment.ProcessorCount;

    public static void Start(int sampleIntervalMs, double cpuSpikeThreshold, int wsDeltaAnnotateThresholdMb, int idleExitSeconds)
    {
        lock (_startStopLock)
        {
            _sampleIntervalMs = sampleIntervalMs;
            _cpuSpikeThreshold = cpuSpikeThreshold;
            _wsDeltaAnnotateThresholdMb = wsDeltaAnnotateThresholdMb;

            if (_running)
                return;

            lock (_targetLock)
            {
                _idleExitSeconds = Math.Max(1, idleExitSeconds);
                _idleExpired.Reset();
                _acceptingTargets = true;
                ArmIdleTimer();
            }
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
            _targetsChanged.Set();
            if (_thread != null)
            {
                try { _thread.Join(1500); } catch { }
                _thread = null;
            }
            lock (_targetLock)
            {
                _acceptingTargets = false;
                CancelIdleTimer();
                foreach (TargetRegistration registration in _registrations.Values)
                {
                    registration.Process.Exited -= registration.Exited;
                    registration.Process.Dispose();
                }
                _registrations.Clear();
                _targets.Clear();
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

        lock (_targetLock)
        {
            if (!_acceptingTargets)
                return false;

            if (_registrations.ContainsKey(pid))
            {
                _targets[pid] = processName;
                return true;
            }

            Process process = null;
            EventHandler exited = null;
            try
            {
                process = Process.GetProcessById(pid);
                exited = delegate {
                    lock (_targetLock)
                    {
                        TargetRegistration current;
                        if (_registrations.TryGetValue(pid, out current) && object.ReferenceEquals(current.Process, process))
                            RemoveTarget(pid);
                    }
                };
                process.Exited += exited;
                process.EnableRaisingEvents = true;
                if (process.HasExited)
                {
                    process.Exited -= exited;
                    process.Dispose();
                    return false;
                }
            }
            catch (ArgumentException) { SafeDispose(process); return false; }
            catch (InvalidOperationException) { SafeDispose(process); return false; }
            catch (System.ComponentModel.Win32Exception) { SafeDispose(process); return false; }

            _registrations.Add(pid, new TargetRegistration { Process = process, Exited = exited });
            _targets[pid] = processName;
            CancelIdleTimer();
            _targetsChanged.Set();
            return true;
        }
    }

    public static bool RemoveTarget(int pid)
    {
        lock (_targetLock)
        {
            string oldName;
            if (!_targets.TryRemove(pid, out oldName))
                return false;

            TargetRegistration registration;
            if (_registrations.TryGetValue(pid, out registration))
            {
                _registrations.Remove(pid);
                registration.Process.Exited -= registration.Exited;
                registration.Process.Dispose();
            }
            if (_targets.IsEmpty && _acceptingTargets)
                ArmIdleTimer();
            return true;
        }
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
            if (_targets.IsEmpty)
            {
                prev.Clear();
                threadHist.Clear();
                _targetsChanged.WaitOne();
                continue;
            }

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
                    RemoveTarget(pid);
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
    #Start-Process powershell.exe -ArgumentList @(
        #'-NoExit', 
		'-NoLogo','-NoProfile','-ExecutionPolicy','Bypass',
        '-File', "`"$scriptPath`"",
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

    $result = Invoke-StutterHunterPipeCommand -CommandLine $commandLine -ConnectTimeoutMs 200
    if (-not $result.Ok) {
        Start-StutterHunterServerIfNeeded

        for ($i = 0; $i -lt 10; $i++) {
            Start-Sleep -Milliseconds 200
            $result = Invoke-StutterHunterPipeCommand -CommandLine $commandLine -ConnectTimeoutMs 500
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

    $stopRequested = $false
    $pipe = $null

    try {
        Write-Host 'Starting Stutter-Hunter-IPC server...' -ForegroundColor Cyan
        Restore-PowerShellWindow
        _D "creating NamedPipeServerStream pipeName='$pipeName' maxInstances=1"
        $pipe = New-Object System.IO.Pipes.NamedPipeServerStream(
            $pipeName,
            [System.IO.Pipes.PipeDirection]::InOut,
            1,
            [System.IO.Pipes.PipeTransmissionMode]::Byte,
            [System.IO.Pipes.PipeOptions]::Asynchronous
        )

        [StutterHunterIpcRunner]::Start($SampleIntervalMs, $CpuSpikeThreshold, $HardFaultThreshold, $IdleExitSeconds)

        while (-not $stopRequested) {
            _D "WAIT begin (connection or idle expiry)"
            $connectionTask = $pipe.WaitForConnectionAsync()
            $connectionWaitHandle = ([System.IAsyncResult]$connectionTask).AsyncWaitHandle
            try {
                # Both handles are signaled by events. No periodic wakeups.
                $signaled = [System.Threading.WaitHandle]::WaitAny([System.Threading.WaitHandle[]]@(
                    [StutterHunterIpcRunner]::IdleExpired,
                    $connectionWaitHandle
                ))
                if ($signaled -eq 0) {
                    Write-Host "No monitored targets for $IdleExitSeconds seconds. Exiting server." -ForegroundColor DarkGray
                    break
                }
                $connectionTask.GetAwaiter().GetResult()
            }
            finally {
                # On idle expiry the connection may still be pending. Disposing
                # the pipe cancels it before its task/wait handle are released.
                if (-not $connectionTask.IsCompleted) {
                    $pipe.Dispose()
                    try { $connectionTask.GetAwaiter().GetResult() } catch {}
                }
                $connectionTask.Dispose()
            }
            _D "WAIT end (client connected)"

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
                $line = Read-LineWithTimeout -Reader $reader -TimeoutMs 5000 -Label 'request line'               
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
                                    Write-Host ("Adding target PID={0} Name='{1}'" -f $procid, $name) -ForegroundColor Green
                                    if ([StutterHunterIpcRunner]::AddTarget($procid, $name)) {
                                        $reply = "OK ADDED $procid"
                                    }
                                    else {
                                        $reply = 'ERR target unavailable or server shutting down'
                                    }
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
                                    Write-Host ("Removing target PID={0} Name='{1}'" -f $procid, $name) -ForegroundColor Green
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
