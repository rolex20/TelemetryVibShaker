# War Thunder mission PHP tools

These PHP pages let me design and launch dogfights from the couch: I tweak loadouts, terrain, and AI skill on my phone, and a PowerShell watcher instantly regenerates missions so I can dive back into VR without touching the desktop.

**Tech flex:** PHP 8+, JSON hand-off to PowerShell, cookie-based state, dynamic form rendering, mission templates driven by `warthunder.json`, and a rename-triggered pipeline caught by `WaitFor-Json-Commands.ps1` + `WT_MissionType1.ps1`.

## Why the features matter
- **Dogfight builder (`dogfight_generator.php`/`dogfight_setup_1.php`)** saves every selection to cookies so my favorite aircraft/armament combos are one tap away—perfect for rapid VR testing.
- **Mission data exporter** writes a temporary JSON then renames it (`mission_data.json`), guaranteeing the watcher sees a single clean rename event with no flapping.
- **Template-driven dropdowns** pull aircraft, weapons, weather, and terrain from `warthunder.json`, so expanding the library is just a data edit, not a code rewrite.
- **Viewer helpers (`viewer_setup_1.php`)** let me inspect mission parameters from the phone before committing, reducing headset-off moments.
- **PowerShell glue**: once `mission_data.json` flips, `WaitFor-Json-Commands.ps1` calls `WT_MissionType1.ps1` to rebuild the mission—no manual file copying or sim restarts.

## Architecture and file map
- `dogfight_generator.php` — primary form that captures mission parameters, persists them via cookies, and writes `mission_data.json` on submit (using a `.tmp` rename to trigger the watcher).
- `dogfight_setup_1.php` — alt layout for the same data capture, pointing at the same JSON output.
- `mission1.php` / `generate_mission.php` — legacy/simple generators that also write `mission_data.json` for the watcher.
- `viewer_setup_1.php` — read-only mission viewer useful on mobile.
- `warthunder.json` — master data for aircraft, armament, environments, weather, and maps.
- `mission_data.json` — the latest request consumed by `ps_scripts/WT_MissionType1.ps1` when the watcher notices the rename.

## Common updates
- **Add aircraft or weapons**: edit `warthunder.json`; dropdowns will reflect the new entries automatically.
- **Change mission defaults**: update the initial form values or cookie keys in `dogfight_generator.php` to match your favorite loadouts.
- **Integrate new mission types**: mirror the rename-and-watch pattern—write to `mission_data.tmp`, rename to `mission_data.json`, then create a matching PowerShell script to consume it.

## Future plans
1. **Central data service** so all mission forms read/write through one PHP include, cutting duplication across generator variants.
2. **Validation and presets** to ensure impossible combinations (e.g., wrong weapons for an airframe) never reach the PowerShell side.
3. **Async status callbacks** from `WT_MissionType1.ps1` back to the page (via AJAX or a small status file) to confirm mission build success.
4. **Modular CSS/JS** shared across the forms for faster UI tweaks and a consistent mobile feel.
5. **Mission history** stored server-side so I can replay or tweak recent sorties without re-entering everything.
