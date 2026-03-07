#watchdog: Make sure the file system watcher is still working properly

function Get-NewFileName {
    param (
        [string]$FilePath,
        [string]$NewExtension
    )

    # Extract the directory, filename without extension, and current extension
    $directory = [System.IO.Path]::GetDirectoryName($FilePath)
    $filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)

    # Combine the directory, filename without extension, and new extension to form the new filename
    $newFileName = [System.IO.Path]::Combine($directory, "$filenameWithoutExtension.$NewExtension")

    return $newFileName
}




function Watchdog_Operationsold {

    # Guard: watchdog sanity check requires command_file (set only when remoteCommandsWatcher is enabled)
    $watchdogCheckEnabled = $false
    $watchdogWarnedMissingCommandFile = $false

    if ($globalcfg.features.watchdog) {
        if ([string]::IsNullOrWhiteSpace($command_file)) {
            # Avoid crash: without command_file we cannot do the rename-based watchdog test.
            Write-VerboseDebug -Timestamp (Get-Date) -Title "WATCHDOG" `
                -Message "Watchdog enabled but command_file is not set (remoteCommandsWatcher likely off). Skipping watchdog sanity check." `
                -ForegroundColor "Yellow" -Speak $true
            $watchdogCheckEnabled = $false
            $watchdogWarnedMissingCommandFile = $true
        }
        else {
            #$watchdog_json = Include-Script "watchdog.json" "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts" "C:\Users\ralch"
            $watchdog_json = "watchdog.json"
            $tmp_json = Get-NewFileName -FilePath $command_file -NewExtension "tmp"
            $watchdogCheckEnabled = $true
        }
    }

	$failure = $false
    $watchers_OK = $true
	

    # CHANGE HERE FOR DIFFERENT WAIT TIME
	$wait_minutes = 10 #Change this	to customize
	$wait_seconds = 60 * $wait_minutes


    $additional_sleep = 10
    do
    {
        if ($Global:Watcher_Continue -eq $false) {
            return $false # Intentionally return false so the orchestrator does not auto-restart the watcher.
        }

        # Poll config at the watchdog cadence (minutes), not via a tight loop.
        # This keeps overhead near-zero and piggybacks on an already-existing wait cycle.
        $configRefresh = Refresh-WebScriptsConfigIfChanged
        if ($configRefresh.RestartWillOccur) {
            # Reuse the normal StopWatcher signal so any pending waits/event checks unwind
            # through the same cleanup path used by manual EXIT_WATCHER.
            New-Event -SourceIdentifier "StopWatcher" -MessageData "CONFIG_RESTART" | Out-Null
            # Returning $true tells Start-CommandWatchers to relaunch the whole orchestrator.
            # Restarting centrally avoids partially-rewired watchers.
            return $true
        }

        # Wait-Event waits while staying responsive to events
        # Start-Sleep in contrast would NOT work and ignore incoming events
		
		$found = $false
        #Remove manual events if any
		Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue | ForEach-Object { $additional_sleep = $_.MessageData ; Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue ; $found = $true} | Out-Null
		if ($found) {
            if ($additional_sleep -EQ 0) { $additional_sleep = 5 } # if foreach failed
			Write-VerboseDebug -Timestamp (Get-Date) -Title "INFO" -Message "Starting a Watchdog sanity check in $additional_sleep seconds..." -ForegroundColor "DarkGray"
		}

        # Keep this delay interruptible so EXIT_WATCHER/StopWatcher can break out in <1s.
        $remainingSleep = [double]$additional_sleep
        while ($remainingSleep -gt 0) {
            if ($Global:Watcher_Continue -eq $false) {
                return $false # Intentionally return false so the orchestrator does not auto-restart the watcher.
            }

            $chunk = [Math]::Min(0.5, $remainingSleep)
            Wait-Event -Timeout $chunk | Out-Null

            $stopEvents = Get-Event -SourceIdentifier "StopWatcher" -ErrorAction SilentlyContinue
            if ($stopEvents) {
                $stopEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }  | Out-Null
                return $false # Intentionally return false so the orchestrator does not auto-restart the watcher.
            }

            # Consume any queued manual watchdog triggers without delaying responsiveness.
            $manualEvents = Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue
            if ($manualEvents) {
                $manualEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }  | Out-Null
            }

            $remainingSleep -= $chunk
        }

        if ($watchdogCheckEnabled) {    
                if (Test-Path "watchdog.txt") { Remove-Item "watchdog.txt" }
                if (Test-Path $command_file) { Remove-Item $command_file }
                Copy-Item $watchdog_json $tmp_json  | Out-Null
                Rename-Item $tmp_json $command_file  | Out-Null

                Start-Sleep -Milliseconds 200  | Out-Null
                if (Test-Path "watchdog.txt") {
                    # We are good
                } else {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "Events are not being processed" -ForegroundColor "Red"
                    $watchers_OK = $false
                    $failure = $true
                    break;
                }
        }

		Wait-Event -Timeout $wait_seconds  | Out-Null # Check every five minutes or when a manual event has been signaled        

        $stopEvents = Get-Event -SourceIdentifier "StopWatcher" -ErrorAction SilentlyContinue
        if ($stopEvents) {
            $stopEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }  | Out-Null
            return $false # Intentionally return false so the orchestrator does not auto-restart the watcher.
        }

        $additional_sleep = 0
    } while ($watchers_OK)
	
	return $failure
}

