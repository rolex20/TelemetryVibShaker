# RemoteWindowControl — Standalone WinForms Window Manager

## Tech-Flex
- **Win32 Window Management P/Invoke:** Directly invokes user32 APIs (`SetForegroundWindow`, `ShowWindow`, `SetWindowPos`, `GetWindowRect`) to control target application window states.
- **Embedded HTML Remote Web Panel (`Remote.Control.html`):** Standalone web control interface for triggering window actions from mobile devices.
- **Named-Pipe IPC Listener & Tester (`TestPipeMessaging.ps1`):** Listens for IPC pipe commands to reposition, minimize, or maximize window handles remotely.
- **JSON Program Catalog (`programs.json`):** Configurable mapping of friendly process names to executable paths and window slots.

---

## Overview & Purpose

RemoteWindowControl is a standalone WinForms utility and web panel prototype designed to move, minimize, maximize, and bring application windows to the foreground over IPC.

When flying in VR across multi-monitor setups, kneeboards, telemetry overlays, or chat windows often open on the wrong monitor or fall behind full-screen sim windows. RemoteWindowControl allows you to manipulate window positions and states remotely without taking off the VR headset.

*System Evolution Note:* The core window manipulation logic developed in this project was later integrated directly into [`WebScripts/ps_scripts`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/WebScripts/ps_scripts/README.md) (`Set-WindowsPosition.ps1`, `Set-ForegroundProcess.ps1`) and [`WebScripts/remote_control`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/WebScripts/remote_control/README.md).

---

## Architecture & Code Components

- **`Form1.cs`:** WinForms interface for inspecting window handles and executing position moves.
- **`Remote.Control.html`:** Single-page web interface for triggering window movement commands.
- **`TestPipeMessaging.ps1`:** PowerShell testing script for verifying named-pipe IPC commands.
- **`programs.json`:** Executable paths and friendly names for target flight apps.

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Standalone Utility:** GUI utility. Process positioning logic has been migrated to `WebScripts`.

### ToDos & Project Goals
- [x] Implement Win32 window positioning (`SetWindowPos`) over IPC.
- [ ] **Multi-Monitor Coordinate Presets:** Add saved layout profiles (e.g. "Primary VR Mirror", "Secondary Monitor Kneeboard").
