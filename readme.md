# L2.005‑GA — VRChat Game World (VR‑first, 32‑player‑safe)

**Status:** Design‑Docs First (specs locked, implementation next)

This repository hosts the living specs for a high‑performance, VR‑first **hub + dungeon** world for VRChat built with **UdonSharp**. Goals: **crisp melee combat**, **deterministic rules**, and **32‑player‑safe** multiplayer with an **instance‑master authority** model.

We follow a **spec‑first** workflow (Dex loop): research → plan → implement → compact. Code only begins once specs are green.

## Hub devices at a glance

| Device           | Role (source of truth) |
|------------------|------------------------|
| Save Terminal    | First-time registration and post-run debrief/banking (Imprint). |
| Amnion Vat       | Resuscitation if sufficient **Lumen** is banked. |
| Printer          | Craft and upgrade gear. |
| Locker           | Store crafted gear for later runs. |
| Descent Core     | Single-terminal elevator to dungeon. Hub has **no access doors**; only the elevator has a sealing shutter. |


---

## 🎮 Core Loop

1. **Spawn in Hub** → socialize, mirror, check progress.  
2. **Prepare** → bank Lumen, craft/upgrade gear at the Printer, and check your loadout.  
3. **Group Elevator** → 3–5 s countdown captures the **participant set**. The hub room has no access doors; only the elevator mechanism uses a sealing shutter.
4. **Dungeon Run** → modular rooms, enemies, loot, materials, XP.  
5. **Return to Hub** → bank rewards, upgrade, repeat.

**Late joiners** remain in hub and join the **next** run.

---

## 🧠 Architecture (synced to research.md)

- **Simulation vs View**: strict split. Simulation = combat logic, HP, timers, AI; View = VFX, audio, ragdolls.
- **Combat Feel**: animation‑gated trigger hitboxes; **0 GC/frame**; input→hit feedback **< 80 ms**.
- **AI Decisions**: AI 10 Hz; CHASE_DIST²=25; ATTACK_DIST²=4; FOVcos≈0.1736; LOS every 3rd tick on `Environment`.
- **Movement Model**: **NavMesh-free at runtime**. Tiles use local `WaypointGroup` waypoints; cross-tile routing uses a **global portal navgraph** (doorways only) with **BFS** (fewest-door path) or an optional precomputed `nextHop` table. Owner-only AI ticks at **50–100 ms**; zero allocations in hot paths.
- **Ranged:** Lumen-charged by **manual hold** from the tablet; weapons have **multi-shot magazines** (by quality). Hitscan, single shot, long cooldown.
- **Authority**: single **`GameAuthority`** owned by instance master manages `enemyHp[]`/`enemyAlive[]`. Players send compact hit requests.
- **Throttling & Sync**: ≤ **8 hit requests/s/player** (125 ms window). Enemy HP diffs serialized at **2 Hz** and on death. Zone enter/exit debounced **200 ms**.
- **Presence/Run Semantics**: only one dungeon run active. Participant set drives state while `participantsInDungeon > 0`. Late joiners stay hub‑side.
- **Failover**: If the instance master leaves, authority transfers to the lowest `playerId`, then `RequestSerialization()` is called immediately.

**Performance Budgets (Quest‑like target):** Scripts ≤ **1.5 ms**, Physics ≤ **2.0 ms**, Draw calls **< 90** (mirror off).

---

## 🧩 Dungeon Generation (v1 constraints)

- **Tile size**: **10×10×10 m**; **Y=0** (no verticality v1; system future‑proofed for shafts).  
- **Entrances**: edge‑center connectors at `(0,5,5)`, `(10,5,5)`, `(5,5,0)`, `(5,5,10)`.  
- **Randomization**: length **6–10** tiles; branching **1.6–2.0**; dead‑ends **15–25%**; loop chance **10%**.  
- **Adjacency**: compatible edge pairing, connected graph, `usedEntrances` bitmask; `allowRepeat` per tile.

---

## 📚 Documents

- **Design** → [`Design Documents/research.md`](./Design%20Documents/research.md)  
- **Plan** → [`Design Documents/plan.md`](./Design%20Documents/plan.md)

> These two files are the single source of truth and remain in lock‑step.

---

## ✅ Milestones & Acceptance

**Milestones** (from plan): windows → weapon/damageable → CombatLoop (60 Hz) → AI (10 Hz) → waypoint stitcher → GameAuthority + throttling/failover → pooled spawner → visualizers/tests → Scripts ≤1.5 ms; Physics ≤2.0 ms; Draw calls < 90; 0 GC/frame.

**Acceptance (must pass):**
- Scripts **≤ 1.5 ms** (Quest-like target)
- Physics **≤ 2.0 ms** (Quest-like target)
- Draw calls **< 90** in dungeon (mirror off)
- **0 GC allocations per frame** in combat scenes
- **Latency**: input→hit FX **< 80 ms**
- **Determinism**: dummy target → identical hit counts across **10 runs**
- **Load**: 32 players × 10 active enemies (≈ 320 entities) → frame **< 16 ms**, **0 GC spikes**
- **Networking**: throttle respected (≤8/s/player); HP diffs at **~2 Hz**; seamless **owner failover** to lowest `playerId`

---

## MVP Loop Wiring

### TabletController (/scripts/World/)
- **tabletVisual**: NetworkedToggle component to show/hide tablet
- **prompt**: BillboardText component for UI prompt display

### ElevatorPortal (/scripts/World/)
- **destination**: Transform marking teleport destination position
- **blackoutUI**: GameObject for fade overlay (optional)

### EnemySpawner (/scripts/Enemies/)
- **enemyPrefab**: GameObject prefab for enemy instances
- **spawnPoints**: Transform array of spawn locations

### RangedWeapon (/scripts/Weapons/)
- **muzzle**: Transform for raycast origin point
- **limiter**: RateLimiter component for rate limiting

### DropItem (/scripts/World/)
- **type**: ItemType enum (Resource or Part)
- **id**: Type-specific ID for the item

### DropTable (/scripts/Enemies/)
- **dropPrefabs**: DropItem prefab array for possible drops
- **weights**: Float array of drop weights

### BankTerminal (/scripts/World/)
- **playerInventory**: Inventory component to deposit from
- **displayText**: BillboardText for showing balances

### PersistenceManager (/scripts/Core/)
- **tabletController**: TabletController for hasInitiated state
- **bankTerminal**: BankTerminal for bank balance persistence

---

## 🤝 Contributing

- PRs **must** update `research.md`/`plan.md` when changing behavior.  
- Use the Dex loop: **research → plan → implement → compact**.  
- Attach profiler captures + acceptance notes in a `progress.md` entry.

---

## 🔒 License

TBD — choose a license that matches your goals (e.g., MIT for code, CC‑BY‑NC for art).
