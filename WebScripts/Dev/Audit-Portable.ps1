$ErrorActionPreference = 'Stop'
$bad = @()
$targets = Get-ChildItem -Path (Join-Path $PSScriptRoot '..') -Recurse -Filter *.ps1 | Where-Object { $_.FullName -notmatch '\\Dev\\' }
foreach ($f in $targets) {
    $text = Get-Content -Raw $f.FullName
    if ($text -match 'Include-Script' -or $text -match '\$search_paths' -or $text -match '\. "[A-Za-z]:\\') { $bad += $f.FullName }
}
if ($bad.Count -gt 0) { throw "Portable audit failed. Forbidden patterns found: $($bad -join ', ')" }

$modulePath = Join-Path $PSScriptRoot '..\ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psm1'
$moduleText = Get-Content -Raw $modulePath
$scriptRefs = [regex]::Matches($moduleText, "'([^']+\.ps1)'") | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
foreach ($ref in $scriptRefs) {
    $path = Join-Path (Split-Path $modulePath -Parent | Split-Path -Parent) $ref
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($path,[ref]$null,[ref]$null)
    foreach ($stmt in $ast.EndBlock.Statements) {
        if ($stmt.GetType().Name -ne 'FunctionDefinitionAst') {
            throw "Module imports non-function top-level statement in $ref"
        }
    }
}
Write-Host 'Audit OK'
