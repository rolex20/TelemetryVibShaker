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
Write-Host "DoWatchDogCheck"
	New-Event -SourceIdentifier "DoWatchDogCheck" -MessageData $delay # This will wake up Watchodg-Operations
}

function Process-CommandFromJson {
    param (
        [string]$JsonFilePath
    )

    # Read the JSON file
    $jsonContent = Get-Content -Path $JsonFilePath -Raw | ConvertFrom-Json
    #Remove-Item -Path $JsonFilePath

    # Extract command type and parameters
    $commandType = $jsonContent.command_type
    $parameters = $jsonContent.parameters

    # Process the command based on the command type
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

        "GAME" {
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