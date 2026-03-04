# WebScripts/ps_scripts — Performance-first Windows watcher toolkit (PowerShell 5.1)

This folder contains the PowerShell side of my “hands-off” gaming/VR rig automation: lightweight watchers that react to **file events**, **process start/stop events**, and **IPC commands** to help me find stutters, diagnose scheduling drift, and apply repeatable tuning without touching the desktop mid-session.

**Tech flex:** deep **Windows scheduling control via PowerShell + C# Win32 interop**—CPU Sets topology discovery and per-process/per-thread CPU-set steering (`GetSystemCpuSetInformation`, `SetProcessDefaultCpuSets`, `SetThreadSelectedCpuSets`) combined with **thread-level EcoQoS / Efficiency Mode** via `SetThreadInformation` (Win11 class 3 → Win10 class 1 fallback) so background/support threads stay “cheap” on hybrid Alder/Raptor Lake rigs; **real-time process lifecycle automation** using `Register-CimIndicationEvent` on `Win32_ProcessStartTrace/StopTrace` to apply per-game policies the moment a sim starts/exits; an **asynchronous Named Pipe IPC layer** (`NamedPipeServerStream`) with explicit **PipeSecurity/ACL**, command parsing, and CPU-time delta introspection for stutter hunting; **kernel32 priority + background I/O tuning** (`SetPriorityClass(PROCESS_MODE_BACKGROUND_BEGIN)` and idle priority) to keep the watcher from stealing performance; and a PS 5.1-friendly **precompiled + disk-cached C# pipeline** that builds an optimized (`/optimize+`) DLL once, reuses it when valid, and loads from a byte array to avoid file locking—so even older laptops don’t pay the “recompile every run” tax. Rounding it out: **event-driven filesystem orchestration** with `FileSystemWatcher` + atomic rename handoffs (PHP/WAMP drops `.tmp` → `.json`), **multi-machine config layering** (defaults + per-host overrides) via `config/hosts.config.json`, **multithreaded background execution** with `Start-ThreadJob`, and **TTS feedback** (System.Speech) so state changes are audible when you’re busy / in VR with the headset strapped to your face.

---

## Why this exists (hybrid-core reality, no marketing)

This setup is optimized today for my Alder/Raptor Lake hybrid gaming CPU. Early on, VR sims + Windows background activity could end up on the wrong cores at the wrong time. I wanted flexibility beyond generic tooling: per-process policies, per-thread steering, and quick visibility into CPU time while the sim is running.

Everything here is modular. You can enable/disable watchers per-machine and customize game profiles to match your rig.

---

## Quick start (PowerShell 5.1)

1. Edit host config: `ps_scripts/config/hosts.config.json`
   - Enable only the watchers you want for your machine.
2. Run: `Start-CommandWatchers.ps1`

> Note on paths: some watchers reference real paths from my rig (ex: WAMP locations). Treat them as working examples—if you enable that watcher, you’ll likely customize the paths.

---

## Architecture and file map

### Entry point / orchestrator
**`Start-CommandWatchers.ps1` wires everything together:**
- Enforces single instance (named mutex)
- Tunes itself to stay out of the way (efficiency-core affinity + idle/background behavior)
- Loads shared helpers via `Include-Script.ps1`
- Conditionally spins up watchers based on `config/hosts.config.json`
- Maintains a watchdog loop and can self-restart if eventing gets stuck

### Event sources (inputs)
There are three main “inputs” that can drive actions:

1) **Process start/stop watcher (game lifecycle)**
- Implemented via CIM indication events on:
  - `Win32_ProcessStartTrace`
  - `Win32_ProcessStopTrace`
- The authoritative game list is `Gaming-Programs.ps1` (see below).
- On start/stop it can switch power schemes and trigger “boost” actions.

2) **Remote JSON command pipeline (file rename handoff)**
- A `FileSystemWatcher` listens for **rename** events (atomic handoff).
- Typical flow: a PHP page writes `command.tmp` then renames to `command.json`.
- On rename, `Process-CommandFromJson.ps1` reads the JSON and dispatches actions.

3) **IPC (named pipe commands)**
- `Declare-IPC-Server-Action.ps1` runs a NamedPipe server (thread job).
- It accepts simple commands (speak, window ops, show-process CPU deltas, etc.).
- Designed as a fast side-channel for “do X now” commands or lightweight introspection.

### Command dispatcher (the “router”)
**`Process-CommandFromJson.ps1` reads JSON commands and calls into:**

- **Window control**
  - `Set-ForegroundProcess.ps1`
  - `Set-Minimize.ps1`
  - `Set-Maximize.ps1`
  - `Set-WindowsPosition.ps1`
  - `Get-WindowLocation.ps1`

