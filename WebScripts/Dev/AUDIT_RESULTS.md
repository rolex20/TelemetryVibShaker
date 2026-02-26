# Local Execution Results (Codex environment)

The container does not have PowerShell (`pwsh`) installed, so runtime smoke/audit scripts could not be executed here.

## Attempted commands

- `pwsh -NoProfile -File WebScripts/Dev/SmokeTest-Portable.ps1`
  - Result: `bash: command not found: pwsh`
- `pwsh -NoProfile -File WebScripts/Dev/Audit-Refactor.ps1`
  - Result: `bash: command not found: pwsh`

## Static shell checks executed

- `rg -n 'Include-Script|\$search_paths' WebScripts --glob '*.ps1'`
  - Only expected hits in `WebScripts/Dev/Audit-Refactor.ps1` (test pattern literals) and `WebScripts/ps_scripts/Include-Script.ps1` (legacy file retained).
- `rg -n "\.\s+\"[A-Z]:\\\\|\.\s+'[A-Z]:\\\\" WebScripts --glob '*.ps1'`
  - No active script hits outside Dev tooling.
