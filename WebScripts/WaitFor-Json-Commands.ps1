$modulePath = Join-Path $PSScriptRoot 'ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psd1'
Import-Module $modulePath -Force

$config = Get-TvsConfig
$command_file = $config.paths.commandJson
$mission1 = $config.paths.wtMissionJson
$wtConfigBlk = $config.paths.wtConfigBlk
$ipcPipeName = $config.names.ipcPipeName
$ipcMutexName = $config.names.ipcMutexName

$need_restart = $false
$Global:Watcher_Continue = $true

try {
    $title = 'Watcher for JSON Gaming Commands'
    $Host.UI.RawUI.WindowTitle = $title

    Check-Admin-Privileges | Out-Null

    $EfficiencyAffinity = 983040
    try { [System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity = $EfficiencyAffinity }
    catch { Write-VerboseDebug -Timestamp (Get-Date) -Title 'WARNING' -Message 'Not an Intel 12700K or 14700K ' -ForegroundColor 'Yellow' }

    SetAffinityAndPriority -SetEfficiencyAffinity $true -SetBackgroudPriority $false -SetIdlePriority $true

    $mutexName = "$title"
    $isNew = $false
    $mutex_owner = $false
    $mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$isNew)
    $r = $mutex.WaitOne(1000)
    if ($r) {
        $mutex_owner = $true
        Write-VerboseDebug -Timestamp (Get-Date) -Title 'STARTING' -Message 'This is the only instance running' -ForegroundColor 'Gray'
    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title 'ERROR' -Message 'There is another instance running' -ForegroundColor 'Red'
        Start-Sleep -Seconds 5
        Exit
    }

    $remote_commands = {
        $path = $Event.SourceEventArgs.FullPath
        $changeType = $Event.SourceEventArgs.ChangeType
        Write-Host ' '
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$changeType" -Message $path
        Process-CommandFromJson $path
    }
    $cm_watcher_objects = Get-RenamesWatcher $command_file $remote_commands

    $generate_mission1 = {
        $path = $Event.SourceEventArgs.FullPath
        $changeType = $Event.SourceEventArgs.ChangeType
        Write-Host ' '
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$changeType" -Message $path
        Generate_WT_Mission_Type1
    }
    $wt_watcher_objects = Get-RenamesWatcher $mission1 $generate_mission1

    $processAction = {
        $event_pId = $Event.SourceEventArgs.NewEvent.ProcessID.ToString()
        $pName = $Event.SourceEventArgs.NewEvent.ProcessName.ToString()
        $traceName = $Event.SourceEventArgs.NewEvent.ToString()
        Write-Host ' '
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title 'PROCESS' -Message "$traceName - $pName [$event_pId]"
        Set-GamePowerScheme -traceName $traceName -programName $pName -processId $event_pId
    }
    $processWatcher = Get-ProcessWatchers $processAction

    $job = Start-ThreadJob -ArgumentList @($modulePath, $ipcPipeName, $ipcMutexName) -ScriptBlock {
        param($jobModulePath, $pipeName, $mutexName)
        Import-Module $jobModulePath -Force
        Start-IpcPipeServer -PipeName $pipeName -MutexName $mutexName
    } -ThrottleLimit 5 -StreamingHost $Host

    $wtDistanceWatcher = Start-WarThunderDistanceMultiplierMonitor -WatchFile $wtConfigBlk

    $Global:Watcher_Continue = $true
    $need_restart = Watchdog_Operations -CommandFilePath $command_file
}
finally {
    Set-MpPreference -DisableRealtimeMonitoring $false -Force
    Set-MpPreference -ScanOnlyIfIdleEnabled $false

    Send-IPC-ExitCommand $ipcPipeName

    Write-VerboseDebug -Timestamp (Get-Date) -Title 'FINALLY' -Message 'Disposing objects' -ForegroundColor 'Yellow'

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

    if ($wtDistanceWatcher) {
        $wtDistanceWatcher[1].EnableRaisingEvents = $false
        $wtDistanceWatcher[1].Dispose()
        $wtDistanceWatcher[0].Dispose()
    }

    if ($processWatcher) {
        $processWatcher | ForEach-Object { $_.Dispose() }
    }

    if ($job) {
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    }

    if ($mutex_owner) { $mutex.ReleaseMutex() }
    $mutex.Close()
    $mutex.Dispose()

    Write-VerboseDebug -Timestamp (Get-Date) -Title 'FINALLY' -Message 'Removing CimIndicationEvents' -ForegroundColor 'Yellow'
    Get-Event | Remove-Event -ErrorAction SilentlyContinue
    Get-EventSubscriber | Unregister-Event -ErrorAction SilentlyContinue
}

if ($need_restart) {
    $delay_sec = 5
    Write-VerboseDebug -Timestamp (Get-Date) -Title 'WARNING.  ' -Message "WARNING: Restarting the Watcher in $delay_sec seconds." -ForegroundColor 'Red'
    Start-Sleep -Seconds $delay_sec
    Start-Process -FilePath 'powershell.exe' -ArgumentList "-File `"$PSCommandPath`""
}
