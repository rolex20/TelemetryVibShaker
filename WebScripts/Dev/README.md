# Portable ZIP test steps (PowerShell 5.1)

1. Zip/extract the `WebScripts` folder anywhere.
2. Optionally copy `config/local.json.example` to `config/local.json` and override paths/names.
3. Open Windows PowerShell 5.1.
4. `cd <extract>\WebScripts`
5. Run `powershell -ExecutionPolicy Bypass -File .\Dev\Audit-Portable.ps1`
6. Run `powershell -ExecutionPolicy Bypass -File .\Dev\SmokeTest-Portable.ps1`
7. Run `powershell -ExecutionPolicy Bypass -File .\WaitFor-Json-Commands.ps1`

## local.json override example

```json
{
  "paths": {
    "commandJson": "C:/MyPrograms/wamp/www/remote_control/command.json",
    "wtMissionJson": "C:/MyPrograms/wamp/www/warthunder/mission_data.json",
    "wtUserMissionsDir": "C:/MyPrograms/Steam/steamapps/common/War Thunder/UserMissions"
  },
  "names": {
    "ipcPipeName": "ipc_pipe_vr_server_commands",
    "ipcMutexName": "ipc_pipe_vr_server_mutex"
  }
}
```
