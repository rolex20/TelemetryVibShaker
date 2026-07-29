# MicroTaskScheduler — Background Windows Service for E-Core QoS Task Isolation

## Tech-Flex
- **Windows Service Architecture (`ServiceBase`):** Standalone background service managing system-level tasks with zero desktop interactive UI overhead.
- **Dynamic Processor Topology Detection:** Interrogates Windows Registry (`HARDWARE\DESCRIPTION\System\CentralProcessor`) to auto-detect CPU architecture (`Intel_12700K` vs `Intel_14700K`).
- **EcoQoS & E-Core P/Invoke Isolation:** Restricts background execution threads exclusively to E-Core hard affinity masks (`SetThreadAffinityMask`) and sets EcoQoS power throttling (`CPU_QoS.cs`).
- **Asynchronous Task Cancellation Engine:** Employs `CancellationTokenSource` and `Task.Run` loops for clean service shutdown and pause/resume lifecycle management.

---

## Overview & Purpose

MicroTaskScheduler is a specialized background Windows Service designed for gaming rigs. Its primary mission is to handle routine background chores—such as scheduled antivirus checks, hourly audio reminders, and background process maintenance—**exclusively on Efficiency Cores (E-Cores)**.  Believe it or not, the Windows Task Scheduler can't reliably sound alarms or do some of the other simple checks...

When running flight simulators like DCS World or MSFS 2020, background tasks waking up on Performance Cores (P-Cores) cause micro-stutters and frame drops. By pinning itself and its child micro-tasks strictly to E-Cores, MicroTaskScheduler ensures background work gets done without ever stealing CPU cycles from game threads.

*CPU Evolution Note:* MicroTaskScheduler includes explicit topology detection for both **Intel Core i7-12700K** (8P+4E) and **Intel Core i7-14700K** (8P+12E) hardware profiles.

---

## Architecture & Code Components

- **`MicroTaskSchedulerService.cs`:** Service lifecycle (`OnStart`, `OnStop`), topology inspection, background task loops, and E-Core affinity enforcement.
- **`CPU_QoS.cs`:** Windows 10/11 EcoQoS and CPU Set management engine.
- **`ProcessorAssigner.cs`:** Legacy processor assignment helper.
- **`ProjectInstaller.cs`:** `ServiceInstaller` and `ServiceProcessInstaller` configuration for installing via `InstallUtil.exe` or `sc.exe`.

---

## Installation & Service Management

### Install as Windows Service (Admin Command Prompt)
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe MicroTaskScheduler.exe
```

### Start Service
```cmd
net start MicroTaskSchedulerService
```

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Current Implementation:** `ACTIVE` (Uses `CPU_QoS.cs` to isolate service threads onto E-Cores).
- **Target Migration:** Planned migration to reference [`CPU_QoS_Desktop.cs`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/CPU_QoS_Desktop.cs) shared source.

### ToDos & Project Goals
- [x] Implement E-core thread isolation for Intel 12700K and 14700K.
- [ ] **Migrate to `CPU_QoS_Desktop.cs`:** Refactor `CPU_QoS.cs` to link directly with root `CPU_QoS_Desktop.cs`.
- [ ] **Dynamic Background Job Queue:** Add a JSON file watcher queue allowing external scripts to submit background micro-jobs to be run on E-cores.
