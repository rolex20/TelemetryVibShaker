. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"

$search_paths = @("C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts", "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts")

$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-ForegroundProcess.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-Minimize.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-Maximize.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Send-MessageViaPipe.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-WindowsPosition.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Get-WindowLocation.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-PowerScheme.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-IdealProcessor.ps1" -Directories $search_paths
. $include_file

function ScheduleWatchdogCheck($delay = 10) {
    # This is a "nudge" event, not a hard timer. We use it to wake the watchdog loop early
    # after interactive commands so health checks happen soon after a state-changing action.
    # Keeping this event-driven avoids tight polling.
Write-Host "DoWatchDogCheck"
	New-Event -SourceIdentifier "DoWatchDogCheck" -MessageData $delay # This will wake up Watchodg-Operations
}

function Process-CommandFromJson {
    param (
        [string]$JsonFilePath
    )

    # Read all at once so we parse a consistent snapshot of the command payload.
    # The producer writes via .tmp -> rename, so -Raw should typically see a complete JSON blob.
    $jsonContent = Get-Content -Path $JsonFilePath -Raw | ConvertFrom-Json
    #Remove-Item -Path $JsonFilePath

    # Extract command type and parameters
    $commandType = $jsonContent.command_type
    $parameters = $jsonContent.parameters

    # Dispatcher contract: each command should stay small and predictable.
    # Complex policy belongs in specialized scripts so this router stays maintainable.
    # Most commands schedule a watchdog check to validate event + IPC health soon after changes.
    switch ($commandType) {
        "RUN" {
            ScheduleWatchdogCheck
            Write-VerboseDebug -Timestamp (Get-Date) -Title "RUN" -Message $parameters.program
            try {
                $p = Start-Process -FilePath $parameters.program -ErrorAction SilentlyContinue
            } catch {
                Write-VerboseDebug -Timestamp (Get-Date) -Title "RUN-ERROR" -Message $_ -ForegroundColor "Red"
            }
			
        }

        "KILL" {
            ScheduleWatchdogCheck
            Write-VerboseDebug -Timestamp (Get-Date) -Title "KILL" -Message $parameters.processName
            $p=Stop-Process -Name $parameters.processName -ErrorAction SilentlyContinue
			
        }

        "MAXIMIZE" {
            ScheduleWatchdogCheck
            Set-Maximize $parameters.processName $parameters.instance			
        }


        "MINIMIZE" {
            ScheduleWatchdogCheck
            Set-Minimize $parameters.processName $parameters.instance
			
        }

        "FOREGROUND" {            
            ScheduleWatchdogCheck
            Set-ForegroundProcess $parameters.processName $parameters.instance
			
        }

        "CHANGE_LOCATION" {
            ScheduleWatchdogCheck
            Set-WindowPosition -processName $parameters.processName -instance $parameters.instance -x $parameters.x -y $parameters.y
			
        }

        "GET-LOCATION" {            
            ScheduleWatchdogCheck
            Get-WindowLocation -processName $parameters.processName -instance $parameters.instance -outfile $parameters.outfile
			
        }


        "PIPE" {
            # Pipe commands can trigger follow-up activity (UI/telemetry scripts), so give watchdog
            # a longer window before forcing a check to reduce false alarms while work is in-flight.
            ScheduleWatchdogCheck 60
            Send-MessageViaPipe -pipeName $parameters.pipename -message $parameters.message
			
        }


        "READPOWERSCHEME" {
            ScheduleWatchdogCheck
            $currentScheme =  Get-ActivePowerPlanName
            $msg = "The current power plan is: $currentScheme"           
            Write-VerboseDebug -Timestamp (Get-Date) -Title "P-TRACE" -Message $msg -ForegroundColor "White" -Speak $true			
        }


        "POWERSCHEME" {
            ScheduleWatchdogCheck
            Set-PowerScheme -schemeName $parameters.schemeName
			
        }


        "WATCHDOG" {
			#If this code is being executed is because the system is still responding well to file system events

			#Let's send a simple request to the ipc pipe server to see if it still responding ok to commands
            if (Send-MessageViaPipe -pipeName "ipc_pipe_vr_server_commands" -message "ECHO") {
                # watchdog.txt is the handshake artifact consumed by Watchdog-Operations.ps1.
                # Writing this file is the success signal that both file events and IPC are alive.
                Set-Content -Path $parameters.outFile "WATCHDOG" # This file is expected by Watchdog-Operations.ps1 
                Write-VerboseDebug -Timestamp (Get-Date) -Title "WATCHDOG [PID=$PID]" -Message "File System events are still active: OK." -ForegroundColor "Green"
                if ($parameters.sound) {
                    Add-Type -AssemblyName System.Speech
                    $synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer
                    $synthesizer.Speak("PS-Watcher is OK")
                }    
            } else {
                if ($parameters.sound) {
                    Add-Type -AssemblyName System.Speech
                    $synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer
                    $synthesizer.Speak("ERROR: IPC Thread is not working.")
                }    

            }

        }

        "GAME_BOOST" {
            ScheduleWatchdogCheck 20
            Run-Actions-Per-Game $parameters.processName $parameters.jsonFile $parameters.threadsLimit			
        }


        "SHOW_THREADS" {
            ScheduleWatchdogCheck 20
			Show-Process-Thread-Times "joystick_gremlin" $parameters.threadsLimit
            Show-Process-Thread-Times "FlightSimulator" $parameters.threadsLimit
            #Show-Process-Thread-Times "aces"  $parameters.threadsLimit
            Show-Process-Thread-Times "dcs"  $parameters.threadsLimit
            Show-Process-Thread-Times "OVRServer_x64"  $parameters.threadsLimit			
        }
		
		"GAME_RESTORE" {
			ScheduleWatchdogCheck 20
			Restore-Processes-To-Default $parameters.processNames
		}

        "EXIT_WATCHER" {
            # Two-part shutdown signal:
            # 1) global flag handles normal loop checks
            # 2) StopWatcher event unblocks Wait-Event immediately (fast exit, no long timeout wait)
            $Global:Watcher_Continue = $false
            New-Event -SourceIdentifier "StopWatcher" -MessageData "EXIT_WATCHER" | Out-Null
            Write-VerboseDebug -Timestamp (Get-Date) -Title "EXIT_WATCHER [PID=$PID]" -Message "Watcher stop requested. Signaled StopWatcher event to unblock wait loop." -ForegroundColor "Yellow"
        }		

        default {
            ScheduleWatchdogCheck 
            Write-VerboseDebug -Timestamp (Get-Date) -Title "UNKNOWN COMMAND" -Message $commandType -ForegroundColor "Red"
			
        }
    }
}



# Examples

<#


cd C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts


if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\watchdog.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"

if (Test-Path "command.out") { Remove-Item command.out }
if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\get-location.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"
Get-Content "command.out"


Start-Sleep 1

#Bring warthunderexporter to foreground
if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\warthunderexporter.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"

Start-Sleep 1


if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\change_location.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"

Start-Sleep 1



#change to monitor tab
if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\pipe.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"

Start-Sleep 1

#cycle statistics
if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\pipe2.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"

Start-Sleep 2


if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\foreground.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"

Start-Sleep 1


if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\run.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"

Start-Sleep 1

if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\minimize.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"


Start-Sleep 1

if (Test-Path "command.json") { Remove-Item command.json }
Copy-Item .\kill.json command.tmp ; Rename-Item command.tmp command.json
Process-CommandFromJson -JsonFilePath "command.json"

#>
