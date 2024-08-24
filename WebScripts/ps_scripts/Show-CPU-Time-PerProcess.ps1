<#
.SYNOPSIS
    Displays a list of processes with their CPU times, prompts user interaction, and shows updated CPU times with differences.

.DESCRIPTION
    This PowerShell script initially retrieves a list of all running processes along with their CPU times. The `CPU` property represents the **total processor time** used by the process since it started. It includes both user mode and kernel mode execution time. The unit of measurement for CPU time is **seconds**.

    The script displays this list with two columns: process name and elapsed CPU time. The user is prompted to press Enter to proceed. After the prompt, the script waits for a short duration, then retrieves an updated list of processes, displaying them with four columns: process name, old CPU elapsed time, new CPU elapsed time, and the difference in CPU time in seconds. Only processes with a positive difference in CPU time are shown in the final output.

.EXAMPLE
    PS> .\ProcessCPUTime.ps1
    This example runs the script, showing the initial and updated CPU times of processes with the differences.

.NOTES
    Author: [Your Name]
    Version: 1.0
    Created: [Date]
    Requirements: PowerShell 5.0 or higher.

.LINK
    For more information about PowerShell Get-Process, visit:
    https://docs.microsoft.com/powershell/module/microsoft.powershell.management/get-process
#>

# PowerShell script to display process information with old and new CPU times (filtered for positive differences)

# Use Efficiency cores only
$EfficiencyAffinity = 983040 # HyperThreading enabled for 12700K
$EfficiencyAffinity = 268369920 # HyperThreading enabled for 14700K
[System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity

$LogEngineLifeCycleEvent = $false
$LogEngineHealthEvent = $false
$LogProviderLifeCycleEvent = $false
$LogProviderHealthEvent = $false
$LogCommandLifeCycleEvent = $false
$LogCommandHealthEvent = $false
Import-Module Microsoft.PowerShell.Archive
$m = Get-Module Microsoft.PowerShell.Archive
$m.LogPipelineExecutionDetails = $false
$m = Get-PSSnapin Microsoft.PowerShell.Core
$m.LogPipelineExecutionDetails = $false


$title = 'Show-Elapsed-Times'
$Host.UI.RawUI.WindowTitle = $title



# First, make sure this is the only instance running.

# Define the name of the mutex, to prevent other instances
$mutexName = "Global\$title"

# Initialize the variable that will be used to check if the mutex is new
$isNew = $false

# Create the Mutex
$mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$isNew)

if (-NOT $isNew) { 
	Write-Host -ForegroundColor Red "There is another instance of this script already running."
	Start-Sleep -Seconds 5
	Exit 
}


Write-Host "Starting in 5 minutes.  Open your programs and start working."
#Start-Sleep -Seconds 300



# Get the initial list of processes with their CPU times
$initialProcesses = Get-Process | Select-Object Id, Name, CPU | Sort-Object -Property CPU -Descending

# Display the initial list of processes
Write-Host "Initial list of processes:"
$initialProcesses | Format-Table -Property Name, CPU -AutoSize

# Prompt the user to press Enter to continue
Read-Host "Press Enter to continue..."

while ($true) {
	# Get the updated list of processes
	$updatedProcesses = Get-Process | Select-Object Id, Name, CPU

	# Calculate the CPU time differences and filter for positive differences
	$processesDiff = $updatedProcesses | ForEach-Object {
		$currentProcess = $_
		$initialProcess = $initialProcesses | Where-Object { $_.Id -eq $currentProcess.Id }
		if ($initialProcess) {
			$diff = $currentProcess.CPU - $initialProcess.CPU
			if ($diff -gt 0) {
				[PSCustomObject]@{
					Name = $currentProcess.Name
                    Id = $currentProcess.Id
					OldCPUTime = $initialProcess.CPU
					NewCPUTime = $currentProcess.CPU
					Difference = $diff
				}
			}
		} else {
				[PSCustomObject]@{
					Name = $currentProcess.Name
                    Id = $currentProcess.Id
					OldCPUTime = 0
					NewCPUTime = $currentProcess.CPU
					Difference = $currentProcess.CPU
				}			
		}
	}


# Display the filtered list of processes with positive CPU time differences
Write-Host "Processes with positive CPU time differences:"

# Sort the processes by the Difference column in descending order
$sortedProcessesDiff = $processesDiff | Sort-Object -Property Difference -Descending

# Display the sorted list in a table format
$sortedProcessesDiff | Format-Table -Property Name, Id, OldCPUTime, NewCPUTime, Difference -AutoSize


    Write-Host "PID=$PID"
	Read-Host "The list of processes is in the [processesDiff] list.  Press Ctrl+C to Exit..."
}