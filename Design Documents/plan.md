# plan.md — Crisp, Lagless Combat MVP (Synced to Research v1)

This plan is **1:1 aligned** with the latest `research.md` (Refined) for L2.005‑GA. All thresholds, states, and budgets here exactly match the research doc to avoid drift.

## Hub devices at a glance

| Device           | Role (source of truth) |
|------------------|------------------------|
| Save Terminal    | First-time registration and post-run debrief/banking (Imprint). |
| Amnion Vat       | Resuscitation if sufficient **Lumen** is banked. |
| Printer          | Craft and upgrade gear. |
| Locker           | Store crafted gear for later runs. |
| Descent Core     | Single-terminal elevator to dungeon. Hub has **no access doors**; only the elevator has a sealing shutter. |


---

## 0) Success Criteria (must‑meet)

* Latency (swing → first hit FX) **< 80 ms**
* **0 GC allocations per frame** in combat scenes
* CPU budgets (Quest-like): **Scripts ≤ 1.5 ms**, **Physics ≤ 2.0 ms**
* Draw calls **< 90** in dungeon (mirror off)

Plan enforces deterministic active-window hits, a hitbox visualizer for validation, and per-player bandwidth budget ≤1 KB/s sustained at 32 players.

---

## 1) Architecture (Simulation / View / Glue)

* **Simulation** (deterministic, local‑authoritative feel)

  * Weapon & enemy active‑window colliders
  * Hit registration → HP application → i‑frames / timers
  * AI decisions (10 Hz), waypoint pathing, CombatLoop (60 Hz), AI LOS every 3rd tick
  * Files: `MeleeWeapon`, `EnemyAttackHitBox`, `Damageable`, `PlayerDamageable`, `CombatLoop`, `EnemyAI`
* **View** (pure cosmetic)

  * Trails, sounds, hit flashes, haptics, ragdoll triggers
  * Files: `AttackAnimationHooks`, `RagdollController`, pooled FX
* **Glue**

  * `MasterRunController` (InHub → Generating → InDungeon → Cleanup → InHub)
  * `DungeonPresenceZone` (participant tracking, zone enter/exit)
  * `DungeonSpawner` (pools only; no runtime Instantiate)

