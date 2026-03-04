# WebScripts — VR headset-friendly controls + performance-first Windows automation

This folder is the “glue layer” for my gaming/VR rigs: lightweight web pages that generate **atomic JSON commands**, and a PowerShell orchestrator that reacts to **file rename events**, **process start/stop traces**, and **IPC** to apply repeatable tuning and stutter-hunting workflows. It’s built with an efficiency mindset: event-driven first, minimal overhead, and everything is optional per PC.

If you only read one thing: **`ps_scripts/Start-CommandWatchers.ps1` is the main orchestrator**. It can run standalone (process watcher + IPC) or pair with the web UI modules (remote control + War Thunder mission tools).

---

## Folder map

### `ps_scripts/` — the engine
- **Main entry point:** `Start-CommandWatchers.ps1`
- **What it does:**
  - Watches for JSON rename handoffs (remote commands, War Thunder mission JSON)
  - Subscribes to process start/stop events and triggers per-game policies
  - Hosts an optional named-pipe IPC server (fast “do X now” commands / light introspection)
  - Keeps itself low-impact (priority/QoS/affinity strategies) and uses a watchdog loop for resilience
- **Customization:**
  - `Gaming-Programs.ps1` holds per-host game profiles (power schemes, optional boosts, aux tools)
  - `config/hosts.config.json` enables/disables features per machine and overrides tuning

➡️ Start here: `ps_scripts/README.md`

---

### `remote_control/` — phone UI for “don’t take off the headset”
A simple web UI (PHP) that writes a command file as `command.tmp` and then renames it to `command.json`.
That rename is the trigger the PowerShell watcher listens for.

- Typical actions:
  - launch/kill apps
  - foreground/minimize/maximize/move windows
  - change/read power plans
  - trigger boost profiles
  - send messages to named pipes

There are also a few **experimental “commander” variants** (generated with different AIs) that explore nicer layouts and controls. They may change or be removed later; the stable contract is the JSON command output.

➡️ Read: `remote_control/README.md`

---

### `warthunder/` — optional War Thunder mission tooling
PHP pages that generate `mission_data.tmp` → rename to `mission_data.json`, which the PowerShell side can consume **when enabled for that PC**.

This is intentionally optional: I use it on one machine but not others, and it’s controlled via the same per-host feature toggles in `ps_scripts/config/hosts.config.json`.

➡️ Read: `warthunder/README.md`

---

## How the pieces talk (the 10-second architecture)

1) **Web UI writes a temp file**  
   Example: `command.tmp` or `mission_data.tmp`

2) **Web UI atomically renames it**  
   Example: `.tmp → .json` (rename avoids partial reads)

3) **PowerShell reacts to the rename**  
   `Start-CommandWatchers.ps1` catches the rename and dispatches the action (often through a JSON router script)

This same “event-in / action-out” approach is used across modules to keep overhead low.

---

## Quick start

### Minimal (no web UI)
- Edit `ps_scripts/config/hosts.config.json` to enable only what you want (often: process watcher + IPC)
- Run: `ps_scripts/Start-CommandWatchers.ps1`
- Customize per-game policies in `ps_scripts/Gaming-Programs.ps1`

### With web remote control
- Set up a local PHP environment (WAMP/XAMPP/etc.)
- Update paths in config/scripts as needed (paths in this repo are real examples from my rigs)
- Enable the remote command watcher for your machine in `hosts.config.json`
- Use the pages in `remote_control/` to emit `command.json`

### With War Thunder module
- Enable the War Thunder watcher for your machine in `hosts.config.json`
- Use the pages in `warthunder/` to emit `mission_data.json`

---

## Customization notes (read before you rage at “it doesn’t work”)

- **Paths are examples.** Some watchers reference real paths from my PCs (WAMP roots, JSON locations, etc.). If you enable those watchers, you’ll likely adjust paths to your layout.
- **Profiles are intentionally host-specific.** `Gaming-Programs.ps1` contains per-machine blocks. Copy/modify the block for your hostname and shape it to your rig.
- **Everything is opt-in.** If you don’t need a watcher, don’t enable it. The point is flexibility, not running every module everywhere.

---

## Where to go next

- For the “engine room” and full technical details: `ps_scripts/README.md`
- For the phone UI and JSON command format: `remote_control/README.md`
- For the War Thunder mission tooling: `warthunder/README.md`

---