- **Power / scheduling / affinity tuning**
  - `Set-PowerScheme.ps1`
  - `Set-GamePowerScheme.ps1`
  - `SetAffinityAndPriority.ps1`
  - CPU-set + hybrid-core helpers (C# `Add-Type` tooling used by the tuning layer)

- **IPC and telemetry**
  - `Send-MessageViaPipe.ps1` (client)
  - `Declare-IPC-Server-Action.ps1` (server)
  - `Show-CPU-Time-PerProcess.ps1` (CPU-time deltas / stutter-hunt support)

- **Boost profiles**
  - JSON-driven profiles: `action-per-process-boost*.json`
  - Used by game profiles and/or remote commands to apply repeatable tuning

### Game-aware configuration (the source of truth)
**`Gaming-Programs.ps1` defines `$Global:GameProfiles`:**
- This is intentionally *host-specific* (my different PCs have different needs).
- It defines:
  - which games are watched
  - start/stop power plans
  - optional `BoostAction` JSON
  - auxiliary tools to auto-launch
  - optional `AuxProgramsDelaySeconds` (integer seconds, `>= 0`, default fallback `5`)
  - optional `WindowStyle` for AuxPrograms (`Normal|Hidden|Minimized|Maximized`, default `Minimized`; invalid values warn and fall back)
- Everything else consumes this table (process watcher queries, boost triggers, etc.).

### File watcher wrapper + support utilities
- **`Get-RenamesWatcher.ps1`** wraps `FileSystemWatcher` to listen for rename events (used by remote control + optional War Thunder modules).
- **`Watchdog-Operations.ps1`** keeps the system honest (detect stuck eventing, help restart cleanly).
- Optional helpers can be dropped in and wired via `Include-Script.ps1` without bloating the entry script.

---

## Game profiles and customization

### `Gaming-Programs.ps1`
This is the place you customize first.

- Add games by adding entries under your machine hostname block.
- Set per-game start/stop power schemes.
- Optionally reference a boost JSON (`action-per-process-boost*.json`).
- Add auxiliary tools to auto-launch with a game if you want.
- Optionally set `AuxProgramsDelaySeconds` per game to control when auxiliaries launch.
- Optionally set `WindowStyle` per game to control how all AuxPrograms windows are shown.

Example profile snippet:
```json
"forza_steamworks_release_final.exe": {
  "NickName": "Forza",
  "Start": "Balanced",
  "Stop": "Balanced",
  "AuxPrograms": [
    "C:\\Users\\ralch\\Desktop\\C-Fanatec Monitor.lnk"
  ],
  "AuxProgramsDelaySeconds": 12,
  "WindowStyle": "Hidden"
}
```

The design is: **my defaults are my rig**, but anyone can fork/tune it to match their own CPU, GPU, VR stack, and “background junk” profile.

---

## Common tweaks (most common edits)

### 1) Add a new game / change power plans
Edit `$Global:GameProfiles` in `ps_scripts/Gaming-Programs.ps1`:
- Choose start/stop schemes (High Performance / Balanced / your custom scheme)
- Optionally attach a `BoostAction`
- Optionally add auxiliary launchers
- Optionally set `AuxProgramsDelaySeconds` for per-game delayed aux launch
- Optionally set `WindowStyle` (`Normal|Hidden|Minimized|Maximized`) for AuxPrograms; default is `Minimized` and invalid values are auto-fallbacked with a warning

### 2) Create a new boost profile
Copy an existing `action-per-process-boost*.json` and adjust:
- per-process priority
- per-process CPU affinity / CPU sets intent
- per-thread steering (when applicable)
Then reference it from:
- `Gaming-Programs.ps1` (`BoostAction`)
- or a remote JSON `GAME_BOOST` command (if you use the web remote)

### 3) Enable/disable watchers per PC (feature flags)
Edit `ps_scripts/config/hosts.config.json`:
- `defaults.features.*` are the baseline
- `machines.<HOSTNAME>.features.*` overrides per machine
This is how you keep one PC minimal (just process watcher + IPC) while another runs optional modules.

### 4) Adjust watchdog behavior / cadence
Two places matter:
- `Watchdog-Operations.ps1` (watchdog logic + check behavior)
- The watchdog scheduling calls inside the watcher/orchestrator loop and/or command dispatcher paths
Goal: keep it responsive but not noisy (event-driven first; watchdog as a safety net, not a poller).

### 5) Update “example paths” for your layout
If you enable the remote JSON watcher or War Thunder file watchers, you’ll likely need to customize file paths (WAMP roots, mission json locations, etc.). The repo contains real working examples from my machines—not a universal layout.

---

## Troubleshooting

- **Nothing happens:** confirm the relevant feature flag is enabled for your hostname in `hosts.config.json`.
- **Remote commands not firing:** validate the watched path (and that your PHP page uses rename handoff).
- **IPC server conflicts:** single-instance protections may prevent a second server from starting.
- **Watcher restarts:** if the watchdog detects stuck event processing, the orchestrator can restart after cleanup.

---

## Future roadmap

- Move remaining hard-coded paths into the main config file (so enabling a watcher never requires editing script strings).
- Detect CPU topology dynamically and generate affinity/cpu-set strategies based on the machine (instead of fixed masks).
- Make each watcher a cleaner module with sharper boundaries and fewer cross-dependencies.
- Lightweight “health/status” output (minimal dashboard/API) for headset-first workflows.
