# Define the SetProcessDefaultCpuSets, SetThreadSelectedCpuSets, GetThreadSelectedCpuSets, and GetSystemCpuSetInformation functions from kernel32.dll
# See and search for SetGet-DefaultCpuSets.ps1 for extended information
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


# Define constants
$THREAD_SET_INFORMATION = 0x0020
$THREAD_SET_PRIORITY = 0x0040
$THREAD_PRIORITY_IDLE = -15
$IDLE_PRIORITY_CLASS = 0x40
$SE_PRIVILEGE_ENABLED = 0x00000002
$SE_INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege"

# Import necessary functions from kernel32.dll and advapi32.dll
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class NativeMethods {
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetThreadIdealProcessor(IntPtr hThread, uint dwIdealProcessor);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, System.Text.StringBuilder lpBuffer, uint nSize, IntPtr Arguments);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_PRIVILEGES {
        public uint PrivilegeCount;
        public LUID Luid;
        public uint Attributes;
    }
}
"@


#12700K
#$EfficiencyAffinity = 3840  # HyperThreading disabled
#$EfficiencyAffinity = 983040 # HyperThreading enabled
#$PowerAffinity = 21845 # No HyperThreading cores
#$PowerAffinity = 65535 # HyperThreading enabled

#14700K
#$EfficiencyAffinity = 268369920 # HyperThreading enabled
#$PowerAffinity = 65535 # HyperThreading enabled

#Configured for my 14700K
function Get-EfficiencyCoreAffinity {
    param (
        [int]$cpuCount
    )

    switch ($cpuCount) {
        28 { return 268369920 } # Efficiency cores with hyperthreading enabled
        20 { return 1048320 } # Efficiency cores with hyperthreading disabled
        8  { return 255} # Hyperthreading and E-cores disabled
        default { throw "Not-Implemented CPU count: $cpuCount" }
    }
}

#Configured for my 14700K
function Get-PerformanceCoreAffinity {
    param (
        [int]$cpuCount
    )

    switch ($cpuCount) {
        28 { return 65535 } # P-cores with hyperthreading enabled
        20 { return 255 } # P-cores with hyperthreading disabled
        8  { return 255} # Hyperthreading and E-cores disabled
        default { throw "Not-Implemented CPU count: $cpuCount" }
    }
}

function Get-AllCoreAffinity {
    param (
        [int]$cpuCount
    )

    switch ($cpuCount) {
        28 { return 268435455 } # All cores with hyperthreading enabled
        20 { return 1048575 } # All cores with hyperthreading disabled
        8 { return 255} # Hyperthreading and E-Cores disabled
        default { throw "Not-Implemented CPU count: $cpuCount" }
    }
}


function Get-CPU-Count {
    try {
        $cpuKeyPath = "HKLM:\HARDWARE\DESCRIPTION\System\CentralProcessor"
        $cpuCount = (Get-ChildItem -Path $cpuKeyPath).Count
        return $cpuCount
    } catch {
        Write-Error "Failed to retrieve CPU count from the registry: $_"
        return $null
    }
}


function Get-ErrorMessage {
    param (
        [int]$ErrorCode
    )
    $messageBuffer = New-Object System.Text.StringBuilder 512
    [NativeMethods]::FormatMessage(0x00001000 -bor 0x00000200, [IntPtr]::Zero, $ErrorCode, 0, $messageBuffer, $messageBuffer.Capacity, [IntPtr]::Zero) | Out-Null
    return $messageBuffer.ToString().Trim()
}

function Enable-Privilege {
    param (
        [string]$Privilege
    )
    $tokenHandle = [IntPtr]::Zero
    if (-not [NativeMethods]::OpenProcessToken([System.Diagnostics.Process]::GetCurrentProcess().Handle, 0x0020 -bor 0x0008, [ref]$tokenHandle)) {
        return $false
    }

    $luid = New-Object NativeMethods+LUID
    if (-not [NativeMethods]::LookupPrivilegeValue($null, $Privilege, [ref]$luid)) {
        [NativeMethods]::CloseHandle($tokenHandle) | Out-Null
        return $false
    }

    $tp = New-Object NativeMethods+TOKEN_PRIVILEGES
    $tp.PrivilegeCount = 1
    $tp.Luid = $luid
    $tp.Attributes = $SE_PRIVILEGE_ENABLED

    if (-not [NativeMethods]::AdjustTokenPrivileges($tokenHandle, $false, [ref]$tp, 0, [IntPtr]::Zero, [IntPtr]::Zero)) {
        [NativeMethods]::CloseHandle($tokenHandle) | Out-Null
        return $false
    }

    [NativeMethods]::CloseHandle($tokenHandle) | Out-Null
    return $true
}

