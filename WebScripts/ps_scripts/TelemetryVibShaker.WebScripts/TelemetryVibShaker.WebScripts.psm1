$root = Split-Path -Parent $PSScriptRoot
$scripts = @(
    'Write-VerboseDebug.ps1','Get-TvsConfig.ps1','Get-RenamesWatcher.ps1','Get-ProcessWatcher.ps1',
    'Check-Admin-Privileges.ps1','SetAffinityAndPriority.ps1','Tvs-PerfSafe.ps1',
    'Send-MessageViaPipe.ps1','Send-IPC-ExitCommand.ps1','Set-ForegroundProcess.ps1','Set-Minimize.ps1','Set-Maximize.ps1',
    'Set-WindowsPosition.ps1','Get-WindowLocation.ps1','Set-PowerScheme.ps1','Set-IdealProcessor.ps1','Gaming-Programs.ps1',
    'Cpu-Snapshots.ps1','Set-GamePowerScheme.ps1','Watchdog-Operations.ps1','WT_MissionType1.ps1',
    'Monitor-War-Thunder-Distance-Multiplier.ps1','Process-CommandFromJson.ps1','Start-IpcPipeServer.ps1','Declare-IPC-Server-Action.ps1',
    'Show-CPU-Time-PerProcess.ps1'
)
foreach ($s in $scripts) { . (Join-Path $root $s) }
Export-ModuleMember -Function *
