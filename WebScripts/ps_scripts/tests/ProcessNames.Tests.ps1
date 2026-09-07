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

function Assert-ArrayEqual {
    param(
        $Expected,
        $Actual,
        [string]$Name
    )

    $expArr = if ($null -eq $Expected) { @() } else { @($Expected) }
    $actArr = if ($null -eq $Actual) { @() } else { @($Actual) }

    $expStr = if ($expArr.Count -gt 0) { $expArr -join ', ' } else { '<empty>' }
    $actStr = if ($actArr.Count -gt 0) { $actArr -join ', ' } else { '<empty>' }

    if ($expArr.Count -eq 0 -and $actArr.Count -eq 0) {
        Assert-True $true "$Name ([<empty>])"
        return
    }

    if ($expArr.Count -ne $actArr.Count) {
        Assert-True $false "$Name (length mismatch: expected $($expArr.Count), actual $($actArr.Count))"
        return
    }

    for ($i = 0; $i -lt $expArr.Count; $i++) {
        if ($expArr[$i] -ne $actArr[$i]) {
            Assert-True $false "$Name (mismatch at index $($i): expected '$($expArr[$i])', actual '$($actArr[$i])')"
            return
        }
    }

    Assert-True $true "$Name ([$actStr])"
}

# -------------------------------------------------------------------------
# Test Group 1: Get-TrimmedProcessNames Functionality
# -------------------------------------------------------------------------

# 1. Single string (legacy format)
$single = Get-TrimmedProcessNames "notepad"
Assert-ArrayEqual @("notepad") $single "Single process string returns 1-element array"

# 2. Comma-separated string
$csv = Get-TrimmedProcessNames "notepad, TiWorker, CompatTelRunner"
Assert-ArrayEqual @("notepad", "TiWorker", "CompatTelRunner") $csv "Comma-separated string returns array of processes"

# 3. Comma-separated string with irregular whitespace
$whitespace = Get-TrimmedProcessNames "   notepad   ,  TiWorker  ,   CompatTelRunner   "
Assert-ArrayEqual @("notepad", "TiWorker", "CompatTelRunner") $whitespace "Irregular whitespace is properly trimmed"

# 4. JSON / PowerShell array
$array = Get-TrimmedProcessNames @("notepad", "TiWorker", "CompatTelRunner")
Assert-ArrayEqual @("notepad", "TiWorker", "CompatTelRunner") $array "Array input is returned as trimmed string array"

# 5. Mixed array with comma-separated items
$mixed = Get-TrimmedProcessNames @("notepad, TiWorker", "CompatTelRunner")
Assert-ArrayEqual @("notepad", "TiWorker", "CompatTelRunner") $mixed "Mixed array with comma string is flattened and trimmed"

# 6. Automatic .exe stripping (case-insensitive)
$withExe = Get-TrimmedProcessNames "notepad.exe, TiWorker.EXE, CompatTelRunner"
Assert-ArrayEqual @("notepad", "TiWorker", "CompatTelRunner") $withExe "Accidental .exe extensions are stripped"

# 7. Case-insensitive deduplication
$duplicates = Get-TrimmedProcessNames @("notepad", "Notepad", "NOTEPAD.exe", "TiWorker")
Assert-ArrayEqual @("notepad", "TiWorker") $duplicates "Duplicate process names with different casing/.exe are deduplicated"

# 8. Empty and null inputs
Assert-ArrayEqual @() (Get-TrimmedProcessNames $null) "Null input returns empty array"
Assert-ArrayEqual @() (Get-TrimmedProcessNames "") "Empty string returns empty array"
Assert-ArrayEqual @() (Get-TrimmedProcessNames "   ") "Whitespace string returns empty array"
Assert-ArrayEqual @() (Get-TrimmedProcessNames @("", "  ")) "Array of empty strings returns empty array"
Assert-ArrayEqual @("notepad", "TiWorker") (Get-TrimmedProcessNames ",notepad,,TiWorker,") "Leading/trailing/empty commas are ignored"

