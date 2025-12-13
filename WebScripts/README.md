# WaitFor-Json-Commands.ps1

WaitFor-Json-Commands.ps1 is the always-on orchestration script that keeps my VR flight-sim rig optimized by reacting to filesystem events, process lifecycle changes, and IPC messages in real time.
**Advanced stack:** PowerShell 7, `System.IO.FileSystemWatcher`, CIM/WMI process trace events, named-pipe IPC, background thread jobs, custom C# interop for process priority/affinity, JSON-driven command routing, and text-to-speech notifications.

## What the script does and why it helps
- **Launches with safe defaults for VR workloads.** Forces the host session onto efficiency cores, idles its own priority, and applies Process Lasso–style background tuning via `SetAffinityAndPriority` so game-critical threads keep the performance cores free.【F:WebScripts/WaitFor-Json-Commands.ps1†L34-L61】【F:WebScripts/ps_scripts/SetAffinityAndPriority.ps1†L4-L61】
- **Enforces a single owner.** Uses a mutex to prevent multiple watchers, avoiding duplicate boosts or conflicting affinity changes when starting simulations.【F:WebScripts/WaitFor-Json-Commands.ps1†L64-L86】
- **Listens for remote JSON commands.** Watches `command.json` for rename events and routes them through `Process-CommandFromJson`, enabling remote RUN/KILL, window focus, window positioning, power-scheme swaps, IPC pipe calls, and per-game boost/restore routines—all of which keep VR launch/exit flows hands-free.【F:WebScripts/WaitFor-Json-Commands.ps1†L95-L125】【F:WebScripts/ps_scripts/Process-CommandFromJson.ps1†L17-L124】
- **Auto-generates War Thunder missions.** When mission JSON drops from the PHP front-end, the watcher triggers `WT_MissionType1.ps1` to rebuild scenarios without manual clicks.【F:WebScripts/WaitFor-Json-Commands.ps1†L128-L147】
- **Auto-tunes power schemes per game.** CIM process start/stop events feed into `Set-GamePowerScheme.ps1` so each title immediately switches to the right Windows power plan and reverts on exit—essential for smooth VR frame times.【F:WebScripts/WaitFor-Json-Commands.ps1†L150-L166】【F:WebScripts/ps_scripts/Get-ProcessWatcher.ps1†L9-L30】
- **Exposes an IPC command server.** A background thread job registers named-pipe actions for low-latency commands such as thread-time inspection, keeping the main watcher responsive.【F:WebScripts/WaitFor-Json-Commands.ps1†L169-L174】
- **Guards against silent failures.** A watchdog loop continuously injects sentinel JSON commands and expects responses; if event handling stalls, it relaunches itself to restore automation before the next sortie.【F:WebScripts/WaitFor-Json-Commands.ps1†L180-L201】【F:WebScripts/ps_scripts/Watchdog-Operations.ps1†L19-L63】

## Architecture and file layout
- `WebScripts/WaitFor-Json-Commands.ps1` — entry point; composes watchers, IPC thread, and watchdog lifecycle.【F:WebScripts/WaitFor-Json-Commands.ps1†L34-L201】
- `WebScripts/ps_scripts/Include-Script.ps1` — dynamic loader to resolve helper scripts from multiple install paths (used by every module).【F:WebScripts/WaitFor-Json-Commands.ps1†L16-L33】
- `WebScripts/ps_scripts/` — helper modules:
  - `Process-CommandFromJson.ps1` routes remote commands to window control, power schemes, game boosts, and IPC sends.【F:WebScripts/ps_scripts/Process-CommandFromJson.ps1†L17-L124】
  - `Get-RenamesWatcher.ps1` defines rename/modify filesystem watcher helpers used by the main loop.【F:WebScripts/ps_scripts/Get-RenamesWatcher.ps1†L43-L80】
  - `Get-ProcessWatcher.ps1` + `Gaming-Programs.ps1` subscribe to CIM start/stop events for configured game executables.【F:WebScripts/ps_scripts/Get-ProcessWatcher.ps1†L1-L30】【F:WebScripts/ps_scripts/Gaming-Programs.ps1†L34-L94】
  - `SetAffinityAndPriority.ps1` enforces efficiency-core affinity and background/idle priority for self-preservation.【F:WebScripts/ps_scripts/SetAffinityAndPriority.ps1†L4-L61】
  - `Watchdog-Operations.ps1` injects heartbeat JSON commands and signals restarts when event queues stall.【F:WebScripts/ps_scripts/Watchdog-Operations.ps1†L19-L63】
  - Additional utilities cover window placement, power plan swaps, thread-time inspection, named-pipe messaging, and mission generation (see individual `*.ps1` files under `ps_scripts`).
- `WebScripts/remote_control/` — PHP front-end that writes JSON command files consumed by the watcher.

## Common customization points
- **Add or change a game profile** — Edit `ps_scripts/Gaming-Programs.ps1`'s `$Global:GameProfiles` hash to set start/stop power schemes, boost JSONs, spoken prompts, or auxiliary auto-launch programs. New entries are immediately picked up by CIM watchers without touching other scripts.【F:WebScripts/ps_scripts/Gaming-Programs.ps1†L34-L83】
- **Tune per-game boosts** — Update or add `action-per-process-boost*.json` files referenced by `Process-CommandFromJson` to pin threads or priorities for specific executables.
- **Adjust watchdog cadence** — Modify `$wait_minutes` inside `ps_scripts/Watchdog-Operations.ps1` to change how aggressively the health check fires.【F:WebScripts/ps_scripts/Watchdog-Operations.ps1†L28-L43】
- **Point to a different web root** — Change the `$command_file`/`$mission1` paths in `WaitFor-Json-Commands.ps1` if the PHP layer lives elsewhere.【F:WebScripts/WaitFor-Json-Commands.ps1†L95-L147】

## Future plans (priority order)
1. **Normalize module loading and paths.** Replace hard-coded absolute paths with config files and `$PSScriptRoot`-relative discovery to simplify installs and reduce breakage when moving directories.
2. **Refactor command handling into discrete classes/modules.** Break `Process-CommandFromJson` into focused handlers (window control, power, affinity boosts, IPC) with unit tests to tame growth and prevent spaghetti coupling.
3. **Centralize logging/telemetry.** Wrap `Write-VerboseDebug` with a structured logger (JSON output + rolling files) to troubleshoot watchdog resets and IPC timeouts faster.
4. **Add automated health/self-test.** Provide a `-SelfTest` switch that fires sample JSON commands, validates watcher responses, and reports readiness before launching VR sessions.
5. **Package as a service.** Offer an install script that registers the watcher as a scheduled task or service with proper recovery actions for even better reliability on reboot.
