@{
    RootModule = 'TelemetryVibShaker.WebScripts.psm1'
    ModuleVersion = '1.0.0'
    GUID = '4c59db56-58f4-4f95-b05b-3f57a75e6cce'
    Author = 'TelemetryVibShaker'
    PowerShellVersion = '5.1'
    FunctionsToExport = @(
        'Write-VerboseDebug','Get-TvsConfig','Get-GamingPrograms','Check-Admin-Privileges','SetAffinityAndPriority',
        'Get-RenamesWatcher','Get-ModificationWatcher','Get-ProcessWatchers','Create-ProcessWatcherQuery',
        'Process-CommandFromJson','ScheduleWatchdogCheck','Watchdog_Operations','Generate_WT_Mission_Type1',
        'Set-GamePowerScheme','Send-IPC-ExitCommand','Start-IpcPipeServer','Start-WarThunderDistanceMultiplierMonitor',
        'Set-ForegroundProcess','Set-Minimize','Set-Maximize','Set-WindowPosition','Get-WindowLocation',
        'Send-MessageViaPipe','Run-Actions-Per-Game','Restore-Processes-To-Default','Show-Process-Thread-Times'
    )
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
}
