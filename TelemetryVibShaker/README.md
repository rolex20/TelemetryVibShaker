# TelemetryVibShaker — Real-Time Haptic & Audio Feedback Engine

## Tech-Flex
- **Asynchronous Multi-Threaded Telemetry Server:** High-throughput non-blocking UDP socket listener receiving telemetry packets from flight simulators with zero dropped frames.
- **NAudio Custom Audio Engine:** Low-latency dynamic audio playback using custom `LoopStream` providers, dynamic volume scaling, and multi-device sound routing (`MMDeviceEnumerator`).
- **Named-Pipe IPC Remote Control (`GlobalIpc`):** Multithreaded Win32 named-pipe server (`\\.\pipe\TelemetryVibShakerPipeCommands`) enabling instant remote tab switching and UI controls from external web scripts and phone panels.
- **Single-Instance Win32 Mutex Enforcement:** System-wide kernel `Mutex` locking preventing duplicate process instantiation while preserving single-port UDP binding.
- **Dynamic JSON Aircraft Telemetry Parser:** High-speed JSON serialization (`units.json`) mapping complex non-linear AoA curves, sound file triggers, and motor vibration intensity profiles per aircraft.

---

## Real-Time Haptic & Audio Feedback for Flight Simulation

TelemetryVibShaker is a high-performance C# Windows application providing real-time tactile vibration and audio feedback for flight sim pilots. Designed for use with **DCS World**, **Falcon BMS**, **MSFS 2020**, and **War Thunder**, it interfaces directly with hardware like Arduino-driven vibration motors (sticked to throttle and joystick) and the **LILYGO T-Watch 2020 V3**.

By delivering physical tactile cues when your aircraft reaches optimal Angle of Attack (AoA), or when airframe drag devices (speed brakes, flaps, landing gear) are extended, TelemetryVibShaker gives virtual pilots the critical physical sensations experienced in real combat aircraft.

---

## Key Features

- **Real-Time Telemetry Processing:** Parses incoming UDP packets containing airspeed, AoA, flap state, speed brake state, and gear position.
- **Haptic Vibration Motors:** Output motor pulse-width modulation (PWM) values (0-255) over serial or UDP to Arduino R4 and T-Watch controllers.
- **Dynamic Audio Feedback:** Plays dynamic sound effects (e.g. cockpit buffet audio, stall warnings) matching optimal AoA ranges with per-device volume control.
- **Named-Pipe IPC Server:** Accepts remote control commands (`TAB:Main`, `TAB:Monitor`, `TAB:Settings`, `START`, `STOP`) via `GlobalIpc.cs` from phone panels or external tools.
- **Aircraft Profile Engine (`units.json`):** Customizable AoA thresholds and vibration response curves per aircraft module (F-16C, F-14B, F/A-18C, Su-27, etc.).

---

## Screenshots & Hardware Integration

### DCS World Integration
![DCS Interface](Screenshots/DCS/DCS_Main.png)

### MSFS 2020 Configuration
![MSFS Interface](Screenshots/MSFS/MSFS_Settings.png)

### War Thunder Setup
![War Thunder Setup](Screenshots/WarThunder/WarThunder_Main.png)

### Vibration Motor Hardware Setup
Small cellphone vibration motors mounted on joystick and throttle controls:

![Hardware Setup 1](Screenshots/Small%20Vibration%20Motors/VibMotor_Joystick.png)
![Hardware Setup 2](Screenshots/Small%20Vibration%20Motors/VibMotor_Throttle.png)

*(Note: Additional screenshots available in the `Screenshots/` directory)*

---

## Configuration Guide (`units.json`)

The `units.json` configuration file defines AoA sound ranges and vibration profiles for each aircraft. Example structure:

```json
{
  "Aircraft": "F-16C_50",
  "OptimalAoAMin": 11.0,
  "OptimalAoAMax": 14.0,
  "SoundFile": "F16_AoA_Buffet.wav",
  "SpeedBrakeMotorIntensity": 200,
  "FlapsMotorIntensity": 180
}
```

- **`OptimalAoAMin` / `OptimalAoAMax`:** Specifies the Angle of Attack range where optimal turn performance or landing approach occurs.
- **`SoundFile`:** Audio file triggered during optimal AoA range.
- **`SpeedBrakeMotorIntensity` / `FlapsMotorIntensity`:** PWM vibration strength sent to Motor 1 and Motor 2.

---

## Named Pipe IPC Protocol (`GlobalIpc.cs`)

TelemetryVibShaker hosts a named pipe server at `\\.\pipe\TelemetryVibShakerPipeCommands`. External applications (like `commander.php` in `WebScripts/remote_control`) can send text commands:

| Command | Action |
| :--- | :--- |
| `MONITOR` | Switches active UI tab to Monitor tab |
| `SETTINGS` | Switches active UI tab to Settings tab |
| `NOT_FOUNDS` | Switches active UI tab to Aircraft Not Found log tab |
| `START` | Starts telemetry server listening loop |
| `STOP` | Stops telemetry server listening loop |
| `CYCLE_STATISTICS` | Cycles through runtime performance stats |

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Current Implementation:** `LEGACY` (Uses older `ProcessorAssigner.cs` heuristic for thread affinity).
- **Target Migration:** Planned migration to [`CPU_QoS_Desktop.cs`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/CPU_QoS_Desktop.cs).

### ToDos & Recommended Improvements
- [ ] **Migrate to `CPU_QoS_Desktop.cs`:** Replace `ProcessorAssigner.cs` with `CPU_QoS_Desktop.cs` to enable hybrid P-Core / E-Core discovery and EcoQoS for background audio threads.
- [ ] **Add Telemetry Auto-Detection:** Automatically switch active aircraft profile in `units.json` when simulator sends aircraft type strings.
- [ ] **Multi-Motor Curve Editor:** Add graphical curve editing in WinForms UI for fine-tuning PWM output points.
