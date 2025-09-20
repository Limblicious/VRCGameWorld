- Read and obey before doing anything.
- You MUST follow every guardrail. If a requested change conflicts, refuse and propose a compliant alternative.
- After edits, run a repo-wide validation: search for forbidden patterns and print matches.

**MVP Task Note:** Always read and follow CLAUDE.md before each task; adhere to repo paths shown in the VS Code screenshot; never create new folders or rename files for MVP tasks; prefer minimal [UdonSynced] and explicit save via PersistenceManager.

# Claude Code — UdonSharp Project Staging (VRChat)

**Repo mode:** staging-only. All scripts live under `/scripts/...` and will later be copy-pasted into Unity. Do **not** move or rename design docs.

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

**Workstyle:**
1. Add code only under `/scripts/...`.
2. Keep TODOs for inspector wiring; do not alter design docs.
3. Prefer explicit arrays and preallocated buffers.
4. Each PR: compile-safe, small diff, no broad refactors.

**Phase plan:**
- Phase A (now): Core, Utils, World (graph), Enemies (navigator), Weapons (base/health), UI (billboard)
- Phase B (next): Interactables (SaveTerminal, AmnionVat, PrinterConsole, LockerBay), Tablet suite, Elevator controllers, Weapons (RangedEmitter, MeleeBlade), EnemySpawner/Drone

### Udon API Exposure Guardrail — No `Camera.main`

`Camera.main` and many `UnityEngine.Camera` members are **not exposed to Udon** and will fail UdonSharp compilation.

**Always do (Udon-safe):**

```csharp
using VRC.SDKBase;

var lp = Networking.LocalPlayer;
if (lp != null)
{
    var head = lp.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
    Vector3 targetPos = head.position;
    // e.g., billboard:
    transform.rotation = Quaternion.LookRotation(targetPos - transform.position, Vector3.up);
}
```

**Editor fallback (optional):**

```csharp
public Transform fallbackTarget;
if (lp == null && fallbackTarget != null) { /* use fallbackTarget.position */ }
```

**Never do:**

```csharp
// ❌ Not exposed to Udon — breaks compile
transform.LookAt(Camera.main.transform);
var pos = Camera.main.transform.position;
```

**Pre-commit checks (must pass):**

1. No `Camera.main` in any `Assets/` or `scripts/` C# files.
2. Use LocalPlayer head tracking for player view data.
3. Keep other UdonSharp guardrails: no `GetComponent(typeof(T))` on user types; no multidimensional arrays `T[,]`; correct VRChat SDK namespaces.

### Override Guardrail — No Non-Existent Base Methods

**Do not create overrides for non-existent base methods.** Before writing `override` in a subclass, open the base type and confirm the exact signature is declared `virtual` or `abstract`. If it isn't, stop and adapt to existing base APIs (e.g., use `TryFire()`/`OnHit(...)` in `WeaponBase`). As a compile-time check, search the base file for the exact method name/signature you plan to override. If not found, this task fails.

**Pre-Commit Checklist:**
* If a subclass adds `override`, paste the base method signature above it as a comment and cite the source file/line. If you cannot cite it, remove the override.

### UdonSharp "No Reflection" Guardrail (Do Not Break)
UdonSharp cannot use `typeof()` on user-defined types or reflection patterns. Use generic APIs instead.

**Always do:**
```csharp
// ✅ OK in UdonSharp
var dmg = go.GetComponent<Damageable>();
var pool = go.GetComponent<SimpleObjectPool>();
```

**Never do:**
```csharp
// ❌ Forbidden: reflection / typeof on user types
var dmg = (Damageable)go.GetComponent(typeof(Damageable));
var pool = (SimpleObjectPool)go.GetComponent(typeof(SimpleObjectPool));
```

**If generics are not available (rare):**
```csharp
// ⚠️ Acceptable fallback
var dmg = (Damageable)go.GetComponent("Damageable");
```

**Pre-commit self-check:**
1. Reject any matches of `GetComponent(typeof(` in `Assets/` or `scripts/`.
2. Prefer generic `GetComponent<T>()` for all MonoBehaviours/UdonSharpBehaviours.
3. Keep attributes and SDK enums short-form with correct `using` statements:
   - `VRCObjectSync` → `using VRC.SDK3.Components;`
   - `[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]` with `using VRC.Udon.Common.Interfaces;`

