# research.md – L2.005-GA (VRChat World, Refined)

## 🧭 Purpose

This document maps out the current system structure of the VRChat world `L2.005-GA`, as seen in the Unity scene hierarchy and codebase. It is refined to remove generalizations — every system is explicitly described with intended behavior and relationships.

---

## 🌍 Scene Hierarchy Overview

```
VRDefaultWorldScene
├── Main Camera
├── Directional Light
├── EventSystem
├── Player
│   ├── PlayerHitBox (damage receiver)
│   └── Weapon (with MeleeWeapon + hitbox collider)
├── Hub
│   ├── HubRoom (social hangout)
│   ├── Elevator (teleporter to dungeon entry)
│   └── VRCWorld prefab (VRChat integration)
├── Dungeon
│   ├── D_Root (parent container for runtime dungeon tiles)
│   ├── Elevator_Dungeon (exit point / return path)
│   ├── Tiles_Pool (pooled modular blocks)
│   └── Enemies_Pool (pooled enemy prefabs)
│       └── Enemy prefab (with EnemyAI, EnemyHealth, Damageable)
├── Systems
│   ├── MasterRunController (global run/session state)
│   ├── DungeonGenerator (procedural room layout)
│   ├── DungeonSpawner (enemy/item spawns)
│   └── DungeonPresenceZone (activates content on player entry)
├── UI
│   ├── Canvas (2D fallback)
│   └── VRCanvas
│       ├── VRBlackout (fade layer)
│       └── BlackoutUIManager (fade control)
└── Debug
```

### Key Modular Systems

- **Hub**: Social space with elevator to dungeon. Will expand with class selection, shop, and progress display.
- **Dungeon**: Runtime-generated level built from modular 10×10m blocks defined in `TileMeta`.
- **Systems**: Centralized managers for runs, procedural generation, spawning, and zone activation.
- **UI**: Handles immersion effects (blackout fades).

---

## 📁 Script Inventory (Refined)

### ⚙️ Core Systems

| Script                   | Role                   | Explicit Behavior                                                                                                                                     |
| ------------------------ | ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| `MasterRunController.cs` | Orchestrates game runs | Tracks run state (InHub, InDungeon, Completed). Controls when generators/spawners are enabled. Handles fail/complete transitions.                     |
| `DungeonGenerator.cs`    | Procedural layout      | Builds dungeon by instantiating `TileMeta`-driven blocks into `D_Root`. Ensures entrances align at `(0,5)`, `(5,0)`, etc. Randomizes layouts per run. |
| `DungeonSpawner.cs`      | Enemy & item spawns    | Spawns enemies into `Enemies_Pool`. Uses pooling only (no Instantiate at runtime). Handles wave pacing per room.                                      |
| `DungeonPresenceZone.cs` | Zone activation        | Attached to each tile. When player enters, activates local enemies, props, and triggers spawn logic. When empty, deactivates to save performance.     |

### 🧱 Entity Components

| Script                 | Role                 | Explicit Behavior                                                                                                                                                   |
| ---------------------- | -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `EnemyAI.cs`           | State machine        | States: Idle → Patrol → Chase → Attack → Dead. Decision updates every 0.1s (10Hz). Movement delegated to Unity NavMesh. Attack windows tied to `EnemyAttackHitBox`. |
| `EnemyAttackHitBox.cs` | Enemy melee          | Collider activated only during active attack window. Calls `ApplyHit` on `PlayerDamageable`. No random crits or hidden modifiers.                                   |
| `PlayerHitBox.cs`      | Player hit detection | Collider for receiving hits. Only accepts hits from enemy colliders. Delegates to `PlayerDamageable`.                                                               |
| `PlayerDamageable.cs`  | Player health        | Implements `IDamageable`. Tracks HP, applies hits, triggers death/respawn logic. Handles i-frames via CombatLoop.                                                   |
| `Damageable.cs`        | Shared damage        | Base implementation for health + hit reaction. Reused by both enemies and players.                                                                                  |
| `MeleeWeapon.cs`       | Player weapon        | Uses trigger colliders enabled by animation events. Maintains dedupe list of hits per swing. Calls `ApplyHit` with `HitInfo`.                                       |
| `RagdollController.cs` | Physics death        | Enables ragdoll on death. Triggered by `Damageable.OnDeath`. Cosmetic only.                                                                                         |

### 🎮 Gameplay Utilities

