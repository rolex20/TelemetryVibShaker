$modulePath = Join-Path $PSScriptRoot 'ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psd1'
Import-Module $modulePath -Force

$config = Get-TvsConfig -WebScriptsRoot $PSScriptRoot
$command_file = $config.paths.commandJson
$mission_file = $config.paths.wtMissionJson
$wt_config_file = $config.paths.wtConfigBlk
$pipeName = $config.names.ipcPipeName
$title = $config.names.ipcMutexName

$runtimeDir = Join-Path $PSScriptRoot 'runtime'
if (-not (Test-Path $runtimeDir)) { New-Item -Path $runtimeDir -ItemType Directory | Out-Null }
if (-not (Test-Path $command_file)) { '{}' | Set-Content -Path $command_file -Encoding UTF8 }
if (-not (Test-Path $mission_file)) { '{}' | Set-Content -Path $mission_file -Encoding UTF8 }
if (-not (Test-Path $wt_config_file)) { '' | Set-Content -Path $wt_config_file -Encoding UTF8 }

$need_restart = $false
$Global:Watcher_Continue = $true
$job = $null
try {
    $Host.UI.RawUI.WindowTitle = $title
    Check-Admin-Privileges | Out-Null

    Try-SetCurrentProcessAffinity -Mask 983040 -ContextTitle 'STARTUP' | Out-Null
    Try-ApplyPerfTuning -ContextTitle 'STARTUP' | Out-Null

    $isNew = $false
    $mutex_owner = $false
    $mutex = New-Object System.Threading.Mutex($false, $title, [ref]$isNew)
    $r = $mutex.WaitOne(1000)
    if ($r) { $mutex_owner = $true } else { throw 'There is another instance running.' }

    $remote_commands = {
        $path = $Event.SourceEventArgs.FullPath
        $changeType = $Event.SourceEventArgs.ChangeType
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$changeType" -Message $path
        Process-CommandFromJson $path
    }
    $cm_watcher_objects = Get-RenamesWatcher $command_file $remote_commands

    $generate_mission1 = {
        $path = $Event.SourceEventArgs.FullPath
        $changeType = $Event.SourceEventArgs.ChangeType
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$changeType" -Message $path
        Generate_WT_Mission_Type1
    }
    $wt_watcher_objects = Get-RenamesWatcher $mission_file $generate_mission1

    $processAction = {
        $event_pId = $Event.SourceEventArgs.NewEvent.ProcessID.ToString()
        $pName = $Event.SourceEventArgs.NewEvent.ProcessName.ToString()
        $traceName = $Event.SourceEventArgs.NewEvent.ToString()
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title 'PROCESS' -Message "$traceName - $pName [$event_pId]"
        Set-GamePowerScheme -traceName $traceName -programName $pName -processId $event_pId
    }
    $processWatcher = Get-ProcessWatchers $processAction

    $ipcRunner = {
        param($ModPath, $PipeName)
        Import-Module $ModPath -Force
        Start-IpcPipeServer -PipeName $PipeName
    }
    if (Get-Command Start-ThreadJob -ErrorAction SilentlyContinue) {
        $job = Start-ThreadJob -ScriptBlock $ipcRunner -ArgumentList $modulePath, $pipeName -StreamingHost $Host
    } else {
        $job = Start-Job -ScriptBlock $ipcRunner -ArgumentList $modulePath, $pipeName
    }

    $need_restart = Watchdog_Operations
}
finally {
    Send-IPC-ExitCommand $pipeName | Out-Null
    if ($job) { Stop-Job -Job $job -ErrorAction SilentlyContinue; Remove-Job -Job $job -Force -ErrorAction SilentlyContinue }

    if ($cm_watcher_objects) {
        $cm_watcher_objects[1].EnableRaisingEvents = $false
        $cm_watcher_objects[1].Dispose()
        $cm_watcher_objects[0].Dispose()
    }
    if ($wt_watcher_objects) {
        $wt_watcher_objects[1].EnableRaisingEvents = $false
        $wt_watcher_objects[1].Dispose()
        $wt_watcher_objects[0].Dispose()
    }
    if ($processWatcher) {
        $processWatcher | ForEach-Object { $_.Dispose() }
    }
    if ($mutex_owner) { $mutex.ReleaseMutex() }
    if ($mutex) { $mutex.Dispose() }

    Get-Event | Remove-Event -ErrorAction SilentlyContinue
    Get-EventSubscriber | Unregister-Event -ErrorAction SilentlyContinue
}

if ($need_restart) {
    Start-Sleep -Seconds 5
    Start-Process -FilePath 'powershell.exe' -ArgumentList "-File `"$PSCommandPath`""
}
