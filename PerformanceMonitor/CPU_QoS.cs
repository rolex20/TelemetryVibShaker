using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PerformanceMonitor
{
    internal static class CPU_QoS
    {
        // ==========================================================
        // Enums and Public API
        // ==========================================================

        /// <summary>
        /// Defines the target CPU type for the thread affinity.
        /// </summary>
        public enum CpuSetType
        {
            /// <summary>
            /// Removes any specific CPU Set restrictions. The OS scheduler decides where to run the thread.
            /// </summary>
            None = 0,

            /// <summary>
            /// Restricts the thread to the most efficient cores (E-Cores).
            /// </summary>
            Efficiency = 1,

            /// <summary>
            /// Restricts the thread to the highest performance cores (P-Cores).
            /// </summary>
            Performance = 2
        }

        /// <summary>
        /// Retrieves a handle to the current thread, which can be used for setting QoS and CPU Set configurations in PowerShell or other contexts.
        public static IntPtr GetCurrentWinThreadHandle()
        {
            return GetCurrentThread();
        }

        /// <summary>
        /// Sets or Unsets EcoQoS (Efficiency Mode / Power Throttling) for the current thread.
        /// </summary>
        /// <param name="enable">True to enable EcoQoS, False to disable it.</param>
        public static void SetEcoQoS(bool enable)
        {
            var state = new THREAD_POWER_THROTTLING_STATE
            {
                Version = THREAD_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = THREAD_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = enable ? THREAD_POWER_THROTTLING_EXECUTION_SPEED : 0
            };

            IntPtr hThread = GetCurrentThread();
            uint size = (uint)Marshal.SizeOf(typeof(THREAD_POWER_THROTTLING_STATE));

            // Try Windows 11 API first (Class ID 3)
            bool result = SetThreadInformation(hThread, TIC_THREAD_POWER_THROTTLING_WIN11, ref state, size);

            if (!result)
            {
                int err = Marshal.GetLastWin32Error();
                // Fallback for Windows 10 (Class ID 1) if parameter is invalid
                if (err == ERROR_INVALID_PARAMETER)
                {
                    result = SetThreadInformation(hThread, TIC_THREAD_POWER_THROTTLING_WIN10, ref state, size);
                }

                if (!result)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set EcoQoS state.");
                }
            }
        }

        /// <summary>
        /// Configures the CPU Sets for the current thread using a string (e.g., "Performance", "Efficiency").
        /// Useful for PowerShell or dynamic input.
        /// </summary>
        public static void SetCpuSets(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                SetCpuSets(CpuSetType.None);
                return;
            }

            // Case-insensitive parsing
            if (Enum.TryParse(mode, true, out CpuSetType result))
            {
                SetCpuSets(result);
            }
            else
            {
                // Fallback: Check for partial matches or specific keywords
                string m = mode.ToLowerInvariant();
                if (m.Contains("eff") || m.Contains("eco")) SetCpuSets(CpuSetType.Efficiency);
                else if (m.Contains("perf")) SetCpuSets(CpuSetType.Performance);
                else SetCpuSets(CpuSetType.None);
            }
        }

        /// <summary>
        /// Configures the CPU Sets for the current thread using the typed Enum.
        /// </summary>
        public static void SetCpuSets(CpuSetType type)
        {
            IntPtr hThread = GetCurrentThread();

            // 1. If None, clear restrictions immediately
            if (type == CpuSetType.None)
            {
                if (!SetThreadSelectedCpuSets(hThread, null, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to clear CPU Sets.");
                return;
            }

            // 2. Discover all CPU Sets and their Efficiency Classes
            byte minEff, maxEff;
            var cpuSets = GetAllCpuSets(out minEff, out maxEff);

            if (cpuSets.Count == 0) return; // Should not happen on standard hardware

            // 3. Filter IDs based on requested Enum
            var selectedIds = new List<uint>();

            foreach (var cpu in cpuSets)
            {
                // Skip CPUs allocated strictly to other processes (system exclusive)
                if (cpu.Allocated && !cpu.AllocatedToTargetProcess) continue;

                if (type == CpuSetType.Efficiency && cpu.EfficiencyClass == minEff)
                {
                    selectedIds.Add(cpu.Id);
                }
                else if (type == CpuSetType.Performance && cpu.EfficiencyClass == maxEff)
                {
                    selectedIds.Add(cpu.Id);
                }
            }

            // 4. Apply
            // Note: If system is homogeneous (min==max), both modes select all allowed CPUs.
            if (selectedIds.Count > 0)
            {
                uint[] idsArray = selectedIds.ToArray();
                if (!SetThreadSelectedCpuSets(hThread, idsArray, (uint)idsArray.Length))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set specific CPU Sets.");
                }
            }
            else
            {
                // If logic finds no cores (rare), revert to default
                SetThreadSelectedCpuSets(hThread, null, 0);
            }
        }

        // ==========================================================
        // Internal Helper Structs & Logic
        // ==========================================================

        private struct InternalCpuSet
        {
            public uint Id;
            public byte EfficiencyClass;
            public bool Allocated;
            public bool AllocatedToTargetProcess;
        }

        private static List<InternalCpuSet> GetAllCpuSets(out byte minEff, out byte maxEff)
        {
            minEff = 255;
            maxEff = 0;
            var results = new List<InternalCpuSet>();

            IntPtr hProcess = GetCurrentProcess();
            uint reqLength = 0;

            // Query size
            GetSystemCpuSetInformation(IntPtr.Zero, 0, out reqLength, hProcess, 0);

            if (reqLength == 0) return results;

            IntPtr buffer = Marshal.AllocHGlobal((int)reqLength);
            try
            {
                // Fetch Data
                if (!GetSystemCpuSetInformation(buffer, reqLength, out reqLength, hProcess, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "GetSystemCpuSetInformation failed");

                // Manually walk the buffer
                long current = buffer.ToInt64();
                long end = current + reqLength;

                while (current < end)
                {
                    IntPtr p = new IntPtr(current);
                    int size = Marshal.ReadInt32(p); // SYSTEM_CPU_SET_INFORMATION.Size
                    if (size <= 0) break;

                    int type = Marshal.ReadInt32(p, 4); // Type (CpuSetInformation = 0)

                    if (type == 0)
                    {
                        uint id = (uint)Marshal.ReadInt32(p, 8);
                        byte effClass = Marshal.ReadByte(p, 18); // EfficiencyClass offset
                        byte flags = Marshal.ReadByte(p, 19);    // AllFlags offset

                        // Flag bit 1 = Allocated, bit 2 = AllocatedToTargetProcess
                        bool allocated = (flags & 0x02) != 0;
                        bool allocatedToThis = (flags & 0x04) != 0;

                        if (effClass < minEff) minEff = effClass;
                        if (effClass > maxEff) maxEff = effClass;

                        results.Add(new InternalCpuSet
                        {
                            Id = id,
                            EfficiencyClass = effClass,
                            Allocated = allocated,
                            AllocatedToTargetProcess = allocatedToThis
                        });
                    }
                    current += size;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return results;
        }

        // ==========================================================
        // Native API Imports
        // ==========================================================

        [StructLayout(LayoutKind.Sequential)]
        private struct THREAD_POWER_THROTTLING_STATE
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        private const uint THREAD_POWER_THROTTLING_CURRENT_VERSION = 1;
        private const uint THREAD_POWER_THROTTLING_EXECUTION_SPEED = 1;
        private const int TIC_THREAD_POWER_THROTTLING_WIN10 = 1;
        private const int TIC_THREAD_POWER_THROTTLING_WIN11 = 3;
        private const int ERROR_INVALID_PARAMETER = 87;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetThreadInformation(
            IntPtr hThread,
            int ThreadInformationClass,
            ref THREAD_POWER_THROTTLING_STATE ThreadInformation,
            uint ThreadInformationSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemCpuSetInformation(
            IntPtr Information,
            uint BufferLength,
            out uint ReturnedLength,
            IntPtr Process,
            uint Flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetThreadSelectedCpuSets(
            IntPtr Thread,
            uint[] CpuSetIds,
            uint CpuSetIdCount);
    }


}
