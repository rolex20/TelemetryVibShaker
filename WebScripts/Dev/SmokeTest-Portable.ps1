$ErrorActionPreference = 'Stop'
$webScriptsRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $webScriptsRoot 'ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psd1'

$importOutput = Import-Module $modulePath -Force -PassThru | Out-String
if (-not [string]::IsNullOrWhiteSpace($importOutput)) {
    throw 'Module import produced unexpected output.'
}

$config = Get-TvsConfig -WebScriptsRoot $webScriptsRoot
foreach ($p in @($config.paths.commandJson, $config.paths.wtMissionJson, $config.paths.wtConfigBlk)) {
    if (-not ($p.StartsWith((Join-Path $webScriptsRoot 'runtime')))) { throw "Path not resolved under runtime: $p" }
}

$tempFile = Join-Path $webScriptsRoot 'runtime\smoke_rename_target.json'
'{}' | Set-Content $tempFile
$actionHitFile = Join-Path $webScriptsRoot 'runtime\smoke_action_hit.txt'
if (Test-Path $actionHitFile) { Remove-Item $actionHitFile -Force }
$action = { 'hit' | Set-Content -Path $using:actionHitFile }
$watch = Get-RenamesWatcher -WatchFile $tempFile -Action $action
Rename-Item $tempFile ($tempFile + '.tmp')
Start-Sleep -Milliseconds 300
if (-not (Test-Path $actionHitFile)) { throw 'Rename watcher action did not execute.' }
$watch[1].Dispose(); $watch[0].Dispose()
Write-Host 'OK'