function Write-Watchdog2MissingCommandFileWarning {
    Write-VerboseDebug -Timestamp (Get-Date) -Title "WATCHDOG" `
        -Message "Watchdog enabled but command_file is not set (remoteCommandsWatcher likely off). Skipping watchdog sanity check." `
        -ForegroundColor "Yellow" -Speak $true
}

function New-Watchdog2State {
    $state = @{
        WatchdogCheckIsEnabled = $false
        WatchdogProbeTemplate = $null
        WatchdogProbeCommandFile = $null
        WatchdogProbeTempFile = $null
        InitialPreCheckDelaySeconds = 10
        LoopCadenceSeconds = 60 * 10
    }

    if (-not $globalcfg.features.watchdog) {
        return $state
    }

    if ([string]::IsNullOrWhiteSpace($command_file)) {
        Write-Watchdog2MissingCommandFileWarning
        return $state
    }

    $state.WatchdogCheckIsEnabled = $true
    $state.WatchdogProbeTemplate = "watchdog.json"
    $state.WatchdogProbeCommandFile = $command_file
    $state.WatchdogProbeTempFile = Get-NewFileName -FilePath $command_file -NewExtension "tmp"

    return $state
}

function Test-Watchdog2StopRequested {
    if ($Global:Watcher_Continue -eq $false) {
        return $true
    }

    $stopEvents = Get-Event -SourceIdentifier "StopWatcher" -ErrorAction SilentlyContinue
    if (-not $stopEvents) {
        return $false
    }

    $stopEvents | ForEach-Object {
        Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue
    } | Out-Null

    return $true
}

function Get-Watchdog2PreCheckDelay {
    param(
        [int]$CurrentDelaySeconds = 0
    )

    $nextDelaySeconds = $CurrentDelaySeconds
    $foundManualRequest = $false

    Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue | ForEach-Object {
        $nextDelaySeconds = $_.MessageData
        Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue
        $foundManualRequest = $true
    } | Out-Null

    if ($foundManualRequest -and $nextDelaySeconds -eq 0) {
        $nextDelaySeconds = 5
    }

    return @{
        Found = $foundManualRequest
        DelaySeconds = $nextDelaySeconds
    }
}

function Wait-Watchdog2InterruptibleDelay {
    param(
        [double]$DelaySeconds
    )

    $remainingDelay = $DelaySeconds
    while ($remainingDelay -gt 0) {
        $chunkSeconds = [Math]::Min(0.5, $remainingDelay)
        Wait-Event -Timeout $chunkSeconds | Out-Null

        if (Test-Watchdog2StopRequested) {
            return $false
        }

        # Once the watchdog is already on its way to a check, extra nudges do not need
        # to reschedule anything. We consume them here to keep the queue clean.
        Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue | ForEach-Object {
            Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue
        } | Out-Null

        $remainingDelay -= $chunkSeconds
    }

    return $true
}

function Invoke-Watchdog2Probe {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WatchdogProbeTemplate,

        [Parameter(Mandatory = $true)]
        [string]$WatchdogProbeTempFile,

        [Parameter(Mandatory = $true)]
        [string]$WatchdogProbeCommandFile
    )

    if (Test-Path "watchdog.txt") {
        Remove-Item "watchdog.txt"
    }

    if (Test-Path $WatchdogProbeCommandFile) {
        Remove-Item $WatchdogProbeCommandFile
    }

    Copy-Item $WatchdogProbeTemplate $WatchdogProbeTempFile | Out-Null
    Rename-Item $WatchdogProbeTempFile $WatchdogProbeCommandFile | Out-Null

    # The downstream watcher writes watchdog.txt only after the rename event is observed
    # and the JSON command is fully dispatched, so this file is the end-to-end handshake.
    Start-Sleep -Milliseconds 200 | Out-Null

    if (Test-Path "watchdog.txt") {
        return $true
    }

    Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "Events are not being processed" -ForegroundColor "Red"
    return $false
}

