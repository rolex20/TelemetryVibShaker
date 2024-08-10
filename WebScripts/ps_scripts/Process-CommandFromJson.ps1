. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Include-Script.ps1"

<#
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Write-VerboseDebug.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Set-ForegroundProcess.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Set-Minimize.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Send-MessageViaPipe.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Set-WindowsPosition.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Get-WindowLocation.ps1"
#>


$include_file = Include-Script "Write-VerboseDebug.ps1" "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts" "C:\Users\ralch"
. $include_file

$include_file = Include-Script "Set-ForegroundProcess.ps1" "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts" "C:\Users\ralch"
. $include_file

$include_file = Include-Script "Set-Minimize.ps1" "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts" "C:\Users\ralch"
. $include_file

$include_file = Include-Script "Send-MessageViaPipe.ps1" "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts" "C:\Users\ralch"
. $include_file

$include_file = Include-Script "Set-WindowsPosition.ps1" "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts" "C:\Users\ralch"
. $include_file

$include_file = Include-Script "Get-WindowLocation.ps1" "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts" "C:\Users\ralch"
. $include_file

<#
. Include-Script 'Write-VerboseDebug.ps1' 'C:\Users\ralch'
. Include-Script 'Set-ForegroundProcess.ps1' 'C:\Users\ralch'
#>


function Process-CommandFromJson {
    param (
        [string]$JsonFilePath
    )

    # Read the JSON file
    $jsonContent = Get-Content -Path $JsonFilePath -Raw | ConvertFrom-Json
    Remove-Item -Path $JsonFilePath

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

        "WATCHDOG" {
            Set-Content -Path $parameters.outFile "WATCHDOG"
            Write-VerboseDebug -Timestamp (Get-Date) -Title "WATCHDOG" -Message "File System events are still active: OK" -ForegroundColor "Green"
        }

        default {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "UNKNOWN COMMAND" -Message $commandType -ForegroundColor "Red"        
        }
    }
}



# Examples

<#


cd C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts


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