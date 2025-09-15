# L2.005‑GA — VRChat Game World (VR‑first, 32‑player‑safe)

**Status:** Design‑Docs First (specs locked, implementation next)

This repository hosts the living specs for a high‑performance, VR‑first **hub + dungeon** world for VRChat built with **UdonSharp**. Goals: **crisp melee combat**, **deterministic rules**, and **32‑player‑safe** multiplayer with an **instance‑master authority** model.

We follow a **spec‑first** workflow (Dex loop): research → plan → implement → compact. Code only begins once specs are green.

---

## 🎮 Core Loop

1. **Spawn in Hub** → socialize, mirror, check progress.  
2. **Prepare** → choose class/abilities, buy upgrades with run currency.  
3. **Group Elevator** → 3–5s countdown captures the **participant set**.  
4. **Dungeon Run** → modular rooms, enemies, loot, materials, XP.  
5. **Return to Hub** → bank rewards, upgrade, repeat.

**Late joiners** remain in hub and join the **next** run.

---

## 🧠 Architecture (synced to research.md)

- **Simulation vs View**: strict split. Simulation = combat logic, HP, timers, AI; View = VFX, audio, ragdolls.
- **Combat Feel**: animation‑gated trigger hitboxes; **0 GC/frame**; input→hit feedback **< 80 ms**.
- **AI Decisions**: **10 Hz** fixed cadence via `CombatLoop`; distance² thresholds (CHASE_DIST²=**25**, ATTACK_DIST²=**4**), **FOVcos≈0.1736** (160° FOV), **LOS** raycast **every 3rd tick** on `Environment` layer.
- **Movement Model**: **NavMesh‑free at runtime** (procedural dungeon). Enemies use a **stitched waypoint graph** per tile; light **A*** only when target tile changes; otherwise steer‑to‑node. If LOS is clear, steer directly.
- **Authority**: single **`GameAuthority`** owned by instance master manages `enemyHp[]`/`enemyAlive[]`. Players send compact hit requests.
- **Throttling & Sync**: ≤ **8 hit requests/s/player** (125 ms window). Enemy HP diffs serialized at **2 Hz** and on death. Zone enter/exit debounced **200 ms**.
- **Presence/Run Semantics**: only one dungeon run active. Participant set drives state while `participantsInDungeon > 0`. Late joiners stay hub‑side.
- **Failover**: Instance master leaves → transfer to lowest playerId; RequestSerialization()

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
- **Latency**: input→hit FX **< 80 ms**  
- **Determinism**: dummy target → identical hit counts across **10 runs**  
- **Load**: 32 players × 10 active enemies (≈ 320 entities) → frame **< 16 ms**, **0 GC spikes**  
- **Networking**: throttle respected (≤8/s/player); HP diffs at **~2 Hz**; seamless **owner failover** to lowest `playerId`

---

## 🤝 Contributing

- PRs **must** update `research.md`/`plan.md` when changing behavior.  
- Use the Dex loop: **research → plan → implement → compact**.  
- Attach profiler captures + acceptance notes in a `progress.md` entry.

---

## 🔒 License

TBD — choose a license that matches your goals (e.g., MIT for code, CC‑BY‑NC for art).
