<#
.SYNOPSIS
    Configures default CPU sets for a specified process and its threads to optimize performance.

.DESCRIPTION
    This script utilizes Windows API functions from kernel32.dll to configure default CPU sets for a specified process and its threads. 
    Unlike directly setting processor affinities, this approach allows the operating system to manage thread scheduling with a preference for the specified CPU sets. 
    This is particularly useful for optimizing performance in applications where it is desirable to favor high-performance cores over efficiency cores while still allowing 
    the OS to balance power management and performance.

    The script prompts the user to input the target process name and a list of CPU IDs to designate as preferred CPU sets. It updates the default CPU sets for the process 
    and iteratively applies these settings to each thread within the process. This is particularly useful in scenarios like optimizing Microsoft Flight Simulator 2020 
    (MSFS 2020) performance at ultra settings on high-resolution VR setups (2800x2800 per eye), ensuring that important threads are run on performance cores whenever possible.
	
	Setting hard affinities prevents using Efficiency cores on Intel hybrid architectures wasting CPU processing when all performance cores are busy.
	
	Originally, ..."CPU Sets provide APIs to declare application affinity in a 'soft' manner that is compatible with OS power management".

.PARAMETER processName
    The name of the process for which the default CPU sets should be managed. The script prompts the user to input this parameter.

.PARAMETER cpuIds
    A comma-separated list of CPU IDs that will be used to set the default CPU sets for the target process and its threads. The script prompts the user to input this parameter.

.NOTES
    Requires administrative privileges to run successfully.
    This script is designed for Windows environments and utilizes specific kernel32.dll functions, such as SetProcessDefaultCpuSets, SetThreadSelectedCpuSets, 
    GetThreadSelectedCpuSets, and GetSystemCpuSetInformation.
    It uses P/Invoke to call these unmanaged functions within PowerShell.

.EXAMPLE
    # Run the script to set default CPU sets for MSFS 2020, a process named "FlightSimulator" with my 8 Performance Cores in my 12700K.  Windows IDs processors starting from 256 for very complicated reasons to explain here.
    PS> .\Set-ProcessCpuSets.ps1

    Enter the name of the target process: FlightSimulator
    Enter the list of CPU IDs separated by commas: 256,257,258,259,260,261,262,263

    The script will set the specified CPUs as the default CPU sets for the "FlightSimulator" process and update its threads accordingly, 
    allowing Windows to manage thread scheduling with a preference for performance cores.

.LINK
    For more information on CPU sets and Windows API functions:
    https://learn.microsoft.com/en-us/windows/win32/procthread/cpu-sets
	https://learn.microsoft.com/en-us/windows/uwp/xbox-apps/cpusets-games	
	

#>

# Define the SetProcessDefaultCpuSets, SetThreadSelectedCpuSets, GetThreadSelectedCpuSets, and GetSystemCpuSetInformation functions from kernel32.dll
Add-Type @"
using System;
using System.Runtime.InteropServices;

