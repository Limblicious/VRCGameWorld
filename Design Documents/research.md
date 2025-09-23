# research.md – L2.005-GA (VRChat World, Refined)

## 🧭 Purpose

This document maps out the current system structure of the VRChat world `L2.005-GA`, as seen in the Unity scene hierarchy and codebase. It is refined to remove generalizations — every system is explicitly described with intended behavior and relationships.

## Hub devices at a glance

| Device           | Role (source of truth) |
|------------------|------------------------|
| Save Terminal    | First-time registration and post-run debrief/banking (Imprint). |
| Amnion Vat       | Resuscitation if sufficient **Lumen** is banked. |
| Printer          | Craft and upgrade gear. |
| Locker           | Store crafted gear for later runs. |
| Descent Core     | Single-terminal elevator to dungeon. Hub has **no access doors**; only the elevator has a sealing shutter. |


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
| `EnemyAI.cs`           | State machine        | States: Idle → Patrol → Chase → Attack → Dead. Decision updates every 0.1s (10 Hz). NavMesh-free at runtime; stitched per-tile waypoint graph; A* only on tile change; steer-to-node otherwise. Attack windows tied to `EnemyAttackHitBox`. |
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

### Script Inventory (additions)

- EnemyEffectController.cs — Plays pooled VFX/SFX/material swaps for effects.
- TabletController.cs — Player tablet state & proximity activation; insert/eject hooks.
- PrinterController.cs — Constructor UI/anim; local print timers; sends to Locker.
- LockerController.cs — Per-player visual slots; open/occupied states.
- LeaderboardClient.cs — Spec for Udon behavior that handles: /mint, /link, /clear, /leaderboard.txt, /me.txt calls via VRCStringDownloader. Master-only submit; passive pulls for display.
- LeaderboardDisplay.cs — Spec: parses leaderboard.txt (“rank|name|clears” per line) and updates TextMeshPro in tablet/terminal. Pull every 30–60s or on demand.
- Tablet UI (update) — Add opt-in toggle + status line; call LeaderboardClient methods.
- AmnionVatController.cs — Handles **Resuscitation only** (no registration, no imprint); short synced FX; debounces repeats.
- SaveTerminalController.cs — Handles first-time **Registration** and post-run **Debrief/Imprint** (bank/bind); shows summary UI; guards double-write via lastImprintRunId.
- DescentCoreTerminalController.cs — Single-terminal elevator controller: manages Key Dock ownership, tier list, confirm cooldown, arm/launch, and auto-eject. Emits Launch(tier, runId, runSeed) once; absorbs prior “Depth Relay” selection if it existed. The hub room has no access doors; only the elevator mechanism uses a sealing shutter.
- TabletDockBay.cs — Generic station bay behavior used by the Core (and reusable by Printer/Crucible/GA terminals): captures ownerId on insert, disables non-owner colliders, handles 0.5 s debounce and 120 s idle auto-eject.
- DepthRelayTerminal.cs — Difficulty selection consolidated into **Descent Core**; see DescentCoreTerminalController.cs.
- ClearanceManager.cs — Centralizes awarding of Clearance on authoritative death/objective resolution; buffers “this-run” values until Imprint; exposes lifetime after bank.

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

- **Cadence & thresholds:** 10 Hz decisions; `CHASE_DIST²=25`; `ATTACK_DIST²=4`; `FOVcos≈0.1736`; LOS every 3rd tick on `Environment`.
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

## 🧠 Navigation — Global Portal Navgraph (BFS)

**Primary model.** Cross-tile routing uses a tiny **global navgraph** composed of **portal nodes** (doorways). Edges connect opposing doorways when tiles are stitched at generation. On this graph, **BFS** returns the **fewest-door transitions** route (unweighted). Inside each tile, actors still move via the tile’s local `WaypointGroup`.

**Data model (arrays only, zero-GC):**
- **Nodes** = portals/doorways only. Target cap: **20–60** (upper bound ~120 on PC).
- `nodePos: Vector3[]`, `nodeGroup: WaypointGroup[]`, `nodeGroupIndex: int[]`
- Adjacency: `neighbors[node, slot]` with `neighborCount[node]`, **MAX_NEIGHBORS = 4**
- Scratch: `queue[]`, `prev[]`, `visited[]` (preallocated)

**Routing:**
- **BFS** for unweighted shortest-hop paths (O(V+E)); early exit on goal.
- Optional precompute: **`nextHop[src,dst]`** (e.g., 128×128 ints ≈ 64 KB) built once after stitching → O(1) hop lookup.

