# LUA Script — DCS World Telemetry Export Hook

## Tech-Flex
- **Direct DCS World Lua Engine Hooks (`Export.lua`):** Subscribes to DCS World `LuaExportStart()`, `LuaExportAfterNextFrame()`, and `LuaExportStop()` simulation callbacks.
- **Native LuaSocket Interop:** Direct binding to `LuaSocket` (`socket.udp()`) for non-blocking (`settimeout(0)`) UDP telemetry datagram streaming.
- **High-Frequency Fixed-Interval Sampling:** Uses `os.clock()` sampling at 20 updates per second (50ms intervals) to minimize CPU load while preserving high-resolution flight dynamics.
- **Aircraft State Extraction:** Extracts `LoGetAngleOfAttack()`, `LoGetPlayerPlaneId()`, aircraft name, speed brake state, flap position, and gear status directly from DCS memory.

---

## Overview & Purpose

This directory contains the `Export.lua` script installed into DCS World's `Saved Games\DCS\Scripts\` directory. It acts as the primary telemetry bridge for DCS World, extracting real-time flight metrics from aircraft modules (F-16C Viper, F-14B Tomcat, F/A-18C Hornet, A-10C, Su-27, etc.) and streaming UDP datagrams to [`TelemetryVibShaker`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/TelemetryVibShaker/README.md).

For sim pilots, this script provides the raw AoA and airframe state data required to generate dynamic cockpit buffet sounds and haptic throttle/joystick vibrations.

---

## Installation & Setup

1. Copy `Export.lua` to your DCS World Saved Games directory:
   `C:\Users\<YourUsername>\Saved Games\DCS\Scripts\Export.lua`
   *(Or append its functions to your existing `Export.lua` file if you use other export tools like Tacview or SimAppPro)*.
2. Ensure target IP (`127.0.0.1`) and UDP Port (`54671`) match TelemetryVibShaker's receiver settings.
3. Launch DCS World and fly any module.

---

## Technical Flow

```
[DCS World Sim Engine]
       │ (Every frame: LuaExportAfterNextFrame)
       ▼
[Check os.clock() ➔ 20Hz Sampling Rate]
       │
       ├─► LoGetPlayerPlaneId() ➔ Aircraft Module Name
       ├─► LoGetAngleOfAttack() ➔ Real-time AoA
       ├─► MechState ➔ Flaps, Speed Brakes, Gear
       ▼
[LuaSocket UDP Datagram ➔ 127.0.0.1:54671] ➔ TelemetryVibShaker
```

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Embedded Lua Script:** Runs inside DCS World's internal Lua sandbox environment. Execution CPU priority and core affinity match the `dcs.exe` process (managed by `ps_scripts` and `PerformanceMonitor`).

### ToDos & Project Goals
- [x] Implement 20Hz non-blocking UDP telemetry export for DCS World.
- [ ] **Expanded MechState Exports:** Add wheel brake pressure and G-force metrics for landing roll and G-buffet haptics.
