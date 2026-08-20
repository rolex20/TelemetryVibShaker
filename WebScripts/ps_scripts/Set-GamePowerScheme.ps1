$scriptDir = $PSScriptRoot
. (Join-Path $scriptDir 'Write-VerboseDebug.ps1')
. (Join-Path $scriptDir 'Gaming-Programs.ps1')
. (Join-Path $scriptDir 'Aux-Programs.ps1')
. (Join-Path $scriptDir 'Set-PowerScheme.ps1')
. (Join-Path $scriptDir 'Set-IdealProcessor.ps1')
. (Join-Path $scriptDir 'Cpu-Snapshots.ps1')

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
            # Optional dependency-level escape hatch:
            # Some helper processes (for example steamwebhelper) are intentionally left in their
            # boosted state even after the game exits. We check this flag BEFORE expanding process
            # names or querying running processes so we avoid unnecessary work and noise.
            #
            # Important: only a JSON boolean true should skip restore.
            # - Missing key -> default false (legacy restore behavior)
            # - null/strings/numbers -> treated as false for safety and predictability
            # This keeps schema evolution resilient without letting malformed values silently alter behavior.
            $rawDontRestoreBoost = $null
            if ($dependence -is [System.Collections.IDictionary]) {
                if ($dependence.Contains('dont_restore_boost')) {
                    $rawDontRestoreBoost = $dependence['dont_restore_boost']
                }
            }
            elseif ($dependence.PSObject -and ($dependence.PSObject.Properties.Name -contains 'dont_restore_boost')) {
                $rawDontRestoreBoost = $dependence.dont_restore_boost
            }

            $skipDependencyRestore = (($rawDontRestoreBoost -is [bool]) -and $rawDontRestoreBoost)
            if ($skipDependencyRestore) {
                # Non-fatal by design: one dependency opt-out should not impact other dependencies.
                # We log explicitly so operators can confirm why a dependency was not restored.
                Write-VerboseDebug -Timestamp (Get-Date) -Title "RESTORE" -Message "Skipping restore for dependency '$($dependence.process_name)' because dont_restore_boost=true in '$boostJsonPath'." -ForegroundColor "DarkYellow"
                continue
            }

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

function Get-SeatBeltWavPath {
    if (-not $Global:WebScriptsConfig -or
        -not ($Global:WebScriptsConfig -is [System.Collections.IDictionary]) -or
        -not $Global:WebScriptsConfig.ContainsKey('paths') -or
        -not ($Global:WebScriptsConfig.paths -is [System.Collections.IDictionary]) -or
        -not $Global:WebScriptsConfig.paths.ContainsKey('seatbeltWav')) {
        return $null
    }

    $seatBeltWavPath = [string]$Global:WebScriptsConfig.paths.seatbeltWav
    if ([string]::IsNullOrWhiteSpace($seatBeltWavPath)) {
        return $null
    }

    return $seatBeltWavPath
}

function Play-SeatBelt {
    param(
        [string]$SeatBeltWavPath = (Get-SeatBeltWavPath)
    )

    if ([string]::IsNullOrWhiteSpace($SeatBeltWavPath)) {
        return
    }

    if (-not (Test-Path -LiteralPath $SeatBeltWavPath)) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Seatbelt sound configured but not found: '$SeatBeltWavPath'" -ForegroundColor "DarkYellow" -Speak $false
        return
    }

    try {
        Add-Type -AssemblyName PresentationCore
        $player = New-Object System.Windows.Media.MediaPlayer
        $resolvedPath = (Resolve-Path -LiteralPath $SeatBeltWavPath).ProviderPath
        $player.Open([uri]$resolvedPath)
        $player.Play()
        Start-Sleep -Seconds 1
    }
    catch {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Failed to play seatbelt sound '$SeatBeltWavPath': $($_.Exception.Message)" -ForegroundColor "DarkYellow" -Speak $false
    }
}

function Wait-UntilDueTime {
    param(
        [Parameter(Mandatory)]
        [datetime]$DueAt
    )

    # This helper is intentionally "absolute-time based" instead of "sleep N seconds".
    # Why: when a flow has multiple milestones (aux + boost), some work may already have
    # consumed time before the next wait. Sleeping a fixed duration again can drift or
    # overshoot. Sleeping only the remaining delta keeps the timeline predictable.
    $remainingMs = [int][Math]::Ceiling(($DueAt - (Get-Date)).TotalMilliseconds)
    if ($remainingMs -gt 0) {
        Start-Sleep -Milliseconds $remainingMs
    }
}

