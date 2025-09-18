# Claude Code — UdonSharp Project Staging (VRChat)

**Repo mode:** staging-only. All scripts live under `/scripts/UdonSharp/...` and will later be copy-pasted into Unity. Do **not** move or rename design docs.

**Source of truth (read-only):**
- Design Documents/research.md  → Navigation (Global Portal Navgraph BFS), EconomySpec
- Design Documents/plan.md      → Navigation specs, Hub devices, Weapons
- Design Documents/lore.md, readme.md → Roles & constraints

**Environment:**
- Unity + VRChat SDK3 (Udon), UdonSharp (C# → Udon)
- No GC allocations on hot paths; arrays only (no Lists/LINQ/closures; no `new` inside Update/ticks)
- Network: owner-only AI ticks every 0.05–0.1 s; use VRCObjectSync for transforms; call `RequestSerialization()` only on state changes

**Global constraints:**
- Primary navigation model: **Portal Navgraph (BFS)**
- Rate limits per EconomySpec: hits ≤ 8/s, charge ≤ 10/s
- Colliders must be primitives
- Use object pooling for FX/projectiles

**Acceptance (for Phase A):**
- Compiles clean in UdonSharp when moved to `Assets/UdonSharp/...`
- Graph built & sealed before enemies enable; enemies traverse tiles via portals
- No per-frame GC

**Workstyle:**
1. Add code only under `/scripts/UdonSharp/...`.
2. Keep TODOs for inspector wiring; do not alter design docs.
3. Prefer explicit arrays and preallocated buffers.
4. Each PR: compile-safe, small diff, no broad refactors.

**Phase plan:**
- Phase A (now): Core, Utils, World (graph), Enemies (navigator), Weapons (base/health), UI (billboard)
- Phase B (next): Interactables (SaveTerminal, AmnionVat, PrinterConsole, LockerBay), Tablet suite, Elevator controllers, Weapons (RangedEmitter, MeleeBlade), EnemySpawner/Drone

**Definition of done for a change:**
- Zero compile errors under UdonSharp
- No new allocations in Update/owner ticks
- Serialized fields documented with TODOs for inspector wiring