**Definition of done for a change:**
- Zero compile errors under UdonSharp
- No new allocations in Update/owner ticks
- Serialized fields documented with TODOs for inspector wiring

## UdonSharp Guardrails & Validation Rules (Addendum v2)

**Never repeat these mistakes:**

1) **No multi-dimensional arrays** in UdonSharp (e.g., `bool[,]`).
   - Use a **flattened 1D array** instead. Index = `row * width + col`.
   - Example:
     ```csharp
     public bool[] adj; // length = maxNodes * maxNodes
     int Idx(int a, int b) => a * maxNodes + b;
     ```

2) **Sync attribute must be fully qualified** unless the `using` is present.
   - Preferred form:
     ```csharp
     [UdonBehaviourSyncMode(VRC.Udon.Common.Interfaces.BehaviourSyncMode.Manual)]
     ```
   - Reject any diff with `[UdonBehaviourSyncMode(BehaviourSyncMode.*)]` that lacks `using VRC.Udon.Common.Interfaces;`.

3) **VRChat SDK types require proper assembly references.**
   - If an `.asmdef` exists for the scripts, it must reference:
     - `UdonSharp.Runtime`, `VRC.Udon`, `VRCSDKBase`.
   - If references cannot be guaranteed, **do not hard-type SDK components** in public fields; use `UnityEngine.Component` and cast at runtime in owner code.
   - Do **not** create new asmdefs under `Assets/scripts` unless these references are added.

4) **Pre-commit validations to run for every change:**
   - Reject if any `\[[^\]]+,\s*[^\]]+\]` pattern appears in `.cs` (multi-dimensional arrays).
   - Reject if any `[UdonBehaviourSyncMode(BehaviourSyncMode.` appears without the proper `using`.
   - Reject if `using System.Linq`, `List<`, lambdas (`=>`) in hot paths, `foreach`, or `new ` occurs inside `Update`, `LateUpdate`, `FixedUpdate`, `Tick*`.
   - Confirm navigation BFS uses preallocated buffers and produces **zero-GC**.

5) **Notes from design specs (for context):**
   - Global nav = portal navgraph (BFS) as primary model.
   - Owner-only AI ticks every 0.05–0.1s; transforms via `VRCObjectSync`; `RequestSerialization` only on state changes.
   - Respect EconomySpec limits (hits ≤ 8/s, charge ≤ 10/s); primitive colliders; pooled FX/projectiles.

### VRChat SDK Namespaces (Don't Break This)
- `VRCObjectSync` → `using VRC.SDK3.Components;`
- `VRCObjectPool` → `using VRC.SDK3.Components;`
- `VRCStation` → `using VRC.SDK3.Components;`
- `VRCPlayerApi`, `Networking` → `using VRC.SDKBase;`
- `UdonBehaviour` (references) → `using VRC.Udon;`

### Udon Sync Mode — Namespace Guardrail
**Do this always:**
- Import the correct namespace for sync mode:
  ```csharp
  using VRC.Udon.Common.Interfaces;
  // (Optionally, for some SDKs)
  using VRC.Udon.Common;
  ```
- Use the short enum form in attributes:
  ```csharp
  [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
  ```

**Never do this:**
- Don't fully-qualify the enum in attributes:
  ```csharp
  // ❌ brittle across SDK versions
  [UdonBehaviourSyncMode(VRC.Udon.Common.Interfaces.BehaviourSyncMode.Manual)]
  ```

**Pre-commit self-check:**
1. Search for `UdonBehaviourSyncMode(` and ensure `using VRC.Udon.Common.Interfaces;` is present.
2. Reject any match containing `VRC.Udon.Common.Interfaces.BehaviourSyncMode`.
3. If you see `CS0234` or `CS0246`, verify namespaces for VRChat SDK3 types:
   - `VRCObjectSync`, `VRCObjectPool`, `VRCStation` → `using VRC.SDK3.Components;`
   - `VRCPlayerApi`, `Networking` → `using VRC.SDKBase;`
   - `UdonBehaviour` (references) → `using VRC.Udon;`
