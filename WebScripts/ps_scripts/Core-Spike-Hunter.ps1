<#
.SYNOPSIS
    Detects per-logical-processor utility spikes and reports the busiest
    process/thread candidates observed in the same sampling window.

.DESCRIPTION
    PowerShell 5.1 proof of concept for a future /perf integration.

    The trigger is the English PDH counter:
        \Processor Information(<group>,<processor>)\% Processor Utility

    When any logical processor reaches ThresholdPercent, the script prints:
      - local timestamp and measured window
      - hot processor group/logical processor and utility
      - busiest thread candidates in that same window
      - PID, TID, thread CPU delta/percent, process CPU delta/percent,
        kernel ratio, and thread priorities

    If TopThreads is omitted (or set to zero), candidates are printed in
    descending CPU order until their cumulative thread CPU time accounts for
    the estimated busy time of the hot logical processors. If inaccessible,
    newly created, or system threads prevent full accounting, all measurable
    candidates are printed and the final coverage remains below 100%.

    IMPORTANT ATTRIBUTION LIMIT:
    Performance counters and ProcessThread CPU totals are independent data
    sources. They prove which logical processors were hot and which threads
    consumed CPU during the same window, but they do not preserve an exact
    thread-to-processor relationship. Therefore the output says CORRELATED.
    Exact attribution requires scheduler context-switch ETW events (CSwitch),
    for example a WPR trace inspected with WPA CPU Usage (Precise).

.PARAMETER ThresholdPercent
    Per-logical-processor utility that triggers an event. % Processor Utility
    can exceed 100 on systems whose current performance exceeds the nominal
    reference frequency.

.PARAMETER SampleIntervalMs
    Requested sampling interval. Short intervals find shorter spikes but make
    the all-process/all-thread snapshot more expensive. Start with 250-500 ms.

.PARAMETER TopThreads
    Maximum number of busiest thread candidates to print for each event.
    When omitted or zero, prints as many candidates as necessary to account
    for the hot-processor workload, or all measurable candidates if necessary.

.PARAMETER ProcessId
    Optional PID allow-list. Core monitoring remains system-wide, but thread
    candidates are limited to these PIDs. Omit it to inspect all processes.
    Supplying it also activates a faster direct-PID snapshot path.

.PARAMETER IncludeSelf
    Includes this powershell.exe process among candidates. It is excluded by
    default so the detector does not nominate its own polling work. When an
    included candidate has the monitor's exact PID, its name starts with
    "Self:" and its line is printed in magenta.

.PARAMETER DurationSeconds
    Stops automatically after this many seconds. Zero means run until Ctrl+C.

.EXAMPLE
    .\Core-Spike-Hunter.ps1 -ThresholdPercent 90 -SampleIntervalMs 250

.EXAMPLE
    .\Core-Spike-Hunter.ps1 -ThresholdPercent 80 -ProcessId 1234,5678 -TopThreads 5

.NOTES
    Windows PowerShell 5.1 compatible. "LP" means logical processor, not a
    guaranteed physical-core number. Processor groups are shown explicitly.
    Automatic coverage is a time-window correlation, not an exact LP-to-thread
    assignment. Exact assignment requires ETW scheduler CSwitch events.
    System-wide attribution must read every accessible thread every interval;
    that enumeration remains the dominant cost. Use ProcessId when the relevant
    processes are known, or a longer SampleIntervalMs to reduce monitor load.
#>

[CmdletBinding()]
param(
    [ValidateRange(0.1, 1000.0)]
    [double]$ThresholdPercent = 90.0,

    [ValidateRange(50, 60000)]
    [int]$SampleIntervalMs = 250,

    [ValidateRange(0, 10000)]
    [int]$TopThreads = 0,

    [ValidateScript({ $_ -gt 0 })]
    [int[]]$ProcessId = @(),

    [switch]$IncludeSelf,

    [ValidateRange(0, 86400)]
    [int]$DurationSeconds = 0
)

. "$PSScriptRoot\Import-OptimizedCSharp.ps1"

