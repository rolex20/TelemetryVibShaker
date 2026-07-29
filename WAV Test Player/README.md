# WAV Test Player — NAudio Loop Stream Test Bench

## Tech-Flex
- **NAudio Dual-Device Simultaneous Playback:** Creates two independent `WaveOut` instances each bound to a different Windows audio device (`MMDeviceEnumerator`), allowing A/B comparison of sound file playback on different output endpoints.
- **Custom `LoopStream` Zero-Gap Seamless Looping:** Wraps `AudioFileReader` in a `LoopStream : WaveStream` that rewinds to position 0 the instant the source stream is exhausted, achieving gapless infinite looping for cockpit buffet audio.
- **`MMDeviceEnumerator` Audio Device Enumeration:** Lists all active `DataFlow.Render` output devices into a dropdown, so sounds can be routed to specific headphones, speakers, or VR audio endpoints.

---

## Story & Purpose

The cockpit buffet sound that plays when your AoA hits the optimal range needs to loop **perfectly**. Even a tiny gap or pop at the loop boundary is distracting mid-dogfight. I built this test player to:

1. Load any WAV file.
2. Play it through a selected audio output device.
3. Loop it continuously using the custom `LoopStream` class.
4. Compare how it sounds across different sound devices side by side.

The `LoopStream` class developed here was the prototype that later got copied directly into the main [`TelemetryVibShaker`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/TelemetryVibShaker/README.md) application for production use.

---

## Architecture & Code Components

- **`Form1.cs`:** WinForms UI for device selection, file picking, and playback start/stop.
- **`LoopStream.cs`:** Custom `WaveStream` subclass. `Read()` detects end-of-stream and resets `sourceStream.Position = 0` seamlessly, with an `EnableLooping` flag to switch to one-shot playback for testing.

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Internal Test Tool:** Single-process WinForms audio player. CPU QoS isolation not required.

### ToDos & Project Goals
- [x] Implement gapless `LoopStream` seamless WAV looping.
- [x] Implement multi-device A/B sound comparison.
- [ ] **Volume Fade-In / Fade-Out Test:** Add a slider to test gradual volume ramps, since TelemetryVibShaker uses dynamic volume scaling with AoA intensity.
