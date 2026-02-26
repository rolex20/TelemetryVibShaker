$ErrorActionPreference = 'Stop'

$modulePath = Join-Path (Split-Path -Parent $PSScriptRoot) 'ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psd1'
Import-Module $modulePath -Force

$config = Get-TvsConfig
if (-not $config.paths.commandJson -or -not $config.names.ipcPipeName) {
    throw 'Config did not load expected keys.'
}

$requiredCommands = @(
    'Get-TvsConfig','Process-CommandFromJson','Watchdog_Operations','Get-RenamesWatcher',
    'Set-GamePowerScheme','Start-IpcPipeServer','Start-WarThunderDistanceMultiplierMonitor'
)

foreach ($commandName in $requiredCommands) {
    if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
        throw "Missing exported command: $commandName"
    }
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("tvs-smoke-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null
$file = Join-Path $tempDir 'watch.json'
Set-Content -Path $file -Value '{}'

$script:smokeFired = $false
$action = { $script:smokeFired = $true }
$watcher = Get-RenamesWatcher -WatchFile $file -Action $action
if (-not $watcher) { throw 'Get-RenamesWatcher returned null for valid temp path.' }

$tmpFile = Join-Path $tempDir 'watch.tmp'
Move-Item -Path $file -Destination $tmpFile
Wait-Event -Timeout 2 | Out-Null

$watcher[1].EnableRaisingEvents = $false
$watcher[1].Dispose()
$watcher[0].Dispose()
Remove-Item -Path $tempDir -Recurse -Force

if (-not $script:smokeFired) {
    throw 'Rename watcher did not fire.'
}

Write-Output 'OK'