**Edge flags & capability masks (multi-enemy support):**
- Edge flags: `WALK`, `WIDE_ONLY`, `STAIRS`, `FLY_ONLY`, `QUIET_ROUTE`, `LOCKED`
- Enemy capability mask filters edges during BFS or precompute.

**Runtime policy:**
- **Owner-only AI** ticks (instance master or spawner owner).
- Tick throttle: **50–100 ms** per enemy (staggered).
- **Seam cooldown** 0.3–0.5 s after a portal hop to prevent oscillation.
- **Distance culling**: pause ticks for tiles with no nearby players.

**New specs (summary):**
- `DungeonGraphManager.cs` — registers portals, links neighbors at stitch time, serves BFS & optional `nextHop`. Arrays only.
- `EnemyNavigator.cs` — consumes graph routes; performs **portal-to-portal** hops while local movement uses the tile’s `WaypointGroup`.

**Acceptance (navigation):**
- Arrays-only (no `List<>`, no LINQ, no runtime allocations).
- Graph nodes **≤ 60** (tests include 100-node stress).
- Per-enemy ticks at **≥ 0.05 s**; **0 GC** in profiler.
- Late-joiners immediately see motion (owner drives; transforms via ObjectSync).

See detailed class specs in [`Design Documents/plan.md`](./plan.md#navigation-—-global-portal-navgraph-specs).

Legacy fallback: per-tile-only patrol loops remain for debug builds but are not the shipping behavior.
### Enemy AI — State Machine (update)
States: Idle → Patrol → Chase → Attack → Stagger → Knockdown → Recover → Dead
- Stagger: anim flinch; timer; cancels Attack.
- Knockdown: authority-driven; disables locomotion/attacks/hitboxes; timer.
- Recover: authority computes pose (root/hips or nearest valid tile) and broadcasts RecoverFromKnockdown(pos,rot); clients snap & resume anim.


## 🔫 Ranged Combat — Lumen-Charged, Manual Hold, Multi-Shot

**Intent:** Powerful, precision **single-shot** weapon that must be **manually charged** from the tablet. Weapons have a **magazine** (≥1) whose capacity may scale with **quality tier**. Charging is gated only by **time + proximity** (no enemy LOS gating).

**Data (ScriptableObjects)**
- `RangedWeaponSpec { weaponId, lumenPerCharge:int, chargeTimeSec:float, capacity:int, cooldownSec:float, spreadDeg:float, maxRangeM:float, qualityTier:int }`
- `TabletChargerSpec { holdRadiusM:float=0.25, tickHz:int=10, moveCancelSpeed:float=999f, damageCancels:bool=false }`
- *(optional)* `WeaponQualitySpec { tier:int → capacity:int, cooldownScalar:float, lumenPerCharge:int }`

**Weapon states**  
`Empty → Charging → Armed(n) → Firing → Cooldown → Armed(n-1)`  
- **Manual charge:** player **holds** the tablet’s charge port within `holdRadiusM` of the weapon’s port for `chargeTimeSec` to add **one shot** to the weapon’s magazine.  
- **No LOS gating:** charging does not check for enemies; it only requires proximity + time.  
- **Movement/damage cancel (designer-tunable):** default **off**; can be enabled via `moveCancelSpeed` / `damageCancels`.  
- **Firing:** authority-validated **single-ray hitscan**; `cooldownSec` is long. Shots consume **from the weapon’s magazine** (no auto siphon mid-fight).

**Ergonomics**  
`VRC_Pickup.AutoHold = Yes`; Exact Grip set. **Use** toggles hand-lock. Charging requires **holding weapon and tablet together** (no holster charging).

**Determinism**  
Charge progress ticks at **tickHz** (default 10 Hz). Lumen transfers **only on charge completion**. Dedupe key: `(playerId, weaponId, chargeIndex, tick)`. Weapon state diffs are serialized ascending by `weaponId`.

## ⚡ Lumen Economy (Ammo & Currency)

See [EconomySpec](./research.md#economyspec) for synchronized numeric tuning across documents.

## EconomySpec <!-- SOURCE OF TRUTH: Do not duplicate numeric values elsewhere -->

| Key                               | Value | Notes |
|-----------------------------------|-------|-------|
| Amnion: Resuscitation SURVEY Cost | 6 **Lumen** | Cost in **Lumen** deducted on recovery. |
| Amnion: Resuscitation BREACH Cost | 8 **Lumen** |  |
| Amnion: Resuscitation SIEGE Cost  | 10 **Lumen** |  |
| Amnion: Resuscitation COLLAPSE Cost | 12 **Lumen** |  |
| Amnion: Recommended Reserve Buffer | ≈2× tier cost | Tablet and Descent Core displays prompt this buffer. |
| Tablet→Weapon charge time         | See WeaponQualitySpec | Time to transfer charge; not usable mid-combat. |
| Weapon charge capacity by quality | See WeaponQualitySpec | Refer to WeaponQualitySpec. |

`Lumen` is the single integer resource used as **currency** and as the source for **ranged ammo**.
The **tablet** stores Lumen; **weapons store shots**. Manual charging moves `Lumen → shots` (one full charge = one shot). There is **no automatic mid-combat top-up**.

## 🗃️ Data-First Design

### Data-First Design (schemas — append)
WeaponSpec:
- Add: impulse:int, effectTags: EffectTagEntry[] { tag, power:int, durationTicks:int }, fxId_primary, fxId_overcharge

EnemySpec:
- Add: traits:Trait[], effectRules: Map<EffectTag, Affinity> (IMMUNE|RESIST|NORMAL|FRAGILE),
  thresholds: { minImpulseForKnockdown?: int }, staggerDurTicks:int, knockdownDurTicks:int

EffectCaps (new SO):
- dotMaxStacks:int=3, slowMaxPermille:int=600, pushMax:int, ccImmunityTicks:int=45, knockdownDurTicks:int=105

TabletSpec (rename from any legacy key-carrier spec if present):
- holdRadiusM:float, chargeTickHz:int, moveCancelSpeed:float, overchargeAllowed:bool

### Data Tables (config; ScriptableObjects or equivalent)
- `EnemyType` registry (align names with existing roster): ScavDrone, Husk, Guardian, Turret, Wraith, Miniboss, …
- `ClearanceTable`:
  - `perKill:{ ScavDrone:2, Husk:3, Guardian:5, Turret:1, Wraith:4, Miniboss:20 }`
  - `perObjective:{ DeadPort:2, Shortcut:2, ReservoirPuzzle:3 }`
  - `perLayerClear:int = 15`
  - Optional anti-farm: `perLayerSoftcap:int = 30`, `penaltyPercent:int = 50`
- `DepthTierSpec`:
  - `id:string` — SURVEY | BREACH | SIEGE | COLLAPSE
  - `requiredRank:int` — 0 | 1 | 2 | 3
  - `spawnBudgetMultiplier:float` — 1.0, 1.1, 1.2, 1.35
  - `enemyHpMultiplier:float` — 1.0, 1.1, 1.2, 1.3
  - `reservoirYieldBonus:int` — 0, +1, +2, +3
  - `minibossGuarantee:bool` — false, true, true, true
  - `blueprintExtraRollChance:float` — 0, 0.05, 0.10, 0.15

### Authority & Net (per Descent cycle)
- Authority state: `dockedOwnerId:int`, `tierSelection:int`, `armed:bool`, `launching:bool`, `runId:string`, `runSeed:int`, `lastRequestTs:float`.
- Events: `TierConfirm(tier)`, `Launch(tier, runId, runSeed)`, `RequestControl(requesterId)` (owner-local notification).
- No per-frame replication; only state toggles and single fire events.

### PlayerData / Settings (read-only here)
- Reads `clearanceRank:int` (banked) for tier eligibility.
- Optional player pref: `autoEjectOnHigherRankRequest:bool` (default false); not required for MVP.

### Config (ScriptableObjects)
- **CoreTerminalSpec**
  - `confirmCooldownSec:int = 5`
  - `armSeconds:int = 2`
  - `autoEjectIdleSec:int = 120`
  - `allowRequestCue:bool = true`
  - `ownerOnlyEject:bool = true`
  - `requestSpamGuardSec:int = 3`
- **DepthTierSpec** (reuse from Clearance section)
  - `id:string` = SURVEY | BREACH | SIEGE | COLLAPSE
  - `requiredRank:int` = 0|1|2|3
  - `spawnBudgetMultiplier:float`
  - `enemyHpMultiplier:float`
  - `reservoirYieldBonus:int`
  - `minibossGuarantee:bool`
  - `blueprintExtraRollChance:float`

### Tablet & Panel Strings
- Idle: `Dock key to select pressure.`
- On dock: `[KEY DOCKED] Clearance: {rank} — Eligible: {tiers}`
- Locked: `Access denied. Required: Clearance {rank}.`
- Confirm: `Pressure set: {tier}. Prepare descent.`
- Arming: `Door seal in 2…`
- Request (owner only): `Higher Clearance requests control. Remove your key to hand over.`
- Auto-eject: `[KEY RETURNED] Maintain tablet custody.`

### Performance Notes
- All visuals pooled: panel text, dock light, arming cue, door animation, launch siren.
- Insert/eject debounce 0.5 s to avoid collider thrash; no Update-driven allocation.
- With 32 players, only one Core is interactive; others see synced toggles only.

### Tablet Debrief & Ledger (UI strings)
- Debrief header: `Run Summary`
- Columns: `This Run` | `Lifetime`
- Rows: `Scav Drone, Husk, Guardian, Turret, Wraith, Miniboss, Total`
- Clearance line: `Clearance +{value}`
- Imprint confirm: `Actions archived to lattice.`
- Descent Core deny: `Access denied. Required: Clearance {rank}.`
- Descent Core confirm: `Pressure set: {tier}. Prepare descent.`

## ✨ Simulation & Networking Notes

### VFX Visibility Policy (Simulation ↔ View)
- Weapons: trails/pulses/impacts are synced triggers; no per-frame net.
- Tablet: expand/compress; insert/eject; charge glow — synced bool/anim events.
- Enemies: EffectStart/End drive pooled VFX (EMP flicker, Slow bubble, Tether line), hit flash (material swap).
- Ragdolls: death = local only; non-lethal = local with single recover snap.

### Networking Cadence & Budgets
- Authority tick: 10 Hz; hit aggregation window: 80–100 ms.
- Per-enemy broadcast budget: ≤3 state events/sec.
- No runtime Instantiate/Destroy in hot paths; all VFX/rigs pooled.
- Target: up to 32 players, 5–10 active enemies.

### PlayerData Keys (this world)
- `clears:int` — Total layers cleared (local authoritative for UI).
- `shareOptIn:bool` — Whether this player publishes to the site for this world.
- `shareToken:string` — Random opaque token minted from /mint, stored for reuse.
- `lastDisplayName:string` — Snapshot to detect rename; on change, call /link.
- `clearanceBanked:int` — Lifetime banked Clearance.
- `clearanceRun:int` — Accumulated this-run Clearance (reset on Imprint or new run).
- `killsLifetime:map<EnemyType,int>` — Lifetime kills by type.
- `killsRun:map<EnemyType,int>` — This-run kills by type (reset on new run).
- `clearanceRank:int` — Computed from `clearanceBanked` (e.g., 0..3).
- `lastImprintRunId:string` — Prevents double write for the same run summary.

### Networking Alignment
- Award Clearance within the **authoritative combat resolution** that already exists (same tick as death broadcast).
- Imprint performs the only PlayerData write for Clearance/Ledger; guard with `lastImprintRunId`.
- Depth tier is an authority flag consumed by spawn/encounter systems on the next descent.

### Performance Notes
- No per-frame network traffic added by these systems.
- All hub device FX must be pooled; visual durations ≤2 s.
- Debrief and Ledger are text-only tablet pages; avoid heavy UI prefabs.

### Networking Endpoints (external site, this world only)
- `GET /mint` → returns a random token string.
- `GET /link?token=…&name=<displayName>&world=cradle` → associates latest name to token for this world. Idempotent.
- `GET /clear?token=…&layer=N&run=R&world=cradle` → +1 clear; server de-dupes by (token, run, layer, world).
- `GET /leaderboard.txt?world=cradle&limit=50` → lines: `rank|name|clears`.
- `GET /me.txt?token=…` → `clears=NN|title=<tier>` for tablet UI.

### Net Hygiene & Budgets
- Only instance master fires `/clear` and `/link`. All clients may pull `/leaderboard.txt` (shared display object preferred).
- Pull cadence: 30–60s; avoid frequent polling.
- All outbound is GET; no per-frame calls; no POST/sockets.

### Titles (lore)
- Thresholds (local + site): 0–4 Initiate, 5–19 Courier, 20–49 Warden, 50–99 Overwatch, 100+ Archon. Tablet renders title from local `clears`; site mirrors same logic.

### Security / Abuse Notes
- Site de-dupes by (token, run, layer, world); add basic rate limits per token/IP.
- Token is opaque and stored only in PlayerData; no PII; display name is public label only.
- If desired, add `/rotate` server endpoint later to replace a token (not required for MVP).

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

- When the elevator is triggered, `MasterRunController` builds the dungeon and determines the **participant set** = all players currently inside the elevator volume during countdown (3–5 s).
- `DungeonPresenceZone` on each tile reports **which participants** are inside. The run is active while `participantsInDungeon > 0`.
- **Late joiners** (not in participant set) remain in hub; cannot enter the active dungeon until the next run.
- **Zone enter/exit** signals are debounced at **200 ms** to prevent churn on boundaries.
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

- On owner leave, transfer to lowest `playerId`; call `Networking.SetOwner(newOwner, GameAuthority.gameObject)` and `RequestSerialization()` immediately.

**Event Throttling (performance‑first):**

- **Hit requests:** max **8/s per player**; additional requests within 125 ms are dropped.
- **Enemy HP sync:** batch **2 Hz** (500 ms) or on state changes (death). Serialize diffs only.
- **Join/leave events:** debounced at 200 ms to avoid spam when players strafe on zone edges.
- **Charge requests:** ≤ **10/s per player**; processed on 10 Hz charge tick; spend Lumen on completion; diff sync **~2 Hz**.  
- **Shot requests (ranged):** ≤ **20/s per player**; authority recomputes ray from seed + muzzle; HP diffs **~2 Hz** (+ immediate on death).  

---

### 🧩 Modularity & Overrides
- **Bricks (reusable):** `RangedWeapon`, `TabletCharger`, `MeleeWeapon`, `Damageable`, `EnemyAI`, `CombatLoop`.  
- **Glue (world-specific):** `MasterRunController`, `DungeonGenerator`, `DungeonPresenceZone`, `DungeonSpawner`.  
- Each brick reads tunables from its **own ScriptableObject**. Prefabs may **override** spec references per instance to create unique feel (capacity, cooldown, spread, charge time, Lumen cost).

---

## 🗄️ Data‑First Design (Performance‑oriented)

- Arrays over Lists in hot paths; no `Resources.Load` at runtime; quantize ints where viable.
- Use **ScriptableObjects** for immutable specs. Load once on scene start; keep references warm.

**Schemas (summary):**

- `WeaponSpec { id, baseDamage:int, staminaCost:int, ActiveWindow[]{ start, end, windowId } }`
- `EnemySpec { id, maxHp:int, speed:float, CHASE_DIST2:float=25, ATTACK_DIST2:float=4, FOVcos:float≈0.1736, cooldown:float, AttackPattern[]{ windup, active, cooldown, attackId } }`
- `TileMeta { tileId, type: Room|Hall|Corner|Junction|Shaft, entrances:Vector3Int[], spawnPoints:Vector3[], themeId, allowRepeat:bool, waypoints:Vector3[] }`
- `RangedWeaponSpec { weaponId, lumenPerCharge:int, chargeTimeSec:float, capacity:int, cooldownSec:float, spreadDeg:float, maxRangeM:float, qualityTier:int }`
- `TabletChargerSpec { holdRadiusM:float, tickHz:int, moveCancelSpeed:float, damageCancels:bool }`
- `WeaponQualitySpec { tier:int → capacity:int, cooldownScalar:float, lumenPerCharge:int }`
- `EnergySpec { id:"Lumen", maxTablet:int, maxBank:int }`

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
## Combat — Staff Tip vs Enemy Core  *(2025-09-23)*

**Intent:** Replace generic melee with a **precision touch** mechanic that is readable in VR and cheap to simulate.

**Mechanic:**
- Kill condition: `StaffTip` trigger enters `EnemyCore` collider.
- Orbiters provide moving coverage that creates timing/gap reads without AI-heavy decisions.

**Spec alignment:**
- **EconomySpec:** Use `RateLimiter` to cap interactions at ≤ 8 hits/s. Charge ≤ 10/s remains reserved for future powered devices.
- **Networking:** Owner-only orbit updates; transforms replicate via VRCObjectSync. Serialize only on state change (e.g., health).
- **Perf:** No allocations in Update; primitive colliders throughout; pooled FX if any.

**Failure modes & mitigations:**
- Tip-trigger missing rigidbody → **require Kinematic RB** on tip.
- Core using trigger collider → must be **non-trigger** so entering tip (trigger) fires reliably.
- Excessive orbit speed → cap angular speed per-spec; keep deterministic radii/phase arrays (no lists).
