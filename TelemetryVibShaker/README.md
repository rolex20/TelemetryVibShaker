# TelemetryVibShaker (README.md in progress)

## Real-Time Haptic and Audio Feedback for Flight Simulation

TelemetryVibShaker is a high-performance Windows application that provides real-time haptic (vibration) and audio feedback for flight simulation enthusiasts. Designed for use with DCS World, War Thunder, Falcon BMS and MSFS 2020 and compatible with hardware such as Arduino-driven vibration motors and the LILYGO T-Watch 2020 V3, it helps pilots optimize their turn rate, manage speed brakes, flaps, and landing gear through tactile and auditory cues.

**Key C# Technologies used:**
- Advanced multithreading and asynchronous programming (Thread, Task, Invoke/BeginInvoke)
- High-performance UDP server programming and interprocess communication (named pipes)
- Direct hardware integration (P/Invoke, DllImport, processor affinity, priority class, core assignment)
- Windows Forms (WinForms) UI with custom resource and settings management
- Audio processing and device management (NAudio, MediaPlayer, MMDeviceEnumerator)
- Efficient JSON parsing and serialization (System.Text.Json)
- Robust error handling, single-instance enforcement (Mutex), and performance monitoring (Stopwatch, custom stats)
- System-level optimizations for modern CPUs (Intel hybrid core support, background processing)

---

## Features
- **Real-Time Telemetry:** Receives and processes UDP telemetry packets from flight simulators.  See the additional programs/scripts to read/pull/extract Telemetry from DCS World, War Thunder, Falcon BMS, MSFS 2020.
- **Haptic Feedback:** Controls vibration motors via Arduino and T-Watch for speed brakes, flaps, gear, and AoA feedback.
- **Audio Feedback:** Plays customizable sound effects for optimal AoA and other flight states, with per-device volume control.
- **Performance Optimized:** Designed for minimal latency and efficient CPU usage, with advanced thread/core management.
- **Rich UI:** Tabbed Windows Forms interface for configuration, monitoring, and testing.
- **Remote Control:** Named pipe server for remote tab switching and control (e.g., from a web browser or mobile device).
- **Settings Persistence:** Automatic saving/restoring of all user settings and window state.
- **Single Instance:** Prevents multiple instances from running simultaneously.

---

## Installation
1. **Requirements:**
   - Windows 10/11
   - .NET 7.0 Runtime
   - Arduino (for vibration motors) and/or LILYGO T-Watch 2020 V3 (optional)
2. **Clone the Repository:**
   ```
   git clone https://github.com/yourusername/TelemetryVibShaker.git
   ```
3. **Build the Solution:**
   - Open `TelemetryVibShaker.sln` in Visual Studio 2022 or later.
   - Restore NuGet packages (NAudio, etc.).
   - Build the solution.
4. **Configure Hardware:**
   - Connect vibration motors to Arduino and/or T-Watch as described in the documentation.
   - Set the correct IP addresses and ports in the application.
5. **Run the Application:**
   - Launch `TelemetryVibShaker.exe` from the build output.
   - Configure sound files, JSON AoA definitions, and hardware settings via the UI.

---

## Usage
- Start your flight simulator and ensure it is sending telemetry to the configured UDP port.
- Use the UI to test vibration motors and sound effects.
- Monitor real-time feedback and statistics in the Monitor tab.
- Adjust settings as needed for your aircraft and hardware.

---

## Screenshots
*(Add screenshots of the main UI, settings, and monitor tabs here)*

---

## License
This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

---


## Contributing
Contributions, bug reports, and feature requests are welcome! Please open an issue or submit a pull request.

---

## Support
For questions or support, please open an issue on GitHub.