**Networking authority**: single **instance‑owner `GameAuthority`** (enemy HP + alive flags). Players send **compact hit requests**. **Failover**: on owner leave, transfer to lowest `playerId` via `Networking.SetOwner(newOwner, GameAuthority.gameObject)` and call `RequestSerialization()` immediately.

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

  * **Cadence & thresholds**: 10 Hz; `CHASE_DIST²=25`; `ATTACK_DIST²=4`; `FOVcos≈0.1736`; LOS every 3rd tick on `Environment`.
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
* **Movement model**: Primary **portal navgraph (BFS)** for cross-tile routing; local steering stays on the tile’s `WaypointGroup`. Seam cooldown + owner-only tick policy per [`Navigation — Global Portal Navgraph (specs)`](#navigation-—-global-portal-navgraph-specs).

### `DungeonSpawner.cs`

* **Pools only**. Warm on scene load: \~**64 enemies**, **32 hit FX**, **32 audio sources**.
* Spawn only inside **active** tiles; return to pool on deactivation.

### `MasterRunController.cs` + `DungeonPresenceZone.cs`

* **Participant set** captured during elevator countdown (3–5 s).
* Run remains active while `participantsInDungeon > 0`.
* Late joiners stay in hub; queued for next run.
* Zone enter/exit debounce **200 ms** shared across presence + networking events.
* Zones toggle **AI ticks**, **enemy colliders**, **spawn routines**, **ambient SFX/VFX**.
* All zone events route through instance owner.

## Navigation — Global Portal Navgraph (specs)

### `DungeonGraphManager.cs`

*Purpose*: Global portal-node graph for cross-tile routing (primary model).

*Data (arrays only)*:
- `const int MAX_NODES = 128`, `const int MAX_NEIGHBORS = 4`
- `Vector3[] nodePos`
- `WaypointGroup[] nodeGroup`
- `int[] nodeGroupIndex`
- `int[,] neighbors` (size `MAX_NODES × MAX_NEIGHBORS`)
- `int[] neighborCount`
- Scratch: `int[] queue`, `int[] prev`, `bool[] visited`
- Optional: `int[,] nextHop` (precomputed first-hop table)

*API*:
- `int RegisterPortal(WaypointPortal p)`
- `bool LinkNodes(int a, int b)` // call for matched doorway pairs
- `void AutoLinkClosePairs(float maxDoorGap = 0.25f)` // proximity fallback
- `int GetNearestNode(Vector3 pos)`
- `int GetNodeForGroupIndex(WaypointGroup g, int idx)`
- `int GetPath(int startNode, int goalNode, int[] outPath)` // BFS, returns length
- `void PrecomputeNextHop()` // optional: fill `nextHop[src,dst]`

*Edge flags (future-proof)*:
- `EDGE_WALK, EDGE_WIDE_ONLY, EDGE_STAIRS, EDGE_FLY_ONLY, EDGE_QUIET, EDGE_LOCKED`
- Filter in BFS/Precompute by enemy capability mask.

*Perf targets*: O(V+E) per BFS; or O(1) hop with `nextHop`. Zero allocations after Start.

### `WaypointPortal.cs` (tile prefab component)

- Fields: `WaypointGroup localGroup; int localIndex; int nodeId (runtime)`
- Placed at each doorway; generator registers and links these.

### `EnemyNavigator.cs`

*Purpose*: Route across rooms via graph; step inside tiles via `WaypointGroup`.

*State*:
- `DungeonGraphManager Graph`
- `WaypointGroup Group` (current tile)
- Route buffers: `int[] route` (~32), `int routeLen`, `int routeIdx`
- Tick throttle: `const float AI_TICK = 0.05f;` (staggered globally)

*Flow*:
- `SetDestinationByNode(goalNode)`:
  - Compute path via BFS or iterate using `nextHop`.
  - Initialize `routeIdx = 0`.
- `Update()` (owner-only, every `AI_TICK`):
  - Move toward current portal’s `Group.GetPoint(portalIndex)` using existing local stepper.
  - On arrival: **hop** to next node → `Group = nodeGroup[next]; local index = nodeGroupIndex[next];`
  - Apply **seam cooldown** `0.3–0.5 s`.
  - `RequestSerialization()` only on state changes (route advance).

*Enemy type support*:
- `capabilityMask` filters edges; examples:
  - Brute: `WALK | WIDE_ONLY`
  - Drone: `FLY_ONLY`
  - Ranged: `WALK | QUIET_ROUTE`

### Generation integration

- After tiles are placed and stitched:
  - `RegisterPortal(...)` for all portals.
  - `LinkNodes(a.nodeId, b.nodeId)` for each matched doorway.
  - (Optional) `PrecomputeNextHop()` if using fast hop lookup.

### Acceptance (plan)

- Graph build **before** enemies spawn/enable.
- Owner-only navigation tick at **≥ 0.05 s**; **0 GC** in profiler.
- Door lock/unlock = flip an edge flag → routes adapt (BFS) or re-run `PrecomputeNextHop()` (small N).

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
* **Throttle**: **≤ 8 hit requests/s/player** (requests within 125 ms dropped).
* **HP sync**: batch at **2 Hz** and on **death events**; send diffs only.
* **Zone enter/exit**: debounced **200 ms**.
* **Failover**: on owner leave, call `Networking.SetOwner(newOwner, GameAuthority.gameObject)` for the lowest `playerId`; `RequestSerialization()` immediately.

### Tuning Guide

| Parameter | Effect | Safe Range |
| --------- | ------ | ---------- |
| CombatLoop AI cadence | Reaction speed vs CPU | Fixed at **10 Hz** |
| `CHASE_DIST²` | When enemies chase | **25** (tune ±5 if needed) |
| `ATTACK_DIST²` | Attack initiation radius | **4** (tune ±1) |
| `FOVcos` | Peripheral awareness | ≈ **0.1736** (150°–170°) |
| LOS cadence | Raycast load vs responsiveness | Every **3rd** tick |
| Hit throttle | Prevent spam | **≤ 8** requests/s/player (125 ms window) |
| HP sync rate | Network load vs freshness | **2 Hz** + on death |
| Zone debounce | Entry/exit churn filter | **200 ms** |
| Elevator countdown | Participant capture window | **3–5 s** |

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

---

## 9) Combat Effects & Ragdolls (Deterministic, Net-Safe)

Goals (extends §0 Success Criteria):
- Weapon VFX visible to all (synced triggers), zero GC, ≤3 state events/sec/enemy.
- Multi-attacker hits resolve deterministically per 10 Hz authority tick.
- Death = local ragdoll only. Non-lethal knockdown = local ragdoll + single recover snap.

Effect Tags (global, small set): Knockdown, Stagger, Push, SlowField, Tether, EMP, PhasePierce, Corrupt
(Weapons emit tags; enemies gate/shape them.)

Arbitration (authoritative @ 10 Hz):
- Exclusive: Knockdown (pick 1) → priority ▸ magnitude ▸ earliest ts ▸ playerId.
- Solo-owner: Tether (stronger replaces weaker; else ignore + Resist cue).
- Max-of: SlowField, EMP (apply strongest; refresh duration).
- Stacking (cap N=3): Corrupt (DOT/instability).
- Additive (clamped): Push (sum this tick, clamp; ignored while Knockdown).
- Soft CC: Stagger refreshes timer; never overlaps Knockdown.
- Anti-stunlock: 0.75 s CC-immunity after Knockdown ends (downgrade Knockdown→Stagger).

Ragdoll Policy:
- Death: EnemyDie → all clients play local ragdoll immediately; timed despawn; no post-death sync.
- Non-lethal Knockdown: StartKnockdown(dur) → local ragdoll; on end, authority sends one Recover(pos,rot) → snap & resume anim.
- Rigs pre-wired in prefab, pooled only, ≤ ~14 RBs, duration 1.5–2.0 s.

Networking (extends authority section):
- Client → Authority: HitRequest(enemyId, damageInt, effectTags[], impulseInt, ts, playerId)
- Authority → Clients (delta only, ≤3/sec/enemy):
  - EnemyDamaged(enemyId,newHp), EffectStart/End(enemyId,tag,params),
    ResistCue(enemyId,tag), StartKnockdown(enemyId,dur), RecoverFromKnockdown(enemyId,pos,rot), EnemyDie(enemyId), TetherOwner(enemyId,playerId,strength)

Exact Implementation Changes (file-by-file addendum):
- GameAuthority.cs: add 80–100 ms aggregation buffer; resolve effects per rules each 10 Hz tick; emit deltas only; ≤3 events/sec/enemy.
- EnemyAI.cs: add states Stagger, Knockdown, Recover; timers in fixed CombatLoop; disable locomotion/attacks/hitboxes during Knockdown.
- RagdollController.cs: add PlayKnockdown(float dur) and RecoverTo(TransformPose pose) alongside death flow.
- EnemyEffectController.cs (new spec): map EffectStart/End/ResistCue/TetherOwner → pooled VFX/SFX/material swaps/icon flags.
- MeleeWeapon.cs / EnemyAttackHitBox.cs: on overlap, build quantized HitInfo and push HitRequest(..effectTags[]); keep animation-gated windows & 32-id dedupe.
- VFX assets: one pooled prefab per effect tag; weapon trails/impact sparks are synced triggers (anim/event IDs), not per-frame net.


---

## 10) Hub Interactables (Tablet / Printer / Locker)

Tablet (Stick ↔ Tablet):
- Client-local logic, synced visibility (expand/compress bool, insert/eject anim, glow states).
- Acts as energy/currency store (session) and interaction key (proximity activation).

Printer (Constructor):
- Insert tablet → blueprint check (session vars) → timed print → output to Locker.
- Sync door/arm anim + SFX only; logic stays local.

Locker (Stasis Vault):
- Per-player slots (UI affordance). Synced open/occupied cues; items spawned from pools.

Exact Implementation Changes (addendum):
- TabletController.cs (spec): states Stick/Tablet, proximity activation, insert/eject hooks.
- PrinterController.cs (spec): Constructor UI/anim; local print timers; sends to Locker.
- LockerController.cs (spec): per-player visual slots; open/occupied states.


---

## Ranged Combat — Plan & Acceptance

**Plan**
- Add `RangedWeaponSpec` and `TabletChargerSpec` assets; optional `WeaponQualitySpec`.
- Implement **manual hold charging**: proximity ≤ `holdRadiusM=0.25 m`, tick at `10 Hz`, one shot added per `chargeTimeSec`.
- Shots live in the weapon **magazine**; firing consumes from magazine; **no auto siphon** from tablet mid-fight.
- Fire path: authority-validated **single-ray hitscan**; long `cooldownSec`; magazine capacity obeys `capacity` or `qualityTier` mapping.
- Networking throttles: `charge ≤ 10/s/player`, `shot ≤ 20/s/player`; diffs ≈ **2 Hz**; dedupe keys as in research.

**Acceptance**
- Charge completes in `chargeTimeSec ± 0.1 s` when held within `0.25 m`; progress ticks at **10 Hz**.
- Weapon capacity respects spec/quality (e.g., tiered: 1/2/3 shots).
- Cannot fire during `cooldownSec`; cannot overfill magazine; cannot auto-charge.
- Ammo correctness: Lumen is decremented only on **charge completion**; never double-charged.
- Perf under 8 players charging/firing: scripts ≤ **1.5 ms**, **0-GC**; state diffs **~2 Hz**; draw calls remain < 90.

## 11) Opt-in Leaderboard (This World Only)

**Goal:** Track per-player total **layer clears** for this world only, visible both in-world and on a minimal external site. Zero friction for players; opt-in governs any outbound beacons.

**Constraints:**
- Worlds can **only GET** URLs (no POST/sockets).
- No access to stable VRChat user IDs in Udon.
- Performance at 32 players; no spam; master-only outbound beacons.

**Identity & Opt-in:**
- Each player has **local persistence** via VRChat PlayerData:
  - `clears:int` – total layers cleared (authoritative for local UI).
  - `shareOptIn:bool` – whether to publish to site.
  - `shareToken:string` – random opaque token for the site (minted once).
  - `lastDisplayName:string` – last name we sent to site (for change detection).
- **Opt-in toggle** on the tablet. First enable → GET `/mint` to obtain a token; store `shareToken` and set `shareOptIn=true`.
- On join or name change, if `shareOptIn && shareToken` then GET `/link?token=…&name=<displayName>&world=cradle`.

**Submitting clears (master-only):**
- On layer clear: always update local `clears` in PlayerData.
- If `shareOptIn && shareToken` and **Networking.IsMaster**:
  - Compute `runId` when a run starts (short random string or start timestamp).
  - GET `/clear?token=…&layer=N&run=R&world=cradle`.
  - Server de-dupes by `(token, run, layer, world)`.

**Displaying the board in-world:**
- Tablet/terminal periodically GETs `/leaderboard.txt?world=cradle&limit=50` (every **30–60s**) and renders lines `rank|name|clears`.
- For local player, GET `/me.txt?token=…` → `clears=NN|title=…` to show rank/title on tablet.

**Privacy & UX:**
- Default is **opt-out**; no outbound calls until player enables opt-in.
- Site shows **current display name** associated with the token for this world only.
- No PII, no usernames, no VRChat IDs.

**Networking & Perf Budgets:**
- Only **instance master** sends `/clear` and `/link`.
- GETs are **event-based** (on clear, on join/name change) plus a passive **pull** every 30–60s for display.
- Keep total broadcasts ≤ existing budgets; no per-frame net.

**Success Criteria:**
- Clearing a layer increments local `clears` instantly and appears on the site within one event.
- Leaderboard text renders cleanly on tablet/terminal; no frame spikes; zero GC in hot paths.
- Works at 32p with 5–10 active enemies.

**Exact Implementation Changes (spec-only):**
- Tablet UI: add “Publish to Leaderboard (this world)” toggle and a small status line.
- **LeaderboardClient.cs (spec)**: helper to:
  - `MintToken()` → GET `/mint` and store `shareToken`.
  - `LinkDisplayName()` → GET `/link?token&name&world=cradle` if name changed.
  - `SubmitClear(layer, runId)` → GET `/clear?token&layer&run&world=cradle` (master-only).
  - `PullLeaderboard()` → GET `/leaderboard.txt?world=cradle&limit=50`.
  - `PullMe()` → GET `/me.txt?token=…`.
- Game flow: create `runId` on run start (elevator engage) and reuse for that run’s clears.
- PlayerData keys: `clears:int`, `shareOptIn:bool`, `shareToken:string`, `lastDisplayName:string`.

## 12) Clearance & Kill Ledger (Access-Only Progression; No Perks)

**Intent:** Make enemies and objectives rewarding without touching the Lumen economy. Players earn **Clearance** (operator authorization) and maintain a **Kill Ledger** (by type + totals) during runs. These only **persist** when the player returns to the hub and performs an **Imprint**. Clearance gates **difficulty tiers** (instance pressure) via a hub terminal; it never grants stat buffs or discounts. Lumen remains the sole currency for ammo, healing devices, Printer crafting, and GA trade.

### Hub Devices (Diegetic Save & Access)
- **Save Terminal** — registration kiosk tied to the tablet. Handles first-time registration and post-run debrief/banking (Imprint).
  - Debrief: This Run — Kills by type, Layers Cleared, Objective Beats, **Clearance Gained**
  - Post-Imprint: Lifetime Totals (by type), **Clearance Rank**

- **Amnion Vat** — ominous white-blue liquid clone vat. Resuscitates the operator on death if Amnion Reserve ≥ cost; unbanked runs are lost on wipe.
  - Recall: `"[AMNION ONLINE] Reserve debit authorized. Shell integrity restored."`
  - Death fiction: respawn is a fresh shell from the Amnion; any unspent **Lumen** is lost as already specified in the economy.

- **Depth Relay** — Consolidated into the Elevator’s Descent Core; see section “Elevator — Descent Core (Single Terminal, Tablet-Keyed)”.

### Earning Clearance (Run-Time Awards; Shared Credit)
Award Clearance on the same authoritative tick that finalizes enemy death/goal completion to avoid new message types.
- **Enemy defeats** (shared to all contributors who damaged within ~10 s and are within ~20 m; tunable):
  - Scav Drone **+2**, Husk **+3**, Guardian **+5**, Turret **+1**, Wraith **+4**, Miniboss **+20**
- **Objectives & exploration:**
  - Capillary Tap siphon **+1**
  - Reservoir Vat siphon **+3**
  - Dead Port reactivation **+2**
  - Shortcut opened **+2**
  - Layer clear **+15**
- **Optional anti-farm:** After ~30 defeats within the same layer, enemy-based Clearance awards in that layer are reduced by **50%** for that player; objective awards unaffected.

### Banking & Ranks (No Perks)
- Clearance and Kill Ledger **do not** persist mid-run. Bank only via **Imprint at the Save Terminal** (debrief displayed at the terminal).
- If a player dies or leaves before imprint, they keep previously banked Clearance but **lose the unbanked** portion from that run.
- **Rank thresholds** (tunable; access-only):
  - Clearance **I** at **100**
  - Clearance **II** at **250**
  - Clearance **III** at **500**
- Cosmetic titles may mirror ranks (Initiate → Courier → Warden → Overwatch → Archon); no gameplay effects.

### Difficulty Tiers (Instance Pressure; Access-Gated)
Set once at the **Descent Core** (single terminal); applied to the **next** descent.
- **SURVEY (Tier 0):** baseline
- **BREACH (Tier 1):** spawn budget **+10%**, enemy HP **+10%**, Reservoir yield **+1**
- **SIEGE (Tier 2):** budget **+20%**, HP **+20%**, elites more frequent, Reservoir **+2**
- **COLLAPSE (Tier 3):** budget **+35%**, HP **+30%**, miniboss guaranteed, Reservoir **+3**, blueprint extra roll **small chance**
Tiers never alter player stats or Lumen costs; they only change world pressure and certain world yields within performance budgets.

### Interop With Lumen (No Overlap)
- Lumen is harvested from **set points** and spent on **ammo** (weapon ports), **healing devices** (Mediports/Crucible), **Printer** (craft/upgrades), and **GA Ledger** (Scrip). Enemies do **not** drop Lumen.
- Clearance gates Depth tiers and fills the Kill Ledger; it never discounts or replaces Lumen systems.

### Tablet & Terminal Copy (Diegetic Lines)
- Save Terminal (Debrief header): `"Run Summary"`
- Depth Relay: Consolidated into the Elevator’s Descent Core; see section “Elevator — Descent Core (Single Terminal, Tablet-Keyed)” for copy.

### Networking & Performance (Consistent With Existing Spec)
- **Local logic** for all hub devices; sync only short **FX/anim toggles** (no per-frame net).
- **Depth selection** sets a single authority-owned **instance difficulty flag**; applied on next descent.
- **Clearance awards** piggyback on existing authoritative combat resolution; no new high-frequency messages.
- **No runtime instantiate**; Amnion/Relay/Save Terminal use pooled VFX with ≤2 s visuals.

### Success Criteria
- Players can finish a run, read Debrief, **Imprint once**, and unlock higher tiers via banked Clearance.
- Lumen economy remains intact and unchanged (see [EconomySpec](./research.md#economyspec)); 32-player hub stays performant and readable.



## 13) Elevator — Descent Core (Single Terminal, Tablet-Keyed)

**Intent:** Make the elevator the ritual that starts every descent. One **central terminal** in the elevator (the **Descent Core**) accepts exactly one tablet at a time (“Key Dock”). The docked tablet’s **owner** becomes the **Conductor** for that cycle. Tiers (SURVEY/BREACH/SIEGE/COLLAPSE) unlock based on the **owner’s banked Clearance rank**. Everyone can see the docked tablet and tier choice; only the **owner** can interact, preventing theft. The hub room has no access doors; only the elevator mechanism uses a sealing shutter.

### Device Identity & Presence
- **Descent Core (Key Dock + Tier Panel):** Waist-height **Key Dock** in the center ring; a tall **Tier Panel** above the doors shows the current selection and locks.
- **Tablet visibility:** The docked tablet is visible to all (full mesh, live Lumen vial, rank glyph). Non-owners cannot interact; the dock captures **ownerId** and blocks grabs/uses from others.
- **Launch control:** A waist-high **Launch bar** (or a large “BEGIN DESCENT” tile) arms and starts the cycle.

### State Machine
`IDLE → DOCKED(owner) → TIER_SELECT(owner) → ARMED → LAUNCHING → IDLE`

- **IDLE:** No tablet docked. Panel: “Dock key to select pressure.”
- **DOCKED(owner):** Owner’s tablet locked; panel shows owner’s **Clearance rank** and eligible tiers.
- **TIER_SELECT(owner):** Only owner can change tier; others see live updates.
- **ARMED:** Tier confirmed; 2 s arming (amber). Owner may cancel within the arm window.
- **LAUNCHING:** Doors close; authority broadcasts `Launch(tier, runId, runSeed)`; tablet **auto-ejects** to owner’s hand.

### Player Flow
1) Approach Core → tablet **auto-expands** at ~2 m.
2) Insert tablet → **Key Dock** captures `ownerId` and locks.
3) Panel reveals tiers **unlocked by the owner’s banked Clearance** (I/II/III).
4) Owner selects tier (SURVEY/BREACH/SIEGE/COLLAPSE). Locked tiers show the **required rank**.
5) Owner **confirms** (cooldown 5 s before another change).
6) Owner pulls **Launch bar** (or presses BEGIN DESCENT). 2 s arm → doors seal → descent.
7) Tablet **auto-ejects** as doors seal; owner resumes custody.

