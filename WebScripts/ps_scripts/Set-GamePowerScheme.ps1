. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"

$search_paths = @("C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts", "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts")

$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Gaming-Programs.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-PowerScheme.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-IdealProcessor.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Cpu-Snapshots.ps1" -Directories $search_paths
. $include_file

function Restore-GameBoost {
    <#
    .SYNOPSIS
        Restores processes affected by a specific game's boost profile back to a default state intelligently.
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programNameWithExt,
        
        [Parameter(Mandatory)]
        [string]$boostJsonPath,
        
        [int]$threadsLimit = 50
    )
    
    # Normalize the incoming program name by removing the .exe extension for JSON lookup.
    $normalizedProgramName = $programNameWithExt -replace '\.exe$', ''
    
    $actions = Get-ActionsPerGame $boostJsonPath

    Write-VerboseDebug -Timestamp (Get-Date) -Title "RESTORE" -Message "Attempting to restore processes affected by '$normalizedProgramName' boost." -ForegroundColor "Yellow"

    # Find the specific action block for the program that just stopped using the NORMALIZED name.
    $gameAction = $actions | Where-Object { $_.process_name -eq $normalizedProgramName } | Select-Object -First 1

    if (-not $gameAction) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "RESTORE" -Message "No matching boost profile found for '$normalizedProgramName' in '$boostJsonPath'. Nothing to restore." -ForegroundColor "Gray"
        return
    }

    # The main game process is already stopped, so we only need to restore its dependencies that may still be running.
    if ($gameAction.parameters.dependencies) {
        foreach ($dependence in $gameAction.parameters.dependencies) {
            $dep_proc_names_array = Get-TrimmedProcessNames $dependence.process_name
            foreach ($dep_proc_name in $dep_proc_names_array) {
                $depProcesses = Get-Process -Name $dep_proc_name -ErrorAction SilentlyContinue
                foreach($depProcess in $depProcesses) {
                    Write-Host "RESTORING (Dependency): [$($depProcess.Name)]" -ForegroundColor Cyan
                    # Call the new intelligent restore function, passing the dependency's original parameters.
                    Restore-ProcessToDefaults -Process $depProcess -OriginalParameters $dependence -threadsLimit $threadsLimit
                }
            }
        }
    }
}

function Play-SeatBelt() {
        Add-Type -AssemblyName PresentationCore
        $player = New-Object System.Windows.Media.MediaPlayer
        $player.Open([uri] "N:\MyPrograms\MySounds\Thirdwire\fasten_seatbelt.wav")
        $player.Play()	
		Start-Sleep -Seconds 1
}


if (-not $Global:GameRuntimeByPid) {
    $Global:GameRuntimeByPid = @{}
}




