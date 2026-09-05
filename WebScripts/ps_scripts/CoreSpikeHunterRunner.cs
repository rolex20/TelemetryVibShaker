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

    private const int SystemProcessInformation = 5;
    private const int STATUS_SUCCESS = 0x00000000;
    private const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

    private static volatile bool _stopRequested;

    private sealed class CpuCounter : IDisposable
    {
        public ushort Group;
        public uint Number;
        public IntPtr Handle;
        public int PCoreMaxIndex;

        public string CoreType
        {
            get { return (Number <= PCoreMaxIndex) ? "P-Core" : "E-Core"; }
        }

        public string Label
        {
            get
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "G{0}:LP#{1} [{2}]",
                    Group,
                    Number,
                    CoreType);
            }
        }

        public void Dispose()
        {
            Handle = IntPtr.Zero;
        }
    }

    private sealed class PdhCpuQuery : IDisposable
    {
        private IntPtr _query;
        private readonly List<CpuCounter> _counters = new List<CpuCounter>();
        private string _counterName;
        private readonly int _pCoreMaxIndex;

        public IList<CpuCounter> Counters { get { return _counters; } }
        public string CounterName { get { return _counterName; } }

        public PdhCpuQuery(int pCoreMaxIndex)
        {
            _pCoreMaxIndex = pCoreMaxIndex;
            uint status = PdhOpenQuery(null, UIntPtr.Zero, out _query);
            ThrowIfPdhError(status, "PdhOpenQuery");

            if (!TryAddCounters("% Processor Utility"))
            {
                _counters.Clear();
                if (!TryAddCounters("% Processor Time"))
                    throw new InvalidOperationException("Unable to add per-processor PDH counters.");
            }

            status = PdhCollectQueryData(_query);
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
                        for (int i = 0; i < added.Count; i++)
                            PdhRemoveCounter(added[i]);
                        return false;
                    }

                    added.Add(counter);
                    _counters.Add(new CpuCounter
                    {
                        Group = group,
                        Number = number,
                        Handle = counter,
                        PCoreMaxIndex = _pCoreMaxIndex
                    });
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

    private sealed class NativeDoubleBuffer : IDisposable
    {
        public IntPtr BufferPrev;
        public IntPtr BufferCurr;
        private int _bufferSize;
        public int ThreadsOffset;

        public NativeDoubleBuffer(int initialSize = 1024 * 1024)
        {
            _bufferSize = initialSize;
            BufferPrev = Marshal.AllocHGlobal(_bufferSize);
            BufferCurr = Marshal.AllocHGlobal(_bufferSize);
            ThreadsOffset = -1;
        }

        public void Capture(ref IntPtr targetBuffer, out int returnLength)
        {
            while (true)
            {
                int retLen;
                int status = NtQuerySystemInformation(SystemProcessInformation, targetBuffer, _bufferSize, out retLen);
                if (status == STATUS_SUCCESS)
                {
                    returnLength = retLen;
                    if (ThreadsOffset == -1)
                        ThreadsOffset = DetectThreadsOffset(targetBuffer);
                    return;
                }
                else if (status == STATUS_INFO_LENGTH_MISMATCH)
                {
                    _bufferSize = Math.Max(retLen + 65536, _bufferSize * 2);
                    Marshal.FreeHGlobal(BufferPrev);
                    Marshal.FreeHGlobal(BufferCurr);
                    BufferPrev = Marshal.AllocHGlobal(_bufferSize);
                    BufferCurr = Marshal.AllocHGlobal(_bufferSize);
                    targetBuffer = BufferPrev;
                }
                else
                {
                    throw new Win32Exception(status, "NtQuerySystemInformation failed with NTSTATUS 0x" + status.ToString("X8"));
                }
            }
        }

        private static int DetectThreadsOffset(IntPtr buffer)
        {
            int defaultOffset = (IntPtr.Size == 8) ? 0x100 : 0xB8;
            int stiPidOff = (IntPtr.Size == 8) ? 0x28 : 0x20;
            int spiPidOff = (IntPtr.Size == 8) ? 0x50 : 0x44;

            IntPtr curr = buffer;
            while (curr != IntPtr.Zero)
            {
                uint nextOffset = (uint)Marshal.ReadInt32(curr, 0);
                uint threadCount = (uint)Marshal.ReadInt32(curr, 4);
                long pid = (IntPtr.Size == 8)
                    ? Marshal.ReadInt64(curr, spiPidOff)
                    : Marshal.ReadInt32(curr, spiPidOff);

                if (pid > 0 && threadCount > 0)
                {
                    long checkPid = (IntPtr.Size == 8)
                        ? Marshal.ReadInt64(curr, defaultOffset + stiPidOff)
                        : Marshal.ReadInt32(curr, defaultOffset + stiPidOff);

                    if (checkPid == pid)
                        return defaultOffset;

                    for (int test = 0x80; test <= 0x140; test += IntPtr.Size)
                    {
                        long testPid = (IntPtr.Size == 8)
                            ? Marshal.ReadInt64(curr, test + stiPidOff)
                            : Marshal.ReadInt32(curr, test + stiPidOff);
                        if (testPid == pid)
                            return test;
                    }
                    break;
                }

                if (nextOffset == 0) break;
                curr = new IntPtr(curr.ToInt64() + nextOffset);
            }

            return defaultOffset;
        }

        public void Swap()
        {
            IntPtr temp = BufferPrev;
            BufferPrev = BufferCurr;
            BufferCurr = temp;
        }

        public void Dispose()
        {
            if (BufferPrev != IntPtr.Zero) { Marshal.FreeHGlobal(BufferPrev); BufferPrev = IntPtr.Zero; }
            if (BufferCurr != IntPtr.Zero) { Marshal.FreeHGlobal(BufferCurr); BufferCurr = IntPtr.Zero; }
        }
    }

    private struct RawThreadData
    {
        public int Pid;
        public int Tid;
        public long Total100ns;
        public long Kernel100ns;
        public int Priority;
        public int BasePriority;
    }

    private struct RawProcessData
    {
        public int Pid;
        public string Name;
        public long Total100ns;
        public long Kernel100ns;
    }

    private sealed class ThreadDelta
    {
        public int Pid;
        public int Tid;
        public string ProcessName;
        public double CpuMs;
        public double KernelMs;
        public double ProcessCpuMs;
        public double ProcessKernelMs;
        public int BasePriority;
        public int CurrentPriority;
    }

    private struct ProcessDelta
    {
        public double CpuMs;
        public double KernelMs;
    }

    // Reusable structures for zero-allocation performance
    private static readonly Dictionary<int, RawThreadData> _prevThreads = new Dictionary<int, RawThreadData>(4096);
    private static readonly Dictionary<int, RawThreadData> _currThreads = new Dictionary<int, RawThreadData>(4096);
    private static readonly Dictionary<int, RawProcessData> _prevProcesses = new Dictionary<int, RawProcessData>(512);
    private static readonly Dictionary<int, RawProcessData> _currProcesses = new Dictionary<int, RawProcessData>(512);
    private static readonly Dictionary<int, ProcessDelta> _processDeltas = new Dictionary<int, ProcessDelta>(512);
    private static readonly List<ThreadDelta> _deltas = new List<ThreadDelta>(1024);

    public static void Run(
        double thresholdPercent,
        int sampleIntervalMs,
        int topThreads,
        int[] processIds,
        int selfPid,
        bool includeSelf,
        int durationSeconds)
    {
        Run(thresholdPercent, sampleIntervalMs, topThreads, processIds, selfPid, includeSelf, durationSeconds, 15);
    }

    public static void Run(
        double thresholdPercent,
        int sampleIntervalMs,
        int topThreads,
        int[] processIds,
        int selfPid,
        bool includeSelf,
        int durationSeconds,
        int pCoreMaxLpIndex)
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
            using (NativeDoubleBuffer buffers = new NativeDoubleBuffer())
            using (PdhCpuQuery cpu = new PdhCpuQuery(pCoreMaxLpIndex))
            {
                WriteColoredLine("==================================================================", ConsoleColor.Cyan);
                WriteColoredLine("  CORE SPIKE HUNTER (High-Performance Engine v2.0)", ConsoleColor.Cyan);
                WriteColoredLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "  Trigger: {0} >= {1:N1}% | Interval: {2}ms | LPs: {3}",
                        cpu.CounterName,
                        thresholdPercent,
                        sampleIntervalMs,
                        cpu.Counters.Count),
                    ConsoleColor.Cyan);
                WriteColoredLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "  Hybrid Topology: LP#0..{0} -> P-Core | LP#{1}+ -> E-Core (i7-14700K)",
                        pCoreMaxLpIndex,
                        pCoreMaxLpIndex + 1),
                    ConsoleColor.Cyan);
                WriteColoredLine("  Engine: NtQuerySystemInformation (Single Syscall) + Double Buffer", ConsoleColor.Green);
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
                WriteColoredLine("==================================================================", ConsoleColor.Cyan);

                // Baseline priming
                int initialLen;
                buffers.Capture(ref buffers.BufferPrev, out initialLen);
                cpu.Collect();
                long prevCpuTick = Stopwatch.GetTimestamp();
                long prevThreadTick = prevCpuTick;

                while (!_stopRequested)
                {
                    if (durationSeconds > 0 && runtime.Elapsed.TotalSeconds >= durationSeconds)
                        break;

                    Thread.Sleep(sampleIntervalMs);

                    // 1. Lightweight PDH check (< 0.2ms)
                    Dictionary<CpuCounter, double> cpuValues = cpu.Collect();
                    long currCpuTick = Stopwatch.GetTimestamp();
                    DateTime sampleTime = DateTime.Now;

                    List<KeyValuePair<CpuCounter, double>> hot = new List<KeyValuePair<CpuCounter, double>>();
                    foreach (KeyValuePair<CpuCounter, double> pair in cpuValues)
                    {
                        if (pair.Value >= thresholdPercent)
                            hot.Add(pair);
                    }

                    // 2. Kernel Snapshot into BufferCurr (Single Syscall < 1ms)
                    int currentLen;
                    buffers.Capture(ref buffers.BufferCurr, out currentLen);
                    long currThreadTick = Stopwatch.GetTimestamp();

                    double cpuWindowMs = TicksToMs(currCpuTick - prevCpuTick);
                    double threadWindowMs = TicksToMs(currThreadTick - prevThreadTick);

                    // 3. Two-Tier Activation: Parse ONLY if an LP exceeded the threshold
                    if (hot.Count > 0 && cpuWindowMs > 0.0 && threadWindowMs > 0.0)
                    {
                        hot.Sort(delegate(KeyValuePair<CpuCounter, double> a, KeyValuePair<CpuCounter, double> b)
                        {
                            return b.Value.CompareTo(a.Value);
                        });

                        ParseSnapshot(buffers.BufferPrev, buffers.ThreadsOffset, filter, selfPid, includeSelf, _prevProcesses, _prevThreads);
                        ParseSnapshot(buffers.BufferCurr, buffers.ThreadsOffset, filter, selfPid, includeSelf, _currProcesses, _currThreads);

                        BuildDeltas(_prevProcesses, _currProcesses, _prevThreads, _currThreads, _deltas);
                        _deltas.Sort(delegate(ThreadDelta a, ThreadDelta b) { return b.CpuMs.CompareTo(a.CpuMs); });

                        PrintEvent(
                            hot,
                            _deltas,
                            cpuWindowMs,
                            threadWindowMs,
                            topThreads,
                            filter != null,
                            sampleTime,
                            selfPid,
                            includeSelf);
                    }

                    // 4. Zero-allocation pointer swap
                    buffers.Swap();
                    prevCpuTick = currCpuTick;
                    prevThreadTick = currThreadTick;
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            WriteColoredLine("Core-Spike-Hunter stopped.", ConsoleColor.DarkGray);
        }
    }

    private static void ParseSnapshot(
        IntPtr buffer,
        int threadsOffset,
        HashSet<int> filter,
        int selfPid,
        bool includeSelf,
        Dictionary<int, RawProcessData> outProcesses,
        Dictionary<int, RawThreadData> outThreads)
    {
        outProcesses.Clear();
        outThreads.Clear();

        bool is64 = (IntPtr.Size == 8);
        int spiPidOff = is64 ? 0x50 : 0x44;
        int spiUserOff = is64 ? 0x28 : 0x18;
        int spiKernelOff = is64 ? 0x30 : 0x20;
        int spiNameLenOff = 0x38;
        int spiNameBufOff = is64 ? 0x40 : 0x3C;

        int stiSize = is64 ? 0x50 : 0x40;
        int stiKernelOff = 0x00;
        int stiUserOff = 0x08;
        int stiTidOff = is64 ? 0x30 : 0x24;
        int stiPriOff = is64 ? 0x38 : 0x28;
        int stiBasePriOff = is64 ? 0x3C : 0x2C;

        IntPtr curr = buffer;
        while (curr != IntPtr.Zero)
        {
            uint nextOffset = (uint)Marshal.ReadInt32(curr, 0);
            uint threadCount = (uint)Marshal.ReadInt32(curr, 4);
            int pid = is64
                ? (int)Marshal.ReadInt64(curr, spiPidOff)
                : Marshal.ReadInt32(curr, spiPidOff);

            bool skip = (pid == 0) ||
                        (!includeSelf && pid == selfPid) ||
                        (filter != null && !filter.Contains(pid));

            if (!skip)
            {
                short nameLen = Marshal.ReadInt16(curr, spiNameLenOff);
                IntPtr nameBuf = Marshal.ReadIntPtr(curr, spiNameBufOff);
                string procName;
                if (nameBuf != IntPtr.Zero && nameLen > 0)
                {
                    procName = Marshal.PtrToStringUni(nameBuf, nameLen / 2);
                    if (procName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        procName = procName.Substring(0, procName.Length - 4);
                }
                else
                {
                    procName = (pid == 4) ? "System" : ("PID:" + pid);
                }

                long procUser = Marshal.ReadInt64(curr, spiUserOff);
                long procKernel = Marshal.ReadInt64(curr, spiKernelOff);

                outProcesses[pid] = new RawProcessData
                {
                    Pid = pid,
                    Name = procName,
                    Total100ns = procUser + procKernel,
                    Kernel100ns = procKernel
                };

                IntPtr threadPtr = new IntPtr(curr.ToInt64() + threadsOffset);
                for (uint t = 0; t < threadCount; t++)
                {
                    int tid = is64
                        ? (int)Marshal.ReadInt64(threadPtr, stiTidOff)
                        : Marshal.ReadInt32(threadPtr, stiTidOff);

                    long threadKernel = Marshal.ReadInt64(threadPtr, stiKernelOff);
                    long threadUser = Marshal.ReadInt64(threadPtr, stiUserOff);
                    int currentPri = Marshal.ReadInt32(threadPtr, stiPriOff);
                    int basePri = Marshal.ReadInt32(threadPtr, stiBasePriOff);

                    outThreads[tid] = new RawThreadData
                    {
                        Pid = pid,
                        Tid = tid,
                        Total100ns = threadKernel + threadUser,
                        Kernel100ns = threadKernel,
                        Priority = currentPri,
                        BasePriority = basePri
                    };

                    threadPtr = new IntPtr(threadPtr.ToInt64() + stiSize);
                }
            }

            if (nextOffset == 0) break;
            curr = new IntPtr(curr.ToInt64() + nextOffset);
        }
    }

    private static void BuildDeltas(
        Dictionary<int, RawProcessData> prevProcesses,
        Dictionary<int, RawProcessData> currProcesses,
        Dictionary<int, RawThreadData> prevThreads,
        Dictionary<int, RawThreadData> currThreads,
        List<ThreadDelta> outDeltas)
    {
        outDeltas.Clear();
        _processDeltas.Clear();

        foreach (KeyValuePair<int, RawThreadData> pair in currThreads)
        {
            RawThreadData prevThread;
            RawThreadData currThread = pair.Value;

            if (!prevThreads.TryGetValue(pair.Key, out prevThread) || prevThread.Pid != currThread.Pid)
                continue;

            long cpu100ns = currThread.Total100ns - prevThread.Total100ns;
            if (cpu100ns <= 0)
                continue;

            double cpuMs = cpu100ns / 10000.0;
            double kernelMs = Math.Max(0.0, (currThread.Kernel100ns - prevThread.Kernel100ns) / 10000.0);

            ProcessDelta procDelta;
            if (!_processDeltas.TryGetValue(currThread.Pid, out procDelta))
            {
                RawProcessData currProc;
                RawProcessData prevProc;
                double pCpuMs = 0.0;
                double pKernelMs = 0.0;

                if (currProcesses.TryGetValue(currThread.Pid, out currProc) &&
                    prevProcesses.TryGetValue(currThread.Pid, out prevProc))
                {
                    pCpuMs = Math.Max(0.0, (currProc.Total100ns - prevProc.Total100ns) / 10000.0);
                    pKernelMs = Math.Max(0.0, (currProc.Kernel100ns - prevProc.Kernel100ns) / 10000.0);
                }

                procDelta = new ProcessDelta { CpuMs = pCpuMs, KernelMs = pKernelMs };
                _processDeltas[currThread.Pid] = procDelta;
            }

            string procName = "Unknown";
            RawProcessData pData;
            if (currProcesses.TryGetValue(currThread.Pid, out pData))
                procName = pData.Name;

            outDeltas.Add(new ThreadDelta
            {
                Pid = currThread.Pid,
                Tid = currThread.Tid,
                ProcessName = procName,
                CpuMs = cpuMs,
                KernelMs = kernelMs,
                ProcessCpuMs = procDelta.CpuMs,
                ProcessKernelMs = procDelta.KernelMs,
                BasePriority = currThread.BasePriority,
                CurrentPriority = currThread.Priority
            });
        }
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
            bool isSelf = includeSelf && delta.Pid == selfPid;
            string processName = isSelf ? ("Self:" + delta.ProcessName) : delta.ProcessName;

            WriteColoredLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "  #{0} {1} | PID:{2} TID:{3} | ThreadCPU: {4:N1}ms ({5:N1}% core) K:{6:N0}% | ProcessCPU: {7:N1}ms ({8:N1}% cores) K:{9:N0}% | Pri:{10}/{11}",
                    i + 1,
                    processName,
                    delta.Pid,
                    delta.Tid,
                    delta.CpuMs,
                    threadPercent,
                    threadKernelPercent,
                    delta.ProcessCpuMs,
                    processPercent,
                    processKernelPercent,
                    delta.BasePriority,
                    delta.CurrentPriority),
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

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        int systemInformationClass,
        IntPtr systemInformation,
        int systemInformationLength,
        out int returnLength);

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