if (-not ("CoreSpikeHunterRunner" -as [type])) {
    $code = @'
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public static class CoreSpikeHunterRunner
{
    private const uint ERROR_SUCCESS = 0;
    private const uint PDH_CSTATUS_VALID_DATA = 0;
    private const uint PDH_CSTATUS_NEW_DATA = 1;
    private const uint PDH_FMT_DOUBLE = 0x00000200;

    private static volatile bool _stopRequested;

    private sealed class CpuCounter : IDisposable
    {
        public ushort Group;
        public uint Number;
        public IntPtr Handle;

        public string Label
        {
            get { return string.Format(CultureInfo.InvariantCulture, "G{0}:LP{1}", Group, Number); }
        }

        public void Dispose()
        {
            Handle = IntPtr.Zero; // The owning PDH query releases counter handles.
        }
    }

    private sealed class PdhCpuQuery : IDisposable
    {
        private IntPtr _query;
        private readonly List<CpuCounter> _counters = new List<CpuCounter>();
        private string _counterName;

        public IList<CpuCounter> Counters { get { return _counters; } }
        public string CounterName { get { return _counterName; } }

        public PdhCpuQuery()
        {
            uint status = PdhOpenQuery(null, UIntPtr.Zero, out _query);
            ThrowIfPdhError(status, "PdhOpenQuery");

            // Prefer frequency-aware Utility, then fall back to busy time.
            if (!TryAddCounters("% Processor Utility"))
            {
                _counters.Clear();
                if (!TryAddCounters("% Processor Time"))
                    throw new InvalidOperationException("Unable to add per-processor PDH counters.");
            }

            status = PdhCollectQueryData(_query); // Prime rate counters.
            ThrowIfPdhError(status, "PdhCollectQueryData (prime)");
        }

        private bool TryAddCounters(string counterName)
        {
            ushort groupCount = GetActiveProcessorGroupCount();
            if (groupCount == 0 || groupCount == ushort.MaxValue)
                groupCount = 1;

            List<IntPtr> added = new List<IntPtr>();

            for (ushort group = 0; group < groupCount; group++)
            {
                uint processorCount = GetActiveProcessorCount(group);
                if (processorCount == 0 || processorCount == UInt32.MaxValue)
                    processorCount = (group == 0) ? (uint)Environment.ProcessorCount : 0;

                for (uint number = 0; number < processorCount; number++)
                {
                    string path = string.Format(
                        CultureInfo.InvariantCulture,
                        "\\Processor Information({0},{1})\\{2}",
                        group,
                        number,
                        counterName);

                    IntPtr counter;
                    uint status = PdhAddEnglishCounter(_query, path, UIntPtr.Zero, out counter);
                    if (status != ERROR_SUCCESS)
                    {
                        // A partial set would make event interpretation unsafe.
                        for (int i = 0; i < added.Count; i++)
                            PdhRemoveCounter(added[i]);
                        return false;
                    }

                    added.Add(counter);
                    _counters.Add(new CpuCounter { Group = group, Number = number, Handle = counter });
                }
            }

            _counterName = counterName;
            return _counters.Count > 0;
        }

        public Dictionary<CpuCounter, double> Collect()
        {
            uint status = PdhCollectQueryData(_query);
            ThrowIfPdhError(status, "PdhCollectQueryData");

            Dictionary<CpuCounter, double> values = new Dictionary<CpuCounter, double>();
            for (int i = 0; i < _counters.Count; i++)
            {
                uint type;
                PdhFmtCounterValue value;
                status = PdhGetFormattedCounterValue(
                    _counters[i].Handle,
                    PDH_FMT_DOUBLE,
                    out type,
                    out value);

                if (status == ERROR_SUCCESS &&
                    (value.CStatus == PDH_CSTATUS_VALID_DATA || value.CStatus == PDH_CSTATUS_NEW_DATA) &&
                    !Double.IsNaN(value.DoubleValue) && !Double.IsInfinity(value.DoubleValue))
                {
                    values[_counters[i]] = Math.Max(0.0, value.DoubleValue);
                }
            }
            return values;
        }

        public void Dispose()
        {
            if (_query != IntPtr.Zero)
            {
                PdhCloseQuery(_query);
                _query = IntPtr.Zero;
            }
        }
    }

    private sealed class ProcessSnap
    {
        public int Pid;
        public string Name;
        public double TotalMs;
        public double KernelMs;
    }

    private sealed class ThreadSnap
    {
        public int Pid;
        public int Tid;
        public string ProcessName;
        public double TotalMs;
        public double KernelMs;
        public int BasePriority;
        public int CurrentPriority;
    }

    private sealed class Snapshot
    {
        public long Tick;
        public double CaptureDurationMs;
        public Dictionary<int, ProcessSnap> Processes;
        public Dictionary<int, ThreadSnap> Threads;

        public Snapshot(int expectedProcesses, int expectedThreads)
        {
            // Avoid repeated dictionary growth, without allocating a system-wide
            // table when the caller supplied a small PID filter.
            Processes = new Dictionary<int, ProcessSnap>(expectedProcesses);
            Threads = new Dictionary<int, ThreadSnap>(expectedThreads);
        }
    }

    private sealed class ThreadDelta
    {
        public ThreadSnap Current;
        public double CpuMs;
        public double KernelMs;
        public double ProcessCpuMs;
        public double ProcessKernelMs;
    }

    private sealed class ProcessDelta
    {
        public double CpuMs;
        public double KernelMs;
    }

    public static void Run(
        double thresholdPercent,
        int sampleIntervalMs,
        int topThreads,
        int[] processIds,
        int selfPid,
        bool includeSelf,
        int durationSeconds)
    {
        _stopRequested = false;
        ConsoleCancelEventHandler cancelHandler = delegate(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _stopRequested = true;
        };
        Console.CancelKeyPress += cancelHandler;

        HashSet<int> filter = null;
        if (processIds != null && processIds.Length > 0)
            filter = new HashSet<int>(processIds);

        Stopwatch runtime = Stopwatch.StartNew();

        try
        {
            using (PdhCpuQuery cpu = new PdhCpuQuery())
            {
                WriteColoredLine("=========================================================", ConsoleColor.Cyan);
                WriteColoredLine("  CORE SPIKE HUNTER (PowerShell 5.1 POC)", ConsoleColor.Cyan);
                WriteColoredLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "  Trigger: {0} >= {1:N1}% | interval: {2}ms | LPs: {3}",
                        cpu.CounterName,
                        thresholdPercent,
                        sampleIntervalMs,
                        cpu.Counters.Count),
                    ConsoleColor.Cyan);
                WriteColoredLine("  Attribution: CORRELATED (exact LP requires ETW CSwitch)", ConsoleColor.Yellow);
                if (filter != null)
                    WriteColoredLine("  Candidate PID filter: " + String.Join(",", processIds), ConsoleColor.Green);
                if (includeSelf)
                    WriteColoredLine("  Self candidate marker: Self:powershell (PID " + selfPid + ", magenta)", ConsoleColor.Magenta);
                WriteColoredLine(
                    (topThreads > 0)
                        ? "  Candidate output limit: " + topThreads
                        : "  Candidate output: automatic until hot-LP workload is accounted for",
                    ConsoleColor.DarkGray);
                WriteColoredLine("  Ctrl+C to stop", ConsoleColor.DarkGray);
                WriteColoredLine("=========================================================", ConsoleColor.Cyan);

                Snapshot previous = CaptureSnapshot(filter, selfPid, includeSelf);
                // Re-baseline PDH immediately after the first thread snapshot so
                // both measurements cover nearly the same interval.
                cpu.Collect();
                long previousCpuTick = Stopwatch.GetTimestamp();

                while (!_stopRequested)
                {
                    if (durationSeconds > 0 && runtime.Elapsed.TotalSeconds >= durationSeconds)
                        break;

                    // Compensate for the previous enumeration cost. This keeps
                    // end-to-end sample windows closer to SampleIntervalMs.
                    int sleepMs = (int)Math.Round(sampleIntervalMs - previous.CaptureDurationMs);
                    if (sleepMs > 0)
                        Thread.Sleep(sleepMs);

                    Snapshot current = CaptureSnapshot(filter, selfPid, includeSelf);
                    Dictionary<CpuCounter, double> cpuValues = cpu.Collect();
                    long currentCpuTick = Stopwatch.GetTimestamp();
                    DateTime sampleTime = DateTime.Now;
                    double cpuWindowMs = TicksToMs(currentCpuTick - previousCpuTick);
                    double threadWindowMs = TicksToMs(current.Tick - previous.Tick);

                    List<KeyValuePair<CpuCounter, double>> hot = new List<KeyValuePair<CpuCounter, double>>();
                    foreach (KeyValuePair<CpuCounter, double> pair in cpuValues)
                    {
                        if (pair.Value >= thresholdPercent)
                            hot.Add(pair);
                    }
                    hot.Sort(delegate(KeyValuePair<CpuCounter, double> a, KeyValuePair<CpuCounter, double> b)
                    {
                        return b.Value.CompareTo(a.Value);
                    });

                    if (hot.Count > 0 && cpuWindowMs > 0.0 && threadWindowMs > 0.0)
                    {
                        List<ThreadDelta> deltas = BuildThreadDeltas(previous, current);
                        deltas.Sort(delegate(ThreadDelta a, ThreadDelta b) { return b.CpuMs.CompareTo(a.CpuMs); });
                        PrintEvent(
                            hot,
                            deltas,
                            cpuWindowMs,
                            threadWindowMs,
                            topThreads,
                            filter != null,
                            sampleTime,
                            selfPid,
                            includeSelf);
                    }

                    previous = current;
                    previousCpuTick = currentCpuTick;
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            WriteColoredLine("Core-Spike-Hunter stopped.", ConsoleColor.DarkGray);
        }
    }

    private static Snapshot CaptureSnapshot(HashSet<int> filter, int selfPid, bool includeSelf)
    {
        long captureStartTick = Stopwatch.GetTimestamp();
        bool filtered = filter != null;
        int expectedProcesses = filtered ? Math.Max(1, filter.Count) : 512;
        int expectedThreads = filtered ? Math.Max(16, filter.Count * 32) : 4096;
        Snapshot snapshot = new Snapshot(expectedProcesses, expectedThreads);
        snapshot.Tick = Stopwatch.GetTimestamp();

        Process[] processes;
        try
        {
            if (filtered)
            {
                // A PID allow-list should not pay the cost of enumerating every
                // process on the machine.
                List<Process> selected = new List<Process>(filter.Count);
                foreach (int pid in filter)
                {
                    try { selected.Add(Process.GetProcessById(pid)); }
                    catch { }
                }
                processes = selected.ToArray();
            }
            else
            {
                processes = Process.GetProcesses();
            }
        }
        catch
        {
            snapshot.Tick = Stopwatch.GetTimestamp();
            snapshot.CaptureDurationMs = TicksToMs(snapshot.Tick - captureStartTick);
            return snapshot;
        }

        for (int pIndex = 0; pIndex < processes.Length; pIndex++)
        {
            Process process = processes[pIndex];
            int pid;
            try { pid = process.Id; }
            catch { SafeDispose(process); continue; }

            if (pid == 0 || (!includeSelf && pid == selfPid) ||
                (filter != null && !filter.Contains(pid)))
            {
                SafeDispose(process);
                continue;
            }

            string processName;
            double processTotalMs;
            double processKernelMs = 0.0;

            try
            {
                processName = process.ProcessName;
                processTotalMs = process.TotalProcessorTime.TotalMilliseconds;
            }
            catch
            {
                SafeDispose(process);
                continue;
            }

            try { processKernelMs = process.PrivilegedProcessorTime.TotalMilliseconds; }
            catch { }

            snapshot.Processes[pid] = new ProcessSnap
            {
                Pid = pid,
                Name = processName,
                TotalMs = processTotalMs,
                KernelMs = processKernelMs
            };

            try
            {
                ProcessThreadCollection threads = process.Threads;
                for (int tIndex = 0; tIndex < threads.Count; tIndex++)
                {
                    ProcessThread thread = threads[tIndex];
                    int tid;
                    double totalMs;
                    double kernelMs = 0.0;
                    int basePriority = 0;
                    int currentPriority = 0;

                    try
                    {
                        tid = thread.Id;
                        totalMs = thread.TotalProcessorTime.TotalMilliseconds;
                    }
                    catch { continue; }

                    try { kernelMs = thread.PrivilegedProcessorTime.TotalMilliseconds; } catch { }
                    try { basePriority = thread.BasePriority; } catch { }
                    try { currentPriority = thread.CurrentPriority; } catch { }

                    snapshot.Threads[tid] = new ThreadSnap
                    {
                        Pid = pid,
                        Tid = tid,
                        ProcessName = processName,
                        TotalMs = totalMs,
                        KernelMs = kernelMs,
                        BasePriority = basePriority,
                        CurrentPriority = currentPriority
                    };
                }
            }
            catch { }

            SafeDispose(process);
        }

        snapshot.Tick = Stopwatch.GetTimestamp();
        snapshot.CaptureDurationMs = TicksToMs(snapshot.Tick - captureStartTick);
        return snapshot;
    }

    private static List<ThreadDelta> BuildThreadDeltas(Snapshot previous, Snapshot current)
    {
        List<ThreadDelta> result = new List<ThreadDelta>(current.Threads.Count);
        Dictionary<int, ProcessDelta> processDeltas = new Dictionary<int, ProcessDelta>(current.Processes.Count);

        // Calculate each process delta once instead of repeating two dictionary
        // lookups and arithmetic for every thread owned by that process.
        foreach (KeyValuePair<int, ProcessSnap> pair in current.Processes)
        {
            ProcessSnap oldProcess;
            if (!previous.Processes.TryGetValue(pair.Key, out oldProcess))
                continue;

            processDeltas[pair.Key] = new ProcessDelta
            {
                CpuMs = Math.Max(0.0, pair.Value.TotalMs - oldProcess.TotalMs),
                KernelMs = Math.Max(0.0, pair.Value.KernelMs - oldProcess.KernelMs)
            };
        }

        foreach (KeyValuePair<int, ThreadSnap> pair in current.Threads)
        {
            ThreadSnap oldThread;
            ThreadSnap newThread = pair.Value;
            if (!previous.Threads.TryGetValue(pair.Key, out oldThread) || oldThread.Pid != newThread.Pid)
                continue;

            double cpuMs = newThread.TotalMs - oldThread.TotalMs;
            if (cpuMs <= 0.0)
                continue;

            double kernelMs = Math.Max(0.0, newThread.KernelMs - oldThread.KernelMs);
            double processCpuMs = 0.0;
            double processKernelMs = 0.0;
            ProcessDelta processDelta;

            if (processDeltas.TryGetValue(newThread.Pid, out processDelta))
            {
                processCpuMs = processDelta.CpuMs;
                processKernelMs = processDelta.KernelMs;
            }

            result.Add(new ThreadDelta
            {
                Current = newThread,
                CpuMs = cpuMs,
                KernelMs = kernelMs,
                ProcessCpuMs = processCpuMs,
                ProcessKernelMs = processKernelMs
            });
        }

        return result;
    }

    private static void PrintEvent(
        List<KeyValuePair<CpuCounter, double>> hot,
        List<ThreadDelta> deltas,
        double cpuWindowMs,
        double threadWindowMs,
        int topThreads,
        bool filtered,
        DateTime sampleTime,
        int selfPid,
        bool includeSelf)
    {
        StringBuilder hotText = new StringBuilder();
        for (int i = 0; i < hot.Count; i++)
        {
            if (i > 0) hotText.Append(", ");
            hotText.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0}={1:N1}%",
                hot[i].Key.Label,
                hot[i].Value);
        }

        WriteColoredLine(
            string.Format(
                CultureInfo.InvariantCulture,
                "[{0}] CORE SPIKE | Window: {1:N1}ms | Hot: {2} | Attribution: CORRELATED",
                sampleTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
                cpuWindowMs,
                hotText.ToString()),
            ConsoleColor.Red);

        if (deltas.Count == 0)
        {
            WriteColoredLine("  No accessible thread consumed measurable CPU in this window.", ConsoleColor.DarkYellow);
            return;
        }

        bool automatic = topThreads <= 0;
        int maximumCount = automatic ? deltas.Count : Math.Min(topThreads, deltas.Count);
        double targetCpuMs = 0.0;
        for (int i = 0; i < hot.Count; i++)
        {
            // Utility is frequency-aware and can exceed 100. Clamp each LP to
            // one elapsed-time window for this time-based coverage estimate.
            targetCpuMs += (Math.Min(100.0, hot[i].Value) / 100.0) * threadWindowMs;
        }

        double cumulativeCpuMs = 0.0;
        int printedCount = 0;

        for (int i = 0; i < maximumCount; i++)
        {
            ThreadDelta delta = deltas[i];
            double threadPercent = (delta.CpuMs / threadWindowMs) * 100.0;
            double processPercent = (delta.ProcessCpuMs / threadWindowMs) * 100.0;
            double threadKernelPercent = (delta.CpuMs > 0.0) ? (delta.KernelMs / delta.CpuMs) * 100.0 : 0.0;
            double processKernelPercent = (delta.ProcessCpuMs > 0.0) ?
                (delta.ProcessKernelMs / delta.ProcessCpuMs) * 100.0 : 0.0;
            bool isSelf = includeSelf && delta.Current.Pid == selfPid;
            string processName = isSelf
                ? "Self:" + delta.Current.ProcessName
                : delta.Current.ProcessName;

            WriteColoredLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  #{0} {1} | PID:{2} TID:{3} | ThreadCPU: {4:N1}ms ({5:N1}% core) K:{6:N0}% | ProcessCPU: {7:N1}ms ({8:N1}% cores) K:{9:N0}% | Pri:{10}/{11}",
                    i + 1,
                    processName,
                    delta.Current.Pid,
                    delta.Current.Tid,
                    delta.CpuMs,
                    threadPercent,
                    threadKernelPercent,
                    delta.ProcessCpuMs,
                    processPercent,
                    processKernelPercent,
                    delta.Current.BasePriority,
                    delta.Current.CurrentPriority),
                isSelf ? ConsoleColor.Magenta : ((i == 0) ? ConsoleColor.Yellow : ConsoleColor.Gray));

            cumulativeCpuMs += delta.CpuMs;
            printedCount++;

            if (automatic && cumulativeCpuMs >= targetCpuMs)
                break;
        }

        if (automatic)
        {
            double coveragePercent = (targetCpuMs > 0.0) ? (cumulativeCpuMs / targetCpuMs) * 100.0 : 100.0;
            WriteColoredLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  Coverage: {0:N1}/{1:N1}ms ({2:N1}%) using {3} thread(s){4}",
                    cumulativeCpuMs,
                    targetCpuMs,
                    coveragePercent,
                    printedCount,
                    (cumulativeCpuMs >= targetCpuMs) ? " - hot-LP workload accounted for" : " - measurable candidates exhausted"),
                (cumulativeCpuMs >= targetCpuMs) ? ConsoleColor.Green : ConsoleColor.DarkYellow);
        }

        if (filtered)
            WriteColoredLine("  Note: candidates are restricted by -ProcessId; hot LP data is system-wide.", ConsoleColor.DarkGray);
    }

    private static double TicksToMs(long ticks)
    {
        return (ticks * 1000.0) / Stopwatch.Frequency;
    }

    private static void SafeDispose(Process process)
    {
        try { process.Dispose(); } catch { }
    }

    private static void WriteColoredLine(string message, ConsoleColor color)
    {
        ConsoleColor original = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
        }
        finally { Console.ForegroundColor = original; }
    }

    private static void ThrowIfPdhError(uint status, string operation)
    {
        if (status != ERROR_SUCCESS)
            throw new Win32Exception(unchecked((int)status), operation + " failed with PDH status 0x" + status.ToString("X8"));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhFmtCounterValue
    {
        public uint CStatus;
        public double DoubleValue;
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhOpenQuery(string dataSource, UIntPtr userData, out IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhAddEnglishCounter(IntPtr query, string fullCounterPath, UIntPtr userData, out IntPtr counter);

    [DllImport("pdh.dll")]
    private static extern uint PdhCollectQueryData(IntPtr query);

    [DllImport("pdh.dll")]
    private static extern uint PdhGetFormattedCounterValue(IntPtr counter, uint format, out uint type, out PdhFmtCounterValue value);

    [DllImport("pdh.dll")]
    private static extern uint PdhRemoveCounter(IntPtr counter);

    [DllImport("pdh.dll")]
    private static extern uint PdhCloseQuery(IntPtr query);

    [DllImport("kernel32.dll")]
    private static extern ushort GetActiveProcessorGroupCount();

    [DllImport("kernel32.dll")]
    private static extern uint GetActiveProcessorCount(ushort groupNumber);
}
'@

    Import-OptimizedCSharp `
        -Source $code `
        -ExpectedTypeName 'CoreSpikeHunterRunner' `
        -Platform 'AnyCPU' `
        -CallerScriptPath $PSCommandPath
}

try {
    [CoreSpikeHunterRunner]::Run(
        $ThresholdPercent,
        $SampleIntervalMs,
        $TopThreads,
        $ProcessId,
        $PID,
        [bool]$IncludeSelf,
        $DurationSeconds
    )
}
catch {
    Write-Host 'CoreSpikeHunterRunner failed:' -ForegroundColor Red
    Write-Host $_.Exception.ToString() -ForegroundColor Red
    throw
}