function Set-ThreadIdealProcessor($threadObject, $newProcessor) {

    $hThread = [NativeMethods]::OpenThread($THREAD_SET_INFORMATION -bor $THREAD_SET_PRIORITY, $false, $threadObject.Id)
    if ($hThread -eq [IntPtr]::Zero) {
        Write-Host "Failed to open thread $($thread.Id)"
        continue
    }

    if (-not [NativeMethods]::SetThreadIdealProcessor($hThread, $newProcessor)) {
        Write-Host "Failed to set ideal processor for thread $($thread.Id): $newProcessor" -ForegroundColor DarkYellow
    } else {
        Write-Host "Set ideal processor for thread $($thread.Id): $newProcessor"
    }


<#
    if (-not [NativeMethods]::SetThreadPriority($hThread, $THREAD_PRIORITY_IDLE)) {
        $errorCode = [System.Runtime.InteropServices.Marshal]::GetLastWin32Error()
        $errorMessage = Get-ErrorMessage -ErrorCode $errorCode
        Write-Host "Failed to set priority for thread $($thread.Id). Error code: $errorCode, Message: $errorMessage"
    } else {
        Write-Host "Set priority for thread $($thread.Id) to idle"
    }
#>

    [NativeMethods]::CloseHandle($hThread) | Out-Null
}

function Set-IdealProcessorForProcess($processName, $newProcessor) {
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue

    if (-not $process) {
        Write-Host "Process not found."
        exit
    }

    if (-not (Enable-Privilege -Privilege $SE_INC_BASE_PRIORITY_NAME)) {
        Write-Host "Failed to enable privilege." -ForegroundColor DarkYellow
        exit
    }

    #$process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::Idle

    foreach ($thread in $process.Threads) {
        Set-ThreadIdealProcessor $thread $newProcessor
    }

    Write-Host "Done."
}

function Get-SetBits {
    param (
        [int]$number
    )
	
	if ($number -lt 0) { return $null }

    $binary = [Convert]::ToString($number, 2)
    $length = $binary.Length
    $setBits = New-Object System.Collections.ArrayList

    for ($i = 0; $i -lt $length; $i++) {
        if ($binary[$length - $i - 1] -eq '1') {
            $setBits.Add($i) | Out-Null
        }
    }

    return [int[]]$setBits
}

function Restore-ProcessToDefaults {
    <#
    .SYNOPSIS
        Restores a process to default settings based on its original boost parameters.
    #>
    param (
        [Parameter(Mandatory)]
        $Process,
        
        [Parameter(Mandatory)]
        $OriginalParameters,
        
        [int]$threadsLimit = 50
    )

    $cpuCount = Get-CPU-Count
    
    # Prepare a parameters hashtable for splatting to Set-ProcessAffinityAndPriority
    $restoreParams = @{
        Process = $Process
        MaximumThreadsToChange = $threadsLimit
        OverrideHigherPriority = $true  # Always override to ensure a clean restore
        ChangeCpuSetForProcessAlso = $false # We are clearing sets, not setting new defaults
    }

    # Conditionally set parameters for restoration only if they were originally changed.
    if ($OriginalParameters.process_priority -ne 'DoNotChange') {
        $restoreParams.ProcessPriority = 'Normal'
    } else {
        $restoreParams.ProcessPriority = 'DoNotChange'
    }

    if ($OriginalParameters.thread_priority -ne 'DoNotChange') {
        $restoreParams.ThreadPriority = 'Normal'
    } else {
        $restoreParams.ThreadPriority = 'DoNotChange'
    }

    if ($OriginalParameters.process_affinity -ne 'DoNotChange') {
        $restoreParams.ProcessAffinity = Get-AllCoreAffinity -cpuCount $cpuCount
    } else {
        $restoreParams.ProcessAffinity = -1 # Signal DoNotChange
    }

    if ($OriginalParameters.thread_ideal_processor -ne 'DoNotChange') {
        $restoreParams.ThreadIdealProcessor = Get-AllCoreAffinity -cpuCount $cpuCount
    } else {
        $restoreParams.ThreadIdealProcessor = -1 # Signal DoNotChange
    }

    if ($OriginalParameters.thread_cpu_sets -ne 'DoNotChange') {
        $restoreParams.CpuSet = @() # An empty array clears the CPU sets
    } else {
        $restoreParams.CpuSet = $null # Signal DoNotChange
    }
    
    # Call the main function with the intelligently constructed restore parameters.
    Set-ProcessAffinityAndPriority @restoreParams
}

