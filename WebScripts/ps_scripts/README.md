# WebScripts/ps_scripts — Performance-first Windows watcher toolkit (PowerShell 5.1)

This folder contains the PowerShell side of my “hands-off” gaming/VR rig automation: lightweight watchers that react to **file events**, **process start/stop events**, and **IPC commands** to help me find stutters, diagnose scheduling drift, and apply repeatable tuning without touching the desktop mid-session.

**Tech flex:** deep **Windows scheduling control via PowerShell + C# Win32 interop**—CPU Sets topology discovery and per-process/per-thread CPU-set steering (`GetSystemCpuSetInformation`, `SetProcessDefaultCpuSets`, `SetThreadSelectedCpuSets`) combined with **thread-level EcoQoS / Efficiency Mode** via `SetThreadInformation` (Win11 class 3 → Win10 class 1 fallback) so background/support threads stay “cheap” on hybrid Alder/Raptor Lake rigs; **real-time process lifecycle automation** using `Register-CimIndicationEvent` on `Win32_ProcessStartTrace/StopTrace` with instant process termination (`ImmediateKill`) for unwanted background hogs before any side effects fire; an **asynchronous Named Pipe IPC layer** (`NamedPipeServerStream`) with explicit **PipeSecurity/ACL**, command parsing, and CPU-time delta introspection for stutter hunting (`Stutter-Hunter-IPC.ps1`); **kernel32 priority + background I/O tuning** (`SetPriorityClass(PROCESS_MODE_BACKGROUND_BEGIN)` and idle priority) to keep the watcher from stealing performance; **PID-based gameplay runtime telemetry** with persistent disk snapshots (`Cpu-Snapshots.ps1`) to reliably capture CPU/wall-clock usage across process handle exits; and a PS 5.1-friendly **precompiled + disk-cached C# pipeline** (`Import-OptimizedCSharp.ps1`) that builds an optimized (`/optimize+`) DLL once, reuses it when valid, and loads from a byte array to avoid file locking—so even older laptops don’t pay the “recompile every run” tax. Rounding it out: **event-driven filesystem orchestration** with `FileSystemWatcher` + atomic rename handoffs (PHP/WAMP drops `.tmp` → `.json`), **multi-machine host configuration layering** with dynamic hot-reloading (`config/hosts.config.json` + `Get-HostConfig.ps1`), **multithreaded background execution** with `Start-ThreadJob`, and **TTS feedback** (System.Speech) so state changes are audible when you’re busy / in VR with the headset strapped to your face.

---

## Why this exists (hybrid-core reality & multi-machine automation)

This setup is optimized for my Alder/Raptor Lake hybrid gaming CPU as well as my fleet of gaming and utility machines. Early on, VR sims + Windows background activity could end up on the wrong cores at the wrong time. Furthermore, legacy/older laptops (such as test machines with thermal paste issues) needed aggressive background process killing to avoid BSODs and CPU hangs. I wanted flexibility beyond generic tooling: per-process policies, immediate process termination on launch, per-thread steering, auxiliary tool lifecycle management, and quick visibility into CPU time while a sim is running.

Everything here is modular and host-aware. You can enable/disable watchers per-machine and customize game profiles via `config/hosts.config.json` without modifying script code.

---

## Quick start (PowerShell 5.1)

1. Edit host config: `ps_scripts/config/hosts.config.json`
   - Configure global `defaults` or add a host-specific block under `machines.<HOSTNAME>`.
   - Enable only the feature flags (`processWatcher`, `ipcServer`, `remoteCommandsWatcher`, etc.) you want for your machine.
   - Define game profiles, TTS options, auxiliary programs, or `ImmediateKill` targets.
2. Run: `Start-CommandWatchers.ps1`

> **Note on configuration**: `Gaming-Programs.ps1` automatically consumes `config/hosts.config.json` via `Bootstrap-Config` / `Get-WebScriptsConfig`. You do **not** edit `Gaming-Programs.ps1` to configure games—all profiles, power plans, TTS messages, and auxiliary applications are declared in `config/hosts.config.json`.