| Script                  | Role                     | Explicit Behavior                                                                                                                 |
| ----------------------- | ------------------------ | --------------------------------------------------------------------------------------------------------------------------------- |
| `TileMeta.cs`           | Tile definition          | ScriptableObject with: entrance positions, theme ID, spawn points, light level. Drives procedural generator.                      |
| `ElevatorTeleporter.cs` | Hub ↔ Dungeon transition | Handles teleport events. Fades screen via `BlackoutUIManager`, repositions player, and triggers `MasterRunController` state swap. |

### 🖥️ UI and Effects

| Script                    | Role             | Explicit Behavior                                                                            |
| ------------------------- | ---------------- | -------------------------------------------------------------------------------------------- |
| `BlackoutUIManager.cs`    | Screen fade      | Performs timed fade in/out for teleport, death, respawn. Preallocated color buffers (no GC). |
| `BlackoutHudFollower.cs`  | Canvas anchor    | Locks blackout canvas to player camera. Updates via cached transform refs only.              |
| `AttackAnimationHooks.cs` | Animation events | Raises `BeginActiveWindow`, `EndActiveWindow`, `PlaySwingFX`. Attached to animation clips.   |

---

## 🔗 Explicit System Relationships

- `MasterRunController` orchestrates run state: **InHub → Generating → InDungeon → Cleanup → InHub**. It gates generators/spawners and owns the participant list for the active run.
- `DungeonGenerator` builds a tile graph using `TileMeta` and hands spawn points to `DungeonSpawner`. `DungeonPresenceZone` lives on each tile root and signals player entry/exit to the RunController.
- `DungeonSpawner` pulls from pools into active tiles only. Deactivates (returns to pool) when a tile goes inactive.
- Player weapon swings (`MeleeWeapon` + `AttackAnimationHooks`) produce local hit overlaps → compact hit requests to `GameAuthority` → authoritative HP update → view proxies (e.g., `EnemyHealth`) reflect results.
- All simulation timers (`Damageable`, i‑frames, AI decisions) tick via `CombatLoop`.

---

## 🧠 Enemy AI — State Machine (Deterministic, No GC)

**States:** `Idle`, `Patrol`, `Chase`, `Attack`, `Recover`, `Dead`.

**Tick Cadence:** 10 Hz via `CombatLoop` (no `Update`).

**Per‑Tick Sensing (cheap & deterministic):**

- **Distance check:** squared magnitude vs thresholds (no sqrt). `CHASE_DIST2 = 25m^2`, `ATTACK_DIST2 = 4m^2` (tunable per `EnemySpec`).
- **FOV check:** dot product vs precomputed cosine (no trig). Default FOV 160°.
- **LOS check:** single `Physics.Raycast` on `Enemy→Target` up to `LOS_MAX=12m`, layers: `Environment` only. Run every 3rd tick to amortize.

**Transitions:**

- `Idle → Patrol`: timer elapsed or player sensed outside attack range.
- `Patrol → Chase`: (distance^2 ≤ CHASE_DIST2) AND (FOV ok) AND (LOS hit == clear).
- `Chase → Attack`: (distance^2 ≤ ATTACK_DIST2) AND (LOS clear) AND (cooldown ≤ 0).
- `Attack → Recover`: immediately after active window ends.
- `Recover → Chase/Patrol`: cooldown elapsed AND target still sensed → `Chase`, else `Patrol`.
- `* → Dead`: HP ≤ 0.

**Movement Model (NavMesh‑free):** VRChat cannot bake navmeshes at runtime, and our dungeon is assembled dynamically. Therefore:

- **No Unity NavMesh at runtime.**
- Use **tile waypoints graph**: each `TileMeta` provides local waypoint nodes and edge connectors at entrances. On generation, the graph is stitched (O(N) joins) into a dungeon‑wide sparse graph.
- **Pathing:** lightweight A* on the graph (nodes ≤ ~8 per tile) when target tile changes; otherwise **steer‑to‑next‑node** with simple obstacle avoidance (two forward raycasts + side offset).
- **Chase fallback:** if LOS is clear, skip pathfinding and steer directly to player (fast path).

---

## 🧩 Dungeon Generator — Tile Adjacency & Randomization

**Tile Size:** 10 × 10 × 10 m. **Y=0** for all entries (no verticality v1).

**Entrances (local tile space):** centered edges at `(0,5,5)`, `(10,5,5)`, `(5,5,0)`, `(5,5,10)`.

**Adjacency Rules:**

- Only connect compatible entrances (edge‑to‑edge, facing inward normals).
- Prevent back‑to‑back duplicates unless `TileMeta.allowRepeat=true`.
- Ensure graph remains connected; maintain a `usedEntrances` bitmask per tile during build.