#Given a Process.ID it reassigs Affinity and Ideal Processor (round robin)
#The new ideal processor will be only from the Affinity allowed
function Set-ProcessAffinityAndPriority {
    param (
        $Process,
        $ProcessAffinity,
        $ProcessPriority,
		$ThreadIdealProcessor,
		$ThreadPriority,
        $CpuSet,
        $ChangeCpuSetForProcessAlso,
        $MaximumThreadsToChange,
        $OverrideHigherPriority
    )

    $ProcessId = $Process.Id

    try {

        if (-not (Enable-Privilege -Privilege $SE_INC_BASE_PRIORITY_NAME)) {
            Write-Host "Failed to enable privilege."
            exit
        }


        # Set the processor affinity	
        if ($ProcessAffinity -ge 0) {
		    Write-Host "Set Process Affinity for process $($Process.Name) = $ProcessAffinity"
		    $Process.ProcessorAffinity = [IntPtr]$ProcessAffinity
	    }

        # Set the process priority
        if ($ProcessPriority -ne "DoNotChange") { 
            Write-Host "Set Priority Class for process $($Process.Name) = $ProcessPriority"
            $Process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::$ProcessPriority
        }

	
	    # The new ideal cpu assignment will select the physical cores first to the most busy threads, and then hyperthreading threads.  This is a soft assignment
	    $sortedThreads = $Process.Threads | Sort-Object TotalProcessorTime -Descending

        # Set the default CpuSet for new threads created by the process
        if ($CpuSet -AND $CpuSet.Count -GT 0 -AND $ChangeCpuSetForProcessAlso) { 
            Write-Host "Set $($Process.Name) Default Cpu Sets to: $($CpuSet -join ', ')"
            [CpuSetHelper]::SetDefaultCpuSets($Process.Handle, $CpuSet)
        }



	    # Prepare ideal processor assignment if requested
        $allowedProcessors = Get-SetBits -number $ThreadIdealProcessor
	    $physicalProcessors = $null
	    $optimizedProcessorAsignment = $null
	    $n = 0
	    $i = 0
	    if ($allowedProcessors) {
            $physicalProcessors = @(0,2,4,6,8,10,12,14,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31) # Valid for #12700K, 14700K, it doesn't affect if it is not found
            $optimizedProcessorAsignment = Add-100ToMatches -array1 $allowedProcessors -array2 $physicalProcessors

            $n = $allowedProcessors.Count
	    }
	
	    # Do the work on each thread
        $t = 1
	    foreach ($thread in $sortedThreads) {
            if ($t++ -GT $MaximumThreadsToChange) { break }

            # Setting thread processor Priority
            $flag_change_process_priority = $true;
            if ($ThreadPriority -EQ "DoNotChange") { $flag_change_process_priority  = $false }
            else {
                $newPriority = [System.Diagnostics.ThreadPriorityLevel]::$ThreadPriority
                if ($thread.PriorityLevel.value__ -GT $newPriority.value__) {                
                    Write-Host "Warning: This Thread $($thread.Id), has higher priority: $($thread.PriorityLevel), new wanted priority: $newPriority"
                    if ($OverrideHigherPriority) { Write-Host "Warning: Override Higer Priority enabled."  }
                    else {$flag_change_process_priority = $false }
                }
            }

            if ($flag_change_process_priority) {
                $newPriority = [System.Diagnostics.ThreadPriorityLevel]::$ThreadPriority
    			Write-Host "Set Priority Class for thread $($thread.Id) = $ThreadPriority"
	    		$thread.PriorityLevel = $newPriority
            }

		
		    # Setting ProcessAffinity for the ThreadCount
		    if ($ProcessAffinity -ge 0) {
			    $thread.ProcessorAffinity = [IntPtr]$ProcessAffinity
		    }
		
		    # Set next ideal processor
		    if ($allowedProcessors) {
                $index = $i++ % $n
			    Set-ThreadIdealProcessor $thread $optimizedProcessorAsignment[$index]			
		    }

            # Set CPU-Sets 
            Set-Thread-Cpu-Sets $thread $CpuSet
	    }
	

        $processName = $process.Name
        Write-Output "Affinity and priority for '$processName' processes/threads have been set."

    } catch {
        Write-Host "An error occurred in Set-ProcessAffinityAndPriority: $($_.Exception.Message)"
        Write-Host "Error occurred at line: $($_.InvocationInfo.ScriptLineNumber)"
        [System.Media.SystemSounds]::Hand.Play()
    }
}