---

## Architecture and file map

### Entry point / orchestrator
**`Start-CommandWatchers.ps1` wires everything together:**
- Enforces single instance (named mutex protection with explicit owner checks)
- Tunes itself to stay out of the way (efficiency-core affinity + idle/background priority via kernel32 interop)
- Bootstraps host configuration via `Get-HostConfig.ps1` / `Bootstrap-Config`
- Loads shared helpers via deterministic `$PSScriptRoot` dot-sourcing
- Conditionally spins up watchers based on `config/hosts.config.json` feature flags
- Initializes auxiliary program lifecycle state tracking (`New-AuxProgramLifecycleState`)
- Maintains a watchdog loop (`Watchdog-Operations.ps1`) and self-restarts cleanly if eventing gets stuck or critical config sections change

### Multi-Machine Host Configuration (`config/hosts.config.json` & `Get-HostConfig.ps1`)
Configuration is centralized in `config/hosts.config.json` and parsed by `Get-HostConfig.ps1`:
- **Defaults vs Host Overrides**: Base settings land under `defaults`, while host-specific overrides live under `machines.<HOSTNAME>` (e.g. `GALVATRON`, `ALIENWARE-V2`, `HP-PAV-BLACK`).
- **Feature Flags**: Toggles per-machine functionality (`processWatcher`, `ipcServer`, `remoteCommandsWatcher`, `warThunderMissionWatcher`, `warThunderDistanceMonitor`, `watchdog`).
- **Dynamic Hot-Reloading (`Refresh-WebScriptsConfigIfChanged`)**: On every watchdog interval, the watcher checks file timestamps.
  - **Live Updates**: Value-only edits (e.g. TTS speak text, nicknames, boost actions) are applied live without interrupting watchers.
  - **Auto-Restart**: Structural edits (changing feature flags, adding/removing game profiles, modifying paths) automatically trigger a clean orchestrator restart when `restart_on_config_change: true`.

### Event sources (inputs)
There are three main “inputs” that drive actions:

1) **Process start/stop watcher (game lifecycle & immediate kill)**
- Implemented via CIM indication events on `Win32_ProcessStartTrace` and `Win32_ProcessStopTrace` (`Get-ProcessWatcher.ps1`).
- Passes process events to `Set-GamePowerScheme.ps1`.
- **Immediate Kill**: Evaluates `"ImmediateKill": true` on start trace *before* any power scheme, aux launch, boost, or telemetry side-effects occur.
- On standard start/stop events, it handles power scheme switching, stutter hunter enrollment, auxiliary tool launching, boost action execution, and gameplay runtime tracking.

2) **Remote JSON command pipeline (file rename handoff)**
- A `FileSystemWatcher` wrapped by `Get-RenamesWatcher.ps1` listens for **rename** events (atomic handoff).
- Typical flow: a PHP page or TelemetryVibShaker app writes `command.tmp` then renames to `command.json`.
- On rename, `Process-CommandFromJson.ps1` parses the JSON payload and dispatches actions.

3) **IPC (named pipe commands)**
- `Declare-IPC-Server-Action.ps1` runs an asynchronous NamedPipe server in a thread job (`Start-ThreadJob`).
- Accepts simple commands (speak, window positioning, foregrounding, show-process CPU deltas, exit commands).
- Serves as a fast side-channel for real-time control without disk I/O overhead.

---

### File Index & Module Breakdown

