# WarThunderExporter — War Thunder Telemetry Exporter

## Tech-Flex
- **High-Frequency HTTP Web API Scraper:** Polls War Thunder's internal local web server (`http://localhost:8111/indicators` and `http://localhost:8111/state`) at high sample rates.
- **Asynchronous JSON Payload Parser:** Deserializes JSON telemetry streams into structured flight variables (`AoA`, `flaps`, `gear`, `speedbrake`, `IAS`).
- **High-Performance UDP Broadcaster:** Encapsulates parsed flight state parameters into UDP telemetry packets bound for TelemetryVibShaker.
- **Statistical Jitter & Rate Calculator (`SimpleStatsCalculator.cs`):** Computes live polling frequencies, packet intervals, and metrics latency.

---

## Overview & Purpose

WarThunderExporter bridges **War Thunder** with [`TelemetryVibShaker`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/TelemetryVibShaker/README.md). When War Thunder runs, it exposes a local HTTP API on port 8111. WarThunderExporter polls this endpoint, parses real-time flight metrics—such as Angle of Attack, flap percentage, speed brake position, and airspeed—and transmits UDP telemetry packets to drive vibration motors and audio cues.

This provides instant tactile haptic feedback during War Thunder dogfights, helping pilots feel flap speed limits and stall AoA without watching on-screen HUD numbers.

---

## Architecture & Code Components

- **`Form1.cs`:** Manages HTTP client polling loop (`HttpClient` / `HttpWebRequest`), parses `/indicators` and `/state` JSON feeds, renders UI telemetry meters, and broadcasts UDP packets.
- **`SimpleStatsCalculator.cs`:** Tracks HTTP response times, sample rates, and packet delivery throughput.
- **`MyControlInfo.cs`:** Data structure containing extracted flight variables.
- **`ProcessorAssigner.cs`:** Thread affinity management.

---

## Configuration & Usage

1. Launch War Thunder.
2. Launch `WarThunderExporter.exe`.
3. The app connects to `http://localhost:8111`.
4. Telemetry is automatically broadcast over UDP to port `54671`.

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Current Implementation:** `LEGACY` (Uses older `ProcessorAssigner.cs` heuristic).
- **Target Migration:** Planned migration to [`CPU_QoS_Desktop.cs`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/CPU_QoS_Desktop.cs).

### ToDos & Recommended Improvements
- [ ] **Migrate to `CPU_QoS_Desktop.cs`:** Replace `ProcessorAssigner.cs` with `CPU_QoS_Desktop.cs` to ensure HTTP polling threads execute cleanly on hybrid Intel E-cores.
- [ ] **Add G-Load Extraction:** Extract vertical G-load from `/state` feed for G-buffet haptic motor output.
