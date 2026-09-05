<#
.SYNOPSIS
    Ultra-low overhead logical-processor utility spike hunter with hybrid architecture
    awareness (Intel Core i7-14700K) and native kernel-level thread attribution.

.DESCRIPTION
    PowerShell 5.1 high-performance tool designed to monitor CPU utility spikes with
    near-zero system impact (< 0.3% single-core overhead).

    PERFORMANCE ARCHITECTURE (v2.0):
      1. Direct Kernel Interop (NtQuerySystemInformation):
         Bypasses slow System.Diagnostics.Process managed wrappers. Replaces ~4,000
         individual OpenThread/GetThreadTimes handle operations per interval with a
         single native kernel syscall (SystemProcessInformation = 5).
      2. Two-Tier Lazy Evaluation:
         During normal state (no core >= ThresholdPercent), the script only polls
         lightweight PDH counters (<0.1ms). The thread buffer is only traversed and
         parsed when an active spike is confirmed.
      3. Zero-Allocation Double Buffering:
         Maintains two unmanaged memory buffers swapped via pointer exchange. Zero
         Gen0/Gen1 Garbage Collection churn, zero thread-scheduling jitter.

    HYBRID TOPOLOGY IDENTIFICATION (Intel Core i7-14700K):
    The 14700K features 20 physical cores and 28 logical processors (LPs):
      - LP #0 through LP #15: 8 Performance Cores (P-Cores) with Hyper-Threading.
      - LP #16 through LP #27: 12 Efficient Cores (E-Cores) single-threaded.
    Spikes are tagged explicitly with core number and type: "G0:LP#3 [P-Core]=99.2%".

.PARAMETER ThresholdPercent
    Per-logical-processor utility that triggers an event. Default: 90.0%.

.PARAMETER SampleIntervalMs
    Sampling interval in milliseconds. Default: 250 ms.

.PARAMETER TopThreads
    Maximum number of busiest thread candidates to print for each event.
    When 0 (default), prints candidates until the hot workload is accounted for.

.PARAMETER ProcessId
    Optional PID allow-list to restrict candidate output.

.PARAMETER PCoreMaxLpIndex
    Highest Logical Processor index belonging to Performance Cores.
    Default is 15 (indexes 0 to 15 are P-Cores; 16+ are E-Cores on i7-14700K).

.PARAMETER IncludeSelf
    Includes this powershell.exe process among candidates. Excluded by default.

.PARAMETER DurationSeconds
    Stops automatically after this many seconds. Zero means run until Ctrl+C.

.EXAMPLE
    .\Core-Spike-Hunter.ps1 -ThresholdPercent 90 -SampleIntervalMs 250

.EXAMPLE
    .\Core-Spike-Hunter.ps1 -ThresholdPercent 85 -TopThreads 5
#>

[CmdletBinding()]
param(
    [ValidateRange(0.1, 1000.0)]
    [double]$ThresholdPercent = 90.0,

    [ValidateRange(50, 60000)]
    [int]$SampleIntervalMs = 250,

    [ValidateRange(0, 10000)]
    [int]$TopThreads = 0,

    [ValidateScript({ $_ -gt 0 })]
    [int[]]$ProcessId = @(),

    [ValidateRange(0, 256)]
    [int]$PCoreMaxLpIndex = 15,

    [switch]$IncludeSelf,

    [ValidateRange(0, 86400)]
    [int]$DurationSeconds = 0
)

. "$PSScriptRoot\Import-OptimizedCSharp.ps1"

if (-not ("CoreSpikeHunterRunner" -as [type])) {
    $code = Get-Content -Path "$PSScriptRoot\CoreSpikeHunterRunner.cs" -Raw
    Import-OptimizedCSharp `
        -Source $code `
        -ExpectedTypeName 'CoreSpikeHunterRunner' `
        -Platform 'AnyCPU' `
        -CallerScriptPath $PSCommandPath
}

try {
    [CoreSpikeHunterRunner]::Run(
        $ThresholdPercent,
        $SampleIntervalMs,
        $TopThreads,
        $ProcessId,
        $PID,
        [bool]$IncludeSelf,
        $DurationSeconds,
        $PCoreMaxLpIndex
    )
}
catch {
    Write-Host 'CoreSpikeHunterRunner failed:' -ForegroundColor Red
    Write-Host $_.Exception.ToString() -ForegroundColor Red
    throw
}