public class CpuSetHelper {

// The SYSTEM_CPU_SET_INFORMATION structure has been modified from its original C++ definition to ensure
// proper interop with the GetSystemCpuSetInformation function in C#. The changes made are as follows:
//
// 1. Flattened Structure:
//    The original structure in C++ uses unions and nested structures, which are complex to replicate in C#.
//    This version flattens the structure by directly declaring all fields. This simplifies the memory layout
//    and avoids potential issues with alignment and incorrect data interpretation.
//
// 2. Correct Field Types and Sizes:
//    The field types have been carefully chosen to match the size and type of their C++ counterparts:
//    - int for Size and Type to match DWORD (4-byte integers in C++).
//    - uint for Id and Reserved to match DWORD (4-byte unsigned integers in C++).
//    - ushort for Group to match WORD (2-byte unsigned integer in C++).
//    - byte for LogicalProcessorIndex, CoreIndex, LastLevelCacheIndex, NumaNodeIndex, EfficiencyClass, 
//      and AllFlags to match BYTE (1-byte unsigned integers in C++).
//    - ulong for AllocationTag to match ULONG_PTR (pointer-sized unsigned integer, 8 bytes on 64-bit systems).
//
// These adjustments ensure that the structure is correctly marshaled and that data from GetSystemCpuSetInformation 
// is interpreted accurately, preventing garbled output.

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_CPU_SET_INFORMATION {
		public int Size;
		public int Type;
        public uint Id; // DWORD
        public ushort Group; // WORD
        public byte LogicalProcessorIndex;
        public byte CoreIndex;
        public byte LastLevelCacheIndex;
        public byte NumaNodeIndex;
        public byte EfficiencyClass;
        public byte AllFlags;
        public uint Reserved; // DWORD
        public ulong AllocationTag; // ULONG_PTR
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetProcessDefaultCpuSets(IntPtr process, uint[] cpuSetIds, uint cpuSetIdCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetProcessDefaultCpuSets(IntPtr process, uint[] cpuSetIds, uint cpuSetIdCount, ref uint requiredIdCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetThreadSelectedCpuSets(IntPtr thread, uint[] cpuSetIds, uint cpuSetIdCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetThreadSelectedCpuSets(IntPtr thread, uint[] cpuSetIds, uint cpuSetIdCount, ref uint requiredIdCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetSystemCpuSetInformation(IntPtr information, uint bufferLength, ref uint returnedLength, IntPtr process, uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    public static SYSTEM_CPU_SET_INFORMATION[] GetSystemCpuSetInfo() {
        uint bufferLength = 0;
        GetSystemCpuSetInformation(IntPtr.Zero, 0, ref bufferLength, IntPtr.Zero, 0);
        IntPtr buffer = Marshal.AllocHGlobal((int)bufferLength);
        try {
            if (!GetSystemCpuSetInformation(buffer, bufferLength, ref bufferLength, IntPtr.Zero, 0)) {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            int count = (int)(bufferLength / Marshal.SizeOf(typeof(SYSTEM_CPU_SET_INFORMATION)));
            SYSTEM_CPU_SET_INFORMATION[] cpuSetInfo = new SYSTEM_CPU_SET_INFORMATION[count];
            IntPtr current = buffer;
            for (int i = 0; i < count; i++) {
                cpuSetInfo[i] = Marshal.PtrToStructure<SYSTEM_CPU_SET_INFORMATION>(current);
                current = IntPtr.Add(current, Marshal.SizeOf(typeof(SYSTEM_CPU_SET_INFORMATION)));
            }
            return cpuSetInfo;
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static void SetDefaultCpuSets(IntPtr processHandle, uint[] cpuSetIds) {
        if (!SetProcessDefaultCpuSets(processHandle, cpuSetIds, (uint)cpuSetIds.Length)) {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static uint[] GetDefaultCpuSets(IntPtr processHandle) {
        uint requiredIdCount = 0;
        GetProcessDefaultCpuSets(processHandle, null, 0, ref requiredIdCount);
        uint[] cpuSetIds = new uint[requiredIdCount];
        if (!GetProcessDefaultCpuSets(processHandle, cpuSetIds, (uint)cpuSetIds.Length, ref requiredIdCount)) {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        return cpuSetIds;
    }

    public static void SetThreadCpuSets(IntPtr threadHandle, uint[] cpuSetIds) {
        if (!SetThreadSelectedCpuSets(threadHandle, cpuSetIds, (uint)cpuSetIds.Length)) {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static uint[] GetThreadCpuSets(IntPtr threadHandle) {
        uint requiredIdCount = 0;
        GetThreadSelectedCpuSets(threadHandle, null, 0, ref requiredIdCount);
        uint[] cpuSetIds = new uint[requiredIdCount];
        if (!GetThreadSelectedCpuSets(threadHandle, cpuSetIds, (uint)cpuSetIds.Length, ref requiredIdCount)) {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        return cpuSetIds;
    }
}
"@

# Define constants for thread access rights
$THREAD_SET_INFORMATION = 0x0020
$THREAD_QUERY_INFORMATION = 0x0040

# Get and print all available CPU set information
# and most importantly, which IDs you can use in your own system.
$cpuSetInfo = [CpuSetHelper]::GetSystemCpuSetInfo()
foreach ($info in $cpuSetInfo) {
    Write-Output "CPU Set ID: $($info.Id), Group: $($info.Group), Logical Processor Index: $($info.LogicalProcessorIndex), Core Index: $($info.CoreIndex), Efficiency Class: $($info.EfficiencyClass)"
}

# Prompt the user for the process name
$processName = Read-Host "Enter the name of the target process"

# Prompt the user for the list of CPU IDs
$cpuIdsInput = Read-Host "Enter the list of CPU IDs separated by commas"
$cpuIds = $cpuIdsInput -split ',' | ForEach-Object { [uint32]$_.Trim() }

# Get the process by name
$process = Get-Process -Name $processName -ErrorAction Stop

# Set the specified CPUs as the default CPU sets for the target process
[CpuSetHelper]::SetDefaultCpuSets($process.Handle, $cpuIds)

# Iterate over each thread in the process and set the specified CPUs
foreach ($thread in $process.Threads) {
    $threadHandle = [CpuSetHelper]::OpenThread($THREAD_SET_INFORMATION -bor $THREAD_QUERY_INFORMATION, $false, $thread.Id)
    if ($threadHandle -eq [IntPtr]::Zero) {
        Write-Error "Failed to open handle for thread ID $($thread.Id)"
        continue
    }

    try {
        [CpuSetHelper]::SetThreadCpuSets($threadHandle, $cpuIds)
    } finally {
        $r = [CpuSetHelper]::CloseHandle($threadHandle)
    }
}

# Verify the CPU sets for each thread
foreach ($thread in $process.Threads) {
    $threadHandle = [CpuSetHelper]::OpenThread($THREAD_QUERY_INFORMATION, $false, $thread.Id)
    if ($threadHandle -eq [IntPtr]::Zero) {
        Write-Error "Failed to open handle for thread ID $($thread.Id)"
        continue
    }

    try {
        $threadCpuSets = [CpuSetHelper]::GetThreadCpuSets($threadHandle)
        Write-Output "Thread ID $($thread.Id) CPU sets: $($threadCpuSets -join ', ')"
    } finally {
        $r = [CpuSetHelper]::CloseHandle($threadHandle)
    }
}

Write-Output "Default CPU sets updated to CPUs $($cpuIds -join ', ') for process $processName and its threads."