if (-not $Global:GameRuntimeByPid) {
    # Runtime state is keyed by PID (not process name) to handle multiple instances safely.
    # This prevents one process exit from accidentally tearing down another instance's tracker.
    $Global:GameRuntimeByPid = @{}
}

if (-not $Global:DelayedGameBoostByPid) {
    # Delayed boost state is keyed by PID so a long GRW-style delay can be canceled
    # if that exact game process exits before its boost is due.
    $Global:DelayedGameBoostByPid = @{}
}

function Test-GameProcessStillMatches {
    param(
        [Parameter(Mandatory)]
        [string]$ProgramName,

        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) {
        return $false
    }

    $expectedProcessName = $ProgramName -replace '\.exe$', ''
    return ($process.ProcessName -ieq $expectedProcessName)
}

function Invoke-GameBoostAction {
    param(
        [Parameter(Mandatory)]
        [string]$ProgramName,

        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$BoostJsonPath,

        [switch]$VerifyProcess
    )

    if ($VerifyProcess -and -not (Test-GameProcessStillMatches -ProgramName $ProgramName -ProcessId $ProcessId)) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Skipping delayed boost for '$ProgramName' because PID:$ProcessId is no longer the expected process." -ForegroundColor "DarkYellow"
        return
    }

    if (-not (Test-Path $BoostJsonPath)) {
        return
    }

    Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Applying boost for '$ProgramName' from '$BoostJsonPath' [PID:$ProcessId]" -ForegroundColor "Green"
    # Call Run-Actions-Per-Game with the EXTENSION-LESS name for JSON compatibility.
    $normalizedProgramName = $ProgramName -replace '\.exe$', ''
    Run-Actions-Per-Game -processName $normalizedProgramName -fileName $BoostJsonPath -threadsLimit 50
    Play-SeatBelt
}