### Social Handoff (No Visual Halo)
- **Higher-rank swap:** If another player present has a higher banked rank and wants a deeper tier, they can press a **REQUEST CONTROL** plate near the railing.
- **Owner prompt only:** This plays a soft chime and shows the owner (and only the owner) a tablet line:  
  `“Higher Clearance requests control. Remove your key to hand over.”`
- **Owner decides:** Only the **owner** can eject their tablet. No auto-eject, no force. (Optional player setting: **Auto-eject on request** = off by default.)
- **AFK guard:** If the dock stays idle for **120 s**, the Core auto-ejects with: `“Dock idle. Key returned.”`

### Rank Gating & Access
- **Eligibility** is based solely on the **owner’s** banked Clearance rank (from the Save Terminal Imprint flow).
- If a tier is out of reach:  
  `“Access denied. Required: Clearance II.”`  
  The higher-rank player may **request control**, and the owner can hand over by ejecting.

### Networking & Authority
- **Authority vars per cycle:** `dockedOwnerId`, `tierSelection`, `armed`, `launching`, `runId`, `runSeed`, `lastRequestTs`.
- **Events (rare):** `TierConfirm`, `Launch(tier, runId, runSeed)`, `RequestControl` (owner-local prompt only).
- **No per-frame sync.** All visuals are local with **synced toggles**: panel text state, dock “occupied” light, door animation, brief arming cue.

