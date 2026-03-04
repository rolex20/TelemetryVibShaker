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




function Watchdog_Operations {

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
		Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue | ForEach-Object { $additional_sleep = $_.MessageData ; Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue ; $found = $true}
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
                $stopEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }
                return $false # Intentionally return false so the orchestrator does not auto-restart the watcher.
            }

            # Consume any queued manual watchdog triggers without delaying responsiveness.
            $manualEvents = Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue
            if ($manualEvents) {
                $manualEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }
            }

            $remainingSleep -= $chunk
        }

        if ($watchdogCheckEnabled) {    
                if (Test-Path "watchdog.txt") { Remove-Item "watchdog.txt" }
                if (Test-Path $command_file) { Remove-Item $command_file }
                Copy-Item $watchdog_json $tmp_json
                Rename-Item $tmp_json $command_file

                Start-Sleep -Milliseconds 200
                if (Test-Path "watchdog.txt") {
                    # We are good
                } else {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "Events are not being processed" -ForegroundColor "Red"
                    $watchers_OK = $false
                    $failure = $true
                    break;
                }
        }

		Wait-Event -Timeout $wait_seconds # Check every five minutes or when a manual event has been signaled        

        $stopEvents = Get-Event -SourceIdentifier "StopWatcher" -ErrorAction SilentlyContinue
        if ($stopEvents) {
            $stopEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }
            return $false # Intentionally return false so the orchestrator does not auto-restart the watcher.
        }

        $additional_sleep = 0
    } while ($watchers_OK)
	
	return $failure
}