function Start-DelayedGameBoost {
    param(
        [Parameter(Mandatory)]
        [string]$ProgramName,

        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$BoostJsonPath,

        [Parameter(Mandatory)]
        [datetime]$DueAt
    )

    $key = [string]$ProcessId
    if ($Global:DelayedGameBoostByPid.ContainsKey($key)) {
        Stop-DelayedGameBoost -ProcessId $ProcessId -RemoveState | Out-Null
    }

    $processStartTimeUtcText = $null
    try {
        $process = Get-Process -Id $ProcessId -ErrorAction Stop
        $processStartTimeUtcText = $process.StartTime.ToUniversalTime().ToString('o')
    }
    catch {
        # Some system/transient processes may deny StartTime. Name/PID verification still applies.
    }

    $timer = New-Object System.Timers.Timer
    $remainingMs = [int][Math]::Ceiling(($DueAt - (Get-Date)).TotalMilliseconds)
    $timer.Interval = [Math]::Max(1, $remainingMs)
    $timer.AutoReset = $false
    $sourceIdentifier = "DelayedGameBoost_${ProcessId}_$([guid]::NewGuid().ToString('N'))"

    $Global:DelayedGameBoostByPid[$key] = @{
        ProgramName                 = $ProgramName
        ProcessId                   = $ProcessId
        ProcessStartTimeUtcText     = $processStartTimeUtcText
        BoostJsonPath               = $BoostJsonPath
        DueAt                       = $DueAt
        Timer                       = $timer
        TimerEventSourceIdentifier  = $sourceIdentifier
        BoostAttempted              = $false
        BoostApplied                = $false
        Canceled                    = $false
        Outcome                     = $null
        ErrorMessage                = $null
        CompletedAtText             = $null
    }

    $messageData = @{
        ProcessId = $ProcessId
        ProgramName = $ProgramName
        BoostJsonPath = $BoostJsonPath
        ScriptDir = $PSScriptRoot
        SeatBeltWavPath = Get-SeatBeltWavPath
    }

    $null = Register-ObjectEvent -InputObject $timer -EventName Elapsed -SourceIdentifier $sourceIdentifier -MessageData $messageData -Action {
        $state = $null
        try {
            [System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = [System.Diagnostics.ProcessPriorityClass]::Idle

            # Timer event actions run outside the original function scope. Re-import
            # the same dependencies used by the normal inline boost path.
            $scriptDir = $Event.MessageData.ScriptDir
            . (Join-Path $scriptDir 'Write-VerboseDebug.ps1')
            . (Join-Path $scriptDir 'Set-IdealProcessor.ps1')

            function Play-SeatBelt {
                param(
                    [string]$SeatBeltWavPath
                )

                if ([string]::IsNullOrWhiteSpace($SeatBeltWavPath)) {
                    return
                }

                if (-not (Test-Path -LiteralPath $SeatBeltWavPath)) {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Seatbelt sound configured but not found: '$SeatBeltWavPath'" -ForegroundColor "DarkYellow" -Speak $false
                    return
                }

                try {
                Add-Type -AssemblyName PresentationCore
                $player = New-Object System.Windows.Media.MediaPlayer
                    $resolvedPath = (Resolve-Path -LiteralPath $SeatBeltWavPath).ProviderPath
                    $player.Open([uri]$resolvedPath)
                $player.Play()
                Start-Sleep -Seconds 1
                }
                catch {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Failed to play seatbelt sound '$SeatBeltWavPath': $($_.Exception.Message)" -ForegroundColor "DarkYellow" -Speak $false
                }
            }

            $eventPid = [int]$Event.MessageData.ProcessId
            $key = [string]$eventPid
            $eventProgramName = [string]$Event.MessageData.ProgramName
            $eventBoostJsonPath = [string]$Event.MessageData.BoostJsonPath
            $eventSeatBeltWavPath = [string]$Event.MessageData.SeatBeltWavPath

            if (-not $Global:DelayedGameBoostByPid.ContainsKey($key)) {
                return
            }

            $state = $Global:DelayedGameBoostByPid[$key]
            if ($state['Canceled']) {
                $state['Outcome'] = 'Canceled'
                return
            }

            $process = Get-Process -Id $eventPid -ErrorAction SilentlyContinue
            $expectedProcessName = $eventProgramName -replace '\.exe$', ''
            if (-not $process -or $process.ProcessName -ine $expectedProcessName) {
                $state['Outcome'] = 'SkippedProcessMissing'
                Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Skipping delayed boost for '$eventProgramName' because PID:$eventPid is no longer the expected process." -ForegroundColor "DarkYellow"
                return
            }

            if (-not [string]::IsNullOrWhiteSpace([string]$state['ProcessStartTimeUtcText'])) {
                try {
                    $currentStartTimeUtcText = $process.StartTime.ToUniversalTime().ToString('o')
                    if ($currentStartTimeUtcText -ne $state['ProcessStartTimeUtcText']) {
                        $state['Outcome'] = 'SkippedPidReuse'
                        Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Skipping delayed boost for '$eventProgramName' because PID:$eventPid start time changed." -ForegroundColor "DarkYellow"
                        return
                    }
                }
                catch {
                    # If the re-check is denied, fall back to the PID/name match above.
                }
            }

            if (-not (Test-Path $eventBoostJsonPath)) {
                $state['Outcome'] = 'MissingBoostFile'
                return
            }

            $state['BoostAttempted'] = $true
            Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Applying delayed boost for '$eventProgramName' from '$eventBoostJsonPath' [PID:$eventPid]" -ForegroundColor "Green"
            $normalizedProgramName = $eventProgramName -replace '\.exe$', ''
            Run-Actions-Per-Game -processName $normalizedProgramName -fileName $eventBoostJsonPath -threadsLimit 50
            $state['BoostApplied'] = $true
            $state['Outcome'] = 'Applied'
            Play-SeatBelt -SeatBeltWavPath $eventSeatBeltWavPath
        }
        catch {
            if ($state) {
                $state['ErrorMessage'] = $_.Exception.Message
                if (-not $state['Outcome']) {
                    $state['Outcome'] = 'Failed'
                }
            }

            Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST ERROR" -Message "Delayed boost failed: $($_.Exception.Message)" -ForegroundColor "Red"
        }
        finally {
            if ($state) {
                $state['CompletedAtText'] = (Get-Date).ToString('o')
                if ($state['Timer']) {
                    $state['Timer'].Stop()
                    $state['Timer'].Dispose()
                    $state['Timer'] = $null
                }
            }

            Unregister-Event -SourceIdentifier $EventSubscriber.SourceIdentifier -ErrorAction SilentlyContinue
        }
    }

    $timer.Start()

    Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Scheduled delayed boost for '$ProgramName' at $($DueAt.ToString('yyyy-MM-dd HH:mm:ss')) [PID:$ProcessId]" -ForegroundColor "Green"
}

function Stop-DelayedGameBoost {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [switch]$RemoveState
    )

    $key = [string]$ProcessId
    if (-not $Global:DelayedGameBoostByPid.ContainsKey($key)) {
        return $null
    }

    $state = $Global:DelayedGameBoostByPid[$key]
    $state['Canceled'] = $true
    if (-not $state['Outcome']) {
        $state['Outcome'] = 'Canceled'
    }

    if ($state.Timer) {
        $state.Timer.Stop()
    }
    if ($state.TimerEventSourceIdentifier) {
        Unregister-Event -SourceIdentifier $state.TimerEventSourceIdentifier -ErrorAction SilentlyContinue
    }
    if ($state.Timer) {
        $state.Timer.Dispose()
        $state.Timer = $null
    }

    if ($RemoveState) {
        $Global:DelayedGameBoostByPid.Remove($key)
    }

    return $state
}

