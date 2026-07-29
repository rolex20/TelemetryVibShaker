# FalconExporter — Falcon BMS Telemetry Exporter

## Tech-Flex
- **Win32 Shared Memory File Mapping (P/Invoke):** Opens and reads `FalconSharedMemoryArea`, `FalconSharedOsbMemoryArea`, and `FalconIntellivibeSharedMemoryArea` shared memory handles via kernel32 `OpenFileMapping` and `MapViewOfFile`.
- **C++ Native Bitfield Structure Marshalling:** Unpacks native C++ struct headers (`LightBits`, `LightBits2`, `LightBits3`, `HsiBits`, `PowerBits`) into managed C# properties.
- **High-Frequency Telemetry UDP Broadcaster:** Continuously marshals extracted flight metrics (airspeed, AoA, gear state, flap state, speed brakes) into binary UDP packets bound for TelemetryVibShaker.
- **Auto-Reconnecting Sim Watchdog:** Dynamically detects Falcon BMS launch/shutdown cycles without throwing exceptions or leaking memory handles.

---

## Overview & Purpose

FalconExporter is a dedicated C# telemetry exporter application built for **Falcon BMS**. It connects to Falcon BMS's native shared memory region (`FalconSharedMemoryArea`), extracts real-time flight parameters—including Angle of Attack (AoA), speed brake position, flap deployment, and landing gear state—and broadcasts those parameters as UDP telemetry packets to [`TelemetryVibShaker`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/TelemetryVibShaker/README.md).

For F-16 virtual pilots in Falcon BMS, this enables tactile throttle/joystick haptic feedback and dynamic audio cues matching the Viper's optimal AoA turn performance.

---

## Architecture & Code Components

- **`Reader.cs`:** Manages Win32 file mapping objects to read Falcon BMS shared memory areas (`FalconSharedMemoryArea`, `FalconSharedOsbMemoryArea`, `FalconIntellivibeSharedMemoryArea`, etc.).
- **`LightBits.cs`, `LightBits2.cs`, `LightBits3.cs`:** Decodes cockpit annunciator indicator bitmasks.
- **`HsiBits.cs`, `PowerBits.cs`, `NavModes.cs`:** Unpacks navigation instrument states and power bus flags.
- **`IntellivibeData.cs`:** Reads native IntelliVibe force feedback and vibration telemetry data.
- **`Form1.cs`:** WinForms interface showing connection status, real-time telemetry meters, and UDP broadcast settings.
- **`ProcessorAssigner.cs`:** Thread affinity management.

---

## Configuration & Usage

1. Launch Falcon BMS.
2. Launch `FalconExporter.exe`.
3. Verify that the UI displays **Connected to Falcon BMS**.
4. Configure target UDP IP (default `127.0.0.1`) and Port (default `54671`) pointing to TelemetryVibShaker.

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Current Implementation:** `LEGACY` (Uses older `ProcessorAssigner.cs` heuristic).
- **Target Migration:** Planned migration to [`CPU_QoS_Desktop.cs`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/CPU_QoS_Desktop.cs).

### ToDos & Recommended Improvements
- [ ] **Migrate to `CPU_QoS_Desktop.cs`:** Replace `ProcessorAssigner.cs` with `CPU_QoS_Desktop.cs` for unified hybrid core assignment on Intel 12700K / 14700K processors.
- [ ] **Direct IntelliVibe Haptic Mapping:** Expose raw IntelliVibe motor intensities directly to TelemetryVibShaker for enhanced g-force and ground-roll vibrations.
