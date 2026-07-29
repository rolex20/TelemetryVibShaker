# commander.php — VR-Friendly Remote Control Panel

## Tech-Flex
- **Atomic File-Rename Command Bus:** Employs `.tmp` ➔ `.json` file creation and atomic renaming (`renameFile()`) to trigger PowerShell file system watchers without race conditions.
- **Base64 Pipe Message Serializer (`JSONB64`):** Encodes complex JSON command payloads into Base64 strings for raw named-pipe socket transport across process boundaries.
- **Multi-AI Layout Explorations:** Features customized PHP control panel implementations (`commander.php`, `commander_by_claude.php`, `commander_by_gemini.php`, `commander_by_gpt.php`) optimizing touch targets and UX for VR gloves and smartphones.
- **Win32 Interprocess Synchronizer:** Shares handshake files (`watchdog.txt`, `outfile.txt`) with retry logic (`tryOpenFile`) to display process locations and watcher status without blocking web requests.

---

## Why This Matters in VR

When you are wearing a VR headset, you are blind to your desktop. If a flight simulator freezes, opens on the wrong monitor, or starts dropping frames because Windows assigned game threads to Efficiency cores, taking off the headset to alt-tab and fix it ruins the experience.

`commander.php` is my lifeline from my smartphone:
- **One-Tap Power Plans:** Instantly switch between `High Performance`, `Balanced`, and `Balanced Max 80` to keep cores turboing without cooking the PC room.
- **Process Terminate (`KILL`) & Launch:** Recover frozen games or relaunch utilities without taking off the headset.
- **Window Repositioning:** Move, minimize, maximize, or bring kneeboard overlay windows to the foreground.
- **Boost Tiers (1–3):** Feed custom affinity and priority profiles to PowerShell depending on how a game behaves that day.
- **IPC Pipe Router:** Send pipe messages directly to `TelemetryVibShaker`, `PerformanceMonitor`, `WarThunderExporter`, and `SimConnectExporter`.

---

## Recovery Code Analysis: Game Terminate (`KILL`) & System Control

When a sim hangs, `commander.php` and its AI variants provide dedicated termination mechanisms:

### PHP Process Termination Code ([commander.php:L268-L280](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/WebScripts/remote_control/commander.php#L268-L280))
```php
// CHECK FOR TERMINATE COMMAND
$post_command = isset($_POST['Exit']) ? $_POST['Exit'] : "";
if ($post_command == "Terminate") { 
    $processName = $_POST['Process'];
    $footer = "Terminate completed - [$processName]";
            
    // Atomic rename handoff to PowerShell watcher
    $jsonData = createJsonCommand("KILL", array("processName" => $processName));
    writeJsonToFile($jsonData, TEMP_FILE);
    renameFile(TEMP_FILE, COMMAND_FILE);
    $time_stamp = getTimestamp();
}
```

In `commander_by_gpt.php`, a safety-held touch gesture prevents accidental taps:
```html
<!-- Press & Hold to confirm process KILL in commander_by_gpt.php -->
<span style="color:rgba(255,59,87,.9)">Hold for 0.8s</span> to send <code>KILL</code>
```

---

## Internal Development Tools (`pipetest.php` & `echo.php`)

These helper scripts were created during development to test and validate named-pipe IPC communication between PHP (WAMP/Apache) and PowerShell:

- **`echo.php`:** Simple HTTP endpoint that echoes parameters, used to test AJAX connectivity and web server response times.
- **`pipetest.php`:** Smoke test bench for named-pipe IPC. Provides interactive buttons to test pipe connectivity:
  1. **Send ECHO:** Sends an `ECHO` packet to `ipc_pipe_vr_server_commands` to confirm the IPC thread is listening.
  2. **Run Notepad (`JSONB64`):** Serializes a `RUN` notepad command into Base64 and sends it through the named pipe.
  3. **Read Power Scheme (`JSONB64`):** Queries current active Windows power plan over named pipes.

---

## File Layout & Module Inventory

- **`commander.php`:** Primary production control panel UI.
- **`commander_by_claude.php`:** Claude-generated UI variant featuring structured tab panels and quick-fire pipe buttons.
- **`commander_by_gemini.php`:** Gemini-generated UI variant focused on dark-mode glassmorphism design.
- **`commander_by_gpt.php`:** GPT-generated UI variant featuring press-and-hold confirmation buttons for high-risk actions (`KILL`).
- **`programs.json`:** Catalog of flight sims and utilities (friendly name, executable process name, file path).
- **`pipetest.php` & `echo.php`:** Pipe IPC smoke testing utilities.
- **`command.json` / `command.tmp`:** Atomic JSON hand-off files.

---

## `programs.json` Schema Reference

```json
[
  {
    "friendlyName": "DCS World",
    "processName": "dcs",
    "path": "C:\\Program Files\\Eagle Dynamics\\DCS World OpenBeta\\bin\\DCS.exe"
  },
  {
    "friendlyName": "MSFS 2020",
    "processName": "FlightSimulator",
    "path": "C:\\XboxGames\\Microsoft Flight Simulator\\Content\\FlightSimulator.exe"
  }
]
```

---

## CPU QoS Status & Roadmap ToDos

### CPU QoS Status
- **Client Interface:** `remote_control` emits high-level boost requests (`BOOST_1`, `BOOST_2`, `BOOST_3`) which `ps_scripts` translates into hard/soft CPU affinity masks.

### ToDos & Recommended Improvements
- [x] Implement atomic `.tmp` ➔ `.json` file rename trigger.
- [x] Add press-and-hold safety gesture for process `KILL` in `commander_by_gpt.php`.
- [ ] **Unified UI Selector:** Add a dropdown top-bar in `commander.php` allowing instant switching between the different AI UI variants (`claude`, `gemini`, `gpt`).
- [ ] **Direct Reboot Button:** Add a red "System Reboot" button that sends `shutdown.exe /r /t 0` via PowerShell for one-tap recovery from hard system lockups.