### Anti-Theft & Visibility (Shared Station Pattern)
- **Owner-only interaction:** The dock records the inserting player’s id. Only that player (or station logic) can eject it.
- **Global visibility:** Others see the tablet and its live status (Lumen vial, rank glyph), but their grab/use colliders are disabled by the dock while occupied.
- **Timeout eject:** **120 s** idle auto-eject with owner notice; insert/eject debounced at **0.5 s**.

### Timing & Cooldowns
- **Tier-change cooldown (after confirm):** 5 s.
- **Launch arming:** 2 s (cancel allowed by owner within this window).
- **Idle auto-eject:** 120 s (configurable).

### Interop With Existing Systems
- **Clearance:** Reads **banked** rank; mid-run changes don’t apply until next Imprint.
- **Lumen economy:** Elevator does **not** consume Lumen; players prepare ammo/healing/printing at their stations before docking.
- **Launch bookkeeping:** On LAUNCHING, authority mints a new **runId** and **runSeed**; dungeon spawners consume `tier` + `runSeed`. (If a prior “Depth Relay” existed, this behavior is now **centralized** here.)

### Tablet & Panel Copy (diegetic)
- Idle: `Dock key to select pressure.`
- On dock (owner): `[KEY DOCKED] Clearance: II — Eligible: SURVEY / BREACH / SIEGE`
- Locked tier: `Access denied. Required: Clearance III.`
- Tier confirm: `Pressure set: SIEGE. Prepare descent.`
- Arming: `Door seal in 2…`
- Request to owner: `Higher Clearance requests control. Remove your key to hand over.`
- Auto-eject: `[KEY RETURNED] Maintain tablet custody.`