# -------------------------------------------------------------------------
# Test Group 2: Action Profile Lookup (Restore-GameBoost logic)
# -------------------------------------------------------------------------

$testActions = @(
    [pscustomobject]@{
        process_name = "FlightSimulator"
        id = "rule-single"
    },
    [pscustomobject]@{
        process_name = "notepad, chrome, explorer"
        id = "rule-csv"
    },
    [pscustomobject]@{
        process_name = @("TiWorker", "CompatTelRunner")
        id = "rule-array"
    }
)

function Find-ActionForProgram($actions, [string]$program) {
    return $actions | Where-Object {
        $actionProcessNames = Get-TrimmedProcessNames $_.process_name
        $actionProcessNames -contains $program
    } | Select-Object -First 1
}

# Single match
$m1 = Find-ActionForProgram $testActions "FlightSimulator"
Assert-Equal "rule-single" $m1.id "Matches single-process rule"

# Single match case-insensitive
$m1Case = Find-ActionForProgram $testActions "flightsimulator"
Assert-Equal "rule-single" $m1Case.id "Matches single-process rule case-insensitively"

# CSV match
$m2 = Find-ActionForProgram $testActions "chrome"
Assert-Equal "rule-csv" $m2.id "Matches middle process in CSV rule"

$m2First = Find-ActionForProgram $testActions "notepad"
Assert-Equal "rule-csv" $m2First.id "Matches first process in CSV rule"

# Array match
$m3 = Find-ActionForProgram $testActions "CompatTelRunner"
Assert-Equal "rule-array" $m3.id "Matches second process in array rule"

$m3First = Find-ActionForProgram $testActions "TiWorker"
Assert-Equal "rule-array" $m3First.id "Matches first process in array rule"

# Non-matching
$mNotFound = Find-ActionForProgram $testActions "NonExistent"
Assert-True ($null -eq $mNotFound) "Non-existent process does not match"

# Partial substring non-collision
$mPartial = Find-ActionForProgram $testActions "Worker"
Assert-True ($null -eq $mPartial) "Substring 'Worker' does not match 'TiWorker'"

# -------------------------------------------------------------------------
# Test Group 3: Real Boost JSON Profile Verification
# -------------------------------------------------------------------------

$boostFiles = @(
    "action-per-process-boost1.json",
    "action-per-process-boost2.json",
    "action-per-process-boost3.json",
    "action-per-process-boost4.json"
)

foreach ($boostFile in $boostFiles) {
    $fullPath = Join-Path $testsRoot $boostFile
    Assert-True (Test-Path $fullPath) "Profile file exists: $boostFile"

    $actions = @(Get-ActionsPerGame $fullPath)
    Assert-True ($actions.Count -gt 0) "Profile $boostFile parsed successfully with $($actions.Count) rules"

    foreach ($action in $actions) {
        $names = Get-TrimmedProcessNames $action.process_name
        Assert-True ($names.Length -gt 0) "Rule in $boostFile has valid process names: $($names -join ', ')"

        if ($action.parameters.dependencies) {
            foreach ($dep in $action.parameters.dependencies) {
                $depNames = Get-TrimmedProcessNames $dep.process_name
                Assert-True ($depNames.Length -gt 0) "Dependency in $boostFile has valid process names: $($depNames -join ', ')"
            }
        }
    }
}

# Verify specific consolidated boost4.json rule
$boost4Path = Join-Path $testsRoot "action-per-process-boost4.json"
$boost4Actions = @(Get-ActionsPerGame $boost4Path)
$boost4Names = Get-TrimmedProcessNames $boost4Actions[0].process_name
Assert-Equal 2 $boost4Names.Length "boost4.json consolidated rule contains exactly 2 processes"
Assert-True ($boost4Names -contains "TiWorker") "boost4.json contains TiWorker"
Assert-True ($boost4Names -contains "CompatTelRunner") "boost4.json contains CompatTelRunner"

Write-Host "============================================================"
Write-Host "ProcessNames tests complete: $script:Passed passed, $script:Failed failed."
if ($script:Failed -gt 0) {
    exit 1
}