function Complete-DelayedGameBoostForStop {
    param(
        [Parameter(Mandatory)]
        [string]$ProgramName,

        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    $key = [string]$ProcessId
    if (-not $Global:DelayedGameBoostByPid.ContainsKey($key)) {
        return @{
            HasDelayedState = $false
            ShouldRestore   = $true
        }
    }

    $state = $Global:DelayedGameBoostByPid[$key]
    if (-not $state['BoostAttempted']) {
        $outcome = [string]$state['Outcome']
        $timerStillPending = ($state['Timer'] -and $state['Timer'].Enabled)
        Stop-DelayedGameBoost -ProcessId $ProcessId -RemoveState | Out-Null
        if ($timerStillPending) {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Canceled delayed boost for '$ProgramName' because PID:$ProcessId exited before the boost delay elapsed." -ForegroundColor "DarkYellow"
        }
        elseif (-not [string]::IsNullOrWhiteSpace($outcome)) {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Skipping boost restore for '$ProgramName' because delayed boost was not attempted. Outcome: $outcome." -ForegroundColor "DarkYellow"
        }
        else {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Skipping boost restore for '$ProgramName' because delayed boost was not attempted." -ForegroundColor "DarkYellow"
        }

        return @{
            HasDelayedState = $true
            ShouldRestore   = $false
        }
    }

    # Boost was attempted, so stop-time restore should run even if the delayed
    # action later reported failure after partially changing process settings.
    Stop-DelayedGameBoost -ProcessId $ProcessId | Out-Null
    $Global:DelayedGameBoostByPid.Remove($key)

    return @{
        HasDelayedState = $true
        ShouldRestore   = $true
    }
}




function Start-GameRuntimeTracker {
    param (
        [Parameter(Mandatory)]
        [string]$ProgramName,

        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    # Defensive cleanup: if we get duplicate start events for same PID, reset prior tracker first.
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

    # Persist an initial snapshot immediately so stop-time logic has at least one fallback source
    # even if the process exits before the first timer tick.
    Save-GameRuntimeCpuSnapshot -ProcessId $ProcessId -CpuSeconds $cpuStartSeconds -HourMark 0

    $timer = New-Object System.Timers.Timer
    # 30-minute cadence keeps long-session visibility without high overhead.
    # CPU is still sampled at stop-time, so this is telemetry/fallback cadence, not precision timing.
    $timer.Interval = 1800000  # 30 minutes for CPU sampling and refreshes
    $timer.AutoReset = $true

    $sourceIdentifier = "GameRuntimeHour_${ProcessId}_$([guid]::NewGuid().ToString('N'))"
    $ttsDisplayName = Get-GameTtsDisplayName -programName $ProgramName
    $messageData = @{
        ProcessId = $ProcessId
        ProgramName = $ProgramName
        TtsDisplayName = $ttsDisplayName
        StartedAt = $startedAt
        ScriptDir = $PSScriptRoot
    }

	$null = Register-ObjectEvent -InputObject $timer -EventName Elapsed -SourceIdentifier $sourceIdentifier -MessageData $messageData -Action {
		try {
            [System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = [System.Diagnostics.ProcessPriorityClass]::Idle
                        
            # Timer callbacks run in an event runspace, which may not inherit all function definitions.
            # Re-importing dependencies here avoids "function not found" failures during long sessions.
            $scriptDir = $Event.MessageData.ScriptDir
            . (Join-Path $scriptDir 'Write-VerboseDebug.ps1')
            . (Join-Path $scriptDir 'Cpu-Snapshots.ps1')

			$eventPid = [int]$Event.MessageData.ProcessId
			$eventProgramName = $Event.MessageData.ProgramName
			$eventTtsDisplayName = $Event.MessageData.TtsDisplayName
			if ([string]::IsNullOrWhiteSpace($eventTtsDisplayName)) {
				$eventTtsDisplayName = $eventProgramName
			}
			$eventStartedAt = [datetime]$Event.MessageData.StartedAt

			$isRunning = Get-Process -Id $eventPid -ErrorAction SilentlyContinue
			if (-not $isRunning) {
                # If process is gone, unsubscribe immediately to avoid zombie timer events.
				Unregister-Event -SourceIdentifier $EventSubscriber.SourceIdentifier -ErrorAction SilentlyContinue
				return
			}

			# Refresh the stored ProcessObject to keep its CPU time updated.
            # Refresh can occasionally fail on transient process states; we intentionally continue.
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

			# Always persist latest sample so stop-time code can recover from dead process handles.
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

    # Stop/unregister first to prevent race where timer fires while we are dismantling state.
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

    # 1. Attempt live reading from process object first (best source when still valid).
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

    # 2. Pull persisted snapshot (survives process-handle edge cases).
    $snapshotData = Read-GameRuntimeCpuSnapshot -ProcessId $ProcessId
    $snapshotCpu = -1.0
    if ($snapshotData -and ($null -ne $snapshotData.CpuSeconds)) {
        $snapshotCpu = [double]$snapshotData.CpuSeconds
    }

    # 3. Validation logic:
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

    # 4. Last resort: in-memory fallback.
    # This is weaker than persisted snapshot but still better than returning garbage/null.
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

function Set-GamePowerScheme {
    param(
        [Parameter(Mandatory)]
        [string]$traceName,

        [Parameter(Mandatory)]
        [string]$programName,

        [Parameter(Mandatory)]
        [int]$processId,

        [hashtable]$AuxLifecycleState
    )

    try {
        $powerSchemes = $null
        $auxDefinitions = @()

        # Event subscriptions pass one shared state reference to start and stop callbacks.
        # Retain a standalone fallback for direct/manual calls to this function.
        if (-not $AuxLifecycleState) {
            if (-not $Global:AuxProgramLifecycleState) {
                $Global:AuxProgramLifecycleState = New-AuxProgramLifecycleState
            }
            $AuxLifecycleState = $Global:AuxProgramLifecycleState
        }

        # Check if there is a Speak action defined
        $speakText = Get-GameSpeakMessage -programName $programName

        # Resolve a TTS-friendly display name (nickname when configured)
        $nickName = Get-GameTtsDisplayName -programName $programName


    # First phase: core lifecycle actions (kill, power scheme, runtime tracking, stutter enrollment).
    # Boost/restore actions run later in a second phase after a short settle delay.
    switch ($traceName) {
        "Win32_ProcessStartTrace" {

            # ImmediateKill is evaluated before any side effects so killed processes do not
            # accidentally trigger aux launches, boost actions, or tracker state.
            if ((Get-ImmediateKill -programName $programName)) {
                

                try {
                    $pidToKill = [int]$processId
                    if ($pidToKill -ne $PID) {
                        Stop-Process -Id $pidToKill -Force -ErrorAction Stop
                    }
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "IMMEDIATE KILL" -ForegroundColor "Magenta" -Message "Killed $nickName per profile."                    
                }
                catch {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "IMMEDIATE KILL ERROR" -ForegroundColor "DarkYellow" -Message "Failed to kill $nickName [PID:$processId]: $($_.Exception.Message)"
                }

                return
            }

            # Capture and normalize the start-time definition once. Stop-time cleanup uses
            # the stored runtime snapshot, so a later config hot reload cannot redirect it.
            $auxPrograms = @(Get-GameAuxPrograms -programName $programName)
            $auxWindowStyle = Get-GameAuxProgramsWindowStyle -programName $programName
            $auxDefinitions = @(ConvertTo-AuxProgramDefinitions -AuxPrograms $auxPrograms -WindowStyle $auxWindowStyle)
            
            $powerSchemes = Get-StartPowerSchemes
            Start-GameRuntimeTracker -ProgramName $programName -ProcessId ([int]$processId)		
            
			if (Get-Stutter -programName $programName) {
				Write-VerboseDebug -Timestamp (Get-Date) -Title "STUTTER HUNTER STARTED:" -ForegroundColor "Yellow" -Message "$programName"
				# LEGACY FALLBACK: Uncomment to revert to one-process-per-game legacy behavior.
				# Start-Process powershell.exe -WindowStyle Minimized -ArgumentList @(
				# 	'-NoProfile',
				# 	'-ExecutionPolicy', 'Bypass',
				# 	'-File', '".\Stutter-Hunter.ps1"',
				# 	'-ProcessId', $processId,
				#     '-GameProcessName', $programName
				# )                
                # IPC mode centralizes stutter tracking state in one coordinator process.
				Start-Process powershell.exe -WindowStyle Minimized -ArgumentList @(
					'-NoLogo','-NoProfile','-ExecutionPolicy','Bypass',
					'-File', (Join-Path $PSScriptRoot 'Stutter-Hunter-IPC.ps1'),
					'-Mode','Client',
					'-Action','Add',
					'-ProcessId', $processId,
					'-GameProcessName', $programName
				)
			}

			if ($speakText) {
				Write-VerboseDebug -Timestamp (Get-Date) -Title "PROCESS STARTED:" -ForegroundColor "Yellow" -Speak $true -Message $speakText
			}

        }
        "Win32_ProcessStopTrace" { 
            
            $powerSchemes = Get-StopPowerSchemes 
            # Registry cleanup is keyed only by the game PID and uses the definition captured
            # at start time. It intentionally does not consult hot-reloaded AuxPrograms values.
            Stop-ConfiguredAuxPrograms -LifecycleState $AuxLifecycleState -ParentProcessId ([int]$processId)
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
                    Write-VerboseDebug -Timestamp $runtimeSummary.StoppedAt -Title "CPU TIME" -ForegroundColor "Yellow" -Speak $true -Message "$nickName stopped, $ttsTotal total"
                }
            }

			if (Get-Stutter -programName $programName) {
				Start-Process powershell.exe -WindowStyle Minimized -ArgumentList @(
					'-NoLogo','-NoProfile','-ExecutionPolicy','Bypass',
					'-File', (Join-Path $PSScriptRoot 'Stutter-Hunter-IPC.ps1'),
					'-Mode','Client',
					'-Action','Remove',
					'-ProcessId', $processId
				)
			}
            
			if ($speakText) {
				Write-VerboseDebug -Timestamp (Get-Date) -Title "PROCESS EXIT:" -ForegroundColor "Yellow" -Speak $true -Message $speakText
			}

		}
	}
	
    # Power schemes are keyed by full executable name.
    # Keep this lookup separate from boost JSON normalization (which strips .exe).
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
            
 			
	# Second phase: boost/restore + aux launches.
    # Boost defaults to the historical fixed 5-second settle delay, but profiles
    # can opt into a longer asynchronous delay without blocking the watcher.
    # Aux launch delay is per-profile and can be earlier/later than boost.
    # We anchor both schedules to the SAME timestamp to avoid cumulative drift:
    # - boostDueAt = start + configured boost delay
    # - auxDueAt   = start + configuredDelay
    # This guarantees boost timing stays stable even when aux is delayed longer.
    $secondPhaseStartedAt = Get-Date
    $inlineBoostDelayLimitSeconds = 5
    $restoreDueAt = $secondPhaseStartedAt.AddSeconds(5)
    switch ($traceName) {
        "Win32_ProcessStartTrace" {
            $auxDelaySeconds = Get-GameAuxProgramsDelaySeconds -programName $programName
            $boostDelaySeconds = Get-GameBoostActionDelaySeconds -programName $programName
            $auxDueAt = $secondPhaseStartedAt.AddSeconds($auxDelaySeconds)
            $boostDueAt = $secondPhaseStartedAt.AddSeconds($boostDelaySeconds)
            # Precompute whether we actually have any usable aux entries.
            # Important pitfall: if no aux entries exist but delay is large, we should NOT
            # wait that delay. We only keep the fixed boost timing in that case.
            $hasAuxPrograms = ($auxDefinitions.Count -gt 0)
            $boostJsonPath = Get-GameBoostActions -programName $programName
            $hasBoostAction = ($boostJsonPath -and (Test-Path $boostJsonPath))
            if (-not $hasBoostAction) {
                # A configured boost delay only matters when there is a valid boost action.
                # Otherwise a typo/missing file could stall the watcher for a long no-op.
                $boostDueAt = $secondPhaseStartedAt.AddSeconds($inlineBoostDelayLimitSeconds)
            }

            $runBoost = {
                if ($hasBoostAction) {
                    Invoke-GameBoostAction -ProgramName $programName -ProcessId ([int]$processId) -BoostJsonPath $boostJsonPath
                }
            }

            if ($hasBoostAction -and $boostDelaySeconds -gt $inlineBoostDelayLimitSeconds) {
                Start-DelayedGameBoost -ProgramName $programName -ProcessId ([int]$processId) -BoostJsonPath $boostJsonPath -DueAt $boostDueAt

                if ($hasAuxPrograms) {
                    Wait-UntilDueTime -DueAt $auxDueAt
                    Start-ConfiguredAuxPrograms `
                        -Definitions $auxDefinitions `
                        -LifecycleState $AuxLifecycleState `
                        -ParentProgramName $programName `
                        -ParentProcessId ([int]$processId)
                }

                break
            }

            if (-not $hasAuxPrograms) {
                # No aux work to schedule. Preserve legacy boost timing only.
                Wait-UntilDueTime -DueAt $boostDueAt
                & $runBoost
            }
            elseif ($auxDueAt -le $boostDueAt) {
                # Aux should happen first (or same time as boost).
                # Ownership discovery may remain active across the fixed boost deadline.
                # A shared action state lets the discovery loop invoke boost when due,
                # preserving the existing +5s schedule without running it twice.
                $boostActionState = @{
                    Action  = $runBoost
                    DueAt   = $boostDueAt
                    Invoked = $false
                    Error   = $null
                }

                Wait-UntilDueTime -DueAt $auxDueAt
                Start-ConfiguredAuxPrograms `
                    -Definitions $auxDefinitions `
                    -LifecycleState $AuxLifecycleState `
                    -ParentProgramName $programName `
                    -ParentProcessId ([int]$processId) `
                    -ScheduledActionState $boostActionState

                Wait-UntilDueTime -DueAt $boostDueAt
                Invoke-AuxScheduledActionIfDue -ScheduledActionState $boostActionState
                if ($boostActionState.Error) {
                    throw $boostActionState.Error
                }
            }
            else {
                # Boost is due before aux. Run boost at fixed +5s, then wait remaining
                # time until aux is due.
                Wait-UntilDueTime -DueAt $boostDueAt
                & $runBoost

                Wait-UntilDueTime -DueAt $auxDueAt
                Start-ConfiguredAuxPrograms `
                    -Definitions $auxDefinitions `
                    -LifecycleState $AuxLifecycleState `
                    -ParentProgramName $programName `
                    -ParentProcessId ([int]$processId)
            }
        }
        "Win32_ProcessStopTrace" { 
            #$powerSchemes = Get-StopPowerSchemes 
            $delayedBoostStop = Complete-DelayedGameBoostForStop -ProgramName $programName -ProcessId ([int]$processId)
            if (-not $delayedBoostStop.ShouldRestore) {
                break
            }

            # Stop flow intentionally keeps the original fixed settle delay before restore.
            # This avoids behavior changes on stop events while adding start-side aux delay.
            Wait-UntilDueTime -DueAt $restoreDueAt

            # Look for a boost action using the full program name (with .exe).
            $boostJsonPath = Get-GameBoostActions -programName $programName
            if ($boostJsonPath -and (Test-Path $boostJsonPath)) {
                Write-VerboseDebug -Timestamp (Get-Date) -Title "RESTORE" -Message "Restoring processes related to '$programName' using '$boostJsonPath' [PID:$processId]" -ForegroundColor "Cyan"
                # Call our corrected restore function.
                Restore-GameBoost -programNameWithExt $programName -boostJsonPath $boostJsonPath -threadsLimit 50
				Play-SeatBelt                
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