| Script / File | Category | Description |
| :--- | :--- | :--- |
| **`Start-CommandWatchers.ps1`** | Orchestrator | Main entry point; handles mutex lock, self-tuning, watcher setup, watchdog loop, and cleanup. |
| **`config/hosts.config.json`** | Configuration | Central multi-machine configuration file for defaults, machine overrides, feature flags, and game profiles. |
| **`Get-HostConfig.ps1`** | Config Engine | JSON parser, hashtable recursive converter, host override merger, and hot-reload diff detector. |
| **`Gaming-Programs.ps1`** | Profile Helper | Exposes profile lookup functions (`Get-StartPowerSchemes`, `Get-GameAuxPrograms`, `Get-ImmediateKill`, etc.) backed by `hosts.config.json`. |
| **`Aux-Programs.ps1`** | Aux Lifecycle | Auxiliary program parser, process/ps1 matcher, PID ownership discovery (`OwnedOnly`), and cleanup controller. |
| **`Set-GamePowerScheme.ps1`** | Lifecycle Handler | Core event handler for process start/stop events; coordinates ImmediateKill, power plans, AuxPrograms, Stutter Hunter, runtime tracking, and two-phase boost actions. |
| **`Cpu-Snapshots.ps1`** | Telemetry | Persistent disk snapshot helper (`Save-GameRuntimeCpuSnapshot`, `Read-GameRuntimeCpuSnapshot`) for tracking process CPU time across exits. |
| **`Stutter-Hunter-IPC.ps1`** | Telemetry / IPC | Centralized IPC coordinator and client script for monitoring CPU time deltas and identifying micro-stutters. |
| **`Stutter-Hunter.ps1`** | Telemetry | Legacy standalone process stutter tracking script. |
| **`Show-CPU-Time-PerProcess.ps1`** | Introspection | Formats and outputs process CPU time deltas over configurable sampling windows. |
| **`Set-PowerScheme.ps1`** | Power Control | Switches Windows power plans (High Performance, Balanced, Power Saver, custom GUIDs). |
| **`Set-IdealProcessor.ps1`** | Scheduling | Configures CPU affinity, thread ideal processors, and process priority. |
| **`SetGet-DefaultCpuSets.ps1`** | Scheduling | Query and set system/process default CPU Sets via Win32 API. |
| **`SetAffinityAndPriority.ps1`** | Scheduling | Self-tuning script to move the watcher onto Efficiency Cores and background priority. |
| **`Import-OptimizedCSharp.ps1`** | Interop Engine | Compiles `/optimize+` C# Win32 interop DLLs on demand with disk caching and byte-array loading to avoid file locking. |
| **`Process-CommandFromJson.ps1`** | Dispatcher | Parses remote control JSON commands (window style, focus, power, boost) and invokes target functions. |
| **`Declare-IPC-Server-Action.ps1`** | IPC Server | Named Pipe server implementation running in a dedicated thread job for async IPC command processing. |
| **`Send-MessageViaPipe.ps1`** | IPC Client | Client script for sending commands to the Named Pipe IPC server. |
| **`Send-IPC-ExitCommand.ps1`** | IPC Client | Client helper to signal the IPC pipe server to terminate gracefully. |
| **`Get-ProcessWatcher.ps1`** | Event Source | Registers CIM indication events on `Win32_ProcessStartTrace` and `Win32_ProcessStopTrace`. |
| **`Get-RenamesWatcher.ps1`** | Event Source | Wraps `FileSystemWatcher` to listen specifically for atomic rename events (`.tmp` → `.json`). |
| **`Watchdog-Operations.ps1`** | Monitoring | Health monitor loop that checks event subscription integrity and signals orchestrator restarts if stuck. |
| **`Check-Admin-Privileges.ps1`** | Utility | Verifies elevation and warns if running without administrative rights required for scheduling/power tweaks. |
| **`Write-VerboseDebug.ps1`** | Utility | Centralized logging helper with timestamp formatting, color coding, and TTS speech integration (`System.Speech`). |
| **`Set-ForegroundProcess.ps1`** | Window Control | Brings target window to foreground via Win32 `SetForegroundWindow` interop. |
| **`Set-Minimize.ps1`** | Window Control | Minimizes target process windows. |
| **`Set-Maximize.ps1`** | Window Control | Maximizes target process windows. |
| **`Set-WindowsPosition.ps1`** | Window Control | Sets exact window position and dimensions via `MoveWindow`. |
| **`Get-WindowLocation.ps1`** | Window Control | Queries exact rectangle coordinates of process windows. |
| **`IP_Tracker_Agent.ps1`** | Network Agent | Tracks external IP changes and records network status. |
| **`Monitor-War-Thunder-Distance-Multiplier.ps1`** | Sim Helper | Watches War Thunder configuration files and restores custom distance multipliers if rewritten by the game. |
| **`WT_MissionType1.ps1`** | Sim Helper | Generates custom War Thunder mission configurations from JSON templates. |