function Set-Thread-Cpu-Sets($thread, $CpuSet) {
    if ($CpuSet -EQ 0 -OR $null -EQ $CpuSet) { return }

    $threadHandle = [CpuSetHelper]::OpenThread($THREAD_SET_INFORMATION -bor $THREAD_QUERY_INFORMATION, $false, $thread.Id)
    if ($threadHandle -eq [IntPtr]::Zero) {
        Write-Error "Failed to open handle for thread ID $($thread.Id)"
        continue
    }

    try {
        [CpuSetHelper]::SetThreadCpuSets($threadHandle, $CpuSet)
    } finally {
        $r = [CpuSetHelper]::CloseHandle($threadHandle)
    }
}


function Get-Cpu-Sets {
    param (
        [int]$ThreadID
    )

    $threadHandle = [CpuSetHelper]::OpenThread($THREAD_QUERY_INFORMATION, $false, $ThreadID)
    if ($threadHandle -eq [IntPtr]::Zero) {
        return "Failed to open handle for thread ID $ThreadID"
    }

    try {
        $threadCpuSets = [CpuSetHelper]::GetThreadCpuSets($threadHandle)
        return "$($threadCpuSets -join ', ')"
    } finally {
        $r = [CpuSetHelper]::CloseHandle($threadHandle)
    }
}

# Show Threads belonging to a process sorted by TotalProcessorTime descending
function Show-Process-Thread-Times {
    param (
        [string]$ProcessName,
		[int]$ThreadsLimit
    )

    # Get the process object
    $processes = Get-Process -Name $ProcessName  -ErrorAction SilentlyContinue
    foreach($process in $processes) {
		
		try {

			# Get the threads of the process
			$threads = $process.Threads

			# Sort the threads by TotalProcessorTime in descending order
			$sortedThreads = $threads | Sort-Object TotalProcessorTime -Descending | Select-Object -First $ThreadsLimit | ForEach-Object { [PSCustomObject]@{ Id = $_.Id; PriorityLevel= $_.PriorityLevel ; TotalProcessorTime = $_.TotalProcessorTime; Cpu_Sets = Get-Cpu-Sets -ThreadID $_.Id } }


			# Print the TotalProcessorTime for each thread
			Write-Host " " 
			Write-Host "[ProcessName=$ProcessName], [PID=$($process.Id)], [PriorityClass=$($process.PriorityClass)], [PriocessorAffinity=$($process.ProcessorAffinity)], [ThreadCount=$($threads.Count)] - Busiest [$ThreadsLimit] threads:" -ForegroundColor Yellow

			$sortedThreads | Select-Object Id, PriorityLevel, @{Name='TotalProcessorTime';Expression={("{0:hh\:mm\:ss\.fff}" -f $_.TotalProcessorTime)}}, Cpu_Sets | Format-Table -AutoSize | Out-String | ForEach-Object { Write-Host $_ }

		} catch {
			Write-Host "An error occurred in Show-Process-Thread-Times: $($_.Exception.Message)"
			Write-Host "Error occurred at line: $($_.InvocationInfo.ScriptLineNumber)"
			[System.Media.SystemSounds]::Hand.Play()
		}
		
    }

}

