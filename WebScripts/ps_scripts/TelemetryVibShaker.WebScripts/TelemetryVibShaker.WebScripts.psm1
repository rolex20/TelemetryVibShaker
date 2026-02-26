$moduleRoot = Split-Path -Parent $PSCommandPath
$psScriptsRoot = Split-Path -Parent $moduleRoot
$webScriptsRoot = Split-Path -Parent $psScriptsRoot

$script:TvsWebScriptsRoot = $webScriptsRoot

$libraryScripts = @(
    'Write-VerboseDebug.ps1',
    'Get-RenamesWatcher.ps1',
    'Watchdog-Operations.ps1',
    'SetAffinityAndPriority.ps1',
    'Get-ProcessWatcher.ps1',
    'Send-IPC-ExitCommand.ps1',
    'Check-Admin-Privileges.ps1',
    'Process-CommandFromJson.ps1',
    'Set-ForegroundProcess.ps1',
    'Set-Minimize.ps1',
    'Set-Maximize.ps1',
    'Send-MessageViaPipe.ps1',
    'Set-WindowsPosition.ps1',
    'Get-WindowLocation.ps1',
    'Set-PowerScheme.ps1',
    'Set-IdealProcessor.ps1',
    'Gaming-Programs.ps1',
    'Set-GamePowerScheme.ps1',
    'WT_MissionType1.ps1',
    'Cpu-Snapshots.ps1',
    'Get-TvsConfig.ps1',
    'Tvs-PerfSafe.ps1',
    'Start-IpcPipeServer.ps1'
)

foreach ($name in $libraryScripts) {
    . (Join-Path $psScriptsRoot $name)
}

Export-ModuleMember -Function *
