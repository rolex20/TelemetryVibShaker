# WebScripts — VR-Friendly Remote Control & Windows Automation Orchestrator

## Tech-Flex
- **Atomic File-Watcher IPC Architecture:** Employs zero-bounce `.tmp` ➔ `.json` file renames to trigger PowerShell `FileSystemWatcher` event handlers without I/O flapping or partial read races.
- **Asynchronous Named-Pipe Inter-Process Communication:** Multi-threaded PowerShell IPC pipe listener (`Stutter-Hunter-IPC.ps1`) handling instant command routing and status replies (`JSONB64`).
- **Dynamic Process QoS & CPU Set Policy Router:** Event-driven process tracking that applies per-game CPU affinity masks, priority classes (`AboveNormal`/`High`), and EcoQoS policies upon game launch.
- **Automated Windows Power Plan Controller:** Direct WMI/C# interop switching active Windows Power Schemes (`High Performance`, `Balanced`, `Balanced Max 80`) dynamically.
- **Modular Multi-Host Profile Engine:** Schema-driven host profiles (`hosts.config.json`) customizing watcher modules and hardware affinity per machine.

---

## Overview & Purpose

WebScripts is the "glue layer" for my gaming and VR rigs. It combines lightweight web interfaces that emit **atomic JSON commands** with a PowerShell orchestrator that listens for **file rename events**, **process start/stop traces**, and **named-pipe IPC messages**.

When you're wearing a VR headset, taking off the headset to alt-tab, move windows, adjust power plans, or restart a hung sim completely ruins the immersion. WebScripts allows controlling the entire rig from a phone or tablet.

> [!NOTE]
> **Primary Orchestrator:** [`ps_scripts/Start-CommandWatchers.ps1`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/WebScripts/ps_scripts/README.md) is the main engine. It runs standalone or pairs with the web UI modules (`remote_control` and `warthunder`).

---

## Folder Map & Module Inventory

### `ps_scripts/` — The PowerShell Engine
- **[ps_scripts/README.md](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/WebScripts/ps_scripts/README.md):** Main orchestrator and background services.
- **`Start-CommandWatchers.ps1`:** Main entry point listening for JSON renames, process start/stop events, and named-pipe IPC commands.
- **`Gaming-Programs.ps1`:** Per-host game profiles (power schemes, process boosts, auxiliary tool launchers).
- **`config/hosts.config.json`:** Per-machine feature toggles and CPU tuning overrides.

### `remote_control/` — VR Phone Interface
- **[remote_control/README.md](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/WebScripts/remote_control/README.md):** Web-based remote control panel.
- **`commander.php`:** Main PHP control panel for launching/killing sims, moving windows, toggling power plans, applying boost profiles, and sending named-pipe messages.
- **`commander_by_*.php`:** Experimental AI-generated UI variants (`gemini`, `claude`, `gpt`) exploring mobile layouts.
- **`pipetest.php` & `echo.php`:** Internal development tools for testing pipe communication.

### `warthunder/` — War Thunder Mission Tooling
- **[warthunder/README.md](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/WebScripts/warthunder/README.md):** Custom mission generator for dogfight practice in VR.
- **`dogfight_generator.php`:** PHP interface that builds dogfight parameters, saves options to cookies, and renames `mission_data.tmp` ➔ `mission_data.json` for instant mission updates.

### Utilities
- **`update_unzip_here.ps1`:** Deployment helper script for extracting and distributing updated WebScripts packages across multiple gaming PCs.

---

## How the Modules Talk (The 10-Second Architecture)

```
[Phone Web UI (PHP)]
       │ (1. Writes command.tmp)
       ▼
[Atomic Rename: command.tmp ➔ command.json]
       │ (2. Triggers FileSystemWatcher event)
       ▼
[Start-CommandWatchers.ps1 (PowerShell)]
       │ (3. Dispatches command type)
       ├─► RUN / KILL / MAXIMIZE ➔ Process Control
       ├─► POWERSCHEME ➔ Set-PowerScheme.ps1
       ├─► GAME_BOOST ➔ CPU Affinity & QoS Boost
       └─► PIPE ➔ Send-MessageViaPipe.ps1 ➔ TelemetryVibShaker / Exporters
```

---

## CPU QoS Implementation Status & Roadmap ToDos

### CPU QoS Status
- **System-Wide Orchestration:** `ps_scripts` manages system-wide CPU QoS, affinity masks, and process priorities for all running applications based on host profiles in `hosts.config.json` and `Gaming-Programs.ps1`.

### ToDos & Recommended Improvements
- [x] Implement atomic `.tmp` ➔ `.json` file rename watcher.
- [x] Implement asynchronous named-pipe IPC server (`Stutter-Hunter-IPC.ps1`).
- [ ] **Unified Web UI Controller:** Refactor `remote_control` PHP scripts into a single modular controller class, separating UI rendering from command serialization.
- [ ] **AJAX Status Polling:** Add live AJAX polling to `commander.php` to display current active power scheme, CPU load, and pipe watchdog status without refreshing the page.
- [ ] **OS Reboot Trigger:** Add explicit `REBOOT` action to `Process-CommandFromJson.ps1` calling `shutdown.exe /r /t 0` for one-tap VR recovery from hard game hangs.

---

## Notes For Future AI Agents: PowerShell Concurrency Patterns

This project has a few known-good concurrency patterns. Prefer these before inventing a new background architecture:

- **`Start-ThreadJob` is isolated from parent `$Global:*` state.** A thread job runs in a separate runspace; do not expect it to see `$Global:WebScriptsConfig`, `$Global:GameProfiles`, or other watcher globals unless values are passed explicitly or the script is designed to bootstrap itself. This was verified with an isolated probe: a thread job could not read parent `$Global:WebScriptsConfig.paths.seatbeltWav`.
- **Use `Start-ThreadJob -StreamingHost $Host` for long-lived service loops that must print to the same console.** This pattern is already used successfully by the IPC server. Without `-StreamingHost`, host output can be buffered or disconnected from the main watcher console.
- **For delayed one-shot actions inside the watcher process, prefer `System.Timers.Timer` + `Register-ObjectEvent`.** This matches the existing gameplay runtime timer pattern in `Set-GamePowerScheme.ps1`. Timer event actions can access watcher globals, but still pass immutable action inputs through `MessageData` so each scheduled action is explicit and stable.
- **Timer event actions should re-import their dependencies.** Event actions may run outside the original function scope, so dot-source required helper scripts inside the action, as the runtime timer does with `Write-VerboseDebug.ps1` and `Cpu-Snapshots.ps1`.
- **Use `MessageData` for event callbacks.** Process watchers, timers, and filesystem watchers should pass paths, PIDs, process names, config-derived values, and shared lifecycle state through `MessageData` instead of relying on ambient variables.
- **Keep long waits out of process watcher callbacks.** A long `Start-Sleep` in a CIM process event handler can block other process lifecycle work. Use a cancellable timer/event state keyed by PID for delayed actions such as long game boost delays.
- **Clean up subscriptions and timers.** Store source identifiers and timer objects in PID-keyed state, then `Stop()`, `Unregister-Event`, and `Dispose()` them on process exit or completion to avoid stale callbacks.

When in doubt, inspect the existing patterns in `Start-CommandWatchers.ps1`, `Get-ProcessWatcher.ps1`, `Watchdog-Operations.ps1`, and `Set-GamePowerScheme.ps1` before introducing new multithreading behavior.