---

## Game profiles and customization (`config/hosts.config.json`)

To add or modify games, edit `config/hosts.config.json` under your machine's entry in `machines.<HOSTNAME>.gameProfiles` (or under `defaults.gameProfiles`).

### Profile Schema Example

```json
"DCS.exe": {
  "NickName": "DCS World",
  "Speak": "Starting Digital Combat Simulator",
  "Start": "High Performance",
  "Stop": "Balanced",
  "ImmediateKill": false,
  "Stutter": true,
  "BoostAction": "action-per-process-boost1.json",
  "AuxProgramsDelaySeconds": 5,
  "WindowStyle": "Minimized",
  "AuxPrograms": [
    "[FanatecMonitor.exe]C:\\Users\\ralch\\Desktop\\C-Fanatec Monitor.lnk",
    {
      "Id": "MyPowerShellHelper",
      "Path": "C:\\Users\\ralch\\Desktop\\My Helper.lnk",
      "MatchType": "PowerShellScript",
      "ScriptPath": "C:\\MyPrograms\\Helpers\\MyHelper.ps1",
      "LaunchMode": "IfNotRunning",
      "StopMode": "OwnedOnly",
      "StartupTimeoutSeconds": 10
    }
  ]
}
```

### Formal Profile Field Reference

| Profile Field | Data Type | Default Value | Allowed Options / Values | Description & Functionality |
| :--- | :--- | :--- | :--- | :--- |
| **`NickName`** | String | Process Name | Any string | Human-readable / TTS-friendly display name (used in session notifications, log titles, and exit summaries). |
| **`Speak`** | String | `null` | Any text string | Custom text-to-speech phrase spoken by `System.Speech` when the process starts or exits. If omitted, default TTS is generated from `NickName`. |
| **`Start`** | String | `null` | Power scheme name (e.g. `"High Performance"`, `"Balanced"`, or GUID) | Windows power plan applied when the monitored game process starts. |
| **`Stop`** | String | `null` | Power scheme name (e.g. `"Balanced"`, `"Power Saver"`, or GUID) | Windows power plan applied when the monitored game process exits. |
| **`ImmediateKill`** | Boolean | `false` | `true`, `false` | When `true`, forcibly terminates (`Stop-Process -Force`) the process immediately upon detection *before* any power schemes, aux programs, boost actions, TTS, or runtime trackers execute. Designed for blocking background hogs (`TiWorker.exe`, `CompatTelRunner.exe`) on legacy machines. |
| **`Stutter`** | Boolean | `false` | `true`, `false` | When `true`, registers the process with `Stutter-Hunter-IPC.ps1` for real-time stutter tracking and CPU time delta introspection. |
| **`BoostAction`** | String | `null` | Path to JSON file (e.g. `"action-per-process-boost1.json"`) | Relative JSON policy path containing process priority, CPU affinity masks, CPU Sets, or thread EcoQoS settings applied 5 seconds post-launch. |
| **`AuxProgramsDelaySeconds`** | Integer | `5` | Non-negative integer (`>= 0`) | Delay in seconds after game launch before auxiliary programs are started. Allows anchoring aux program launches relative to game startup. |
| **`WindowStyle`** | String | `"Minimized"` | `"Normal"`, `"Hidden"`, `"Minimized"`, `"Maximized"` | Window state passed to `Start-Process -WindowStyle` when launching auxiliary program shortcuts or executables. |
| **`AuxPrograms`** | Array | `[]` | Array of Strings or JSON objects | List of auxiliary tools, shortcuts, or scripts to launch and optionally track alongside the game process lifecycle. |

