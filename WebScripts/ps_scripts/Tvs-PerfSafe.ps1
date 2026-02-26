function Test-AffinityMaskValid {
    param([Int64]$Mask)
    if ($Mask -le 0) { return $false }
    $cpuCount = [Environment]::ProcessorCount
    $maxValid = ([Int64]1 -shl $cpuCount) - 1
    return (($Mask -band (-bnot $maxValid)) -eq 0)
}

function Try-SetCurrentProcessAffinity {
    param([Int64]$Mask,[string]$ContextTitle='PERF')
    if (-not (Test-AffinityMaskValid -Mask $Mask)) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title $ContextTitle -Message "Affinity mask $Mask is invalid for CPU count $([Environment]::ProcessorCount); continuing." -ForegroundColor Yellow
        return $false
    }
    try {
        [System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity = $Mask
        return $true
    } catch {
        Write-VerboseDebug -Timestamp (Get-Date) -Title $ContextTitle -Message "Unable to apply affinity mask $Mask: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

function Try-ApplyPerfTuning {
    param([string]$ContextTitle='PERF')
    try {
        SetAffinityAndPriority -SetEfficiencyAffinity $true -SetBackgroudPriority $false -SetIdlePriority $true
        return $true
    } catch {
        Write-VerboseDebug -Timestamp (Get-Date) -Title $ContextTitle -Message "SetAffinityAndPriority failed: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}
