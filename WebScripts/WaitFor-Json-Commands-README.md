# WaitFor-Json-Commands.ps1

I built this watcher to keep my sim-rig alive while I am strapped into VR: it listens for new JSON commands, boosts the right threads, and yells if anything misbehaves so I never have to rip off the headset mid-sortie.

**Tech flex:** PowerShell 7+, CIM/`System.Threading.Mutex` eventing, `System.Diagnostics` affinity tuning, Start-ThreadJob IPC server, JSON-driven orchestration, and WAMP/Apache hand-offs from PHP front ends.

## What this script does (and why it matters)
- **Sets its own priorities and affinities** so the watcher stays on efficiency cores and never steals perf-core time from the sim, preventing stutters when VR is already tight. The helper `SetAffinityAndPriority.ps1` and `Check-Admin-Privileges.ps1` keep the process pinned and safe.
- **Single-instance guard** via a named mutex so two watchers never collide and double-apply affinity changes.
- **Remote command execution** by tailing `command.json` and delegating to `Process-CommandFromJson.ps1`, which can launch/kill apps, move windows, change power plans, or broadcast IPC messages—all without leaving VR.
- **War Thunder mission automation** by watching `mission_data.json` and invoking `WT_MissionType1.ps1` to regenerate missions on the fly.
- **Power plan auto-switching** with `Get-ProcessWatchers` and `Set-GamePowerScheme.ps1`, meaning MSFS/DCS/Warthunder hit High Performance the moment they spawn and fall back to Balanced afterward.
- **IPC side channel** spun up by `Declare-IPC-Server-Action.ps1` to feed debug data (thread times, watchdog checks) without blocking the main file watchers.
- **Watchdog and restart safety net** (`Watchdog-Operations.ps1` plus `Send-IPC-ExitCommand.ps1`) that re-arms the watcher if filesystem events ever hang—critical when flightsim plugins misbehave mid-session.
- **War Thunder distance monitor** (`Monitor-War-Thunder-Distance-Multiplier.ps1`) to catch the game silently resetting my preferred VR scale.

## Architecture and file map
- **Entry point:** `WaitFor-Json-Commands.ps1` wires everything together: loads shared helpers via `Include-Script.ps1`, sets CPU affinity, creates mutex, spins up file watchers, and maintains the watchdog loop.
- **Command pipeline:** `Process-CommandFromJson.ps1` reads JSON commands and calls into:
  - `Set-ForegroundProcess.ps1`, `Set-Minimize.ps1`, `Set-Maximize.ps1`, `Set-WindowsPosition.ps1`, `Get-WindowLocation.ps1` for window control.
  - `Set-PowerScheme.ps1`, `Set-IdealProcessor.ps1`, `Set-GamePowerScheme.ps1`, `SetAffinityAndPriority.ps1` for power/affinity tweaks (great for fixing core assignment when the sim scheduler drifts to E-cores).
  - `Send-MessageViaPipe.ps1` and `Declare-IPC-Server-Action.ps1` for pipe-based telemetry and long-running stats like `Show-CPU-Time-PerProcess.ps1`.
- **Game-aware config:** `Gaming-Programs.ps1` defines the authoritative list of games, power schemes, boost JSONs (`action-per-process-boost*.json`), and auxiliary tools to auto-launch. Add or tweak games here first; everything else consumes this table.
- **File watchers:** `Get-RenamesWatcher.ps1` wraps `FileSystemWatcher` to listen for rename events from PHP pages (remote control and War Thunder mission forms).
- **Support utilities:** `Watchdog-Operations.ps1` keeps the watchers alive; `MonitorJoystickGremlin.ps1` and other helpers can be dropped in via `Include-Script.ps1` without touching the entry script.

## Common tweaks
- **Add a new game or change power plans:** edit `$Global:GameProfiles` in `ps_scripts/Gaming-Programs.ps1` to set High Performance/ Balanced plans, boost JSON, and any auxiliary launchers.
- **Create a new boost profile:** copy an existing `action-per-process-boost*.json` and adjust thread affinities/priorities per process; reference it from `GameProfiles` or the PHP remote commands.
- **Adjust watchdog cadence:** tune intervals in `Watchdog-Operations.ps1` and the `ScheduleWatchdogCheck` calls inside `Process-CommandFromJson.ps1`.

## Future plans
1. **Modularize watchers**: split file-system, process, and IPC watchers into independent modules with unit tests so a crash in one cannot starve the others.
2. **Config over code**: move hard-coded paths (WAMP roots, JSON names, boost files) into a single JSON/YAML config to reduce brittle string edits.
3. **Stronger error telemetry**: push watcher health and last command status into a minimal web dashboard/API so I can see failures from the phone while in VR.
4. **Per-core policy presets**: generate affinity masks dynamically based on detected CPU topology instead of fixed masks, keeping pace with new Intel/AMD layouts.
5. **Remove spaghetti includes**: replace chained `Include-Script` calls with a manifest loader and Pester tests to ensure every dependency resolves before the watcher arms.