---

## Auxiliary program lifecycle formats (`Aux-Programs.ps1`)

The `AuxPrograms` array accepts **four distinct formats** to accommodate simple shortcuts, duplicate suppression, script execution, and exact PID ownership cleanup:

### 1. Legacy Path String
```json
"AuxPrograms": [
  "C:\\Users\\ralch\\Desktop\\Disable-Antivirus.ps1.lnk"
]
```
- **Behavior**: Launches the path every time the game starts. Does not check for existing instances, does not track PID ownership, and **never terminates** the program when the game exits (`LaunchMode = Always`, `StopMode = Never`).

### 2. Executable Shorthand
```json
"AuxPrograms": [
  "[FanatecMonitor.exe]C:\\Users\\ralch\\Desktop\\C-Fanatec Monitor.lnk"
]
```
- **Behavior**: Checks if a process named `FanatecMonitor.exe` is already running. If running, launch is skipped. **Never terminates** the process on game exit (`LaunchMode = IfNotRunning`, `StopMode = Never`).

### 3. PowerShell Script Shorthand
```json
"AuxPrograms": [
  "[ps1:C:\\MyPrograms\\Helpers\\MyHelper.ps1]C:\\Users\\ralch\\Desktop\\My Helper.lnk"
]
```
*(Or filename-only form: `"[ps1:MyHelper.ps1]C:\\Users\\ralch\\Desktop\\My Helper.lnk"`)*
- **Behavior**: Inspects running `powershell.exe` processes via CIM (`Win32_Process`) and parses their command lines for the script path. If matching, launch is skipped (`LaunchMode = IfNotRunning`, `StopMode = Never`).

---

### 4. Structured Definition (Full Lifecycle & Ownership Control)

Structured JSON definitions provide complete precision over matching rules, duplicate checks, ownership tracking, and termination semantics on game exit.

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

#### Formal Field Reference for Structured AuxPrograms Definitions

