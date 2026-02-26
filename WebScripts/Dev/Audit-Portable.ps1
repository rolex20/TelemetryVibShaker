$ErrorActionPreference = 'Stop'
$webScriptsRoot = Split-Path -Parent $PSScriptRoot
$files = Get-ChildItem -Path $webScriptsRoot -Recurse -Filter *.ps1
$violations = @()

foreach ($file in $files) {
    if ($file.Name -eq 'Include-Script.ps1') { continue }
    $i = 0
    $inBlockComment = $false
    foreach ($line in Get-Content $file.FullName) {
        $i++
        $trim = $line.Trim()
        if ($trim -match '<#') { $inBlockComment = $true }
        if ($inBlockComment) {
            if ($trim -match '#>') { $inBlockComment = $false }
            continue
        }
        if ($trim.StartsWith('#')) { continue }
        if ($line -match 'Include-Script|\$search_paths|\.\s+"[A-Za-z]:\\') {
            $violations += "{0}:{1}:{2}" -f $file.FullName, $i, $trim
        }
    }
}

$moduleFile = Join-Path $webScriptsRoot 'ps_scripts\TelemetryVibShaker.WebScripts\TelemetryVibShaker.WebScripts.psm1'
if ((Get-Content $moduleFile -Raw) -match 'Show-CPU-Time-PerProcess\.ps1') {
    $violations += "$moduleFile: Show-CPU-Time-PerProcess.ps1 must not be imported by module"
}

if ($violations.Count -gt 0) {
    Write-Host 'FAIL'
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}
Write-Host 'PASS'