<#
.SYNOPSIS
Runs the watchdog heartbeat loop for the watcher orchestrator.

.DESCRIPTION
Watchdog_Operations2 is a refactored, narrative version of Watchdog_Operations.
It keeps the same operational role while separating the loop into clear phases:
configuration polling, interruptible waiting, and rename-based health probing.

The loop has three responsibilities.
First, it polls the shared host configuration at the watchdog cadence and asks the
orchestrator to restart when a config change requires a clean rewire of watchers.
Second, it waits in an event-responsive way so EXIT_WATCHER and StopWatcher can
break long sleeps immediately. Third, when the watchdog feature is enabled, it
performs a rename-based handshake that proves both file-system event handling and
downstream command processing are still alive.

.OUTPUTS
System.Boolean. Returns $true when the orchestrator should restart itself, returns
$false when the watcher should stop without restart, and returns $true on watchdog
probe failure to preserve the legacy failure contract of Watchdog_Operations.

.NOTES
Dependencies:
- $globalcfg.features.watchdog decides whether the probe is enabled.
- $command_file supplies the rename target for the watchdog command handoff.
- Refresh-WebScriptsConfigIfChanged is used for hot-reload polling.
- Write-VerboseDebug is the operator-visible logging path.
- DoWatchDogCheck nudges the loop into an earlier health check.
- StopWatcher unblocks waits and requests a clean stop path.
- $Global:Watcher_Continue is the shared cross-scope stop flag.

Behavioral contract:
- The old Watchdog_Operations function remains untouched.
- Event names and handshake files are intentionally preserved.
- Wait-Event is used instead of Start-Sleep so the loop remains responsive while idle.
#>
function Watchdog_Operations {
    $watchdogState = New-Watchdog2State
    $watchdogFailed = $false
    $watchersHealthy = $true
    $preCheckDelaySeconds = $watchdogState.InitialPreCheckDelaySeconds

    while ($watchersHealthy) {
        # Guard clauses keep the control flow honest: stop and restart requests win
        # immediately so the caller never has to wait for a long timeout to expire.
        if (Test-Watchdog2StopRequested) {
            return $false
        }

        $configRefresh = Refresh-WebScriptsConfigIfChanged
        if ($configRefresh.RestartWillOccur) {
            New-Event -SourceIdentifier "StopWatcher" -MessageData "CONFIG_RESTART" | Out-Null
            return $true
        }

        $manualCheckRequest = Get-Watchdog2PreCheckDelay -CurrentDelaySeconds $preCheckDelaySeconds
        if ($manualCheckRequest.Found) {
            $preCheckDelaySeconds = $manualCheckRequest.DelaySeconds
            Write-VerboseDebug -Timestamp (Get-Date) -Title "INFO" -Message "Starting a Watchdog sanity check in $preCheckDelaySeconds seconds..." -ForegroundColor "DarkGray"
        }

        # This short pre-check wait is deliberately interruptible. The loop must be able
        # to honor EXIT_WATCHER quickly even if the cadence between checks is long.
        if (-not (Wait-Watchdog2InterruptibleDelay -DelaySeconds $preCheckDelaySeconds)) {
            return $false
        }

        if ($watchdogState.WatchdogCheckIsEnabled) {
            $probeSucceeded = Invoke-Watchdog2Probe `
                -WatchdogProbeTemplate $watchdogState.WatchdogProbeTemplate `
                -WatchdogProbeTempFile $watchdogState.WatchdogProbeTempFile `
                -WatchdogProbeCommandFile $watchdogState.WatchdogProbeCommandFile

            if (-not $probeSucceeded) {
                $watchdogFailed = $true
                $watchersHealthy = $false
                break
            }
        }

        Wait-Event -Timeout $watchdogState.LoopCadenceSeconds | Out-Null
        if (Test-Watchdog2StopRequested) {
            return $false
        }

        $preCheckDelaySeconds = 0
    }

    return $watchdogFailed
}