### Success Criteria
- One terminal controls tier and launch; zero tablet theft; selection is readable to all.
- Exactly one `Launch` event per cycle; pooled FX only; no GC spikes; 32-player hub remains stable.


## Loops

Loop/Feature name: Multi-attacker effect arbitration on a single enemy
Player flow (1–6 steps): 1) Several players hit same enemy; 2) Clients send HitRequest; 3) Authority resolves by class (Knockdown exclusive, Tether owner, max Slow/EMP, capped DoT, clamped Push, Stagger refresh); 4) Apply damage & update effects; 5) Broadcast one delta (+ Knockdown start/recover/resist); 6) Clients play/stop VFX; knockdown ends with single snap.
Entities involved: Players, Weapons, Enemy, GameAuthority, pooled VFX.
Authority & net: Authority 10 Hz; clients send compact hits; server sends delta only.
Ticks/timing: 80–100 ms aggregation; Knockdown 1.5–2.0 s; CC-immunity 0.75 s; DoT tick 0.5 s.
Data (SOs): WeaponSpec, EnemySpec, EffectCaps.
UI/ergonomics: Resist spark+tone; effect icons; tether owner indicator.
Success criteria: ≤3 events/sec/enemy; zero GC spikes; enemies never drift after non-lethal ragdoll (single recover snap); VFX readable with 32 players.

