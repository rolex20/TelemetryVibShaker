using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PerformanceMonitor
{
    internal static class CPU_QoS
    {
        // ==============================================================
        // Public Enum
        // ==============================================================

        /// <summary>
        /// Defines the target CPU core type for thread affinity and CPU-set assignment.
        /// </summary>
        public enum CpuSetType
        {
            /// <summary>
            /// Removes any specific CPU restrictions. The OS scheduler decides where to run.
            /// </summary>
            None = 0,

            /// <summary>
            /// Targets the most efficient cores (E-Cores on hybrid CPUs such as the Intel Core i9-14700K).
            /// On homogeneous CPUs, all cores share the same efficiency class, so all cores are selected.
            /// </summary>
            Efficiency = 1,

            /// <summary>
            /// Targets the highest-performance cores (P-Cores on hybrid CPUs such as the Intel Core i9-14700K).
            /// On homogeneous CPUs, all cores share the same efficiency class, so all cores are selected.
            /// </summary>
            Performance = 2
        }

        // ==============================================================
        // Public API
        // ==============================================================

        /// <summary>
        /// Returns the pseudo-handle to the current thread.
        /// Useful for callers (e.g. PowerShell) that need the raw handle for custom Win32 calls.
        /// </summary>
        public static IntPtr GetCurrentWinThreadHandle() => GetCurrentThread();

        /// <summary>
        /// Determines whether the system exposes a hybrid CPU architecture — i.e., whether at least
        /// two distinct efficiency classes exist among the CPU sets accessible to this process.
        /// Returns <c>true</c> for CPUs like the Intel Core i9-14700K (P-Cores + E-Cores).
        /// </summary>
        /// <returns>
        /// <c>true</c> if multiple efficiency classes are visible and usable by this process;
        /// <c>false</c> on homogeneous CPUs or if the topology query fails.
        /// </returns>
        public static bool IsHybridCpu()
        {
            try
            {
                var topo = QueryUsableCpuTopology();
                return topo.CpuSets.Count > 0 && topo.MinEfficiencyClass != topo.MaxEfficiencyClass;
            }
            catch
            {
                // Conservative default: assume non-hybrid if the topology query fails.
                return false;
            }
        }

        /// <summary>
        /// Enables or disables EcoQoS (Efficiency Mode / Power Throttling) for the current thread.
        /// Attempts the Windows 11 API first (ThreadInformationClass 3), then falls back to the
        /// Windows 10 API (ThreadInformationClass 1) on older systems.
        /// </summary>
        /// <param name="enable"><c>true</c> to enable EcoQoS; <c>false</c> to disable it.</param>
        /// <exception cref="Win32Exception">Thrown if the Win32 call fails on both API levels.</exception>
        public static void SetEcoQoS(bool enable)
        {
            var state = new THREAD_POWER_THROTTLING_STATE
            {
                Version     = THREAD_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = THREAD_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask   = enable ? THREAD_POWER_THROTTLING_EXECUTION_SPEED : 0u
            };

            IntPtr hThread = GetCurrentThread();
            uint   size    = (uint)Marshal.SizeOf<THREAD_POWER_THROTTLING_STATE>();

            // Try the Windows 11 API first (Class ID 3).
            if (SetThreadInformation(hThread, TIC_THREAD_POWER_THROTTLING_WIN11, ref state, size))
                return;

            int err = Marshal.GetLastWin32Error();

            // Fall back to the Windows 10 API (Class ID 1) on ERROR_INVALID_PARAMETER.
            if (err == ERROR_INVALID_PARAMETER &&
                SetThreadInformation(hThread, TIC_THREAD_POWER_THROTTLING_WIN10, ref state, size))
                return;

            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set EcoQoS state.");
        }

        /// <summary>
        /// Configures the CPU Sets (soft affinity hint) for the current thread from a string token.
        /// Supports exact enum names (case-insensitive) and partial keyword matches:
        /// "eff" / "eco" → <see cref="CpuSetType.Efficiency"/>; "perf" → <see cref="CpuSetType.Performance"/>.
        /// Any unrecognized token defaults to <see cref="CpuSetType.None"/>.
        /// Useful for PowerShell or other dynamic-input callers.
        /// </summary>
        /// <param name="mode">String representation of the desired <see cref="CpuSetType"/>.</param>
        public static void SetCpuSets(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                SetCpuSets(CpuSetType.None);
                return;
            }

            if (Enum.TryParse(mode, ignoreCase: true, out CpuSetType parsed))
            {
                SetCpuSets(parsed);
                return;
            }

            // Fallback: partial keyword matching for convenience.
            string m = mode.ToLowerInvariant();
            if      (m.Contains("eff") || m.Contains("eco")) SetCpuSets(CpuSetType.Efficiency);
            else if (m.Contains("perf"))                     SetCpuSets(CpuSetType.Performance);
            else                                             SetCpuSets(CpuSetType.None);
        }

        /// <summary>
        /// Configures the CPU Sets (soft affinity hint) for the current thread.
        /// CPU Sets are a modern Windows scheduler hint: the OS respects them but may still
        /// migrate the thread under extreme load. For a strict guarantee, use
        /// <see cref="SetHardAffinity(CpuSetType)"/> instead.
        /// </summary>
        /// <param name="type">
        /// The desired core type. <see cref="CpuSetType.None"/> clears any existing CPU Set restriction.
        /// </param>
        /// <exception cref="Win32Exception">Thrown if the underlying Win32 call fails.</exception>
        public static void SetCpuSets(CpuSetType type)
        {
            IntPtr hThread = GetCurrentThread();

            if (type == CpuSetType.None)
            {
                if (!SetThreadSelectedCpuSets(hThread, null, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to clear CPU Sets.");
                return;
            }

            var    topo = QueryUsableCpuTopology();
            uint[] ids  = SelectCpuSetIds(type, in topo);

            if (ids.Length > 0)
            {
                if (!SetThreadSelectedCpuSets(hThread, ids, (uint)ids.Length))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set CPU Sets.");
            }
            else
            {
                // No matching cores found (rare on homogeneous CPUs). Revert to unrestricted
                // scheduling rather than leaving the thread pinned incorrectly.
                SetThreadSelectedCpuSets(hThread, null, 0);
            }
        }

        /// <summary>
        /// Sets a hard processor affinity for the current thread via <c>SetThreadGroupAffinity</c>.
        /// Unlike CPU Sets, hard affinity is strictly enforced by the OS scheduler.
        /// <para>
        /// This method is <b>thread-scoped only</b> — it affects solely the calling thread and
        /// never touches the process affinity mask, consistent with every other method in this class.
        /// </para>
        /// <para>
        /// Hard affinity and CPU Sets can coexist; the OS honours the intersection of both constraints.
        /// </para>
        /// </summary>
        /// <param name="type">
        /// The desired core type. <see cref="CpuSetType.None"/> resets the thread's affinity back
        /// to the full process-allowed mask, removing any previous restriction.
        /// </param>
        /// <exception cref="Win32Exception">Thrown if a Win32 call fails.</exception>
        public static void SetHardAffinityThread(CpuSetType type)
        {
            IntPtr hThread = GetCurrentThread();

            if (type == CpuSetType.None)
            {
                ResetThreadAffinityToProcessDefault(hThread);
                return;
            }

            var topo = QueryUsableCpuTopology();
            if (topo.CpuSets.Count == 0) return; // No usable cores visible; leave unchanged.

            byte   targetEff   = GetTargetEfficiencyClass(type, in topo);
            ushort targetGroup = GetCurrentThreadGroup(hThread);

            // Build affinity mask within the thread's current processor group first.
            ulong mask = BuildGroupAffinityMask(in topo, targetEff, targetGroup);

            // If no target cores exist in the current group, search the remaining groups.
            if (mask == 0)
            {
                targetGroup = FindFirstGroupForEfficiencyClass(in topo, targetEff);
                mask        = BuildGroupAffinityMask(in topo, targetEff, targetGroup);
            }

            // Nothing matched anywhere — revert to default rather than locking out the thread.
            if (mask == 0)
            {
                ResetThreadAffinityToProcessDefault(hThread);
                return;
            }

            var affinity = new GROUP_AFFINITY
            {
                Mask  = UIntPtr.Size == 8 ? new UIntPtr(mask) : new UIntPtr((uint)mask),
                Group = targetGroup
            };

            if (!SetThreadGroupAffinity(hThread, ref affinity, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set hard thread affinity.");
        }
		
// ── Add this method in the Public API region, after SetHardAffinity() ─────────────────────────

        /// <summary>
        /// Sets a hard processor affinity for the <b>entire current process</b> based on the requested
        /// core type. Every thread in the process will be constrained to the selected cores.
        /// <para>
        /// Use this only when you intentionally want to restrict the whole application — for per-thread
        /// control, use <see cref="SetHardAffinity(CpuSetType)"/> instead.
        /// </para>
        /// <para>
        /// <b>Limitation:</b> <c>SetProcessAffinityMask</c> operates on processor group 0 only.
        /// On systems with more than 64 logical processors this will only address the first group.
        /// For such systems a per-thread approach with <see cref="SetHardAffinity(CpuSetType)"/>
        /// across each thread is the correct strategy.
        /// </para>
        /// </summary>
        /// <param name="type">
        /// The desired core type. <see cref="CpuSetType.None"/> restores the process affinity to
        /// the full system-allowed mask, removing any previous restriction.
        /// </param>
        /// <exception cref="Win32Exception">Thrown if any underlying Win32 call fails.</exception>
        public static void SetHardAffinityProcess(CpuSetType type)
        {
            IntPtr hProcess = GetCurrentProcess();

            // Always obtain the system mask — it is the ceiling we must never exceed.
            if (!GetProcessAffinityMask(hProcess, out _, out UIntPtr systemMaskPtr))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "GetProcessAffinityMask failed.");

            ulong systemMask = UIntPtr.Size == 8
                ? systemMaskPtr.ToUInt64()
                : systemMaskPtr.ToUInt32();

            if (type == CpuSetType.None)
            {
                // Restore full access: set process affinity to everything the system allows.
                ApplyProcessAffinityMask(hProcess, systemMask);
                return;
            }

            var topo = QueryUsableCpuTopology();

            if (topo.CpuSets.Count == 0)
            {
                // Topology unavailable — safest fallback is unrestricted.
                ApplyProcessAffinityMask(hProcess, systemMask);
                return;
            }

            byte  targetEff = GetTargetEfficiencyClass(type, in topo);
            ulong mask      = BuildGroupAffinityMask(in topo, targetEff, group: 0);

            // Intersect with the system mask: never request bits the OS won't grant.
            mask &= systemMask;

            // If the intersection is empty (target core type not in group 0, or fully locked),
            // fall back to unrestricted rather than deadlocking every thread in the process.
            if (mask == 0)
                mask = systemMask;

            ApplyProcessAffinityMask(hProcess, mask);
        }

        /// <summary>
        /// Applies a process-wide affinity mask, converting to the correct pointer width.
        /// </summary>
        private static void ApplyProcessAffinityMask(IntPtr hProcess, ulong mask)
        {
            if (mask == 0)
                throw new ArgumentException("Affinity mask cannot be zero.", nameof(mask));

            UIntPtr maskPtr = UIntPtr.Size == 8
                ? new UIntPtr(mask)
                : new UIntPtr((uint)mask);

            if (!SetProcessAffinityMask(hProcess, maskPtr))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SetProcessAffinityMask failed.");
        }


		

        // ==============================================================
        // Internal Data Structures
        // ==============================================================

        // Named constants for the AllFlags byte inside SYSTEM_CPU_SET_INFORMATION.
        private const byte CPUSET_FLAG_ALLOCATED            = 0x02;
        private const byte CPUSET_FLAG_ALLOCATED_TO_TARGET  = 0x04;

        /// <summary>
        /// Compact representation of one logical CPU, parsed directly from the Win32 buffer.
        /// </summary>
        private struct InternalCpuSet
        {
            public uint   Id;
            public ushort Group;
            public byte   LogicalProcessorIndex;
            public byte   EfficiencyClass;
            public byte   Flags; // Raw AllFlags byte from SYSTEM_CPU_SET_INFORMATION

            // Derived properties — computed from Flags rather than stored as separate booleans.
            public bool Allocated               => (Flags & CPUSET_FLAG_ALLOCATED)           != 0;
            public bool AllocatedToTargetProcess => (Flags & CPUSET_FLAG_ALLOCATED_TO_TARGET) != 0;

            /// <summary>
            /// <c>true</c> when this core can be scheduled by the current process
            /// (not exclusively owned by another process).
            /// </summary>
            public bool IsUsable => !Allocated || AllocatedToTargetProcess;
        }

        /// <summary>
        /// Snapshot of the system CPU topology, pre-filtered to cores that are usable by this process.
        /// <see cref="MinEfficiencyClass"/> and <see cref="MaxEfficiencyClass"/> reflect only those
        /// usable cores, so <see cref="IsHybridCpu"/> and affinity selection are always accurate.
        /// </summary>
        private struct CpuSetTopology
        {
            public List<InternalCpuSet> CpuSets;
            public byte MinEfficiencyClass;
            public byte MaxEfficiencyClass;
        }

        // ==============================================================
        // Internal Logic
        // ==============================================================

        /// <summary>
        /// Queries <c>GetSystemCpuSetInformation</c> and returns a topology snapshot containing
        /// only the CPU sets usable by the current process, along with the min/max efficiency classes
        /// computed exclusively from those usable cores.
        /// </summary>
        private static CpuSetTopology QueryUsableCpuTopology()
        {
            var topo = new CpuSetTopology
            {
                CpuSets            = new List<InternalCpuSet>(),
                MinEfficiencyClass = 255,
                MaxEfficiencyClass = 0
            };

            IntPtr hProcess = GetCurrentProcess();

            // First call: obtain the required buffer size.
            GetSystemCpuSetInformation(IntPtr.Zero, 0, out uint reqLength, hProcess, 0);

            if (reqLength == 0 || reqLength > (uint)int.MaxValue)
                return Normalize(topo);

            IntPtr buffer = Marshal.AllocHGlobal((int)reqLength);
            try
            {
                if (!GetSystemCpuSetInformation(buffer, reqLength, out reqLength, hProcess, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "GetSystemCpuSetInformation failed.");

                long ptr = buffer.ToInt64();
                long end = ptr + reqLength;

                while (ptr < end)
                {
                    IntPtr p    = new IntPtr(ptr);
                    int    size = Marshal.ReadInt32(p);     // SYSTEM_CPU_SET_INFORMATION.Size
                    if (size <= 0) break;

                    int type = Marshal.ReadInt32(p, 4);     // Type: 0 = CpuSetInformation

                    if (type == 0)
                    {
                        // Parse the CpuSet union at its documented byte offsets:
                        //   +8  : Id                    (DWORD  / uint32)
                        //  +12  : Group                 (WORD   / uint16)
                        //  +14  : LogicalProcessorIndex (BYTE   / uint8)
                        //  +18  : EfficiencyClass       (BYTE   / uint8)
                        //  +19  : AllFlags              (BYTE   / uint8)
                        var cpu = new InternalCpuSet
                        {
                            Id                    = (uint)Marshal.ReadInt32(p,  8),
                            Group                 = (ushort)Marshal.ReadInt16(p, 12),
                            LogicalProcessorIndex = Marshal.ReadByte(p, 14),
                            EfficiencyClass       = Marshal.ReadByte(p, 18),
                            Flags                 = Marshal.ReadByte(p, 19)
                        };

                        // Only track cores this process is allowed to use.
                        // Min/max efficiency class is computed here, not globally, so IsHybridCpu()
                        // and affinity selection always reflect reality for this process.
                        if (cpu.IsUsable)
                        {
                            if (cpu.EfficiencyClass < topo.MinEfficiencyClass)
                                topo.MinEfficiencyClass = cpu.EfficiencyClass;
                            if (cpu.EfficiencyClass > topo.MaxEfficiencyClass)
                                topo.MaxEfficiencyClass = cpu.EfficiencyClass;

                            topo.CpuSets.Add(cpu);
                        }
                    }

                    ptr += size;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return Normalize(topo);
        }

        /// <summary>
        /// Ensures min/max are consistent (both 0) when no usable CPU sets were found.
        /// </summary>
        private static CpuSetTopology Normalize(CpuSetTopology topo)
        {
            if (topo.CpuSets.Count == 0)
            {
                topo.MinEfficiencyClass = 0;
                topo.MaxEfficiencyClass = 0;
            }
            return topo;
        }

        /// <summary>
        /// Maps a <see cref="CpuSetType"/> to the corresponding efficiency class byte.
        /// Must not be called with <see cref="CpuSetType.None"/>; that case must be handled upstream.
        /// </summary>
        private static byte GetTargetEfficiencyClass(CpuSetType type, in CpuSetTopology topo)
        {
            switch (type)
            {
                case CpuSetType.Efficiency:  return topo.MinEfficiencyClass;
                case CpuSetType.Performance: return topo.MaxEfficiencyClass;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type,
                        "CpuSetType.None must be handled before calling this method.");
            }
        }

        /// <summary>
        /// Returns the CPU Set IDs whose efficiency class matches the requested <paramref name="type"/>.
        /// An empty array signals the caller to fall back to unrestricted scheduling.
        /// </summary>
        private static uint[] SelectCpuSetIds(CpuSetType type, in CpuSetTopology topo)
        {
            if (topo.CpuSets.Count == 0) return Array.Empty<uint>();

            byte       target = GetTargetEfficiencyClass(type, in topo);
            var        ids    = new List<uint>();

            foreach (var cpu in topo.CpuSets)
                if (cpu.EfficiencyClass == target)
                    ids.Add(cpu.Id);

            return ids.ToArray();
        }

        /// <summary>
        /// Builds a 64-bit affinity bitmask for the given processor <paramref name="group"/>
        /// and <paramref name="targetEff"/> efficiency class. Bit N is set when
        /// <c>LogicalProcessorIndex == N</c> for a matching core.
        /// </summary>
        private static ulong BuildGroupAffinityMask(in CpuSetTopology topo, byte targetEff, ushort group)
        {
            ulong mask = 0;
            foreach (var cpu in topo.CpuSets)
            {
                if (cpu.Group != group)                  continue;
                if (cpu.EfficiencyClass != targetEff)    continue;
                if (cpu.LogicalProcessorIndex >= 64)     continue; // 64-bit bitmask ceiling

                mask |= 1UL << cpu.LogicalProcessorIndex;
            }
            return mask;
        }

        /// <summary>
        /// Returns the processor group that owns the first usable core with the given efficiency class.
        /// Used as a fallback when the thread's current group contains no matching cores.
        /// Always returns 0 (valid for all consumer hardware) when nothing is found.
        /// </summary>
        private static ushort FindFirstGroupForEfficiencyClass(in CpuSetTopology topo, byte targetEff)
        {
            foreach (var cpu in topo.CpuSets)
                if (cpu.EfficiencyClass == targetEff)
                    return cpu.Group;
            return 0;
        }

        /// <summary>
        /// Returns the processor group the calling thread is currently running in.
        /// Falls back to 0 — always valid on consumer hardware — if the query fails.
        /// </summary>
        private static ushort GetCurrentThreadGroup(IntPtr hThread)
        {
            return GetThreadGroupAffinity(hThread, out GROUP_AFFINITY ga) ? ga.Group : (ushort)0;
        }

        /// <summary>
        /// Resets the calling thread's affinity to the full set of processors allowed by the process.
        /// Uses <c>SetThreadAffinityMask</c> for the common single-group case (all consumer hardware),
        /// and falls back to <c>SetThreadGroupAffinity</c> for threads in a non-zero processor group
        /// (systems with more than 64 logical processors).
        /// </summary>
        /// <exception cref="Win32Exception">
        /// Thrown if <c>GetProcessAffinityMask</c> fails or if both affinity reset attempts fail.
        /// </exception>
        private static void ResetThreadAffinityToProcessDefault(IntPtr hThread)
        {
            IntPtr hProcess = GetCurrentProcess();

            if (!GetProcessAffinityMask(hProcess, out UIntPtr procMask, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "GetProcessAffinityMask failed.");

            // Fast path: works for all threads in processor group 0.
            if (SetThreadAffinityMask(hThread, procMask) != UIntPtr.Zero)
                return;

            // Fallback: thread is in a non-zero processor group (128+ core systems).
            // Query the actual group so we never hardcode 0.
            ushort currentGroup = GetCurrentThreadGroup(hThread);
            var ga = new GROUP_AFFINITY { Mask = procMask, Group = currentGroup };

            if (!SetThreadGroupAffinity(hThread, ref ga, IntPtr.Zero))
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Failed to reset thread affinity to process default.");
        }

        // ==============================================================
        // Native Structures
        // ==============================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct THREAD_POWER_THROTTLING_STATE
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GROUP_AFFINITY
        {
            public UIntPtr Mask;
            public ushort  Group;
            public ushort  Reserved1;
            public ushort  Reserved2;
            public ushort  Reserved3;
        }

        // ==============================================================
        // Native Constants
        // ==============================================================

        private const uint THREAD_POWER_THROTTLING_CURRENT_VERSION = 1;
        private const uint THREAD_POWER_THROTTLING_EXECUTION_SPEED = 1;
        private const int  TIC_THREAD_POWER_THROTTLING_WIN10       = 1;
        private const int  TIC_THREAD_POWER_THROTTLING_WIN11       = 3;
        private const int  ERROR_INVALID_PARAMETER                 = 87;

        // ==============================================================
        // P/Invoke Declarations
        // ==============================================================

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetThreadInformation(
            IntPtr                           hThread,
            int                              ThreadInformationClass,
            ref THREAD_POWER_THROTTLING_STATE ThreadInformation,
            uint                             ThreadInformationSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemCpuSetInformation(
            IntPtr    Information,
            uint      BufferLength,
            out uint  ReturnedLength,
            IntPtr    Process,
            uint      Flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetThreadSelectedCpuSets(
            IntPtr Thread,
            uint[] CpuSetIds,
            uint   CpuSetIdCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetThreadGroupAffinity(
            IntPtr             hThread,
            out GROUP_AFFINITY GroupAffinity);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetThreadGroupAffinity(
            IntPtr             hThread,
            ref GROUP_AFFINITY GroupAffinity,
            IntPtr             PreviousGroupAffinity);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessAffinityMask(
            IntPtr      hProcess,
            out UIntPtr lpProcessAffinityMask,
            out UIntPtr lpSystemAffinityMask);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr SetThreadAffinityMask(
            IntPtr  hThread,
            UIntPtr dwThreadAffinityMask);
			
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessAffinityMask(
            IntPtr  hProcess,
            UIntPtr dwProcessAffinityMask);
			
    }
}
