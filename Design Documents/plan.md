# plan.md — Crisp, Lagless Combat MVP (Synced to Research v1)

This plan is **1:1 aligned** with the latest `research.md` (Refined) for L2.005‑GA. All thresholds, states, and budgets here exactly match the research doc to avoid drift.

---

## 0) Success Criteria (must‑meet)

* **Local latency**: swing → first hit feedback **< 80 ms** (VR‑first).
* **Determinism**: Active‑window hits only; **no random crits/hidden modifiers**.
* **GC**: **0 allocations per frame** in combat scenes.
* **CPU budgets (Quest‑like target)**: **Scripts ≤ 1.5 ms**, **Physics ≤ 2.0 ms**.
* **Networking**: **≤ 1 KB/s per player avg** at **32 players**.
* **Draw calls**: **< 90** in dungeon (mirror off by default).
* **Hitboxes**: match weapon geometry **±3 cm**; require toggleable visualizer.

---

## 1) Architecture (Simulation / View / Glue)

* **Simulation** (deterministic, local‑authoritative feel)

  * Weapon & enemy active‑window colliders
  * Hit registration → HP application → i‑frames / timers
  * AI decisions (10 Hz), waypoint pathing, CombatLoop (60 Hz)
  * Files: `MeleeWeapon`, `EnemyAttackHitBox`, `Damageable`, `PlayerDamageable`, `CombatLoop`, `EnemyAI`
* **View** (pure cosmetic)

  * Trails, sounds, hit flashes, haptics, ragdoll triggers
  * Files: `AttackAnimationHooks`, `RagdollController`, pooled FX
* **Glue**

  * `MasterRunController` (InHub → Generating → InDungeon → Cleanup → InHub)
  * `DungeonPresenceZone` (participant tracking, zone enter/exit)
  * `DungeonSpawner` (pools only; no runtime Instantiate)

**Networking authority**: single **instance‑owner `GameAuthority`** (enemy HP + alive flags). Players send **compact hit requests**. **Failover**: on owner leave, transfer to **lowest playerId**, re‑serialize.

---

## 2) Exact Implementation Changes (file by file)

### `MeleeWeapon.cs`

* Use a **disabled trigger collider** (e.g., `SwingHitbox`) that matches weapon path.
* Enable only via `AttackAnimationHooks.BeginActiveWindow(windowId)`; disable on `EndActiveWindow`.
* Maintain a **per‑swing dedupe array (size 32)** of collider instanceIDs.
* Build a **quantized `HitInfo`** struct: `{ attackerId, enemyId, damageInt, attackId, tick }`.
* Call `IDamageable.ApplyHit(ref HitInfo)` on overlap.
* **No per‑frame raycasts**, no physics queries outside active windows.

### `AttackAnimationHooks.cs`

* Required events on clips: `BeginActiveWindow(int windowId)`, `EndActiveWindow(int windowId)`, `PlaySwingFX()`.
* `windowId` indexes `WeaponSpec.activeWindows[windowId]` (authoritative timing table).

### `EnemyAttackHitBox.cs`

* Mirror weapon pattern: animation‑gated trigger collider.
* On overlap, `ApplyHit(ref HitInfo)` on `PlayerDamageable`.

### `Damageable.cs`

* Implements `IDamageable` with: `int hp`, `bool alive`, `int iFrameTicks`.
* `ApplyHit` ignores hits while `iFrameTicks > 0`; otherwise apply `damageInt`.
* On HP ≤ 0: set `alive=false`, raise `OnDeath` event (view hooks only).

### `PlayerDamageable.cs`

* Extends `Damageable`. On death → blackout → teleport to hub via `ElevatorTeleporter`.
* Ticked by `CombatLoop` for i‑frames / regen.

### `CombatLoop.cs`

* Fixed **60 Hz** step: accumulate `deltaTime`, `while(accum ≥ 1/60) Step()`.
* Calls `Tick(dt)` on registered systems (Damageables, AI, stamina, cooldowns).
* **No per‑object Update** in any combat system.

### `EnemyAI.cs` (NavMesh‑free)

* **States**: `Idle`, `Patrol`, `Chase`, `Attack`, `Recover`, `Dead`.
* **Tick cadence**: **10 Hz** decisions via `CombatLoop`.
* **Sensing per tick** (cheap, deterministic):

  * `dist2` vs thresholds: `CHASE_DIST2 = 25`, `ATTACK_DIST2 = 4` (m²; from `EnemySpec`).
  * **FOV**: dot ≥ `FOVCos` (e.g., 160° FOV → cos(80°) ≈ 0.1736; value stored in spec).
  * **LOS**: single raycast to player on `Environment` layer, every **3rd** AI tick.
* **Transitions**:

  * `Idle → Patrol`: idle timer elapsed or player sensed beyond attack range
  * `Patrol → Chase`: dist2 ≤ CHASE\_DIST2 ∧ FOV ∧ LOS
  * `Chase → Attack`: dist2 ≤ ATTACK\_DIST2 ∧ LOS ∧ cooldown ≤ 0
  * `Attack → Recover`: active window finished
  * `Recover → Chase/Patrol`: cooldown elapsed; if still sensed → `Chase`, else `Patrol`
  * `* → Dead`: HP ≤ 0
