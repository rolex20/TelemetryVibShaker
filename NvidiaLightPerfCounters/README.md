# NvidiaLightPerfCounters — Standalone Lightweight NVIDIA GPU Metrics Service

## Tech-Flex
- **Custom Windows Performance Counter Provider:** Registers a native Windows Performance Counter Category (`GPU`) via `PerformanceCounterCategory.Create` to publish GPU metrics directly into PerfMon.
- **NVAPI C++ DLL Interop:** Invokes NVIDIA NVAPI library endpoints to sample GPU core clock, VRAM usage, fan speed, power draw in Watts, and core temperature.
- **Named-Pipe Metric Broadcaster:** Employs `NamedPipeServerStream` to broadcast GPU metrics to external monitoring tools.
- **Service Process Lifecycle Management (`ServiceBase`):** Low-overhead Windows Service polling loop with zero desktop UI footprint.

---

## Story & Background

This project is a standalone Windows Service created to poll NVIDIA GPU metrics (RTX 4090 / 30-series GPUs) and expose them through standard Windows Performance Counters (`% GPU Time`, `GPU Temperature`, `% GPU Memory Used`, `GPU Power Usage`).

*Historical Note:* While this standalone service was successfully built and tested, I later integrated the NVAPI GPU metric polling directly into [`PerformanceMonitor`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/PerformanceMonitor/Readme.md) (`NvidiaGpu.cs`) to eliminate the need for a separate Windows Service. However, this codebase remains fully functional and serves as an excellent reference for creating custom Windows Performance Counter providers.

---

## Architecture & Code Components

- **`PerfCounterService.cs`:** Service initialization, Windows Performance Counter category registration, sampling timer, and named-pipe broadcaster.
- **`GpuDevice.cs`:** NVAPI interface wrapper for enumerating physical NVIDIA GPU adapters and querying hardware sensors.

---

## Installation (For Standalone Use)

### Install Service
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe NvidiaLightPerfCounters.exe
```

### View Performance Counters
Open Windows `perfmon.mexe` and add counters under the newly registered **GPU** category.

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Standalone Service:** Low-overhead service. GPU polling calls are now built directly into `PerformanceMonitor`.

### ToDos & Project Goals
- [x] Implement NVAPI GPU temperature, power, and VRAM performance counters.
- [ ] **Modern NVML API Binding:** Upgrade NVAPI wrapper to support NVIDIA NVML (NVIDIA Management Library) for newer RTX 40-series ADA Lovelace GPUs.