| Field | Data Type | Default Value | Required? | Allowed Options / Format | Detailed Explanation & Mechanics |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`Id`** | String | Derived from Matcher | Optional (Recommended) | Case-insensitive string identifier (e.g. `"FanatecMonitor"`) | Uniquely identifies a logical shared auxiliary tool. When multiple running games share the same `Id`, the watcher registers them as joint consumers. The auxiliary process remains running until the **final** consumer game exits. |
| **`Path`** | String | *None* | **Required** | Absolute file path, executable, or `.lnk` shortcut | The target shortcut or binary path passed to `Start-Process` when launching the auxiliary program. Backslashes must be doubled (`\\\\`) in JSON. |
| **`MatchType`** | String | *None* | **Required** | `"ProcessName"`, `"PowerShellScript"` | Defines the mechanism used to inspect `Win32_Process` instances when querying running programs before or after launch: <br>• `"ProcessName"`: Matches process image names (e.g., `FanatecMonitor.exe`).<br>• `"PowerShellScript"`: Matches running `powershell.exe` processes by inspecting their command lines. |
| **`ProcessName`** | String | `null` | Required if `MatchType` = `"ProcessName"` | Executable name with or without `.exe` (e.g. `"FanatecMonitor.exe"`) | The exact process image name to match when `MatchType` is `"ProcessName"`. Automatically normalized by stripping `.exe` and trailing spaces for WQL queries. |
| **`ScriptPath`** | String | `null` | Optional (`MatchType` = `"PowerShellScript"`) | Absolute path to `.ps1` file (e.g. `"C:\\Helpers\\Script.ps1"`) | Matches `powershell.exe` processes whose command lines contain this exact absolute script path. Takes precedence over `ScriptName` if both are supplied. |
| **`ScriptName`** | String | `null` | Optional (`MatchType` = `"PowerShellScript"`) | Script filename ending in `.ps1` without path slashes (e.g. `"Script.ps1"`) | Matches `powershell.exe` processes whose command lines contain this script filename. Uses word/path boundary matching so `Script.ps1` does not match `NotScript.ps1`. |
| **`PowerShellHostProcessName`** | String | `"powershell.exe"` | Optional (`MatchType` = `"PowerShellScript"`) | Executable leaf name (e.g. `"powershell.exe"`, `"pwsh.exe"`) | The host process image name searched when inspecting command lines for PowerShell script execution. Defaults to Windows PowerShell 5.1 (`powershell.exe`). |
| **`LaunchMode`** | String | `"Always"` | Optional | `"Always"`, `"IfNotRunning"` | Controls when the `Start-Process` call is triggered upon game start:<br>• **`"Always"`**: Always execute the launch path every time the monitored game starts, regardless of whether a matching process is already running.<br>• **`"IfNotRunning"`**: Query `Win32_Process` first. Skip launching if a matching process (or pending in-flight launch) already exists. |
| **`StopMode`** | String | `"Never"` | Optional | `"Never"`, `"OwnedOnly"` | Controls cleanup behavior when the monitored game exits:<br>• **`"Never"`**: The auxiliary process is left running indefinitely. The watcher takes no termination action when the game exits.<br>• **`"OwnedOnly"`**: The watcher terminates **only** verified process instances that were launched and claimed by this watcher instance for this game. Pre-existing processes are **never** stopped. |
| **`StartupTimeoutSeconds`** | Integer | `10` | Optional | Non-negative integer (`>= 0`) | The maximum bounded window (in seconds) after calling `Start-Process` during which the watcher polls `Win32_Process` to discover newly spawned PID(s) and their creation timestamps for `OwnedOnly` ownership tracking. |

#### Detailed Mechanics of `StopMode = "OwnedOnly"` and Shared Consumers

1. **Before-and-After PID Discovery**:
   - Because Windows shortcuts (`.lnk` files) or intermediate launchers invoke `Start-Process` without returning the final persistent PID directly, `OwnedOnly` uses bounded polling:
     - **Before Launch**: Captures all existing PIDs matching `MatchType`.
     - **Launch**: Executes `Start-Process -FilePath Path -WindowStyle WindowStyle`.
     - **After Launch**: Polls `Win32_Process` for up to `StartupTimeoutSeconds` (until PIDs stabilize for 500ms).
     - **Claiming**: Records only newly appeared PIDs whose WQL `CreationDate` is greater than or equal to the launch timestamp. Pre-existing processes are left unowned.
2. **Shared Consumers across Games**:
   - If Game A launches an auxiliary defined with `Id: "FanatecMonitor"` and `StopMode: "OwnedOnly"`, PID 1234 is claimed.
   - If Game B starts while Game A is still running, Game B checks `Id: "FanatecMonitor"`. Since PID 1234 is running, `LaunchMode: "IfNotRunning"` suppresses relaunching and registers Game B as a second consumer of PID 1234.
   - When Game A exits, the watcher detects that Game B is still active. PID 1234 is **retained**.
   - When Game B exits, the final consumer count drops to 0, and the watcher safely invokes `Stop-Process -Id 1234 -Force`.
3. **Watcher Restart Safety**:
   - Ownership records are maintained in memory. If `Start-CommandWatchers.ps1` restarts or reloads, surviving auxiliary processes are treated as pre-existing and will **never** be forcefully terminated on subsequent exits.

---

## Gameplay Telemetry & CPU Runtime Tracking (`Cpu-Snapshots.ps1`)

The toolkit provides "hands-off" session tracking without polling overhead:

