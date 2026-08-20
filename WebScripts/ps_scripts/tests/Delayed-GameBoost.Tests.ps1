$testsRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $testsRoot 'Set-GamePowerScheme.ps1')

$script:Passed = 0
$script:Failed = 0

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Name
    )

    if ($Condition) {
        $script:Passed++
        Write-Host "PASS: $Name" -ForegroundColor Green
    }
    else {
        $script:Failed++
        Write-Host "FAIL: $Name" -ForegroundColor Red
    }
}

function Assert-Equal {
    param(
        $Expected,
        $Actual,
        [string]$Name
    )

    Assert-True -Condition ([object]::Equals($Expected, $Actual)) -Name "$Name (expected '$Expected', actual '$Actual')"
}

$missingBoostPath = Join-Path $testsRoot 'missing-delayed-boost-test.json'
$processNameWithExt = (Get-Process -Id $PID).ProcessName + '.exe'
$originalWebScriptsConfig = $Global:WebScriptsConfig

try {
    $Global:WebScriptsConfig = $null
    Assert-Equal $null (Get-SeatBeltWavPath) 'Missing host config disables seatbelt sound'

    $Global:WebScriptsConfig = @{
        paths = @{
            seatbeltWav = $null
        }
    }
    Assert-Equal $null (Get-SeatBeltWavPath) 'Null seatbeltWav disables seatbelt sound'

    $Global:WebScriptsConfig = @{
        paths = @{
            seatbeltWav = 'N:\MyPrograms\MySounds\Thirdwire\fasten_seatbelt.wav'
        }
    }
    Assert-Equal 'N:\MyPrograms\MySounds\Thirdwire\fasten_seatbelt.wav' (Get-SeatBeltWavPath) 'Configured seatbeltWav is returned'

    Start-DelayedGameBoost `
        -ProgramName $processNameWithExt `
        -ProcessId $PID `
        -BoostJsonPath $missingBoostPath `
        -DueAt (Get-Date).AddMilliseconds(200)

    Start-Sleep -Seconds 2

    $firedState = $Global:DelayedGameBoostByPid[[string]$PID]
    Assert-True ($null -ne $firedState) 'Delayed boost timer leaves state for stop-time decision'
    Assert-Equal $false $firedState['BoostAttempted'] 'Missing boost file does not mark boost attempted'
    Assert-Equal $false $firedState['BoostApplied'] 'Missing boost file does not mark boost applied'
    Assert-Equal 'MissingBoostFile' $firedState['Outcome'] 'Missing boost file records timer outcome'
    Assert-True ($null -eq $firedState['Timer']) 'Fired one-shot timer disposes itself'

    $stopDecision = Complete-DelayedGameBoostForStop -ProgramName $processNameWithExt -ProcessId $PID
    Assert-Equal $false $stopDecision.ShouldRestore 'Unattempted delayed boost skips restore'
    Assert-True (-not $Global:DelayedGameBoostByPid.ContainsKey([string]$PID)) 'Stop decision removes delayed boost state'

    Start-DelayedGameBoost `
        -ProgramName $processNameWithExt `
        -ProcessId $PID `
        -BoostJsonPath $missingBoostPath `
        -DueAt (Get-Date).AddSeconds(30)

    $pendingState = $Global:DelayedGameBoostByPid[[string]$PID]
    Assert-True ($pendingState['Timer'].Enabled) 'Pending delayed boost timer starts'

    $canceled = Stop-DelayedGameBoost -ProcessId $PID -RemoveState
    Assert-Equal $true $canceled['Canceled'] 'Canceled delayed boost records cancellation'
    Assert-True (-not $Global:DelayedGameBoostByPid.ContainsKey([string]$PID)) 'Canceled delayed boost removes state'
}
finally {
    Stop-DelayedGameBoost -ProcessId $PID -RemoveState | Out-Null
    $Global:WebScriptsConfig = $originalWebScriptsConfig
}

Write-Host "Delayed-GameBoost tests complete: $script:Passed passed, $script:Failed failed."
if ($script:Failed -gt 0) {
    exit 1
}
