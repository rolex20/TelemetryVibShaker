function Test-AffinityMaskValid {
    param([Int64]$Mask)
    $processorCount = [Environment]::ProcessorCount
    if ($Mask -le 0) { return $false }
    if ($processorCount -ge 63) { return $true }
    $maxMaskExclusive = [Int64]1 -shl $processorCount
    return ($Mask -lt $maxMaskExclusive)
}

function Try-SetCurrentProcessAffinity {
    param(
        [Int64]$Mask,
        [string]$ContextTitle = 'AFFINITY'
    )
    $processorCount = [Environment]::ProcessorCount
    if (-not (Test-AffinityMaskValid -Mask $Mask)) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title $ContextTitle -Message "Skipping invalid affinity mask [$Mask] for processor count [$processorCount]." -ForegroundColor 'Yellow'
        return $false
    }
    try {
        [System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity = $Mask
        return $true
    } catch {
        Write-VerboseDebug -Timestamp (Get-Date) -Title $ContextTitle -Message "Affinity set failed. CPU=$processorCount Mask=$Mask Error=$($_.Exception.Message)" -ForegroundColor 'Yellow'
        return $false
    }
}

function Try-ApplyPerfTuning {
    param([string]$ContextTitle = 'PERF')
    try {
        SetAffinityAndPriority -SetEfficiencyAffinity $true -SetBackgroudPriority $false -SetIdlePriority $true
        return $true
    } catch {
        Write-VerboseDebug -Timestamp (Get-Date) -Title $ContextTitle -Message "Perf tuning warning: $($_.Exception.Message)" -ForegroundColor 'Yellow'
        return $false
    }
}