function Start-GameRuntimeTracker {
    param (
        [Parameter(Mandatory)]
        [string]$ProgramName,

        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    if ($Global:GameRuntimeByPid.ContainsKey($ProcessId)) {
        Stop-GameRuntimeTracker -ProgramName $ProgramName -ProcessId $ProcessId | Out-Null
    }

    $processObject = $null
    $cpuStartSeconds = 0.0

    try {
        $processObject = [System.Diagnostics.Process]::GetProcessById($ProcessId)
        $cpuStartSeconds = $processObject.TotalProcessorTime.TotalSeconds
    }
    catch {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "PLAYTIME" -ForegroundColor "DarkYellow" -Message "Could not capture process object for $ProgramName [PID:$ProcessId]."
    }

    $startedAt = Get-Date

    Save-GameRuntimeCpuSnapshot -ProcessId $ProcessId -CpuSeconds $cpuStartSeconds -HourMark 0

    $timer = New-Object System.Timers.Timer
    $timer.Interval = 300000  # 5 minutes for finer-grained CPU sampling and refreshes
    $timer.AutoReset = $true

    $sourceIdentifier = "GameRuntimeHour_${ProcessId}_$([guid]::NewGuid().ToString('N'))"
    $ttsDisplayName = Get-GameTtsDisplayName -programName $ProgramName
    $messageData = @{
        ProcessId = $ProcessId
        ProgramName = $ProgramName
        TtsDisplayName = $ttsDisplayName
        StartedAt = $startedAt
    }

	$null = Register-ObjectEvent -InputObject $timer -EventName Elapsed -SourceIdentifier $sourceIdentifier -MessageData $messageData -Action {
		try {
			#. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1"
			#. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Set-GamePowerScheme.ps1"
			
			. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"

			$search_paths = @("C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts", "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts")

			$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
			. $include_file			
			
			$include_file = Include-Script -FileName "Cpu-Snapshots.ps1" -Directories $search_paths
			. $include_file			
			

			$eventPid = [int]$Event.MessageData.ProcessId
			$eventProgramName = $Event.MessageData.ProgramName
			$eventTtsDisplayName = $Event.MessageData.TtsDisplayName
			if ([string]::IsNullOrWhiteSpace($eventTtsDisplayName)) {
				$eventTtsDisplayName = $eventProgramName
			}
			$eventStartedAt = [datetime]$Event.MessageData.StartedAt

			$isRunning = Get-Process -Id $eventPid -ErrorAction SilentlyContinue
			if (-not $isRunning) {
				Unregister-Event -SourceIdentifier $EventSubscriber.SourceIdentifier -ErrorAction SilentlyContinue
				return
			}

			# Refresh the stored ProcessObject to keep its CPU time updated
			if ($Global:GameRuntimeByPid.ContainsKey($eventPid) -and $Global:GameRuntimeByPid[$eventPid].ProcessObject) {
				try {
					$Global:GameRuntimeByPid[$eventPid].ProcessObject.Refresh()
				}
				catch {
					# Silent fail if refresh temporarily errors (rare)
				}
			}

			$elapsed = (Get-Date) - $eventStartedAt
			$totalWholeHours = [Math]::Floor($elapsed.TotalHours)

			$cpuNowSeconds = 0.0
			try {
				$cpuNowSeconds = [double]$isRunning.CPU
			}
			catch {
				# Keep updates resilient even if CPU sampling temporarily fails.
			}

			$snapshotData = Read-GameRuntimeCpuSnapshot -ProcessId $eventPid
			$prevHourMark = if ($snapshotData) { [int]$snapshotData.HourMark } else { 0 }

			# Always save the snapshot on every tick for accuracy
			Save-GameRuntimeCpuSnapshot -ProcessId $eventPid -CpuSeconds $cpuNowSeconds -HourMark $totalWholeHours

			# Format the CPU seconds (User + Kernel) using your existing helper function
			$cpuFormatted = Format-GameplayDurationText -TotalSeconds $cpuNowSeconds
			# Always print current stats (showing CPU usage time)
			Write-VerboseDebug -Timestamp (Get-Date) -Title "CPU TIME" -ForegroundColor "Yellow" -Message "$eventProgramName [PID:$eventPid] - CPU Time: $cpuFormatted"
			
			# Only speak on new whole hours
			if ($totalWholeHours -gt $prevHourMark -and $totalWholeHours -ge 1) {
				$hoursText = if ($totalWholeHours -eq 1) { "1 hour" } else { "$totalWholeHours hours" }
				Write-VerboseDebug -Timestamp (Get-Date) -Title "SESSION TIME" -ForegroundColor "Yellow" -Speak $true -Message "$eventTtsDisplayName - $hoursText"
			}
		}
		catch {
			Write-Host "[PLAYTIME ERROR] $($_.Exception.Message)" -ForegroundColor Red
		}
	}

    $Global:GameRuntimeByPid[$ProcessId] = @{
        ProgramName = $ProgramName
        StartedAt = $startedAt
        ProcessObject = $processObject
        CpuStartSeconds = $cpuStartSeconds
        LastKnownCpuSeconds = $cpuStartSeconds
        LastKnownCpuCapturedHour = 0
        Timer = $timer
        TimerEventSourceIdentifier = $sourceIdentifier
    }

    $timer.Start()
}

function Stop-GameRuntimeTracker {
    param (
        [Parameter(Mandatory)]
        [string]$ProgramName,

        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    if (-not $Global:GameRuntimeByPid.ContainsKey($ProcessId)) {
        return $null
    }

    $runtimeInfo = $Global:GameRuntimeByPid[$ProcessId]

    # Stop timers
    if ($runtimeInfo.Timer) {
        $runtimeInfo.Timer.Stop()
    }
    if ($runtimeInfo.TimerEventSourceIdentifier) {
        Unregister-Event -SourceIdentifier $runtimeInfo.TimerEventSourceIdentifier -ErrorAction SilentlyContinue
    }
    if ($runtimeInfo.Timer) {
        $runtimeInfo.Timer.Dispose()
    }

    $stoppedAt = Get-Date
    $wallClockSeconds = ($stoppedAt - $runtimeInfo.StartedAt).TotalSeconds

    # --- FIX START ---
    # Initialize cpuTotalSeconds to -1 to distinguish between "0 seconds usage" and "Failed to read"
    $cpuTotalSeconds = -1.0
    $cpuReadUsedFallback = $false
    $cpuFallbackHourMark = 0

    # 1. Attempt to get the final live reading from the process object
    if ($runtimeInfo.ProcessObject) {
        try {
            # Attempt to refresh stats. On an exited process, this often zeroes out data or throws.
            $runtimeInfo.ProcessObject.Refresh()
            $procTime = $runtimeInfo.ProcessObject.TotalProcessorTime.TotalSeconds
            
            # Only accept positive values. If it returns 0, we treat it as a failure 
            # because the game likely ran for some time.
            if ($procTime -gt 0) {
                $cpuTotalSeconds = $procTime
            }
        }
        catch {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "PLAYTIME" -ForegroundColor "DarkYellow" -Message "Could not read OS CPU time for $ProgramName [PID:$ProcessId] (Process likely terminated)."
        }
        finally {
            $runtimeInfo.ProcessObject.Dispose()
        }
    }

    # 2. Retrieve the last saved snapshot (Hourly data)
    $snapshotData = Read-GameRuntimeCpuSnapshot -ProcessId $ProcessId
    $snapshotCpu = -1.0
    if ($snapshotData -and ($null -ne $snapshotData.CpuSeconds)) {
        $snapshotCpu = [double]$snapshotData.CpuSeconds
    }

    # 3. Validation Logic:
    # If the live read failed (-1) OR returned 0 (zombie handle), 
    # check if we have better data in the snapshot.
    # This specifically fixes the "0 seconds" bug when the process handle dies before reading.
    if ($cpuTotalSeconds -le 0 -and $snapshotCpu -gt 0) {
        $cpuTotalSeconds = $snapshotCpu
        $cpuReadUsedFallback = $true
        $cpuFallbackHourMark = [int]$snapshotData.HourMark
    }
    elseif ($cpuTotalSeconds -lt $snapshotCpu) {
        # Even if live read worked, if snapshot says we had MORE time previously (unlikely unless corruption), trust snapshot.
        $cpuTotalSeconds = $snapshotCpu
        $cpuReadUsedFallback = $true
        $cpuFallbackHourMark = [int]$snapshotData.HourMark
    }

    # 4. Last resort: In-memory fallback (usually just the start time, but included for completeness)
    if ($cpuTotalSeconds -le 0 -and ($null -ne $runtimeInfo.LastKnownCpuSeconds)) {
         $memCpu = [double]$runtimeInfo.LastKnownCpuSeconds
         if ($memCpu -gt $cpuTotalSeconds) {
            $cpuTotalSeconds = $memCpu
            $cpuFallbackHourMark = [int]$runtimeInfo.LastKnownCpuCapturedHour
         }
    }

    # If we still have nothing (-1) after all fallbacks, default to 0 to avoid errors in formatting
    if ($cpuTotalSeconds -lt 0) { $cpuTotalSeconds = 0 }
    # --- FIX END ---

    Remove-GameRuntimeCpuSnapshot -ProcessId $ProcessId
    $Global:GameRuntimeByPid.Remove($ProcessId)

    return @{
        ProgramName = $runtimeInfo.ProgramName
        ProcessId = $ProcessId
        StartedAt = $runtimeInfo.StartedAt
        StoppedAt = $stoppedAt
        WallClockSeconds = $wallClockSeconds
        CpuTotalSeconds = $cpuTotalSeconds
        CpuReadUsedFallback = $cpuReadUsedFallback
        CpuFallbackHourMark = $cpuFallbackHourMark
    }
}

function Set-GamePowerScheme($traceName, $programName, $processId) {
    try {
    $powerSchemes = $null

        # Check if there is a Speak action defined
        $speakText = Get-GameSpeakMessage -programName $programName

        # Resolve a TTS-friendly display name (nickname when configured)
        $ttsName = Get-GameTtsDisplayName -programName $programName

        # Retrieve any auxiliary programs configured for this title
        $auxPrograms = Get-GameAuxPrograms -programName $programName

        # First, do the power scheme
    switch ($traceName) {
        "Win32_ProcessStartTrace" {
            $powerSchemes = Get-StartPowerSchemes
            Start-GameRuntimeTracker -ProgramName $programName -ProcessId ([int]$processId)
			if ($speakText) {
				Write-VerboseDebug -Timestamp (Get-Date) -Title "PROCESS STARTED:" -ForegroundColor "Yellow" -Speak $true -Message $speakText
			}
        }
        "Win32_ProcessStopTrace" { 
            $powerSchemes = Get-StopPowerSchemes 
            $runtimeSummary = Stop-GameRuntimeTracker -ProgramName $programName -ProcessId ([int]$processId)
            if ($runtimeSummary) {
                $wallClockFormatted = Format-GameplayDurationText -TotalSeconds $runtimeSummary.WallClockSeconds
                $cpuFormatted = Format-GameplayDurationText -TotalSeconds $runtimeSummary.CpuTotalSeconds
                if ($runtimeSummary.CpuReadUsedFallback -and $runtimeSummary.CpuFallbackHourMark -ge 1) {
                    $cpuCompact = ($cpuFormatted -replace ' ', '')
                    $cpuDisplay = "CpuElapsedTime = $cpuCompact/$($runtimeSummary.CpuFallbackHourMark)h"
                    Write-VerboseDebug -Timestamp $runtimeSummary.StoppedAt -Title "CPU TIME" -ForegroundColor "Cyan" -Message "$programName [PID:$processId] stopped. $cpuDisplay; wall-clock delta: $wallClockFormatted"
                }
                else {
                    Write-VerboseDebug -Timestamp $runtimeSummary.StoppedAt -Title "CPU TIME" -ForegroundColor "Cyan" -Message "$programName [PID:$processId] stopped. TotalCpuTime = $cpuFormatted; wall-clock delta: $wallClockFormatted"
                }

                if ($runtimeSummary.CpuTotalSeconds -ge 2) {
                    $ttsTotal = Format-GameplayDurationText -TotalSeconds $runtimeSummary.CpuTotalSeconds
                    Write-VerboseDebug -Timestamp $runtimeSummary.StoppedAt -Title "CPU TIME" -ForegroundColor "Yellow" -Speak $true -Message "$ttsName stopped, $ttsTotal total"
                }
            }
			if ($speakText) {
				Write-VerboseDebug -Timestamp (Get-Date) -Title "PROCESS EXIT:" -ForegroundColor "Yellow" -Speak $true -Message $speakText
			}			
		}
	}
	
    # The power scheme logic remains correct because it uses the original name (with .exe)
    # which matches the keys in the hash table returned by Get-Start/StopPowerSchemes.
    if ($powerSchemes) {		
        $newPowerScheme = $powerSchemes[$programName]
        if ($newPowerScheme) {
            Set-PowerScheme -schemeName $newPowerScheme -delay 3
        } else {
            # Write-VerboseDebug -Timestamp (Get-Date) -Title "POWER" -ForegroundColor "Yellow" -Speak $false -Message "Power scheme not found for program '$programName'"
        }
    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -ForegroundColor "Red" -Speak $true -Message "Invalid trace name: '$traceName'"
    }	
            
 			
	# Next, do the boost if required by the game
	Start-Sleep -Seconds 5	
    switch ($traceName) {
        "Win32_ProcessStartTrace" {
            #$powerSchemes = Get-StartPowerSchemes

            # Launch auxiliaries linked to the game
            foreach ($aux in $auxPrograms) {
                if ([string]::IsNullOrWhiteSpace($aux)) {
                    continue
                }

                if (Test-Path $aux) {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "AUX START" -ForegroundColor "Green" -Message "Launching $aux"
                    Start-Process -FilePath $aux
                }
                else {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "AUX START" -ForegroundColor "DarkYellow" -Message "Aux program not found: $aux"
                }
            }

            # Look for a boost action using the full program name (with .exe).
            $boostJsonPath = Get-GameBoostActions -programName $programName
            if ($boostJsonPath -and (Test-Path $boostJsonPath)) {
                                Play-SeatBelt
                Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Applying boost for '$programName' from '$boostJsonPath' [PID:$processId]" -ForegroundColor "Green"
                # Call Run-Actions-Per-Game with the EXTENSION-LESS name for JSON compatibility.
                $normalizedProgramName = $programName -replace '\.exe$', ''
                Run-Actions-Per-Game -processName $normalizedProgramName -fileName $boostJsonPath -threadsLimit 50
            }
        }
        "Win32_ProcessStopTrace" { 
            #$powerSchemes = Get-StopPowerSchemes 
            
            # Look for a boost action using the full program name (with .exe).
            $boostJsonPath = Get-GameBoostActions -programName $programName
            if ($boostJsonPath -and (Test-Path $boostJsonPath)) {
				Play-SeatBelt
                Write-VerboseDebug -Timestamp (Get-Date) -Title "RESTORE" -Message "Restoring processes related to '$programName' using '$boostJsonPath' [PID:$processId]" -ForegroundColor "Cyan"
                # Call our corrected restore function.
                Restore-GameBoost -programNameWithExt $programName -boostJsonPath $boostJsonPath -threadsLimit 50
            }
        }
    }
	

	#if ($speakText) {
	#	Write-VerboseDebug -Timestamp (Get-Date) -Title "LAUNCH DETECTED" -ForegroundColor "Yellow" -Speak $true -Message $speakText
	#}

    }
    catch {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "PROCESS HANDLER ERROR" -ForegroundColor "Red" -Speak $false -Message "Set-GamePowerScheme failed for $programName [PID:$processId]: $($_.Exception.Message)"
    }
}
