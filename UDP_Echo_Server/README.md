# UDP_Echo_Server — UDP Echo Diagnostic Tool

## Tech-Flex
- **Raw BSD Socket UDP Binding:** Directly binds a `Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)` to port `54671` for protocol-level diagnostic work.
- **Zero-Allocation Synchronous Echo Loop:** Receives datagrams and immediately bounces them back to the sender using `ReceiveFrom` + `SendTo` in a tight loop, with no intermediate buffering or parsing overhead.
- **Console Hex Dump Output:** Prints each received byte as `[N]` decimal values to the console, making it trivial to inspect raw packet contents.

---

## Story & Purpose

This is a simple console program I wrote during early development when I needed a fast way to verify that UDP packets were actually arriving from `TelemetryVibShaker` and the various telemetry exporter programs, **before** the Arduino or T-Watch firmware was ready.

By running UDP_Echo_Server on the target IP, I could substitute the real microcontroller as a receive endpoint, watch the packet bytes on screen, and confirm packet structure and delivery rates. It was also used in tandem with [`LocalNetworkDelay`](file:///c:/Users/ralch/source/repos/rolex20/TelemetryVibShaker/LocalNetworkDelay/README.md) — LocalNetworkDelay sends a packet and waits for the echo, and UDP_Echo_Server is what provides that echo on port `54671`.

> [!NOTE]
> This program is no longer actively maintained. The comment at the top of `Program.cs` notes it was a temporary testing tool. It remains in the repository as useful documentation of how the packet echo roundtrip works.

---

## Usage

```cmd
UDP_Echo_Server.exe
```
Listens on port `54671` and echoes every received UDP datagram back to the sender. Output appears on the console as byte values `[10][20]`.

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Legacy Console Tool:** Single-threaded synchronous echo loop. Not actively maintained.

### ToDos & Project Goals
- [x] Echo UDP datagrams on port 54671 for local network packet testing.
- [ ] **Async Echo Loop:** Rewrite with `UdpClient.ReceiveAsync` for non-blocking multi-client echo if ever needed again.
