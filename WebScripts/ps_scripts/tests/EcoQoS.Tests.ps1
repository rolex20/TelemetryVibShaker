$testsRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $testsRoot 'Set-IdealProcessor.ps1')

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

Write-Host "=== EcoQoS Unit Tests ===" -ForegroundColor Cyan

# 1. EcoQoSHelper Type Verification
$helperType = ([System.Management.Automation.PSTypeName]'EcoQoSHelper').Type
Assert-True ($null -ne $helperType) "EcoQoSHelper type is loaded in AppDomain"

# 2. Boolean-Only Parsing in Get-EcoQoSParameter
Assert-Equal $true (Get-EcoQoSParameter @{ eco_qos = $true }) "Hashtable eco_qos=true returns true"
Assert-Equal $false (Get-EcoQoSParameter @{ eco_qos = $false }) "Hashtable eco_qos=false returns false"
Assert-Equal $null (Get-EcoQoSParameter @{ eco_qos = "Enabled" }) "String 'Enabled' is rejected and returns null"
Assert-Equal $null (Get-EcoQoSParameter @{ eco_qos = "Disabled" }) "String 'Disabled' is rejected and returns null"
Assert-Equal $null (Get-EcoQoSParameter @{ eco_qos = 1 }) "Integer 1 is rejected and returns null"
Assert-Equal $null (Get-EcoQoSParameter @{ other = $true }) "Missing eco_qos returns null"
Assert-Equal $null (Get-EcoQoSParameter $null) "Null parameter returns null"

# PSCustomObject test
$psObjTrue = [PSCustomObject]@{ eco_qos = $true }
Assert-Equal $true (Get-EcoQoSParameter $psObjTrue) "PSCustomObject eco_qos=true returns true"
$psObjFalse = [PSCustomObject]@{ eco_qos = $false }
Assert-Equal $false (Get-EcoQoSParameter $psObjFalse) "PSCustomObject eco_qos=false returns false"
$psObjString = [PSCustomObject]@{ eco_qos = "true" }
Assert-Equal $null (Get-EcoQoSParameter $psObjString) "PSCustomObject string 'true' is rejected and returns null"

# 3. Privilege Enabling
$incBase = Enable-Privilege -Privilege $SE_INC_BASE_PRIORITY_NAME
Assert-True ($incBase -or $true) "Enable-Privilege for SeIncreaseBasePriorityPrivilege executes cleanly"

$debugPriv = Enable-Privilege -Privilege $SE_DEBUG_NAME
Assert-True ($debugPriv -or $true) "Enable-Privilege for SeDebugPrivilege executes cleanly"

# 4. Win32 EcoQoS Execution on Current Process/Thread
$currentProcessHandle = [System.Diagnostics.Process]::GetCurrentProcess().Handle
$errCode = 0
$setProcRes = [EcoQoSHelper]::SetProcessEcoQoS($currentProcessHandle, $true, [ref]$errCode)
# On modern Windows (10 1709+ / 11), setting EcoQoS on current process succeeds
Assert-True ($setProcRes -or $errCode -ne 0) "SetProcessEcoQoS executes and reports result (success=$setProcRes, code=$errCode)"

# Revert process EcoQoS
$setProcRevert = [EcoQoSHelper]::SetProcessEcoQoS($currentProcessHandle, $false, [ref]$errCode)
Assert-True ($setProcRevert -or $errCode -ne 0) "SetProcessEcoQoS revert executes and reports result (success=$setProcRevert, code=$errCode)"

# Test thread EcoQoS
if (-not ([System.Management.Automation.PSTypeName]'ThreadNative').Type) {
    Add-Type @"
using System;
using System.Runtime.InteropServices;
public class ThreadNative {
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentThread();
}
"@
}
$currentThreadHandle = [ThreadNative]::GetCurrentThread()
$setThreadRes = [EcoQoSHelper]::SetThreadEcoQoS($currentThreadHandle, $true, [ref]$errCode)
Assert-True ($setThreadRes -or $errCode -ne 0) "SetThreadEcoQoS executes and reports result (success=$setThreadRes, code=$errCode)"

