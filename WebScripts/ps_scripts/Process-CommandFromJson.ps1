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
            Write-VerboseDebug -Timestamp (Get-Date) -Title "RUN" -Message $parameters.program
            try {
                $p = Start-Process -FilePath $parameters.program -ErrorAction SilentlyContinue
            } catch {
                Write-VerboseDebug -Timestamp (Get-Date) -Title "RUN-ERROR" -Message $_ -ForegroundColor "Red"
            }
        }

        "KILL" {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "KILL" -Message $parameters.processName
            $p=Stop-Process -Name $parameters.processName -ErrorAction SilentlyContinue
        }

        "MAXIMIZE" {
            Set-Maximize $parameters.processName $parameters.instance
        }


        "MINIMIZE" {
            Set-Minimize $parameters.processName $parameters.instance
        }

        "FOREGROUND" {            
            Set-ForegroundProcess $parameters.processName $parameters.instance
        }

        "CHANGE_LOCATION" {
            Set-WindowPosition -processName $parameters.processName -instance $parameters.instance -x $parameters.x -y $parameters.y
        }

        "GET-LOCATION" {            
            Get-WindowLocation -processName $parameters.processName -instance $parameters.instance -outfile $parameters.outfile
        }


        "PIPE" {
            Send-MessageViaPipe -pipeName $parameters.pipename -message $parameters.message
        }


        "READPOWERSCHEME" {
            $currentScheme =  Get-ActivePowerPlanName
            $msg = "The current power plan is: $currentScheme"           
            Write-VerboseDebug -Timestamp (Get-Date) -Title "P-TRACE" -Message $msg -ForegroundColor "White" -Speak $true
        }


        "POWERSCHEME" {
            Set-PowerScheme -schemeName $parameters.schemeName
        }


        "WATCHDOG" {
            if (Send-MessageViaPipe -pipeName "ipc_pipe_vr_server_commands" -message "ECHO") {
                Set-Content -Path $parameters.outFile "WATCHDOG"
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
            Run-ActionsPerGame $parameters.processName $parameters.jsonFile
        }


        "SHOW_THREADS" {
            Show-Process-Thread-Times "FlightSimulator"
            Show-Process-Thread-Times "aces"
            Show-Process-Thread-Times "dcs"
            Show-Process-Thread-Times "OVRServer_x64"            
        }

        default {
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