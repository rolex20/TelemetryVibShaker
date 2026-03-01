#requires -version 5.1
<#
Downloads the latest TelemetryVibShaker master.zip and extracts ONLY:
  WebScripts/ps_scripts/*
into the folder where this script is located.

Example:
  Save as: C:\myprograms\webscripts\ps_scripts\update_and_unzip_here.ps1
  Run:     powershell -ExecutionPolicy Bypass -File .\update_and_unzip_here.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if ($PSScriptRoot) { return $PSScriptRoot }
    # Fallback for edge cases
    return Split-Path -Parent $MyInvocation.MyCommand.Path
}

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

# --- Config ---
$ZipUrl = 'https://github.com/rolex20/TelemetryVibShaker/archive/refs/heads/master.zip'

$Here = Get-ScriptDirectory
$TargetRoot = $Here  # extract into folder where the script lives

# Temp working area (inside the same drive; safe & easy cleanup)
$TempRoot = Join-Path $env:TEMP ("TelemetryVibShaker_Update_" + [Guid]::NewGuid().ToString("N"))
Ensure-Directory $TempRoot

$ZipPath = Join-Path $TempRoot 'repo.zip'

Write-Host "Target folder: $TargetRoot"
Write-Host "Temp folder:   $TempRoot"
Write-Host "Downloading:   $ZipUrl"

try {
    # Download
    Invoke-WebRequest -Uri $ZipUrl -OutFile $ZipPath -UseBasicParsing

    if (-not (Test-Path -LiteralPath $ZipPath)) {
        throw "Download failed: $ZipPath not found."
    }

    # Open zip for selective extraction
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        # GitHub archives always have a top-level folder like: TelemetryVibShaker-master/
        # We want: TelemetryVibShaker-master/WebScripts/ps_scripts/*
        $prefixRegex = '^[^/]+/WebScripts/ps_scripts/'

        $entries = $zip.Entries | Where-Object {
            $_.FullName -match $prefixRegex -and
            -not [string]::IsNullOrWhiteSpace($_.Name)  # skip directory entries
        }

        if (-not $entries -or $entries.Count -eq 0) {
            throw "No entries matched WebScripts/ps_scripts/* inside the zip. Repo layout may have changed."
        }

        Write-Host ("Found {0} files to extract..." -f $entries.Count)

        foreach ($entry in $entries) {
            # Strip the leading "<repo>-<branch>/WebScripts/ps_scripts/" and write into $TargetRoot
            $relative = ($entry.FullName -replace $prefixRegex, '')
            $destPath = Join-Path $TargetRoot $relative

            $destDir = Split-Path -Parent $destPath
            Ensure-Directory $destDir

            # Extract by streaming (avoids weird overwrite behavior with ZipFileExtensions)
            $inStream  = $entry.Open()
            try {
                $outStream = New-Object System.IO.FileStream($destPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
                try {
                    $inStream.CopyTo($outStream)
                } finally {
                    $outStream.Dispose()
                }
            } finally {
                $inStream.Dispose()
            }
        }

        Write-Host "Done. Extracted WebScripts/ps_scripts/* into: $TargetRoot"
    }
    finally {
        $zip.Dispose()
    }
}
finally {
    # Cleanup temp
    if (Test-Path -LiteralPath $TempRoot) {
        Remove-Item -LiteralPath $TempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

