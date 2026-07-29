# my_ttgo_TelemetryVib_WiFi — Wearable T-Watch 2020 V3 Haptic AoA Display

## Tech-Flex
- **ESP32 Dual-Core FreeRTOS Task Architecture:** Separates UDP network reception onto Core 0 while rendering TFT display graphics and motor PWM on Core 1.
- **Hardware ST7789 TFT Display Driver (TFT_eSPI):** High-speed SPI display rendering featuring dynamic color changes (Yellow, Green, Red) matching cockpit AoA indexer lights.
- **Embedded Haptic Motor Control:** Drives the internal LILYGO T-Watch vibration motor via hardware PWM timer control.
- **WiFi UDP Socket Engine & Power Management:** Listens for telemetry packets over WiFi with low-power AXP202 PMU power management.

---

## Overview & Purpose

This program runs on the **LILYGO T-Watch 2020 V3** (an ESP32-based smartwatch with an integrated ST7789 color display and vibration motor). It connects to the local WiFi network, listens for UDP telemetry packets from [`TelemetryVibShaker`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/TelemetryVibShaker/README.md), and provides real-time Angle of Attack (AoA) feedback directly on your wrist:

- **Visual Indexer Display:** The watch screen turns **Green** when the player's aircraft is within optimal AoA range (mimicking the F-16 cockpit AoA indexer lights), **Yellow** when slightly off-speed, and **Red** when stalling.
- **Wrist Haptic Vibration:** The internal motor vibrates during optimal AoA approach, delivering tactile wrist cues.

*Driver & Library Release:* [v0.9-ttgolibs release download](https://github.com/rolex20/TelemetryVibShaker/releases/tag/v0.9-ttgolibs)

---

## Architecture & Code Structure

- **`my_ttgo_TelemetryVib_WiFi.ino` / `main.cpp`:** Main entry point initializes AXP202 PMU power management, connects to WiFi, binds UDP socket port `54671`, and executes loop logic.
- **`UI_stuff.h`:** Manages screen layouts, color transitions (Green/Yellow/Red), font rendering, and motor vibration intensity.
- **`ScreenSaver.h`:** Low-power screen timeout watchdog to prevent display burn-in when no telemetry packets are received.
- **`config_hw.h` / `config_ip.h` / `wifi_secrets.h`:** Hardware pin definitions, static IP configuration, and WiFi credentials.

---

## Configuration & Flashing

1. Install Arduino IDE with ESP32 board support and `TTGO T-Watch Library`.
2. Edit `wifi_secrets.h` with your local WiFi SSID and WPA2 password.
3. Edit `config_ip.h` with your desired watch IP address.
4. Upload to LILYGO T-Watch 2020 V3 via USB-C.

---

## CPU QoS Implementation Status & Firmware ToDos

### CPU QoS Status
- **Embedded Hardware Firmware:** Not applicable (runs directly on ESP32 FreeRTOS dual-core microcontroller).

### ToDos & Project Goals
- [x] Implement WiFi UDP telemetry listener on ESP32.
- [x] Implement visual AoA color indexer display (Yellow/Green/Red).
- [ ] **Battery Status Indicator:** Display live AXP202 battery voltage and charge status overlay on UI.
- [ ] **Multi-Sim Graphics:** Add aircraft logo graphics (F-16, F-14, F/A-18) on screen depending on active aircraft profile.
