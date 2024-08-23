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


function Get-EfficiencyCoreAffinity {
    param (
        [int]$cpuCount
    )

    switch ($cpuCount) {
        28 { return 268369920 } # Efficiency cores with hyperthreading enabled
        20 { return 1048320 } # Efficiency cores with hyperthreading disabled
        default { throw "Unsupported CPU count: $cpuCount" }
    }
}

function Get-PerformanceCoreAffinity {
    param (
        [int]$cpuCount
    )

    switch ($cpuCount) {
        28 { return 65535 } # Performance cores with hyperthreading enabled
        20 { return 255 } # Performance cores with hyperthreading disabled
        default { throw "Unsupported CPU count: $cpuCount" }
    }
}


function Get-CPUCount {
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
        Write-Host "Failed to set ideal processor for thread $($thread.Id): $newProcessor"
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
        Write-Host "Failed to enable privilege."
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


#Given a Process.ID it reassigs Affinity and Ideal Processor (round robin)
#The new ideal processor will be only from the Affinity allowed
function Set-ProcessAffinityAndPriority {
    param (
        [string]$ProcessId,
        [int]$Affinity,
        [string]$ProcessPriority,
		[string]$ThreadPriority
    )


    $process = Get-Process -Id $processID -ErrorAction SilentlyContinue

    if (-not $process) {
        Write-Host "Process not found."
        exit
    }

    if (-not (Enable-Privilege -Privilege $SE_INC_BASE_PRIORITY_NAME)) {
        Write-Host "Failed to enable privilege."
        exit
    }


    # Set the processor affinity
    if ($Affinity -ge 0) {$process.ProcessorAffinity = [IntPtr]$Affinity}

    # Set the process priority
    if ($ProcessPriority -ne "DoNotChange") { 
        Write-Host "Set Priority Class for process $($process.Name) = $ProcessPriority"
        $process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::$ProcessPriority
    }


    # Reassign PriorityClass and new ideal processor to each thread
	
	# The new process assignment will select the physical cores first to the most busy threads
	$sortedThreads = $process.Threads | Sort-Object TotalProcessorTime -Descending
	
  
    $allowedProcessors = Get-SetBits -number $Affinity
	# Setting optimized Ideal Processor based on affinity
	if ($allowedProcessors) {


        $physicalProcessors = @(0,2,4,6,8,10,12,14,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31) # Valid for #12700K, 14700K, it doesn't affect if it is not found
        $optimizedProcessorAsignment = Add-100ToMatches -array1 $allowedProcessors -array2 $physicalProcessors

        $n = $allowedProcessors.Count
        $i = 0
        foreach ($thread in $sortedThreads) {
            #if ($ThreadPriority -ne "DoNotChange") { $thread.PriorityLevel = [System.Diagnostics.ThreadPriorityLevel]::$ThreadPriority } #cannot rely on doing this here if $allowedProcessors==$null
            $index = $i++ % $n
			if ($Affinity -ge 0) {
				$thread.ProcessorAffinity = [IntPtr]$Affinity
				Set-ThreadIdealProcessor $thread $optimizedProcessorAsignment[$index]				
			}
            
        }
    }
	
	# Setting thread processor Affinity
	foreach ($thread in $sortedThreads) {
		if ($ThreadPriority -ne "DoNotChange") { 
			Write-Host "Set Priority Class for thread $($thread.Id) = $ThreadPriority"
			$thread.PriorityLevel = [System.Diagnostics.ThreadPriorityLevel]::$ThreadPriority 
		}
	}

    $processName = $process.Name
    Write-Output "Affinity and priority for '$processName' processes/threads have been set."
}


# Show Threads belonging to a process sorted by TotalProcessorTime descending
function Get-ProcessThreadTimes {
    param (
        [string]$ProcessName
    )

    # Get the process object
    $process = Get-Process -Name $ProcessName -ErrorAction Stop

    # Get the threads of the process
    $threads = $process.Threads

    # Sort the threads by TotalProcessorTime in descending order
    $sortedThreads = $threads | Sort-Object TotalProcessorTime -Descending

    # Print the TotalProcessorTime for each thread
    Write-Host "Most busy threads:" + $threads.Count
    foreach ($thread in $sortedThreads) {
        [PSCustomObject]@{
            ThreadId = $thread.Id
            TotalProcessorTime = $thread.TotalProcessorTime
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
#input parameter can be "Efficiency", "Performance" or a number
function Get-NumericAffinity($affinity) {
    $cpuCount = Get-CPUCount
    switch($affinity) {
        "Efficiency" {return Get-EfficiencyCoreAffinity -cpuCount $cpuCount}
        "Performance" {return Get-PerformanceCoreAffinity -cpuCount $cpuCount}
		"DoNotChange" {return -1}
        default {return $affinity}
    }
}


function Run-ActionsPerGame($processName, $fileName) {
    $game = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if (-not $game) { return }


    $actions = Get-ActionsPerGame $fileName

    foreach ($action in $actions) {
        $process = Get-Process -Name $action.process_name -ErrorAction SilentlyContinue
        if (-not $process) { continue }
        if (($game.Name) -ne ($process.Name)) {continue}


        $affinity = Get-NumericAffinity $action.parameters.affinity

Write-Host " "
Write-Host "STARTING " + $process.Name -ForegroundColor Yellow

        Set-ProcessAffinityAndPriority -ProcessId $process.Id -Affinity $affinity -ProcessPriority $action.parameters.process_priority -ThreadPriority $action.parameters.thread_priority

        foreach ($dependance in $action.parameters.dependances) {
            $depProcess = Get-Process -Name $dependance.process_name -ErrorAction SilentlyContinue
            if (-not $depProcess) { continue}
            
            $affinity = Get-NumericAffinity $dependance.affinity

Write-Host " "
Write-Host "STARTING " + $depProcess.Name -ForegroundColor Yellow

            Set-ProcessAffinityAndPriority -ProcessId $depProcess.Id -Affinity $affinity -ProcessPriority $dependance.process_priority -ThreadPriority $dependance.thread_priority
        }

    }
}





<#
# Example usage


Get-ProcessThreadTimes -ProcessName "FlightSimulator"
Get-ProcessThreadTimes -ProcessName "NotePad"
Get-ProcessThreadTimes -ProcessName "joystick_gremlin"

Get-ProcessThreadTimes -ProcessName "FlightSimulator"

Run-ActionsPerGame "FlightSimulator" "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\action-per-process.json"





Set-ProcessAffinityAndPriority -ProcessId 22456 -Affinity 5 -Priority "AboveNormal"

Get-ProcessThreadTimes -ProcessName "notepad"

# Example usage:
$array1 = @(10, 20, 30, 40, 50)
$array2 = @(20, 40)
$result = Add-100ToMatches -array1 $array1 -array2 $array2
Write-Output $result


Get-ProcessThreadTimes -ProcessName "notepad"

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
#>