**Randomization Constraints (v1, tunable):**

- **Length:** 6–10 tiles total.
- **Branching factor:** 1.6–2.0 avg (limited side rooms).
- **Dead‑end ratio:** 15–25% of tiles.
- **Loop chance:** 10% (optional small cycles).
- **Boss/Reward room rule:** last depth tile must have ≥2 spawn points and higher loot weight.

**Future‑proofing for Verticality:**

- Reserve `TileMeta.type ∈ {Room, Hall, Corner, Junction, Shaft}`; v1 uses all but `Shaft`.
- Waypoint graph supports `y` offsets but v1 tiles all use `y=0`.

---

## 🎛️ DungeonPresenceZone — Run Participation & Activation

**Purpose:** ensure only **one dungeon run** is active at a time; clean join/leave semantics.

**Mechanics:**

- When the elevator is triggered, `MasterRunController` builds the dungeon and determines the **participant set** = all players currently inside the elevator volume during countdown (3–5s).
- `DungeonPresenceZone` on each tile reports **which participants** are inside. The run is active while `participantsInDungeon > 0`.
- **Late joiners** (not in participant set) remain in hub; cannot enter the active dungeon until the next run.
- **Run end:** when all participants either return to hub via elevator or die/respawn in hub, `participantsInDungeon == 0` → cleanup → ready for next generation.

**Activation Toggles per Zone:** enable/disable **AI ticks**, **enemy colliders**, **spawn routines**, **ambient audio/VFX**. Rendering left on (cheap) unless perf dictates otherwise.

**Network Behavior:**

- All signals go through `MasterRunController` on instance owner. Zones send local enter/exit → owner maintains participant counters.

---

## 🌐 Multiplayer Authority, Concurrency & Failover

**Authority Model:** single `GameAuthority` owned by **instance master** manages enemy HP/alive flags; players send compact hit requests.

**Concurrency Rules:**

- Hub is always open; **non‑participants** are ignored by dungeon logic until next run.
- Only the **participant set** affects dungeon state and receives dungeon events.

**Failover:**

- On `OnPlayerLeft`, if the leaving player was instance master, select next owner deterministically: lowest `playerId` among current participants, else among all players.
- Call `Networking.SetOwner(newOwner, GameAuthority.gameObject)` and re‑RequestSerialization(). The authoritative arrays persist; late owner change is seamless.

**Event Throttling (performance‑first):**

- **Hit requests:** max **8/s per player**; additional requests within 125 ms are dropped.
- **Enemy HP sync:** batch **2 Hz** (500 ms) or on state changes (death). Serialize diffs only.
- **Join/leave events:** debounced at 200 ms to avoid spam when players strafe on zone edges.

---

## 🗄️ Data‑First Design (Performance‑oriented)

- Use **ScriptableObjects** for immutable specs. Load once on scene start; keep references (no Resources.Load at runtime).
- **Arrays over Lists** in hot paths; preallocate capacities.
- **Quantize** floats to ints where viable (damage, HP) to reduce sync payloads.

**Schemas (summary):**

- `WeaponSpec`: `id`, `baseDamage`, `staminaCost`, `ActiveWindow[]` (start, end, windowId).
- `EnemySpec`: `id`, `maxHp`, `speed`, `CHASE_DIST2`, `ATTACK_DIST2`, `cooldowns`, `AttackPattern[]` (windup, active, cooldown, attackId).
- `TileMeta`: `tileId`, `type`, `entrances[]`, `spawnPoints[]`, `themeId`, `allowRepeat`.

---

## ✅ Research Close‑Out

With AI transitions, movement model, tile adjacency/randomization, zone semantics, authority, throttling, and data schemas made explicit, this `research.md` is implementation‑ready and matches our performance goals for a **VR‑first, 32‑player‑safe** world.

| Area                  | Explicit Plan                                                                                                         |
| --------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Multiplayer authority | One `GameAuthority` object owned by instance master. Handles enemy HP + alive flags. Players send compact hit events. |
| Data architecture     | Use ScriptableObjects for: `WeaponSpec`, `EnemySpec`, `TileMeta`. Centralized, no prefab scattering.                  |
| Simulation vs View    | Hard separation: simulation = CombatLoop, HP, state; view = VFX, sounds, ragdolls. No cross-dependencies.             |
| Custom update flow    | Implement `CombatLoop` as fixed-step manager. All Damageables, AI, and timers tick through it.                        |

---

## ✅ Next Step: Plan Phase

With the research clarified and explicit, proceed to `plan.md` for implementation details (combat focus, authority sync, and performance guarantees).