Loop/Feature name: Non-lethal Knockdown ragdoll (sync-safe)
Player flow (1–6 steps): 1) HitRequest with Knockdown potential; 2) Authority starts Knockdown; 3) Clients enable local ragdoll; 4) AI in Knockdown; 5) Authority computes recover pose; 6) Clients snap & resume.
Entities involved: EnemyAI, RagdollController, GameAuthority, VFX pool.
Authority & net: Start/Recover only; no per-frame physics sync.
Ticks/timing: Knockdown 1.5–2.0 s; CC-immunity 0.75 s.
Data (SOs): EnemySpec.thresholds, EffectCaps.
UI/ergonomics: Clear fall VFX; recover cue; zero jitter on snap.
Success criteria: Consistent cross-client timing; pooled rigs; no drift.

Loop/Feature name: Tablet→Weapon charging (visible to all)
Player flow (1–6 steps): 1) Hold stick-tablet to weapon port; 2) Insert anim + synced glow; 3) Local charge timer; 4) Remove tablet; cooldown; 5) Others see insert + glow; 6) Optional overcharge buff.
Entities involved: Tablet, Weapon, VFX pool.
Authority & net: Logic local; FX state toggles synced.
Ticks/timing: Charge 2–5 s; cooldown 1 s.
Data (SOs): WeaponSpec.chargeRate, fx ids; TabletSpec.
UI/ergonomics: Big holo text; audible charge tone.
Success criteria: Zero-GC; responsive; clearly visible to others.

