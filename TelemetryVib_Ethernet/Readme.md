# TelemetryVib_Ethernet — Arduino R4 Low-Latency Haptic Firmware

## Tech-Flex
- **Direct SPI Ethernet Hardware Socket Protocol:** Interfaces directly with W5500 SPI Ethernet controller via `EthernetUdp` for deterministic, zero-jitter network packet delivery.
- **Sub-Millisecond Hardware Latency:** Eliminates the ~20ms internal firmware/network buffer delay inherent to the Arduino R4 WiFi chip.
- **Zero-Allocation Packet Parsing:** Reads raw 2-byte UDP payloads directly into stack buffers without dynamic allocation.
- **Fail-Safe Timeout Watchdog:** Built-in 2-second timestamp tracking (`millis() / 1000`) that automatically kills motor PWM outputs if telemetry streams drop or simulator crashes.
- **Hardware PWM Duty Cycle Management:** Direct `analogWrite` timer control mapping 0-255 byte values to tactile vibration motor intensity on dedicated output pins.

---

## Story & Network Latency Discovery

This program listens on the local Ethernet network for 2-byte UDP packets to drive two small cellphone vibration motors mounted on my throttle and joystick.

- **Byte 0:** Vibration intensity for Motor 1 (Speed Brakes).
- **Byte 1:** Vibration intensity for Motor 2 (Flaps / AoA / Stall Cues).

### Why Ethernet Instead of WiFi?
While developing `TelemetryVib_WiFi`, I created the [`LocalNetworkDelay`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/LocalNetworkDelay/README.md) latency measurement tool to benchmark network packet roundtrips. I discovered that the WiFi subsystem on the Arduino R4 board introduces a consistent **20ms internal latency overhead**. By testing two C# programs on the same PC, I verified my home WiFi network was not the bottleneck—the delay was inside the ESP32/WiFi coprocessor firmware on the R4 board.

Switching to an Arduino Ethernet Shield 2 (W5500 SPI) completely eliminated that 20ms delay. Even though 20ms is barely perceptible in normal flight, eliminating it brought pure tactile instantaneity to every flap click and speed brake deployment.

---

## Hardware & Wiring Specifications

- **Microcontroller:** Arduino UNO R4 Minima / WiFi with Arduino Ethernet Shield 2 (W5500)
- **Motor 1 Pin:** Pin 3 (PWM — Speed Brake haptics on Throttle)
- **Motor 2 Pin:** Pin 6 (PWM — Flap / AoA haptics on Joystick)
- **Status LED:** `LED_BUILTIN` (Pin 13 — illuminates solid HIGH when UDP server is ready and receiving packets)

---

## Network Configuration & Protocol

- **Default MAC Address:** `A8:61:0A:AF:19:04` (Configurable in `setup()`)
- **Default Static IP:** `192.168.1.249`
- **Subnet Mask:** `255.255.255.0`
- **Gateway:** `192.168.1.1`
- **UDP Port:** `54671`

### Packet Payload Structure
Receives UDP packets containing 1 or 2 bytes:
- `buffer[0]` ➔ Motor 1 PWM duty cycle (`0` = stopped, `255` = maximum vibration)
- `buffer[1]` ➔ Motor 2 PWM duty cycle (`0` = stopped, `255` = maximum vibration)

---

## CPU QoS Implementation Status & Firmware ToDos

### CPU QoS Status
- **Hardware Firmware:** Not applicable (runs directly on bare-metal Arduino R4 ARM Cortex-M4 microcontroller).

### ToDos & Project Goals
- [x] Eliminate 20ms WiFi latency overhead using W5500 Ethernet shield.
- [x] Implement 2-second fail-safe timeout motor kill.
- [ ] **Unified Firmware Integration:** Merge `TelemetryVib_Ethernet` and `TelemetryVib_WiFi` into a single smart firmware that autodetects Ethernet link status on boot (`LinkON`), defaulting to Ethernet and falling back to WiFi when un-plugged.
- [ ] **LED Matrix Status Display:** Use the Arduino R4 WiFi built-in LED matrix to render animated status icons (UDP rx indicators, motor activation graphics).
- [ ] **Multi-Motor Expansion:** Expand PWM output pins if additional tactile zones (e.g. rudder pedals or seat shakers) are added.
