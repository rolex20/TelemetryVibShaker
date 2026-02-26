function ScheduleWatchdogCheck {
    param([int]$delay = 10)
    New-Event -SourceIdentifier 'DoWatchDogCheck' -MessageData $delay | Out-Null
}

function Process-CommandFromJson {
    param (
        [Parameter(Mandatory=$true)][string]$JsonFilePath,
        [hashtable]$Config
    )

    $effectiveConfig = $Config
    if (-not $effectiveConfig) {
        $webScriptsRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
        $effectiveConfig = Get-TvsConfig -WebScriptsRoot $webScriptsRoot
    }

    $jsonContent = Get-Content -Path $JsonFilePath -Raw | ConvertFrom-Json
    $commandType = $jsonContent.command_type
    $parameters = $jsonContent.parameters
    $defaultPipeName = $effectiveConfig.names.ipcPipeName

    switch ($commandType) {
        'RUN' { ScheduleWatchdogCheck; Write-VerboseDebug -Timestamp (Get-Date) -Title 'RUN' -Message $parameters.program; try { Start-Process -FilePath $parameters.program -ErrorAction SilentlyContinue | Out-Null } catch { Write-VerboseDebug -Timestamp (Get-Date) -Title 'RUN-ERROR' -Message $_ -ForegroundColor Red } }
        'KILL' { ScheduleWatchdogCheck; Write-VerboseDebug -Timestamp (Get-Date) -Title 'KILL' -Message $parameters.processName; Stop-Process -Name $parameters.processName -ErrorAction SilentlyContinue }
        'MAXIMIZE' { ScheduleWatchdogCheck; Set-Maximize $parameters.processName $parameters.instance }
        'MINIMIZE' { ScheduleWatchdogCheck; Set-Minimize $parameters.processName $parameters.instance }
        'FOREGROUND' { ScheduleWatchdogCheck; Set-ForegroundProcess $parameters.processName $parameters.instance }
        'CHANGE_LOCATION' { ScheduleWatchdogCheck; Set-WindowPosition -processName $parameters.processName -instance $parameters.instance -x $parameters.x -y $parameters.y }
        'GET-LOCATION' { ScheduleWatchdogCheck; Get-WindowLocation -processName $parameters.processName -instance $parameters.instance -outfile $parameters.outfile }
        'PIPE' { ScheduleWatchdogCheck 60; $pipeName = if ($parameters.pipename) { $parameters.pipename } else { $defaultPipeName }; Send-MessageViaPipe -pipeName $pipeName -message $parameters.message }
        'READPOWERSCHEME' { ScheduleWatchdogCheck; $currentScheme = Get-ActivePowerPlanName; Write-VerboseDebug -Timestamp (Get-Date) -Title 'P-TRACE' -Message "The current power plan is: $currentScheme" -ForegroundColor White -Speak $true }
        'POWERSCHEME' { ScheduleWatchdogCheck; Set-PowerScheme -schemeName $parameters.schemeName }
        'WATCHDOG' {
            if (Send-MessageViaPipe -pipeName $defaultPipeName -message 'ECHO') {
                Set-Content -Path $parameters.outFile 'WATCHDOG'
                Write-VerboseDebug -Timestamp (Get-Date) -Title "WATCHDOG [PID=$PID]" -Message 'File System events are still active: OK.' -ForegroundColor Green
            }
        }
        'GAME_BOOST' { ScheduleWatchdogCheck 20; Run-Actions-Per-Game $parameters.processName $parameters.jsonFile $parameters.threadsLimit }
        'SHOW_THREADS' {
            ScheduleWatchdogCheck 20
            Show-Process-Thread-Times 'joystick_gremlin' $parameters.threadsLimit
            Show-Process-Thread-Times 'FlightSimulator' $parameters.threadsLimit
            Show-Process-Thread-Times 'dcs' $parameters.threadsLimit
            Show-Process-Thread-Times 'OVRServer_x64' $parameters.threadsLimit
        }
        'GAME_RESTORE' { ScheduleWatchdogCheck 20; Restore-Processes-To-Default $parameters.processNames }
        'EXIT_WATCHER' {
            $Global:Watcher_Continue = $false
            New-Event -SourceIdentifier 'StopWatcher' -MessageData 'EXIT_WATCHER' | Out-Null
            Write-VerboseDebug -Timestamp (Get-Date) -Title "EXIT_WATCHER [PID=$PID]" -Message 'Watcher stop requested. Signaled StopWatcher event to unblock wait loop.' -ForegroundColor Yellow
        }
        default { ScheduleWatchdogCheck; Write-VerboseDebug -Timestamp (Get-Date) -Title 'UNKNOWN COMMAND' -Message $commandType -ForegroundColor Red }
    }
}