Loop/Feature name: Constructor printing (weapon craft/upgrade)
Player flow (1–6 steps): 1) Insert tablet; 2) Check blueprint/energy; 3) Start print; 4) Output to Locker; 5) Tablet log updates; 6) Others see door/SFX.
Entities involved: Tablet, Printer, Locker.
Authority & net: Sync only anim/SFX; logic local.
Ticks/timing: Print 3–6 s.
Data (SOs): BlueprintSpec, WeaponSpec tiers.
UI/ergonomics: Lock icons; success/fail text.
Success criteria: No net spam; readable for bystanders.

Loop/Feature name: Leaderboard opt-in (this world)
Player flow (1–6 steps): 1) Player toggles “Publish” on tablet; 2) Mint token via /mint (store in PlayerData); 3) Link display name via /link (world=cradle); 4) Show confirmation on tablet; 5) On future joins, auto-link if name changed; 6) Player can toggle off anytime (stop outbound).
Entities involved: Tablet UI, LeaderboardClient (spec), PlayerData.
Authority & net: Client drives opt-in; outbound GETs are event-based; no per-frame net.
Ticks/timing: Name check on join; no polling beyond leaderboard pulls.
Data (SOs): none (config only); PlayerData keys as above.
UI/ergonomics: Simple toggle + status (“Published as <name>” / “Not publishing”).
Success criteria: No outbound calls unless opted-in; immediate feedback; stable across sessions.

Loop/Feature name: Submit clear + display leaderboard (this world)
Player flow (1–6 steps): 1) Player clears a layer; 2) Local PlayerData.clears++ (instant UI); 3) If opted-in and master → GET /clear?token&layer&run&world=cradle; 4) Tablet/terminal pulls /leaderboard.txt every 30–60s; 5) Render rank|name|clears; 6) /me.txt drives title badge.
Entities involved: GameAuthority (runId), LeaderboardClient (spec), Tablet/Terminal display.
Authority & net: Master-only submit; clients pull text for display.
Ticks/timing: Pull cadence 30–60s; submit on event.
Data (SOs): none; PlayerData keys as above.
UI/ergonomics: Big legible list; player highlighted if present.
Success criteria: No spam; board matches site; smooth at 32p.

Loop/Feature name: Debrief → Imprint (Bank Clearance & Ledger)
Player flow (1–6 steps): 1) Return to hub after a run; 2) Save Terminal tallies this-run kills, layer clears, and objectives and displays Clearance gained; 3) Imprint at the Save Terminal writes Clearance & Kill Ledger to persistence; 4) Tablet updates lifetime totals; Clearance rank may increase; 5) Proceed to the Descent Core or Printer; 6) Amnion Vat remains ready for recovery if AR ≥ cost.
Entities involved: Save Terminal, Tablet, PlayerData.
Authority & net: Local logic; synced device FX; write occurs on Save Terminal imprint; no per-frame net.
Ticks/timing: Debrief instant; Imprint interaction ≤2 s.
Data (SOs): none (uses configured tables; see research.md).
UI/ergonomics: “This Run / Lifetime” columns; clear one-line confirmation on success.
Success criteria: Idempotent write (no double-imprint), accurate lifetime totals, zero GC spikes.

Loop/Feature name: Depth Relay tier selection (Access via banked Clearance)
Player flow (1–6 steps): 1) Consolidated into the Elevator’s Descent Core (single terminal); 2) Owner docks tablet to surface eligible tiers; 3) Tier selection obeys the owner’s banked Clearance; 4) Confirm arm window (2 s) precedes Launch; 5) Launch(tier, runId, runSeed) fires once per cycle; 6) Tablet auto-ejects back to owner custody.
Entities involved: Descent Core (Key Dock + Tier Panel), Tablet, GameAuthority.
Authority & net: Tier flag chosen at Descent Core; Launch event carries tier.
Ticks/timing: Aligns with Descent Core confirm cooldown (5 s) and 2 s arm.
Data (SOs): DepthTierSpec (see research.md).
UI/ergonomics: Tier Panel messaging mirrors Descent Core copy.
Success criteria: Deterministic tier; zero duplication of gating devices.

