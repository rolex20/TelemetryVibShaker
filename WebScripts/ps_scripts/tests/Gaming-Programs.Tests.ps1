$testsRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $testsRoot 'Gaming-Programs.ps1')

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

$originalGameProfiles = $Global:GameProfiles

try {
    $Global:GameProfiles = @{
        'DefaultGame.exe'  = @{}
        'ValidGame.exe'    = @{ BoostActionDelaySeconds = 1200 }
        'ZeroGame.exe'     = @{ BoostActionDelaySeconds = 0 }
        'StringGame.exe'   = @{ BoostActionDelaySeconds = '42' }
        'NullGame.exe'     = @{ BoostActionDelaySeconds = $null }
        'BadTextGame.exe'  = @{ BoostActionDelaySeconds = 'abc' }
        'NegativeGame.exe' = @{ BoostActionDelaySeconds = -1 }
        'BadProfile.exe'   = 'not-a-profile-hashtable'
    }

    Assert-Equal 5 (Get-GameBoostActionDelaySeconds -programName 'DefaultGame.exe') 'Missing boost delay preserves legacy default'
    Assert-Equal 1200 (Get-GameBoostActionDelaySeconds -programName 'ValidGame.exe') 'Valid boost delay is returned'
    Assert-Equal 0 (Get-GameBoostActionDelaySeconds -programName 'ZeroGame.exe') 'Zero boost delay is allowed'
    Assert-Equal 42 (Get-GameBoostActionDelaySeconds -programName 'StringGame.exe') 'Numeric string boost delay is accepted'
    Assert-Equal 5 (Get-GameBoostActionDelaySeconds -programName 'NullGame.exe') 'Null boost delay falls back'
    Assert-Equal 5 (Get-GameBoostActionDelaySeconds -programName 'BadTextGame.exe') 'Malformed boost delay falls back'
    Assert-Equal 5 (Get-GameBoostActionDelaySeconds -programName 'NegativeGame.exe') 'Negative boost delay falls back'
    Assert-Equal 5 (Get-GameBoostActionDelaySeconds -programName 'BadProfile.exe') 'Malformed profile falls back'
    Assert-Equal 5 (Get-GameBoostActionDelaySeconds -programName 'UnknownGame.exe') 'Unknown profile falls back'
}
finally {
    $Global:GameProfiles = $originalGameProfiles
}

Write-Host "Gaming-Programs tests complete: $script:Passed passed, $script:Failed failed."
if ($script:Failed -gt 0) {
    exit 1
}
