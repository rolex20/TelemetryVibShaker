# PerformanceMonitor — Hybrid CPU Topology & GPU Monitor

## Tech-Flex
- **Win32 `GetSystemCpuSetInformation` Topology Engine:** Dynamically queries `SYSTEM_CPU_SET_INFORMATION` (winnt.h 32-byte layout) to parse efficiency classes, isolating E-Cores from P-Cores on hybrid Intel desktop processors.
- **Windows 11 EcoQoS & Power Throttling P/Invoke:** Directly invokes `SetThreadInformation` (ThreadInformationClass 3 with fallback to Class 1) to force background threads into low-power Efficiency Mode.
- **CPU Sets & Hard Processor Affinity:** Controls soft thread scheduling hints via `SetThreadSelectedCpuSets` and hard execution masks via `SetThreadAffinityMask` / `SetProcessAffinityMask` (constrained to single-group ≤ 64 logical cores).
- **Core Parking & QoS Tuning:** Directly observes and countermeasures Windows core parking behaviors that induce micro-stutters in flight simulators like DCS World.
- **NVAPI & Performance Counter Metric Pipeline:** Real-time hardware polling for GPU utilization, core temperatures, and dedicated video memory usage via `NvidiaGpu.cs`.

---

## Background & Thermal/Stutter Investigation

Monitors CPU utilization to compare Performance (P-Cores), HyperThreading (HT) logical cores, and Efficiency (E-Cores) visually, along with dedicated GPU metrics. 

I initially created this tool to debug severe micro-stutters in **DCS World 2.9 (Multi-Threading)** on my Intel Core i7-12700K (8 Performance cores + 4 Efficiency cores, 20 logical threads) paired with an ASUS TUF RTX 4090. Since then, I have upgraded my rig to an **Intel Core i7-14700K** (8 Performance cores + 12 Efficiency cores, 28 logical threads), and the current codebase reflects this expanded core topology.

### The DCS 2.9 Core Parking Discovery
During testing, I analyzed how different Windows Power Modes impacted DCS frame pacing:
- **Balanced Mode:** Windows frequently tried to assign game threads to the best boosted P-cores (e.g. Cores 4, 8, and 10 on my chip), but background apps would occasionally get scheduled on those same P-cores, causing sudden frame drops.
- **High Performance Mode:** Thread assignment was distributed more equally across all P-cores and HT threads, but Windows parked almost all Efficiency cores. When DCS 2.9 suddenly needed to read terrain or texture files from disk, the OS un-parked E-cores on the fly, creating micro-stutters.

These observations matched community findings on ED Forums:
- [Disabling Core Parking in Windows fixed my stuttering in menu and game](https://forum.dcs.world/topic/335866-disabling-core-parking-in-windows-fixed-my-stuttering-in-menu-and-game/)
- [MT freezes even in main menu (Page 19)](https://forum.dcs.world/topic/328792-mt-freezes-even-in-main-menu/page/19/#comment-5318289)

Using PerformanceMonitor, I verified how much load was being shifted to E-Cores. This prompted me to build background isolation scripts that force background software—ASUS Armoury Crate, Corsair iCue, Windows Search, ASUS GPU Tweak III, Fanatec software, and telemetry utilities—to run exclusively on E-Cores, leaving all 8 P-Cores completely uninhibited for the flight sim.

---

## Visual Comparison Interface

The major advantage of PerformanceMonitor is that it displays side-by-side graphical meters comparing P-Cores, HT threads, E-Cores, and GPU load in real time with minimal overhead:

![image](https://github.com/rolex20/TelemetryVibShaker/assets/62082564/d75b3043-80e7-4035-ba52-5b54a462ea15)

---

## Architecture & Code Components

- **`CPU_QoS.cs`:** Core topology engine. Queries system CPU set information, identifies hybrid architecture (`IsHybridCpu`), manages EcoQoS power throttling (`SetEcoQoS`), applies CPU Sets (`SetCpuSets`), and sets hard affinity masks (`SetHardAffinity` / `SetHardAffinityProcess`).
- **`ProcessorAssigner.cs` & `ProcessorAssignmentStats.cs`:** Manages process rules and collects assignment metrics.
- **`NvidiaGpu.cs`:** Queries NVIDIA GPU usage, core temperature, and VRAM utilization.
- **`Form1.cs`:** WinForms real-time rendering UI with tabbed monitors and diagnostic displays.

> [!NOTE]
> **Web Server Deprecation Notice:** PerformanceMonitor previously included a light HTTP web server on port 8080 used to reposition windows remotely while in VR. That web server component has been **deprecated and disabled** in favor of the centralized PowerShell and named-pipe IPC orchestrator in [`WebScripts/ps_scripts`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/WebScripts/README.md).

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Current Implementation:** `ACTIVE` (Primary reference implementation of the desktop `CPU_QoS.cs` architecture).
- **Target Architecture:** Desktop single-processor group (≤ 64 logical cores).

### ToDos & Recommended Improvements
- [x] Implement hybrid CPU topology discovery (`SYSTEM_CPU_SET_INFORMATION`).
- [x] Implement EcoQoS power throttling for background threads.
- [ ] Refactor `CPU_QoS.cs` to link directly with root [`CPU_QoS_Desktop.cs`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/CPU_QoS_Desktop.cs) shared source to prevent code duplication.
- [ ] Add support for AMD Ryzen 3D V-Cache CCO topology detection (targeting 3D V-Cache CCD cores vs standard CCD cores).
