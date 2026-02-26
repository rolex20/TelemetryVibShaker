# Portable WebScripts validation

## ZIP test steps (Windows PowerShell 5.1)
1. Download the branch as a ZIP and extract it to any folder.
2. Open Windows PowerShell 5.1.
3. `cd <extract-folder>\WebScripts`
4. Run `./WaitFor-Json-Commands.ps1`
5. Confirm startup does not auto-run CPU monitor output and does not fail on affinity warnings.

## Dev checks
- `./WebScripts/Dev/Audit-Portable.ps1`
- `./WebScripts/Dev/SmokeTest-Portable.ps1`
