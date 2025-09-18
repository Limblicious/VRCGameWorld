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

## UdonSharp Guardrails & Validation Rules (Addendum v1)

These rules are **mandatory** for all code you generate here. If any rule would be violated, STOP and propose a correction instead of writing code.

### A) Types, memory, and hot paths
- **Arrays only.** No `List<>`, no LINQ, no lambdas/closures, no `foreach`. Use `for` loops.
- **No allocations** in `Update()`, owner AI ticks, or other hot paths: no `new`, no string interpolation/concat, no `ToString()` building, no boxing.
- Prefer **preallocated buffers** passed in as parameters and reused (e.g., BFS path arrays).
- Use only **primitive colliders**. No MeshColliders.
- Physics calls must be **NonAlloc** variants if needed.

### B) UdonSharp-safe data structures
- **Never use multi-dimensional arrays** (e.g., `bool[,]`). If you need a matrix, **flatten** to `T[]` with index = `row * width + col`. Example:
  - `int width = maxNodes; int Idx(int a, int b) { return a * width + b; }`
  - Adjacency becomes: `bool[] adj = new bool[maxNodes * maxNodes];`
- Jagged arrays are allowed but flattened 1D is preferred for performance and simplicity.

### C) Networking & sync
- Owner-only ticks: guard with `if (!Networking.IsOwner(gameObject)) return;`
- Transform replication via **VRCObjectSync** only.
- Call `RequestSerialization()` **only** on state changes (e.g., health value changed).
- Use fully qualified sync attribute unless the `using` is present:
  - Preferred: `[UdonBehaviourSyncMode(VRC.Udon.Common.Interfaces.BehaviourSyncMode.Manual)]`
  - If you write `[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]`, you **must** include `using VRC.Udon.Common.Interfaces;` at the top.

### D) Object pooling & FX/audio
- No `Instantiate`/`Destroy` in hot paths. If instantiation is needed, do it **once** in `Start()` (pool warm).
- Pooled objects must **auto-despawn** back to the pool. Provide/require a tiny “PooledLifetime” script that calls `pool.Despawn(gameObject)` after its lifetime.
- Audio/FX routers must reference prewired sources/pools; no runtime resource lookups.

### E) Navigation (Portal Navgraph BFS)
- The **portal navgraph is primary**. Implement BFS with zero allocations using preallocated arrays.
- `DungeonGraphManager` must expose:
  - `int RegisterPortal(WaypointPortal p)`
  - `void LinkNodes(int a, int b)`
  - `int GetPath(int src, int dst, int[] pathBuf)` which fills `pathBuf` and returns hop count
  - `int GetNextHop(int from, int to)` convenience
  - `void SealAndMarkReady()` called after generation and **before** enabling enemies
- Do **not** rely on hardcoded portal indices at authoring time. If prelinking is authored, convert object refs to indices at runtime **after all registrations** or perform linking after generation.

### F) Enemy navigation ticks
- Ticks every **0.05–0.1s** (stagger allowed). Movement and seam crossing must respect a **seam cooldown**.
- No per-tick allocations. Reuse preallocated path buffers.
- When snapping across seams, allow small epsilon checks; transforms replicated by `VRCObjectSync`.

### G) Economy & rate limits
- Respect limits: hits ≤ **8/s**, charge ≤ **10/s** via a `RateLimiter` that resets a 1-second window.
- `EconomyConfig` is a scene singleton referenced by rate-limited systems.

### H) Pre-commit validation (what you must check before writing code)
- **Reject** any diff that introduces:
  - Multi-dimensional arrays (regex: `$begin:math:display$[^$end:math:display$]+,\s*[^\]]+\]`)
  - `using System.Linq` or any `.Select/.Where/.ToArray/.ToList`
  - `new ` inside `Update(` or any method named `Tick`/`TickNav`/`OwnerTick`
  - `[UdonBehaviourSyncMode(BehaviourSyncMode.` without `using VRC.Udon.Common.Interfaces;`
- Ensure every FX prefab referenced by `FXRouter` has a **despawn path** back to its `SimpleObjectPool`.

### I) Minimal snippets to follow (reference only)
**Flattened adjacency example (do this, not bool[,]):**
```
// fields
public int maxNodes = 128;
public bool[] adj; // length = maxNodes * maxNodes

int Idx(int a, int b) { return a * maxNodes + b; }

public void LinkNodes(int a, int b)
{
    if (a < 0 || b < 0 || a >= maxNodes || b >= maxNodes) return;
    adj[Idx(a,b)] = true;
    adj[Idx(b,a)] = true;
}

Sync attribute (fully qualified) example:

[UdonBehaviourSyncMode(VRC.Udon.Common.Interfaces.BehaviourSyncMode.Manual)]
public class Health : UdonSharpBehaviour { /* ... */ }

Owner-only tick guard:

void Update()
{
    if (!Networking.IsOwner(gameObject)) return;
    // tick work here...
}

— End of addendum —
