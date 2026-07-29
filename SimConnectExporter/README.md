# SimConnectExporter — MSFS 2020 Telemetry Exporter

## Tech-Flex
- **Native Microsoft SimConnect SDK P/Invoke Integration:** Subscribes to MSFS 2020 flight data structures via native `SimConnect.dll` C++ API interop.
- **Event-Driven Window Message Loop Dispatching:** Captures SimConnect data callback events through WinForms `WndProc` user message handling (`WM_USER_SIMCONNECT`).
- **Dynamic SimVar Registration:** Maps `INCIDENCE ALPHA` (Angle of Attack), `FLAPS HANDLE PERCENT`, `TRAILING EDGE FLAPS LEFT PERCENT`, `SPOILERS HANDLE POSITION`, and `AIRSPEED INDICATED` into binary telemetry payloads.
- **Asynchronous UDP Streamer:** Broadcasts real-time flight variables over UDP to TelemetryVibShaker with zero impact on MSFS frame pacing.

---

## Overview & Purpose

SimConnectExporter connects to **Microsoft Flight Simulator 2020 (MSFS 2020)** via the native SimConnect SDK. It extracts flight telemetry—including Angle of Attack, flap positions, speed brakes, and airspeed—and transmits UDP telemetry packets to [`TelemetryVibShaker`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/TelemetryVibShaker/README.md).

This brings tactile vibration cues and dynamic AoA sound effects to MSFS 2020 aircraft (such as the F/A-18E Super Hornet or general aviation planes), helping pilots feel approach AoA and flap drag without looking down at instruments.

---

## Architecture & Code Components

- **`Form1.cs`:** Manages SimConnect connection lifecycle (`SimConnect_Open`, `SimConnect_ReceiveMessage`), registers SimVars, processes incoming data structures, and transmits UDP packets.
- **`MyControlInfo.cs`:** Data structure representing telemetry values emitted to UDP listeners.
- **`ProcessorAssigner.cs`:** Thread affinity management.

---

## Configuration & Usage

1. Launch MSFS 2020.
2. Launch `SimConnectExporter.exe`.
3. The application auto-connects to the active MSFS SimConnect session.
4. Telemetry is automatically broadcast to target UDP port `54671`.

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Current Implementation:** `LEGACY` (Uses older `ProcessorAssigner.cs` heuristic).
- **Target Migration:** Planned migration to [`CPU_QoS_Desktop.cs`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/CPU_QoS_Desktop.cs).

### ToDos & Recommended Improvements
- [ ] **Migrate to `CPU_QoS_Desktop.cs`:** Replace `ProcessorAssigner.cs` with `CPU_QoS_Desktop.cs` to ensure SimConnect callback threads run efficiently on hybrid Intel CPUs.
- [ ] **Add G-Force SimVars:** Register `TOTAL G FORCE` SimVar for G-buffet tactile vibration effects.