Loop/Feature name: Kill Ledger (Tablet Page)
Player flow (1–6 steps): 1) Open tablet → Ledger page; 2) View “This Run” and “Lifetime” kill counts by enemy type + total; 3) Optional cosmetic badges on thresholds (no gameplay effects); 4) After Imprint, lifetime increments; this-run resets at new run; 5) Players compare ledgers in hub for social proof; 6) Ledger does not alter gameplay; purely informational and motivating.
Entities involved: Tablet UI, PlayerData.
Authority & net: Local read/write at imprint; no ongoing net traffic.
Ticks/timing: Instant UI updates post-imprint.
Data (SOs): EnemyType registry for labels/icons; ClearanceTable references.
UI/ergonomics: Single page table; top 5 enemy types + total; avoid scroll walls.
Success criteria: Always correct after imprint; zero ambiguity about banking moments.

Loop/Feature name: Single-terminal elevator (tablet-keyed)
Player flow (1–6 steps): 1) Player docks tablet at the Descent Core; 2) Panel unlocks tiers based on the owner’s banked Clearance; 3) Owner selects and confirms a tier (cooldown 5 s); 4) Owner arms launch (2 s) via Launch bar / BEGIN DESCENT; 5) Doors seal; authority broadcasts Launch(tier, runId, runSeed); 6) Tablet auto-ejects to owner as descent begins.
Entities involved: Descent Core (Key Dock + Tier Panel), Tablet, GameAuthority.
Authority & net: Authority owns dockedOwnerId, tier flag, and launch; clients sync FX toggles only.
Ticks/timing: Confirm cooldown 5 s; arm 2 s; idle auto-eject 120 s.
Data (SOs): CoreTerminalSpec, DepthTierSpec.
UI/ergonomics: Large tier tiles; clear locked reasons; owner-only controls.
Success criteria: No theft; deterministic launch; no per-frame sync; zero GC spikes.

Loop/Feature name: Request Control (polite handoff, no force)
Player flow (1–6 steps): 1) A higher-rank player presses REQUEST CONTROL near the Core; 2) Owner receives a chime and on-tablet prompt requesting handoff; 3) Owner may eject the tablet to hand control; otherwise nothing changes; 4) If owner ejects, the requester docks and selects tier; 5) Launch proceeds normally under the new owner; 6) If dock idle for 120 s, auto-eject returns key to owner.
Entities involved: Descent Core, Tablet(Owner), Tablet(Requester).
Authority & net: Authority sets lastRequestTs; owner prompt is local; no forced eject.
Ticks/timing: Request spam-guard 3 s; insert/eject debounce 0.5 s.
Data (SOs): CoreTerminalSpec (allowRequestCue, spam thresholds).
UI/ergonomics: Clear, polite copy; no grief vectors.
Success criteria: Fast human handoff when desired; no coercion; no stuck states.
## Combat (MVP) — Melee Staff & Geometric Enemies  *(2025-09-23)*

### Weapon: Melee Staff (Tip-Touch Kill)
**Core Rule:** An enemy is destroyed only when the **staff tip** overlaps the **enemy core** collider.

**Implementation notes:**
- Staff root holds `MeleeStaff`; child `Tip` holds `StaffTip` with a **primitive trigger collider** (Sphere/Capsule) and a **Kinematic Rigidbody**. No physics forces; just triggers.
- `StaffTip.OnTriggerEnter()` checks for `EnemyCore` on the other collider; on match it calls `MeleeStaff.OnTipTouchCore(core)`.
- Respect **EconomySpec rate limits** via `RateLimiter`: hits ≤ **8/s**; `minInterval` (e.g., 0.08s) guards rapid spam. No projectile/charge usage for MVP.

**Networking & perf:**
- No allocations in hot paths. Arrays only; no LINQ/lists.
- Owner-only logic for local effects; transforms replicated via **VRCObjectSync**. `RequestSerialization()` only on state changes (e.g., health).

**Colliders:**
- Tip = trigger + kinematic RB.
- Enemy core = **non-trigger primitive collider** (Sphere/Box). Orbiters = primitive colliders (non-trigger) to physically block the tip.
- No MeshColliders.

### Enemies: Geometric Core with Orbiters
**Shape Model:** Root enemy has a **central core** (red cube/sphere) plus several **orbiting components** (cubes/spheres) rotating around the core.

**Implementation notes:**
- Root holds `EnemyNavigator` for global pathing (BFS across `DungeonGraphManager`); **seam cooldown** unchanged.
- Root may hold `EnemyOrbitController` which updates orbiters’ positions **owner-only** (no GC); orbiters are plain children with primitive colliders.
- Root holds `Health`; core child holds `EnemyCore` referencing the same `Health`. A lethal touch applies large damage to ensure a kill.

**Navigation (unchanged):**
- Primary model is **Global Portal Navgraph (BFS)** in `DungeonGraphManager`.
- Graph must be built and **sealed** via `SealAndMarkReady()` before enabling enemies/spawners.

### Acceptance (Combat slice)
- Staff tip touching core kills enemy; touching orbiters does **not** kill (they block).
- No per-frame GC; arrays only; owner-only orbit ticks; transforms via `VRCObjectSync`.
- Rate limits respected (hits ≤ 8/s). Primitive colliders only.
- Enemies traverse tiles via portals once graph is sealed.
