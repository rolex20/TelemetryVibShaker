# Portable WebScripts Dev Checks

1. Download the `refactor/portable-webscripts-module` branch ZIP and extract it.
2. Optionally copy `WebScripts/config/local.json.example` to `WebScripts/config/local.json` and customize paths/names.
3. Run `WebScripts/Dev/SmokeTest-Portable.ps1`.
4. Run `WebScripts/Dev/Audit-Refactor.ps1`.
5. Run `WebScripts/WaitFor-Json-Commands.ps1`.
