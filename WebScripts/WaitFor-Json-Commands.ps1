$need_restart = $false
$Global:Watcher_Continue = $true

$modulePath = Join-Path $PSScriptRoot 'ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psd1'
Import-Module $modulePath -Force
$config = Get-TvsConfig -WebScriptsRoot $PSScriptRoot

$runtimeDir = Join-Path $PSScriptRoot 'runtime'
if (-not (Test-Path $runtimeDir)) { New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null }
foreach ($placeholder in @($config.paths.commandJson, $config.paths.wtMissionJson, $config.paths.wtConfigBlk)) {
    $dir = Split-Path -Parent $placeholder
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    if (-not (Test-Path $placeholder)) { '{}' | Set-Content -Path $placeholder }
}

$title = 'Watcher for JSON Gaming Commands'
$Host.UI.RawUI.WindowTitle = $title

$cm_watcher_objects = $null
$wt_watcher_objects = $null
$processWatcher = $null
$job = $null
$mutex_owner = $false

try {
    Check-Admin-Privileges | Out-Null
    Try-SetCurrentProcessAffinity -Mask 983040 -ContextTitle 'WATCHER' | Out-Null
    Try-ApplyPerfTuning -ContextTitle 'WATCHER' | Out-Null

    $isNew = $false
    $mutex = New-Object System.Threading.Mutex($false, $config.names.orchestratorMutexName, [ref]$isNew)
    if ($mutex.WaitOne(1000)) { $mutex_owner = $true } else { throw 'There is another instance running.' }

    $remote_commands = {
        $path = $Event.SourceEventArgs.FullPath
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$($Event.SourceEventArgs.ChangeType)" -Message $path
        Process-CommandFromJson -JsonFilePath $path -Config $config
    }.GetNewClosure()

    if (Test-Path (Split-Path -Parent $config.paths.commandJson)) {
        $cm_watcher_objects = Get-RenamesWatcher $config.paths.commandJson $remote_commands
    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title 'WARNING' -Message "Command watcher directory missing: $(Split-Path -Parent $config.paths.commandJson)" -ForegroundColor Yellow
    }

    $generate_mission1 = {
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$($Event.SourceEventArgs.ChangeType)" -Message $Event.SourceEventArgs.FullPath
        Generate_WT_Mission_Type1 -Config $config
    }.GetNewClosure()
    if (Test-Path (Split-Path -Parent $config.paths.wtMissionJson)) {
        $wt_watcher_objects = Get-RenamesWatcher $config.paths.wtMissionJson $generate_mission1
    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title 'WARNING' -Message "WT watcher directory missing: $(Split-Path -Parent $config.paths.wtMissionJson)" -ForegroundColor Yellow
    }

    $processAction = {
        $event_pId = $Event.SourceEventArgs.NewEvent.ProcessID.ToString()
        $pName = $Event.SourceEventArgs.NewEvent.ProcessName.ToString()
        $traceName = $Event.SourceEventArgs.NewEvent.ToString()
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title 'PROCESS' -Message "$traceName - $pName [$event_pId]"
        Set-GamePowerScheme -traceName $traceName -programName $pName -processId $event_pId
    }
    $processWatcher = Get-ProcessWatchers $processAction

    $ipcScript = {
        param($ModulePathArg,$PipeNameArg,$MutexNameArg)
        Import-Module $ModulePathArg -Force
        Start-IpcPipeServer -PipeName $PipeNameArg -MutexName $MutexNameArg
    }
    if (Get-Command Start-ThreadJob -ErrorAction SilentlyContinue) {
        $job = Start-ThreadJob -ScriptBlock $ipcScript -ArgumentList @($modulePath, $config.names.ipcPipeName, $config.names.ipcMutexName)
    } else {
        $job = Start-Job -ScriptBlock $ipcScript -ArgumentList @($modulePath, $config.names.ipcPipeName, $config.names.ipcMutexName)
    }

    $need_restart = Watchdog_Operations -CommandFilePath $config.paths.commandJson
}
finally {
    Send-IPC-ExitCommand $config.names.ipcPipeName | Out-Null
    if ($job) { Stop-Job -Job $job -Force -ErrorAction SilentlyContinue; Remove-Job -Job $job -Force -ErrorAction SilentlyContinue }
    if ($cm_watcher_objects) { $cm_watcher_objects[1].EnableRaisingEvents = $false; $cm_watcher_objects[1].Dispose(); $cm_watcher_objects[0].Dispose() }
    if ($wt_watcher_objects) { $wt_watcher_objects[1].EnableRaisingEvents = $false; $wt_watcher_objects[1].Dispose(); $wt_watcher_objects[0].Dispose() }
    if ($mutex_owner -and $mutex) { $mutex.ReleaseMutex() }
    if ($mutex) { $mutex.Dispose() }
    Get-Event | Remove-Event -ErrorAction SilentlyContinue
    Get-EventSubscriber | Unregister-Event -ErrorAction SilentlyContinue
}

if ($need_restart) {
    Write-VerboseDebug -Timestamp (Get-Date) -Title 'WARNING' -Message 'Restarting watcher in 5 seconds.' -ForegroundColor Red
    Start-Sleep -Seconds 5
    Start-Process -FilePath 'powershell.exe' -ArgumentList "-File `"$PSCommandPath`""
}
