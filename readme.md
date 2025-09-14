# L2.005‑GA — VRChat Game World (VR‑first, 32‑player‑safe)

**Status:** Design‑Docs First (specs locked, implementation next)

This repository hosts the living specs for a high‑performance, VR‑first **hub + dungeon** world for VRChat built with **UdonSharp**. The goals are **crisp melee combat**, **deterministic rules**, and **32‑player‑safe** multiplayer using an instance‑master authority model.

> We follow a **spec‑first** workflow (Dex‑style): research → plan → implement → compact. Code only begins once specs are green.

---

## 🎮 Core Loop (Player Experience)

1. **Spawn in Hub** → socialize, mirror, check progress.
2. **Prepare** → choose class/abilities, buy upgrades with run currency.
3. **Group Elevator** → capture the *participant set* for the run.
4. **Dungeon Run** → modular rooms, enemies, loot, materials, XP.
5. **Return to Hub** → bank rewards, upgrade, repeat.

**Late joiners** remain in hub; they’ll be eligible for the **next** run.

---

## 🧠 Architecture Highlights (from the specs)

- **Simulation vs View**: strict separation (combat logic, timers, AI decisions vs VFX, audio, ragdolls).
- **Combat Feel**: animation‑gated trigger hitboxes; 0 GC/frame; input→hit feedback < **80 ms** target.
- **AI**: deterministic 10 Hz decisions; distance² + FOV (dot) + amortized LOS raycast; **no NavMesh** (runtime dungeon).
- **Pathing**: stitched **waypoint graph** per tile; A* only when tile changes; steer‑to‑node otherwise.
- **Authority**: single **GameAuthority** on instance owner; players send compact hit requests; HP sync at **~2 Hz** and on death.
- **Throttling**: ≤ **8 hit requests/s** per player; zone enter/exit debounced **200 ms**.
- **Performance Budgets** (Quest‑like target): Scripts ≤ **1.5 ms**, Physics ≤ **2.0 ms**, Draw calls < **90** (mirror off).

---

## 📚 Documents (start here)

- [`research.md`](./Design%20Documents/research.md) — Complete system map (AI, generator, presence, authority, data schemas).
- [`plan.md`](./Design%20Documents/plan.md) — Implementation plan synced to research (file‑by‑file actions, tests, budgets).

> These two files are the single source of truth. Keep them in sync.

---

## 🛠️ Getting Started (after code lands)

> This repo currently ships **design docs** only. Implementation begins after the plan is approved.

When coding begins, the expected setup will be:

1. **Unity**: 2022.3 LTS (recommended).  
2. **VRChat Creator Companion**: install **SDK3 – Worlds** and **UdonSharp** into the project.  
3. **Clone** this repo into your Unity project root (or add as a sub‑folder under `/Assets/Project`).  
4. **Folders (expected)**:
   - `/Assets/Scripts/` — UdonSharp behaviours (`CombatLoop`, `GameAuthority`, `MeleeWeapon`, `EnemyAI`, etc.).
   - `/Assets/Specs/` — ScriptableObjects (`WeaponSpec`, `EnemySpec`, `TileMeta`).
   - `/Assets/Prefabs/` — Hub, elevator, tiles, enemies, pooled FX/audio.
   - `/Assets/Debug/` — visualizers (hitboxes, LOS/FOV, latency probes).
5. **Open** the hub scene and confirm one `CombatLoop` and one `GameAuthority` exist.
6. **Play**: verify hitbox visualizer and latency probe hit the success criteria.

---

## ✅ Implementation Milestones (from the plan)

1. Author `WeaponSpec` active windows → wire animator events.  
2. `MeleeWeapon` (window‑gated colliders + per‑swing dedupe), `Damageable` / `PlayerDamageable`.  
3. `CombatLoop` (60 Hz), register timers (i‑frames, stamina, cooldowns).  
4. `EnemyAI` (10 Hz) with distance² / FOV / amortized LOS + waypoint steering.  
5. Waypoint graph stitcher during dungeon generation.  
6. `GameAuthority` networking + throttling + failover to next lowest `playerId`.  
7. Convert spawner to warmed pools; zone‑gated activation.  
8. Visualizers + telemetry; run acceptance & load tests.


---

## 📈 Acceptance Tests (must pass)

- **Latency**: input→hit FX < **80 ms**.  
- **Determinism**: fixed dummy target yields identical hit counts across **10 runs**.  
- **Load**: 32 players × 10 active enemies (≈320 entities) holds < **16 ms** frame, **0 GC spikes**.  
- **Networking**: throttle respected; HP diffs broadcast at ~**2 Hz**; seamless master **failover**.


---

## 🤝 Contributing

- All PRs must include updates to **`research.md`** and/or **`plan.md`** when changing behavior.  
- Use the Dex loop: **research → plan → implement → compact**.  
- Attach profiler captures and acceptance test notes in `progress.md`.


---

## 🔒 License

TBD — add a license that fits your distribution goals (e.g., MIT for code, CC‑BY‑NC for art).

---

## 🗓 Changelog

- 2025-09-14 — Initial README created to match `research.md` and `plan.md` (design‑docs‑first repo).
