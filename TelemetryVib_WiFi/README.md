# TelemetryVib_WiFi — Arduino R4 WiFi Haptic Motor Firmware

## Tech-Flex
- **Embedded WiFi UDP Socket Listener:** Receives 2-byte binary telemetry payloads over 802.11 b/g/n WiFi.
- **Hardware PWM Duty Cycle Management:** Direct `analogWrite` timer control mapping 0-255 byte values to vibration motor intensity on dedicated pins.
- **Fail-Safe Timeout Watchdog:** Automatic 2-second timestamp check (`millis() / 1000`) that kills motor PWM outputs if telemetry packets stop receiving.
- **Low-Memory Static Buffer Allocations:** Zero-heap allocation packet loop preventing memory fragmentation.

---

## Story & Latency Discovery

This program runs on the **Arduino UNO R4 WiFi** board. It listens on the local WiFi network for 2-byte UDP packets to drive two small cellphone vibration motors mounted on my throttle and joystick:

- **Byte 0:** Motor 1 intensity (Speed Brakes).
- **Byte 1:** Motor 2 intensity (Flaps / AoA / Stall Cues).

### The 20ms Latency Discovery
While testing this wireless implementation with my [`LocalNetworkDelay`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/LocalNetworkDelay/README.md) latency measurement tool, I discovered that the Arduino R4 WiFi coprocessor adds an internal **20ms latency overhead** to incoming UDP packets. This discovery led me to develop the wired [`TelemetryVib_Ethernet`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/TelemetryVib_Ethernet/Readme.md) version using an SPI Ethernet shield, which completely eliminated the 20ms delay.

---

## Hardware & Wiring Specifications

- **Microcontroller:** Arduino UNO R4 WiFi
- **Motor 1 Pin:** Pin 3 (PWM — Speed Brakes)
- **Motor 2 Pin:** Pin 6 (PWM — Flaps / AoA)
- **Status LED:** `LED_BUILTIN` (Pin 13 — Solid HIGH when UDP server is ready)

---

## Configuration & Setup

1. Open `TelemetryVib.ino` in Arduino IDE.
2. Edit `arduino_secrets.h` with your WiFi SSID and password.
3. Upload to Arduino UNO R4 WiFi board.
4. Listen port: `54671`.

---

## CPU QoS Implementation Status & Firmware ToDos

### CPU QoS Status
- **Embedded Hardware Firmware:** Not applicable (runs directly on bare-metal Arduino R4 microcontroller).

### ToDos & Project Goals
- [x] Implement wireless UDP telemetry motor control.
- [x] Discover 20ms internal WiFi latency overhead via `LocalNetworkDelay`.
- [ ] **Unified Firmware Integration:** Merge with `TelemetryVib_Ethernet` into a single smart firmware that autodetects Ethernet link status, defaulting to Ethernet and falling back to WiFi.