1. **PID-Keyed Tracking**: Monitored games spawn a runtime tracking timer (`Start-GameRuntimeTracker`). Tracking is keyed by PID (not process name) so multiple concurrent instances do not collide.
2. **Persistent Disk Snapshots (`Cpu-Snapshots.ps1`)**:
   - Every 30 minutes (and on startup), CPU time is sampled and persisted to disk via `Save-GameRuntimeCpuSnapshot`.
   - **Zombie Handle Protection**: When a process exits, OS process handle stats can occasionally report 0 seconds or fail to refresh. The stop handler (`Stop-GameRuntimeTracker`) cross-checks live data against disk snapshots to guarantee accurate total CPU time reporting.
3. **Audio Feedback**:
   - Speaks hourly milestones during long gaming sessions (*"DCS World - 2 hours"*).
   - Speaks total gameplay duration on exit (*"War Thunder stopped, 1 hour 15 minutes total"*).

---

## Boost profiles and process tuning (`action-per-process-boost*.json`)

Boost profiles define hardware scheduling, affinity masks, CPU Sets, and thread priorities for games and their dependencies.

Example snippet (`action-per-process-boost1.json`):
```json
[
  {
    "process_name": "Ace7Game-Win64-Shipping",
    "parameters": {
      "priority": "High",
      "cpu_affinity": [0, 2, 4, 6, 8, 10, 12, 14],
      "dependencies": [
        {
          "process_name": "steamwebhelper",
          "priority": "BelowNormal",
          "dont_restore_boost": true
        }
      ]
    }
  }
]
```

- **`dont_restore_boost` Flag**: When set to `true` on a dependency block, `Restore-GameBoost` will skip restoring that dependency back to default priorities when the game exits. This keeps background helper processes (like Steam overlay components) pinned to low-priority efficiency cores permanently.

---

## Common tweaks (most common edits)

### 1) Add a new game / change power plans
Edit `config/hosts.config.json` under your machine's `gameProfiles` block:
```json
"ForzaHorizon5.exe": {
  "NickName": "Forza Horizon 5",
  "Start": "High Performance",
  "Stop": "Balanced"
}
```

### 2) Immediately terminate an unwanted background process
Edit `config/hosts.config.json` to enable `ImmediateKill`:
```json
"CompatTelRunner.exe": {
  "ImmediateKill": true,
  "NickName": "Compat Tel Runner"
}
```

### 3) Enable or disable feature flags per machine
In `config/hosts.config.json`, toggle flags under `machines.<HOSTNAME>.features`:
```json
"features": {
  "remoteCommandsWatcher": true,
  "processWatcher": true,
  "ipcServer": true,
  "watchdog": true
}
```

---

## Troubleshooting

- **Nothing happens on process launch:**
  - Verify `processWatcher` is set to `true` for your hostname in `hosts.config.json`.
  - Verify `Start-CommandWatchers.ps1` is running (check window title or Task Manager).
- **Process is killed unexpectedly:**
  - Check if `"ImmediateKill": true` is set for that process name in `hosts.config.json`.
- **Remote JSON commands not executing:**
  - Confirm `remoteCommandsWatcher` is enabled.
  - Verify that your external tool/PHP script is performing an atomic **rename handoff** (`.tmp` → `.json`).
- **IPC Pipe connection issues:**
  - Verify `ipcServer` is enabled.
  - Check that only a single instance of `Start-CommandWatchers.ps1` is running (mutex prevents dual IPC servers).
- **Auxiliary program not stopping on game exit:**
  - Verify the entry uses `StopMode = "OwnedOnly"` (legacy strings and shorthand matchers default to `StopMode = "Never"`).
  - Ensure the auxiliary process was launched by the watcher and discovered during `StartupTimeoutSeconds`.

---

## Future roadmap

- Move remaining static file paths into `config/hosts.config.json`.
- Dynamic CPU topology generation (auto-detecting P-cores / E-cores at startup to construct affinity masks dynamically).
- Web-based status dashboard for VR headset viewable status.