#Returns an array based on $array1 where the matching numbers in $array2 are prioritized
function Add-100ToMatches {
    param (
        [int[]]$array1,
        [int[]]$array2
    )

    foreach ($num in $array2) {
        for ($i = 0; $i -lt $array1.Length; $i++) {
            if ($array1[$i] -eq $num) {
                $array1[$i] += 1000
            }
        }
    }

    # Sort the array in descending order
    $array1 = $array1 | Sort-Object -Descending
    

    for ($i = 0; $i -lt $array1.Length; $i++) {
        if ($array1[$i] -ge 1000) {
            $array1[$i] -= 1000
        }
    }

    return $array1
}

function Get-ActionsPerGame($fileName) {
    $jsonString = Get-Content -Path $fileName -Raw

    # Parse JSON and assign to variable
    $actions = $jsonString | ConvertFrom-Json

    return $actions
}

#Returns numeric affinity for the current 14700K processor
#input parameter can be "E-Cores", "P-Cores" or a number
function Get-NumericAffinity($affinity, $cpuCount) {
    switch($affinity) {
        "E-Cores" {return Get-EfficiencyCoreAffinity -cpuCount $cpuCount}
        "P-Cores" {return Get-PerformanceCoreAffinity -cpuCount $cpuCount}
        "All-Cores" {return Get-AllCoreAffinity -cpuCount $cpuCount}
		"DoNotChange" {return -1}
        $null {return -1}
        default {return $affinity}
    }
}



function Get-Cpu-Set($cpuSet, $cpuCount) {
    switch($cpuSet) {
        "E-Cores" {
            switch ($cpuCount) {
                28 { return [uint32[]](272..283) } # Efficiency cores with hyperthreading enabled
                20 { return [uint32[]](264..275) } # Efficiency cores with hyperthreading disabled
                8  { return [uint32[]](256..263) } # Hyperthreading en Efficiency cores disabled                
                default { throw "Not-Implemented CPU count: $cpuCount" }
            }
         }

        "P-Cores" {
            switch ($cpuCount) {
                28 { return [uint32[]](256..271) } # P-Cores with hyperthreading enabled                
                20 { return [uint32[]](256..263) } # P-Cores cores with hyperthreading disabled
                8 { return [uint32[]](256..263) } # Hyperthreading en Efficiency cores disabled
                default { throw "Not-Implemented CPU count: $cpuCount" }
            }
         }

        "All-Cores" {
            switch ($cpuCount) {
                28 { return [uint32[]](256..283) } # hyperthreading enabled
                20 { return [uint32[]](256..275) } # hyperthreading disabled
                8  { return [uint32[]](256..263) } # Hyperthreading en Efficiency cores disabled                
                default { throw "Not-Implemented CPU count: $cpuCount" }
            }
        }

		"DoNotChange" {return 0}

        $null {return 0}

        default {return 0}
    }
}


function Get-TrimmedProcessNames {
    param (
        [string]$processNames
    )

    # Split the string by comma and trim each process name
    $trimmedNames = $processNames -split ',' | ForEach-Object { $_.Trim() }

    return $trimmedNames
}


