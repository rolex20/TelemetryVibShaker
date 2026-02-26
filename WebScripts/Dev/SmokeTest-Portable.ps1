$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot '..\ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psd1'
$before = $host.UI.RawUI.WindowTitle
$output = & { Import-Module $modulePath -Force } 2>&1
if ($output) { throw "Unexpected module import output: $output" }
$config = Get-TvsConfig -WebScriptsRoot (Join-Path $PSScriptRoot '..')
$runtimeRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\runtime'))
foreach ($p in @($config.paths.commandJson,$config.paths.wtMissionJson)) {
    if (-not ([System.IO.Path]::GetFullPath($p).StartsWith($runtimeRoot))) { throw "Default path not under runtime: $p" }
}
$tempDir = Join-Path $env:TEMP ('tvs-smoke-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $tempDir | Out-Null
$file = Join-Path $tempDir 'test.json'
'{}' | Set-Content $file
$script:hit = $false
$action = { $script:hit = $true; Write-VerboseDebug -Timestamp (Get-Date) -Title 'SMOKE' -Message 'ok' }
$watch = Get-RenamesWatcher -WatchFile $file -Action $action
Rename-Item $file ($file + '.tmp')
Start-Sleep -Milliseconds 500
$watch[1].Dispose(); $watch[0].Dispose()
if (-not $script:hit) { throw 'Watcher action did not execute module function.' }
Write-Host 'OK'
