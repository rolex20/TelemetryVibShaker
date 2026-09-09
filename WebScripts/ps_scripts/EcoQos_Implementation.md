# Walkthrough: Windows EcoQoS (Efficiency Mode / Power Throttling) in Boost Profiles

We have implemented native Windows **EcoQoS** (Efficiency Mode / Power Throttling) into the process scheduling automation framework in [`WebScripts/ps_scripts`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts).

---

## Changes Made

### 1. PowerShell Scheduling Engine
- **[`Set-IdealProcessor.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/Set-IdealProcessor.ps1)**:
  - **P/Invoke & Helper Type (`EcoQoSHelper`)**:
    - Added Win32 P/Invoke declarations for `OpenProcess`, `OpenThread`, `SetProcessInformation`, `SetThreadInformation`, and `CloseHandle`.
    - Declared `PROCESS_POWER_THROTTLING_STATE` and `THREAD_POWER_THROTTLING_STATE` structs with `PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 1`.
    - Implemented `SetProcessEcoQoS(IntPtr hProcess, bool enable, out int errorCode)` using `ProcessPowerThrottling` (`4`).
    - Implemented `SetThreadEcoQoS(IntPtr hThread, bool enable, out int errorCode)` targeting Windows 11 Class `3` with automatic fallback to Windows 10 Class `1`.
  - **Privilege & Cross-User Security**:
    - Defined `$SE_DEBUG_NAME = "SeDebugPrivilege"`.
    - Activated both `$SE_INC_BASE_PRIORITY_NAME` and `$SE_DEBUG_NAME` inside `Set-ProcessAffinityAndPriority` before opening handles.
    - Used targeted least-privilege rights (`PROCESS_SET_INFORMATION = 0x0200` and `THREAD_SET_INFORMATION = 0x0020`).
  - **Boolean Parameter Validation (`Get-EcoQoSParameter`)**:
    - Added helper function accepting strictly boolean `$true` or `$false` (rejecting strings and non-booleans, with `$null` signaling no change).
  - **Process-Level & Thread-Level Application**:
    - Applied `SetProcessEcoQoS` to the process container so that **new threads created by the process automatically inherit EcoQoS**.
    - Applied `SetThreadEcoQoS` to each thread in `$sortedThreads` so that all currently running threads immediately switch to EcoQoS.
    - Added explicit console failure logging in `DarkYellow` if process or thread handle opening or EcoQoS setting fails (`Write-Host "Warning: Failed to set EcoQoS..."`).
  - **Intelligent Restoration (`Restore-ProcessToDefaults`)**:
    - When a boosted process terminates, if `eco_qos: true` was originally set on a dependency, it is restored with `EcoQoS = $false` (unless `dont_restore_boost: true` is active).
  - **Runner Integration (`Run-Actions-Per-Game`)**:
    - Extracts `eco_qos` for both primary games and dependencies and forwards to `Set-ProcessAffinityAndPriority`.

### 2. Watcher Startup
- **[`Start-CommandWatchers.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/Start-CommandWatchers.ps1)**:
  - Dot-sourced `Set-IdealProcessor.ps1` during initialization.
  - Activated `SeDebugPrivilege` and `SeIncreaseBasePriorityPrivilege` at startup directly after admin privilege verification so the process token holds the privileges from launch.

### 3. Boost Configuration Profiles
- **[`action-per-process-boost4.json`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/action-per-process-boost4.json)**:
  - Configured `TiWorker` and `CompatTelRunner` with `"eco_qos": true`, while keeping `"process_affinity": "DoNotChange"`, `"thread_ideal_processor": "DoNotChange"`, and `"thread_cpu_sets": "DoNotChange"`.
- **[`action-per-process-boost1.json`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/action-per-process-boost1.json)**, **[`boost2.json`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/action-per-process-boost2.json)**, **[`boost3.json`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/action-per-process-boost3.json)**:
  - Configured persistent background dependency `steamwebhelper` with `"eco_qos": true`, `"process_affinity": "DoNotChange"`, `"thread_ideal_processor": "DoNotChange"`, and `"thread_cpu_sets": "DoNotChange"`, while preserving `"dont_restore_boost": true`.

### 4. Documentation
- **[`WebScripts/ps_scripts/README.md`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/README.md)**:
  - Documented `"parameters.eco_qos"` in the Formal Parameter Reference table.
  - Updated the Profile Catalog descriptions to reflect EcoQoS usage across `boost1` through `boost4`.

---

## Verification & Test Results

### Automated Test Suite Runs

1. **[`tests/EcoQoS.Tests.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/tests/EcoQoS.Tests.ps1)** (New Dedicated Suite):
   - Verified `EcoQoSHelper` compilation and AppDomain loading.
   - Tested boolean-only parsing (verified strings like `"Enabled"`/`"Disabled"` and integers are rejected).
   - Tested `Enable-Privilege` for `SeDebugPrivilege` and `SeIncreaseBasePriorityPrivilege`.
   - Verified Win32 `SetProcessEcoQoS` and `SetThreadEcoQoS` on live handles.
   - Tested diagnostic error capturing on invalid handles.
   - Tested live process boost (`notepad.exe`) with `EcoQoS = $true` and restore with `Restore-ProcessToDefaults`.
   - Verified JSON schema configurations in `boost1.json` and `boost4.json`.
   - **Result**: `30 passed, 0 failed`.

2. **[`tests/ProcessNames.Tests.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/tests/ProcessNames.Tests.ps1)**:
   - Verified all updated boost profile JSONs (`boost1` through `boost4`) parse cleanly without syntax or schema errors.
   - **Result**: `96 passed, 0 failed`.

3. **[`tests/Delayed-GameBoost.Tests.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/tests/Delayed-GameBoost.Tests.ps1)**:
   - Verified game delayed boosting and seatbelt audio dispatch.
   - **Result**: `13 passed, 0 failed`.

4. **[`tests/Gaming-Programs.Tests.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/tests/Gaming-Programs.Tests.ps1)**:
   - Verified profile resolution and launch timeouts.
   - **Result**: `9 passed, 0 failed`.

5. **[`tests/Aux-Programs.Tests.ps1`](file:///c:/Users/ralch/Documents/VSCode/repos/TelemetryVibShaker/WebScripts/ps_scripts/tests/Aux-Programs.Tests.ps1)**:
   - Verified auxiliary lifecycle, singleton tracking, and stop modes.
   - **Result**: `64 passed, 0 failed`.

**Overall automated test tally: 212 passed, 0 failed.**