function Run-Actions-Per-Game($processName, $fileName, $threadsLimit) {
    $cpuCount = Get-CPU-Count
    $actions = Get-ActionsPerGame $fileName
	
    foreach ($action in $actions) {
		$proc_names_array = Get-TrimmedProcessNames $action.process_name
		foreach ($proc_name in $proc_names_array) {
			$processes = Get-Process -Name $proc_name -ErrorAction SilentlyContinue
			foreach($process in $processes) {

				$paffinity = Get-NumericAffinity $action.parameters.process_affinity $cpuCount
				$taffinity = Get-NumericAffinity $action.parameters.thread_ideal_processor $cpuCount
				$cpuset = Get-Cpu-Set $action.parameters.thread_cpu_sets $cpuCount
				$changeCpuSetForProcesses = $action.parameters.process_change_cpu_sets
                $overrideHigherPriority = ( $action.parameters.override_higher_priority -EQ $true)
                if ($action.parameters.max_threads_to_change -GT 0) { #Check if json/process has different definition for number of threads to change
                    $threadsLimit = $action.parameters.max_threads_to_change
                }

				Write-Host " "
				Write-Host "STARTING [$($process.Name)]"  -ForegroundColor Yellow
				
				
				Set-ProcessAffinityAndPriority -Process $process -ProcessAffinity $paffinity -ProcessPriority $action.parameters.process_priority -ThreadIdealProcessor $taffinity -ThreadPriority $action.parameters.thread_priority -CpuSet $cpuset -ChangeCpuSetForProcessAlso $changeCpuSetForProcesses -MaximumThreadsToChange $threadsLimit -OverrideHigherPriority $overrideHigherPriority

				foreach ($dependence in $action.parameters.dependencies) {
					$process_names_array = Get-TrimmedProcessNames $dependence.process_name
					foreach ($process_name in $process_names_array) {
					
						$depProcesses = Get-Process -Name $process_name -ErrorAction SilentlyContinue
						foreach($depProcess in $depProcesses) {					
							$paffinity = Get-NumericAffinity $dependence.process_affinity $cpuCount
							$taffinity = Get-NumericAffinity $dependence.thread_ideal_processor $cpuCount
							$cpuset = Get-Cpu-Set $dependence.thread_cpu_sets $cpuCount
            				$changeCpuSetForProcesses = $dependence.process_change_cpu_sets
                            $overrideHigherPriority = ( $dependence.override_higher_priority -EQ $true)
                            if ($dependence.max_threads_to_change -GT 0) { #Check if json/process has different definition for number of threads to change
                                $threadsLimit = $dependence.max_threads_to_change
                            }

							

							Write-Host " "
							Write-Host "STARTING SUB [$($depProcess.Name)]" -ForegroundColor Yellow

							Set-ProcessAffinityAndPriority -Process $depProcess -ProcessAffinity $paffinity -ProcessPriority $dependence.process_priority -ThreadIdealProcessor $taffinity -ThreadPriority $dependence.thread_priority -CpuSet $cpuset -ChangeCpuSetForProcessAlso $changeCpuSetForProcesses -MaximumThreadsToChange $threadsLimit -OverrideHigherPriority $overrideHigherPriority
						}
					}
				}
			}
		}
    }
}


<#
# Example usage


Show-Process-Thread-Times -ProcessName "FlightSimulator"
Show-Process-Thread-Times -ProcessName "NotePad"
Show-Process-Thread-Times -ProcessName "joystick_gremlin"

Show-Process-Thread-Times -ProcessName "FlightSimulator"

Run-ActionsPerGame "FlightSimulator" "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\action-per-process.json"





Set-ProcessAffinityAndPriority -ProcessId 22456 -Affinity 5 -Priority "AboveNormal"

Show-Process-Thread-Times -ProcessName "notepad"

# Example usage:
$array1 = @(10, 20, 30, 40, 50)
$array2 = @(20, 40)
$result = Add-100ToMatches -array1 $array1 -array2 $array2
Write-Output $result


Show-Process-Thread-Timess -ProcessName "notepad"

# Example usage:
Set-ProcessAffinityAndPriority -ProcessId 16488 -Affinity 5 -Priority "AboveNormal"

# Example usage:
$setBits = Get-SetBits -number 1
Write-Output $setBits

$setBits = Get-SetBits -number 65535
Write-Output $setBits

# Eample usage:
#$processName = Read-Host "Enter the process name"
#Set-IdealProcessorForProcess $processName 3


# Example usage
$processNames = "notepad, chrome , explorer , powershell"
$trimmedArray = Get-TrimmedProcessNames -processNames $processNames

# Output the result
$trimmedArray

#>


