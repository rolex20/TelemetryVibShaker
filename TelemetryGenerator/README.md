# TelemetryGenerator — Synthetic Telemetry Test Bench

## Tech-Flex
- **Background Thread UDP Packet Emitter:** Spawns a dedicated `Thread` with `CancellationTokenSource` to send configurable telemetry UDP datagrams on demand without blocking the UI.
- **8-Byte Custom Datagram Protocol:** Encodes flight state into a compact binary packet (`flag byte`, `AoA`, `SpeedBrake`, `Flaps`, plus reserved bytes) matching TelemetryVibShaker's live exporter format exactly.
- **Real-Time WinForms Progress Feedback:** Uses thread-safe `BeginInvoke` calls to update a progress bar and counters during live test runs.

---

## Story & Purpose

This is a WinForms test bench I built to test **TelemetryVibShaker**, vibration motors, and audio cues without ever launching a full flight simulator. When you're iterating on AoA curves or debugging haptic motor intensity, waiting for DCS or MSFS to load just to test one value is painful.

TelemetryGenerator lets me dial in any AoA value, speed brake state, or flap percentage via UI spinners, hit "Start", and watch TelemetryVibShaker react in real time—motors vibrating, sounds playing—all from a small window on my second monitor.

---

## Telemetry Packet Format

The 8-byte UDP datagram matches TelemetryVibShaker's expected input:

| Byte Index | Field | Notes |
| :--- | :--- | :--- |
| `[0]` | Flag (`1`) | Identifies datagram as live telemetry (not aircraft name) |
| `[1]` | AoA | 0–255 mapped value |
| `[2]` | Speed Brake | 0–255 position |
| `[3]` | Flaps | 0–255 position |
| `[4-7]` | Reserved | Future expansion |

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Internal Test Tool:** Single-threaded UI + one background telemetry thread. CPU QoS isolation not required.

### ToDos & Project Goals
- [x] Send configurable AoA / Speed Brake / Flaps values over UDP to TelemetryVibShaker.
- [ ] **Gear State Support:** Add a Gear spinner to test landing gear haptic/audio effects.
- [ ] **Sweep Mode:** Add an automatic AoA sweep from 0 → 30 → 0 to test the full vibration curve ramp without manually dragging a slider.