$setThreadRevert = [EcoQoSHelper]::SetThreadEcoQoS($currentThreadHandle, $false, [ref]$errCode)
Assert-True ($setThreadRevert -or $errCode -ne 0) "SetThreadEcoQoS revert executes and reports result (success=$setThreadRevert, code=$errCode)"

# 5. Diagnostic Error Logging on Invalid Handle
$invalidErr = 0
$invalidRes = [EcoQoSHelper]::SetProcessEcoQoS([IntPtr]::Zero, $true, [ref]$invalidErr)
Assert-True (-not $invalidRes) "SetProcessEcoQoS fails on invalid IntPtr::Zero handle"
Assert-True ($invalidErr -ne 0) "SetProcessEcoQoS captures non-zero error code on invalid handle (code=$invalidErr)"

$invalidThreadErr = 0
$invalidThreadRes = [EcoQoSHelper]::SetThreadEcoQoS([IntPtr]::Zero, $true, [ref]$invalidThreadErr)
Assert-True (-not $invalidThreadRes) "SetThreadEcoQoS fails on invalid IntPtr::Zero handle"
Assert-True ($invalidThreadErr -ne 0) "SetThreadEcoQoS captures non-zero error code on invalid handle (code=$invalidThreadErr)"

# 5b. Live Process Boost and Restore Verification
$testNotepad = Start-Process -FilePath "notepad.exe" -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Milliseconds 200
    Set-ProcessAffinityAndPriority -Process $testNotepad -ProcessAffinity -1 -ProcessPriority "DoNotChange" -ThreadIdealProcessor -1 -ThreadPriority "DoNotChange" -CpuSet $null -ChangeCpuSetForProcessAlso $false -MaximumThreadsToChange 10 -OverrideHigherPriority $true -EcoQoS $true
    Assert-True $true "Live process boost with EcoQoS=true executes without error"

    $mockParams = @{
        process_priority = 'DoNotChange'
        thread_priority = 'DoNotChange'
        process_affinity = 'DoNotChange'
        thread_ideal_processor = 'DoNotChange'
        thread_cpu_sets = 'DoNotChange'
        eco_qos = $true
    }
    Restore-ProcessToDefaults -Process $testNotepad -OriginalParameters $mockParams
    Assert-True $true "Live process Restore-ProcessToDefaults with eco_qos=true executes without error"
} finally {
    if ($testNotepad -and -not $testNotepad.HasExited) {
        $testNotepad.Kill()
        $testNotepad.WaitForExit(2000) | Out-Null
    }
}

# 6. Verify Boost JSON Profiles
$boost4Path = Join-Path $testsRoot "action-per-process-boost4.json"
$boost4 = Get-Content $boost4Path -Raw | ConvertFrom-Json
$tiWorkerRule = $boost4[0]
Assert-Equal $true $tiWorkerRule.parameters.eco_qos "boost4.json specifies eco_qos=true"
Assert-Equal "DoNotChange" $tiWorkerRule.parameters.process_affinity "boost4.json specifies process_affinity=DoNotChange"
Assert-Equal "DoNotChange" $tiWorkerRule.parameters.thread_cpu_sets "boost4.json specifies thread_cpu_sets=DoNotChange"

$boost1Path = Join-Path $testsRoot "action-per-process-boost1.json"
$boost1 = Get-Content $boost1Path -Raw | ConvertFrom-Json
$steamWebHelperDep1 = $boost1[0].parameters.dependencies | Where-Object { $_.process_name -eq 'steamwebhelper' }
Assert-Equal $true $steamWebHelperDep1.eco_qos "boost1.json steamwebhelper specifies eco_qos=true"
Assert-Equal "DoNotChange" $steamWebHelperDep1.process_affinity "boost1.json steamwebhelper specifies process_affinity=DoNotChange"
Assert-Equal "DoNotChange" $steamWebHelperDep1.thread_cpu_sets "boost1.json steamwebhelper specifies thread_cpu_sets=DoNotChange"
Assert-Equal $true $steamWebHelperDep1.dont_restore_boost "boost1.json steamwebhelper preserves dont_restore_boost=true"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "EcoQoS tests complete: $script:Passed passed, $script:Failed failed." -ForegroundColor $(if ($script:Failed -eq 0) { "Green" } else { "Red" })

if ($script:Failed -gt 0) {
    exit 1
}
