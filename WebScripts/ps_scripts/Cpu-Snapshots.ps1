function Format-GameplayDurationText {
    param (
        [Parameter(Mandatory)]
        [double]$TotalSeconds
    )

    if ($TotalSeconds -lt 60) {
        return "{0} seconds" -f [Math]::Round($TotalSeconds, 1)
    }

    if ($TotalSeconds -lt 3600) {
        return "{0} minutes" -f [Math]::Round(($TotalSeconds / 60), 1)
    }

    return "{0} hours" -f [Math]::Round(($TotalSeconds / 3600), 1)
}

function Get-GameRuntimeCpuSnapshotPath {
    param (
        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    return Join-Path $env:TEMP ("TelemetryVibShaker-GameRuntimeCpu-{0}.json" -f $ProcessId)
}

function Save-GameRuntimeCpuSnapshot {
    param (
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [double]$CpuSeconds,

        [Parameter(Mandatory)]
        [int]$HourMark
    )

    $snapshotPath = Get-GameRuntimeCpuSnapshotPath -ProcessId $ProcessId
    $payload = @{ CpuSeconds = $CpuSeconds; HourMark = $HourMark }
    $payload | ConvertTo-Json -Compress | Set-Content -Path $snapshotPath -Encoding UTF8
}

function Read-GameRuntimeCpuSnapshot {
    param (
        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    $snapshotPath = Get-GameRuntimeCpuSnapshotPath -ProcessId $ProcessId
    if (-not (Test-Path $snapshotPath)) {
        return $null
    }

    try {
        $raw = Get-Content -Path $snapshotPath -Raw -ErrorAction Stop
        if ([string]::IsNullOrWhiteSpace($raw)) {
            return $null
        }

        $parsed = $raw | ConvertFrom-Json -ErrorAction Stop
        return @{
            CpuSeconds = [double]$parsed.CpuSeconds
            HourMark = [int]$parsed.HourMark
        }
    }
    catch {
        return $null
    }
}

function Remove-GameRuntimeCpuSnapshot {
    param (
        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    $snapshotPath = Get-GameRuntimeCpuSnapshotPath -ProcessId $ProcessId
    Remove-Item -Path $snapshotPath -ErrorAction SilentlyContinue
}
