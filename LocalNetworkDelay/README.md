# LocalNetworkDelay — UDP Latency Diagnostic Tool

## Tech-Flex
- **High-Precision Sub-Millisecond Stopwatch Timing:** Employs C# `System.Diagnostics.Stopwatch` high-resolution hardware performance counter to measure microsecond network roundtrips.
- **Synchronous UDP Datagram Echo Benchmarking:** Measures socket-to-socket datagram transmission and echo response processing across network interfaces.
- **Statistical Roundtrip & Point-to-Point Processing:** Computes mean roundtrip latency ($RTT$) and one-way point-to-point delay ($RTT / 2$) across multi-sample runs.

---

## Story & Internal Testing Findings

I created this lightweight C# tool during initial development to benchmark network latency between my gaming PC and various microcontroller hardware targets (Arduino R4 WiFi, Arduino R4 Ethernet Shield 2, and LILYGO T-Watch 2020 V3).

All tests were performed over my local home network (1Gbps Ethernet Switch):

| Target Hardware / Path | Measured Roundtrip Latency ($RTT$) | Point-to-Point Latency | Notes / Takeaway |
| :--- | :--- | :--- | :--- |
| **PC to PC (Local Network)** | `< 1 ms` | `< 0.5 ms` | Baseline 1Gbps Ethernet network speed. |
| **LILYGO T-Watch 2020 V3 (WiFi)** | `< 10 ms` | `< 5 ms` | Excellent performance for ESP32 WiFi stack. |
| **Arduino R4 Ethernet (W5500 Shield)** | `< 10 ms` | `< 5 ms` | Instantaneous physical response. |
| **Arduino R4 WiFi (Internal WiFi)** | `~ 20 ms` | `~ 10 ms` | **Proved 20ms internal firmware delay in R4 WiFi chip.** |

### The Critical Takeaway
This tool conclusively proved that my home WiFi network was not the bottleneck—the 20ms delay was inside the Arduino R4 WiFi coprocessor firmware. This finding inspired me to build the [`TelemetryVib_Ethernet`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/TelemetryVib_Ethernet/Readme.md) version to achieve zero-delay tactile haptics.

While I no longer run this diagnostic utility continuously, it remains in the repository as a key benchmark tool for evaluating new hardware devices.

---

## Usage

```cmd
LocalNetworkDelay.exe [Target_IP_Address]
```
If no IP address is provided, it defaults to `192.168.1.248` on UDP port `54671`.

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Console Diagnostic Tool:** Single-threaded console application; CPU QoS thread assignment is not required.

### ToDos & Project Goals
- [x] Measure WiFi vs Ethernet latency for Arduino R4 and T-Watch.
- [ ] **Add Jitter Calculation:** Output standard deviation and jitter metrics alongside average roundtrip latency.