* **Movement model**:

  * **Waypoint graph per tile** (from `TileMeta`), stitched at generation time
  * **A**\* when target tile changes; otherwise **steer‑to‑node** with 2 forward raycasts + side offset
  * **Direct chase** if LOS clear (skip pathfinding)

### `DungeonSpawner.cs`

* **Pools only**. Warm on scene load: \~**64 enemies**, **32 hit FX**, **32 audio sources**.
* Spawn only inside **active** tiles; return to pool on deactivation.

### `MasterRunController.cs` + `DungeonPresenceZone.cs`

* **Participant set** captured during elevator countdown (3–5 s).
* Run remains active while `participantsInDungeon > 0`.
* Late joiners stay in hub; queued for next run.
* Zones toggle **AI ticks**, **enemy colliders**, **spawn routines**, **ambient SFX/VFX**.
* All zone events route through instance owner.

---

## 3) Data‑First Design (schemas — immutable, loaded once)

### `WeaponSpec` (ScriptableObject)

* `id: string`
* `baseDamage: int`
* `staminaCost: int`
* `activeWindows: ActiveWindow[]` where `ActiveWindow { float start; float end; int windowId; }`

### `EnemySpec` (ScriptableObject)

* `id: string`
* `maxHp: int`
* `speed: float`
* `CHASE_DIST2: float` (default 25)
* `ATTACK_DIST2: float` (default 4)
* `FOVCos: float` (default ≈ 0.1736 for 160°)
* `cooldown: float`
* `attackPatterns: AttackPattern[]` where `AttackPattern { float windup; float active; float cooldown; int attackId; }`

### `TileMeta` (ScriptableObject)

* `tileId: string`
* `type: enum { Room, Hall, Corner, Junction, Shaft }` (v1 excludes Shaft)
* `entrances: Vector3Int[]` (edge‑center connectors)
* `spawnPoints: Vector3[]`
* `themeId: string`
* `allowRepeat: bool`
* `waypoints: Vector3[]` (local graph nodes for AI)

**Perf notes**: use arrays (not Lists) in hot paths; quantize with ints where viable; **no `Resources.Load` at runtime**; all SO refs cached on start.

---

## 4) Networking Details (32‑player safe)

* **Authority**: `GameAuthority` owned by instance master; arrays `enemyHp[]`, `enemyAlive[]` synchronized.
* **Hit requests**: players send `_ReqEnemyHit(enemyId, damageInt, attackId, attackerId)`.
* **Throttle**: **≤ 8 / second / player** (requests within 125 ms dropped).
* **HP sync**: batch at **2 Hz** or on **death events**; send diffs only.
* **Zone enter/exit**: debounced **200 ms**.
* **Failover**: on owner leave, set owner to **lowest playerId**; `RequestSerialization()` immediately.

---

## 5) Physics & Layers

* Layers: `Player`, `PlayerWeapon`, `Enemy`, `EnemyWeapon`, `Environment`, `IgnoreHit`.
* Matrix: only `PlayerWeapon × Enemy`, `EnemyWeapon × Player` collide.
* Weapon colliders are **kinematic triggers**; enabled strictly inside active windows.
* Keep default **Fixed Timestep = 0.02**; precision comes from animation windows, not heavier physics.

---

## 6) Telemetry, Debug & Tests

* **Hitbox Visualizer** (toggle): draw active volumes and per‑swing hit markers.
* **FOV/LOS Debug** (toggle): show FOV cone and LOS ray when sampled.
* **Latency probe**: on swing start → first hit FX time; assert median < 80 ms.
* **Repro dummy**: fixed‑animation target; 10 runs produce identical hit counts.
* **Load test**: 32 players × 10 active enemies (≈ 320 entities). Frame time < 16 ms; no GC spikes; requests within throttle.

---

## 7) Rollout Order

1. Author `WeaponSpec` windows → wire `AttackAnimationHooks` events.
2. Implement `MeleeWeapon` (active windows + dedupe) and `Damageable` / `PlayerDamageable`.
3. Add `CombatLoop` (60 Hz) and register all timers.
4. Implement `EnemyAI` (10 Hz) with LOS/FOV and waypoint steering.
5. Build **WaypointGraphBuilder**: stitch tile graphs at generation.
6. Add `GameAuthority` networking + throttling + failover.
7. Convert `DungeonSpawner` to warmed pools; gate by zones.
8. Add visualizers + telemetry; run acceptance tests and profiler passes.

---

## 8) Compaction Discipline

* After each step, record profiler + acceptance outcomes in `progress.md`.
* Compact context; start next step with only the relevant spec excerpts.
* If tests fail → fix the plan/research *first*, then re‑implement.

- FOV check: **FOVcos ≈ 0.1736** (160° FOV) using dot product.
