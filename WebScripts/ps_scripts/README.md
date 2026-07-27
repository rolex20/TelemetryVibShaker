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
- Loads shared helpers via deterministic `$PSScriptRoot` dot-sourcing
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
- Optional helpers can be dropped in and wired via explicit `Join-Path $PSScriptRoot ...` dot-sourcing without bloating the entry script.

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

### Auxiliary program lifecycle formats

`AuxPrograms` accepts legacy strings, two shorthand matcher formats, and structured
objects in the same JSON array. All examples below use JSON escaping, so Windows path
backslashes are doubled.

#### Legacy path string

```json
"AuxPrograms": [
  "C:\\Users\\ralch\\Desktop\\Disable-Antivirus.ps1.lnk"
]
```

Legacy strings preserve the original behavior exactly:

- launch the path every time the monitored game starts;
- do not look for an existing process;
- do not track ownership;
- never stop the launched process.

This is equivalent to `LaunchMode = Always` and `StopMode = Never`.

#### Executable shorthand

```json
"AuxPrograms": [
  "[FanatecMonitor.exe]C:\\Users\\ralch\\Desktop\\C-Fanatec Monitor.lnk"
]
```

The value inside `[...]` is the process-name matcher. The `.exe` suffix is optional,
and matching is case-insensitive. The launch path is skipped when at least one matching
process is already running.

Executable shorthand always means `LaunchMode = IfNotRunning` and `StopMode = Never`.
It suppresses duplicates but never claims or terminates a process. A short in-memory
pending marker also prevents simultaneous game-start callbacks from relaunching a
shortcut before its target becomes visible.

#### PowerShell-script shorthand

Prefer a full script path:

```json
"AuxPrograms": [
  "[ps1:C:\\MyPrograms\\Helpers\\MyHelper.ps1]C:\\Users\\ralch\\Desktop\\My Helper.lnk"
]
```

A filename-only matcher is also supported:

```json
"AuxPrograms": [
  "[ps1:MyHelper.ps1]C:\\Users\\ralch\\Desktop\\My Helper.lnk"
]
```

The watcher searches `powershell.exe` processes through `Win32_Process` and inspects
their command lines. Full paths are normalized for slash, quote, and case differences.
Filename-only matching uses complete path-leaf boundaries, so `MyHelper.ps1` does not
match `NotMyHelper.ps1`. Use a full path when scripts in different directories may have
the same filename.

PowerShell shorthand also always means `IfNotRunning` plus `Never`. It intentionally
targets Windows PowerShell 5.1 (`powershell.exe`), not every PowerShell host.

If a shortcut runs a temporary script that starts another executable and then exits,
the script is no longer represented by a running PowerShell command line. Match the
persistent child executable instead.

Strings beginning with `[` are treated as lifecycle shorthand. Malformed bracket
syntax is logged and skipped rather than silently launched as a legacy path.

#### Structured executable definition

```json
"AuxPrograms": [
  {
    "Id": "FanatecMonitor",
    "Path": "C:\\Users\\ralch\\Desktop\\C-Fanatec Monitor.lnk",
    "MatchType": "ProcessName",
    "ProcessName": "FanatecMonitor.exe",
    "LaunchMode": "IfNotRunning",
    "StopMode": "OwnedOnly",
    "StartupTimeoutSeconds": 10
  }
]
```

#### Structured PowerShell definitions

Full-path form:

```json
"AuxPrograms": [
  {
    "Id": "MyPowerShellHelper",
    "Path": "C:\\Users\\ralch\\Desktop\\My Helper.lnk",
    "MatchType": "PowerShellScript",
    "ScriptPath": "C:\\MyPrograms\\Helpers\\MyHelper.ps1",
    "PowerShellHostProcessName": "powershell.exe",
    "LaunchMode": "IfNotRunning",
    "StopMode": "OwnedOnly",
    "StartupTimeoutSeconds": 10
  }
]
```

Filename-only form:

```json
"AuxPrograms": [
  {
    "Id": "MyPowerShellHelper",
    "Path": "C:\\Users\\ralch\\Desktop\\My Helper.lnk",
    "MatchType": "PowerShellScript",
    "ScriptName": "MyHelper.ps1",
    "PowerShellHostProcessName": "powershell.exe",
    "LaunchMode": "IfNotRunning",
    "StopMode": "Never",
    "StartupTimeoutSeconds": 10
  }
]
```

Structured fields:

- `Id` is optional but strongly recommended. IDs are case-insensitive and identify one
  logical shared auxiliary. Without an ID, identity is derived from the normalized matcher.
  Duplicate or incompatible definitions for one identity are logged and skipped while active.
- `Path` and `MatchType` are required. Missing paths or malformed entries are logged and
  skipped without stopping the event handler.
- `MatchType` is `ProcessName` or `PowerShellScript`.
- `ScriptPath` takes precedence when both `ScriptPath` and `ScriptName` are supplied.
- `PowerShellHostProcessName` defaults to `powershell.exe`; a different host is searched
  only when explicitly configured.
- `LaunchMode` is `Always` or `IfNotRunning`. Structured entries default to `Always`.
- `StopMode` is `Never` or `OwnedOnly`. Structured entries default to `Never`.
- `StartupTimeoutSeconds` is a non-negative integer and defaults to 10.
- The profile-level `AuxProgramsDelaySeconds` and `WindowStyle` still apply to all formats.

#### Exact ownership and shared consumers

`OwnedOnly` never means "stop everything with this name." Before launching, the watcher
captures matching PIDs. After launching, it polls for a short bounded window and records
only new matching PIDs plus their creation times. This before/after discovery is required
for `.lnk` files because `Start-Process -PassThru` may describe a shell or intermediary
launcher instead of the persistent target.

On game exit, the watcher uses `Stop-Process -Id` only after the exact PID, matcher, and
creation time still agree. A pre-existing process is never claimed. An unverifiable,
reused, or already-replaced PID is left running.

For `IfNotRunning + OwnedOnly`, monitored game PIDs are consumers of one logical
auxiliary. Multiple games or game instances can share it. The owned helper remains
running until the final consumer exits. For `Always + OwnedOnly`, each game PID owns
only the new process instances discovered for its own launch.

Ownership is kept in memory. If the watcher restarts, existing auxiliaries are treated
as pre-existing and will not be terminated. The general watcher cleanup path also does
not stop auxiliaries, because a configuration or watchdog restart may occur while a game
is still running. Leaving an unverifiable helper running is safer than terminating an
unrelated process.

Run the dependency-free lifecycle tests with Windows PowerShell 5.1:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File ".\tests\Aux-Programs.Tests.ps1"
```

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
- optional per-dependency `dont_restore_boost` flag (`true/false`):
  - when `true`, that dependency is skipped by stop-time restore logic
  - when key is missing, restore behavior stays unchanged (backward compatible default)
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
