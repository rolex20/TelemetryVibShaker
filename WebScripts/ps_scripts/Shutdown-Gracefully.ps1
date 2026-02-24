<#
.SYNOPSIS
Gracefully requests the JSON watcher to stop, then forces an immediate system shutdown.

.DESCRIPTION
This script performs a two-phase shutdown:
1) Signal phase: write an EXIT_WATCHER command through the watcher command file channel.
2) Deadline phase: wait up to 1 second for watcher teardown, then proceed with OS shutdown.

This allows the watcher to unwind cleanup logic while still guaranteeing shutdown is not delayed.

.PARAMETER CommandDir
Directory where the watcher reads command files.
Default: C:\MyPrograms\wamp\www\remote_control

.PARAMETER CommandFileName
Watcher command filename.
Default: command.json

.PARAMETER DryRun
If set, no shutdown is executed; the script logs what would happen.

.EXAMPLE
.\Shutdown-Gracefully.ps1

.EXAMPLE
.\Shutdown-Gracefully.ps1 -DryRun

.EXAMPLE
.\Shutdown-Gracefully.ps1 -CommandDir 'C:\Temp\remote_control' -CommandFileName 'command.json'
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$CommandDir = 'C:\MyPrograms\wamp\www\remote_control',

    [Parameter(Mandatory = $false)]
    [string]$CommandFileName = 'command.json',

    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-MutexExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $openedMutex = $null
    try {
        $openedMutex = [System.Threading.Mutex]::OpenExisting($Name)
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($null -ne $openedMutex) {
            $openedMutex.Dispose()
        }
    }
}

function Test-IpcPipeAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PipeName,

        [Parameter(Mandatory = $false)]
        [int]$ConnectTimeoutMs = 50
    )

    $client = $null
    try {
        $client = [System.IO.Pipes.NamedPipeClientStream]::new('.', $PipeName, [System.IO.Pipes.PipeDirection]::InOut)
        $client.Connect($ConnectTimeoutMs)
        return $client.IsConnected
    }
    catch {
        return $false
    }
    finally {
        if ($null -ne $client) {
            $client.Dispose()
        }
    }
}

$mutexName = 'Watcher for JSON Gaming Commands'
$ipcPipeName = 'ipc_pipe_vr_server_commands'
$finalCommandPath = Join-Path -Path $CommandDir -ChildPath $CommandFileName
$tempCommandPath = Join-Path -Path $CommandDir -ChildPath 'command.tmp'

if (-not (Test-Path -LiteralPath $CommandDir -PathType Container)) {
    throw "CommandDir does not exist: $CommandDir"
}

$payload = @{
    command_type = 'EXIT_WATCHER'
    parameters   = @{}
}

$payloadJson = $payload | ConvertTo-Json -Depth 4 -Compress
Set-Content -LiteralPath $tempCommandPath -Value $payloadJson -Encoding utf8NoBOM

if (Test-Path -LiteralPath $finalCommandPath -PathType Leaf) {
    Remove-Item -LiteralPath $finalCommandPath -Force
}

# Phase 1 (signal): rename temp -> command.json so the rename watcher reliably detects the trigger.
Rename-Item -LiteralPath $tempCommandPath -NewName $CommandFileName -Force
Write-Host "EXIT_WATCHER command sent: $finalCommandPath"

$ipcUp = Test-IpcPipeAvailable -PipeName $ipcPipeName -ConnectTimeoutMs 50
if (-not $ipcUp) {
    Write-Host "IPC pipe appears down or unreachable: $ipcPipeName"
}

# Phase 2 (deadline): strict 1s cap prevents hanging the shutdown sequence on watcher teardown.
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
do {
    if (-not (Test-MutexExists -Name $mutexName)) {
        Write-Host "Watcher mutex disappeared; proceeding to shutdown path."
        break
    }

    Start-Sleep -Milliseconds 50
}
while ($stopwatch.ElapsedMilliseconds -lt 1000)

$stopwatch.Stop()

if (Test-MutexExists -Name $mutexName) {
    Write-Host "Watcher mutex still present after $($stopwatch.ElapsedMilliseconds) ms; continuing due to strict time budget."
}
else {
    Write-Host "Watcher exited within $($stopwatch.ElapsedMilliseconds) ms."
}

if ($DryRun.IsPresent) {
    Write-Host 'DRY RUN: would execute shutdown.exe /s /t 0'
}
else {
    & shutdown.exe /s /t 0
}
