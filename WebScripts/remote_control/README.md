# commander.php — VR-friendly remote control panel

This PHP page is my lifeline when I am blindfolded by a VR headset: from the phone I can launch sims, shove their windows to the right monitor, or fire CPU-boost macros without ever alt-tabbing.

**Tech flex:** PHP 8+, JSON IPC bridge to PowerShell, WAMP/Apache hosting, HTML/JS form helpers, pipe-based messaging, and Windows process/window management via PowerShell modules.

## Why the features matter
- **One-click power plans** switch between Balanced, High Performance, and capped Balanced80 so the right cores turbo for MSFS/DCS without cooking the room.
- **Launch/terminate/foreground controls** let me recover frozen apps or bring overlays into view while still in VR, avoiding the “rip-off-the-headset” routine.
- **Boost tiers** (1–3) feed different `action-per-process-boost*.json` profiles to PowerShell so I can choose AboveNormal + ideal P-core threads or hard affinities depending on how the sim behaves that day.
- **Thread inspector** (`SHOW_THREADS`) triggers per-process thread time dumps, perfect for spotting when critical game threads drift onto E-cores.
- **Window movement/minimize/restore/maximize** keeps kneeboard apps visible on the right monitor even when VR mirroring gets messy.
- **Pipe commands** blast messages to specific IPC pipes (`TelemetryVibShaker`, `WarThunderExporter`, `SimConnectExporter`, etc.) for in-game telemetry tools.
- **Watchdog ping** confirms the PowerShell watcher and IPC server are alive; it even speaks errors so I hear them over the engine noise.

## Architecture and flow
1. **Front-end form** renders program choices from `programs.json` and posts actions back to `commander.php`.
2. **Command builder** (`createJsonCommand`) writes the chosen action to `command.tmp` then renames it to `command.json`—the rename event is what `WaitFor-Json-Commands.ps1` listens for.
3. **JSON consumers**: the PowerShell watcher calls `Process-CommandFromJson.ps1`, which dispatches to window movers, power plan setters, IPC pipe senders, or boost routines.
4. **Program catalog**: update `programs.json` when adding a new sim/utility so the drop-down shows it and launch/foreground commands know the executable path.
5. **Output files**: commands like `GET-LOCATION` write to `outfile.txt`; `commander.php` reopens it with retries (`tryOpenFile`) so coordinates appear without racing the watcher.

## File layout
- `commander.php` — main UI and JSON command generator.
- `programs.json` — friendly names, process names, and launch paths for the dropdown.
- `command.json` / `command.tmp` — hand-off files picked up by the PowerShell watcher.
- `watchdog.txt`, `outfile.txt` — responses from watchdog and window queries.

## Common updates
- **Add a program**: append an entry to `programs.json` with `friendlyName`, `processName`, and `path` (exe or shortcut). Ensure the same process name is covered in `ps_scripts/Gaming-Programs.ps1` if you want power/boost automation.
- **Tune boost profiles**: edit the `action-per-process-boost*.json` files in `ps_scripts` and point the dropdown to the one you prefer.
- **Change default threads/instances**: adjust the default `Instance` value handling near the POST block if your sim spins more (or fewer) busy threads worth corralling.

## Future plans
1. **Split UI from logic** with a lightweight controller class so adding buttons doesn’t bloat the single PHP file.
2. **Server-side validation** for coordinates, pipe names, and thread limits to avoid bad JSON reaching the watcher.
3. **Mobile-first polish** (bigger tap targets, dark mode) to make phone use effortless in VR gloves.
4. **Persistent presets** stored per-game (power plan, boost level, window slot) so I can fire a whole profile with one tap.
5. **Better status polling** via AJAX to show command completion, current power plan, and last watchdog state without refreshing the page.
