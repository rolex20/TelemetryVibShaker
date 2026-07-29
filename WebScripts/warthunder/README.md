# War Thunder Mission Generator — VR Dogfight Builder

## Tech-Flex
- **Template-Driven Dynamic Mission Generator:** Reads master JSON configuration (`warthunder.json`) to dynamically populate aircraft, armament, weather, and map options without hardcoded PHP templates.
- **Cookie-Persisted State Engine:** Automatically saves user selections (`selectedPlayerUnit`, `selectedEnemyUnit`, `selectedTerrain`, `enemySkill`) to HTTP cookies for instant VR session reloads.
- **Atomic File-Watcher Handoff:** Writes mission data to `mission_data.tmp` before renaming to `mission_data.json` to trigger `WT_MissionType1.ps1` without file locking issues.
- **WTRTI / Mission File Injector:** Directly regenerates custom mission `.blk` files on the fly, eliminating the need to restart War Thunder.

---

## Story & Purpose

These PHP pages let me design and launch custom dogfights right from my phone: I tweak aircraft loadouts, terrain, weather, and AI skill levels on my phone, submit the form, and a PowerShell watcher instantly regenerates the mission so I can dive straight back into VR without touching the desktop keyboard or mouse.

---

## File Layout & Module Inventory

- **`dogfight_generator.php`:** Primary web interface for configuring player aircraft, opponent aircraft, AI skill level, weather, terrain, and flight altitude.
- **`dogfight_setup_1.php`:** Alternative UI layout pointing to the same mission generator backend.
- **`generate_mission.php` & `mission1.php`:** Core script generators that format mission parameters into JSON handoffs.
- **`viewer_setup_1.php`:** Mobile-friendly read-only mission parameters viewer.
- **`warthunder.json`:** Master data library for aircraft, armaments, maps, weather, and AI skill presets.
- **`mission_data.json`:** JSON handoff payload consumed by `ps_scripts/WT_MissionType1.ps1`.

---

## Data Schema Reference (`warthunder.json`)

```json
{
  "aircraft": [
    { "name": "f-16c_block_50", "displayName": "F-16C Block 50" },
    { "name": "mig-29_smt", "displayName": "MiG-29SMT" }
  ],
  "weather": ["clear", "good", "cloudy", "rain"],
  "terrains": ["stalingrad", "khalkhin_gol", "britain"]
}
```

---

## CPU QoS Implementation Status & ToDos

### CPU QoS Status
- **Mission Tooling:** Web frontend for mission creation. System QoS and process priority for `aces.exe` (War Thunder process) are managed by `ps_scripts` upon game launch.

### ToDos & Recommended Improvements
- [x] Implement cookie state persistence for instant loadout re-use.
- [x] Implement atomic `.tmp` ➔ `.json` file rename trigger for `WT_MissionType1.ps1`.
- [ ] **Central Data Service:** Refactor mission forms to consume a single PHP data class, eliminating redundant dropdown rendering code across generator pages.
- [ ] **Asynchronous Build Confirmation:** Add live AJAX polling to confirm when `WT_MissionType1.ps1` completes mission file generation.
