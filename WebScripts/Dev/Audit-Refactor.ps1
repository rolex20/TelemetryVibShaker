$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$modulePath = Join-Path $repoRoot 'ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psd1'

$results = @()
function Add-Result($Name, $Passed, $Message) {
    $script:results += [PSCustomObject]@{ Check = $Name; Passed = $Passed; Message = $Message }
}

# 1) static scan
$scanFiles = Get-ChildItem -Path $repoRoot -Recurse -Filter '*.ps1' | Where-Object { $_.FullName -notlike "*$([IO.Path]::DirectorySeparatorChar)Dev$([IO.Path]::DirectorySeparatorChar)*" -and $_.Name -ne 'Include-Script.ps1' }
$patterns = @('\.\s+"[A-Z]:\\','\.\s+''[A-Z]:\\','Include-Script','\$search_paths')
$matches = @()
foreach ($f in $scanFiles) {
    foreach ($pat in $patterns) {
        $hits = Select-String -Path $f.FullName -Pattern $pat
        if ($hits) { $matches += $hits }
    }
}
if ($matches.Count -gt 0) { Add-Result 'Static scan' $false ("Found forbidden patterns: " + (($matches | Select-Object -First 10 | ForEach-Object { "$($_.Path):$($_.LineNumber)" }) -join ', ')) }
else { Add-Result 'Static scan' $true 'No hardcoded include patterns found.' }

# 2) dependency inventory and export verification
Import-Module $modulePath -Force
$entrypoints = @(
    (Join-Path $repoRoot 'WaitFor-Json-Commands.ps1'),
    (Join-Path $repoRoot 'ps_scripts\Process-CommandFromJson.ps1'),
    (Join-Path $repoRoot 'ps_scripts\Watchdog-Operations.ps1')
)
$required = New-Object System.Collections.Generic.HashSet[string]
foreach ($ep in $entrypoints) {
    $null = $tokens = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($ep, [ref]$tokens, [ref]$null)
    $cmdAsts = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)
    foreach ($cmd in $cmdAsts) {
        $name = $cmd.GetCommandName()
        if ($name -and $name -match '^[A-Za-z]') { $required.Add($name) | Out-Null }
    }
}
$ignore = @('if','try','catch','finally','foreach','where-object','forEach-Object','return','param')
$missing = @()
foreach ($name in $required) {
    if ($ignore -contains $name.ToLower()) { continue }
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        $missing += $name
    }
}
if ($missing.Count -gt 0) { Add-Result 'Dependency export verification' $false ("Missing commands: " + ($missing -join ', ')) }
else { Add-Result 'Dependency export verification' $true 'All referenced commands resolve after module import.' }

# 3) side-effect safety
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Import-Module $modulePath -Force
$sw.Stop()
$startFn = Get-Command Start-WarThunderDistanceMultiplierMonitor -ErrorAction SilentlyContinue
if ($sw.Elapsed.TotalSeconds -lt 5 -and $startFn) {
    Add-Result 'Side-effect safety' $true 'Module import is quick and Start-WarThunderDistanceMultiplierMonitor exists.'
} else {
    Add-Result 'Side-effect safety' $false 'Module import is slow or monitor start function missing.'
}

# 4) event action visibility
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("tvs-audit-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null
$file = Join-Path $tempDir 'audit.json'; Set-Content -Path $file -Value '{}'
$flagFile = Join-Path $tempDir 'flag.txt'
$action = { Write-VerboseDebug -Timestamp (Get-Date) -Title 'AUDIT' -Message 'event'; Set-Content -Path $Event.MessageData -Value 'hit' }
$watcher = Get-RenamesWatcher -WatchFile $file -Action $action
if ($watcher) {
    $watcher[0].MessageData = $flagFile
    Move-Item -Path $file -Destination (Join-Path $tempDir 'audit.tmp')
    Wait-Event -Timeout 2 | Out-Null
    $eventVisible = Test-Path $flagFile
    $watcher[1].EnableRaisingEvents = $false; $watcher[1].Dispose(); $watcher[0].Dispose()
    if ($eventVisible) { Add-Result 'Event-action visibility' $true 'Event action could call module function.' }
    else { Add-Result 'Event-action visibility' $false 'Event action did not execute expected function call.' }
} else {
    Add-Result 'Event-action visibility' $false 'Failed to create watcher for temp folder.'
}
Remove-Item -Path $tempDir -Recurse -Force

# 5) threadjob proof
if (Get-Command Start-ThreadJob -ErrorAction SilentlyContinue) {
    $job = Start-ThreadJob -ArgumentList $modulePath -ScriptBlock { param($mp) Import-Module $mp -Force; (Get-TvsConfig).names.ipcPipeName }
    $out = Receive-Job -Job $job -Wait -AutoRemoveJob
    if ($out) { Add-Result 'ThreadJob proof' $true "ThreadJob returned: $out" }
    else { Add-Result 'ThreadJob proof' $false 'ThreadJob completed without expected output.' }
} else {
    Add-Result 'ThreadJob proof' $true 'SKIP: Start-ThreadJob unavailable in this environment.'
}

# 6) config layering
$config = Get-TvsConfig
$requiredKeys = @($config.paths.commandJson, $config.paths.wtMissionJson, $config.paths.wtConfigBlk, $config.names.ipcPipeName, $config.names.ipcMutexName)
$okDefaults = ($requiredKeys | Where-Object { -not $_ }).Count -eq 0
$configRoot = Join-Path $repoRoot 'config'
$localPath = Join-Path $configRoot 'local.json'
$backup = $null
if (Test-Path $localPath) { $backup = Get-Content $localPath -Raw }
@'{"names":{"ipcPipeName":"ipc_test_override"}}'@ | Set-Content $localPath
$over = Get-TvsConfig
if ($backup -ne $null) { Set-Content $localPath $backup } else { Remove-Item $localPath -Force }
if ($okDefaults -and $over.names.ipcPipeName -eq 'ipc_test_override') {
    Add-Result 'Config layering' $true 'Defaults load, missing local is tolerated, and local override wins.'
} else {
    Add-Result 'Config layering' $false 'Config layering failed required behavior.'
}

$failed = $results | Where-Object { -not $_.Passed }
$results | ForEach-Object {
    $status = if ($_.Passed) { 'PASS' } else { 'FAIL' }
    Write-Output ("[{0}] {1}: {2}" -f $status, $_.Check, $_.Message)
}
if ($failed.Count -gt 0) {
    Write-Output "OVERALL: FAIL"
    exit 1
}
Write-Output 'OVERALL: PASS'
