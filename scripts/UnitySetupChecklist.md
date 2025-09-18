# Unity Setup Checklist

## TL;DR Quick Start
- Import the latest VRChat SDK3 Worlds package and UdonSharp into the project.
- Create a new scene using `WorldRoot` as the top-level empty GameObject and add the hierarchy below.
- Drop the provided prefabs (FX pool item, enemy prefab, etc.) into the scene and wire every reference called out in this checklist.
- Enter Play Mode once to allow pools to warm (SimpleObjectPool `Start()` instantiates children).
- Before building, call `DungeonGraphManager.SealAndMarkReady()` after all `WaypointPortal` objects register (e.g., via scene event or editor script) so enemy navigation can start.

## Scene Hierarchy
```
WorldRoot
├── Systems
│   ├── EconomyConfig (EconomyConfig.cs)
│   ├── RateLimiterHub (RateLimiter.cs)
│   ├── FXRouter (FXRouter.cs)
│   │   └── FXPool (SimpleObjectPool.cs)
│   ├── AudioRouter (AudioRouter.cs)
│   ├── DungeonGraph (DungeonGraphManager.cs)
│   └── OwnershipHelpers (OwnershipHelpers.cs)
├── Portals
│   ├── Portal_A (WaypointPortal.cs)
│   ├── Portal_B (WaypointPortal.cs)
│   └── ... additional doorway portals ...
├── Enemies
│   └── EnemyNavigator (EnemyNavigator.cs)
├── Weapons
│   ├── WeaponBase (WeaponBase.cs)
│   └── DamageTarget (Damageable.cs + Health.cs)
└── UI
    └── BillboardLabel (BillboardText.cs)
```

## Assets / Prefabs & Required Wiring
- **EconomyConfig**
  - Component: `EconomyConfig`.
  - Inspector: set `maxHitsPerSecond` (≤8) and `maxChargePerSecond` (≤10) per spec.
  - Referenced by every `RateLimiter` (assign to `economy`).
- **RateLimiterHub**
  - Component: `RateLimiter`.
  - Inspector: assign the scene `EconomyConfig`.
  - Consumers (e.g., `WeaponBase`) reference this `RateLimiter` via serialized field.
- **FXRouter**
  - Component: `FXRouter`.
  - Inspector: assign `pool` to the child `FXPool`.
  - Optional map `fxIdToPoolIndex` if multiple FX types share the pool slots.
- **FXPool**
  - Components: `SimpleObjectPool`.
  - Inspector: set `prefab`, `parentForSpawned`, and `size` (match FX budget).
  - Ensure pooled prefab contains a timed despawn script that calls `SimpleObjectPool.Despawn`.
- **AudioRouter**
  - Component: `AudioRouter`.
  - Inspector: prewire `sources` array with one-shot AudioSource objects (3D settings baked in).
- **DungeonGraph**
  - Component: `DungeonGraphManager`.
  - Inspector: set `maxNodes` ≥ number of portals; arrays auto-resize on `Start()`.
  - After all portals register/link, invoke `SealAndMarkReady()` (e.g., editor script or scene event).
- **OwnershipHelpers**
  - Component: `OwnershipHelpers` (utility only; optional to place on an empty GameObject for reuse).
- **Portals/*** (each doorway)
  - Component: `WaypointPortal`.
  - Inspector: assign shared `DungeonGraphManager`.
  - Optional: populate `prelinkedNeighbors` with known portal indices for authored pairs.
  - Add primitive trigger collider aligned to doorway; hook trigger events to `OnPlayerEnterPortal` as needed.
- **Enemies/EnemyNavigator**
  - Components: `EnemyNavigator`, `VRCObjectSync`, collider/rigidbody per enemy prefab.
  - Inspector: assign `graph`, `objectSync`, `tickInterval` (0.05–0.1), `seamCooldown`, `moveSpeed`.
  - Call `OnSpawnAtPortal(int index)` when spawning from pool to initialize portal and seam timers.
- **Weapons/WeaponBase**
  - Component: `WeaponBase` (extend per-weapon as needed).
  - Inspector: wire `fx`, `audio`, and `limiter`; set `damage` per weapon spec.
- **Weapons/DamageTarget**
  - Components: `Damageable`, `Health`.
  - Inspector: assign `Damageable.health` to the co-located `Health` component.
  - Ensure the `Health` GameObject has proper ownership (instance master) and networking is set to Manual sync.
- **UI/BillboardLabel**
  - Component: `BillboardText`.
  - Inspector: assign `face` (defaults to `transform` if left null).
- **Utils/NetworkedToggle Instances**
  - Component: `NetworkedToggle`.
  - Inspector: fill `targets` array with GameObjects to enable/disable; ensure scene authority owns the toggle object.

## Per-System Wiring Notes
- **Navgraph Initialization**
  - All `WaypointPortal` objects must call `Register()` (automatic in `Start()` when `graph` is set).
  - After registration/linking, call `DungeonGraphManager.SealAndMarkReady()` before enabling `EnemyNavigator` scripts.
  - Link additional portals at runtime using `LinkNodes(a, b)` when procedural tiles generate.
- **Enemy Navigation**
  - Ensure pooled enemies receive a fresh `_pathBuf` sized to `graph.maxNodes` (handled in `Start` once graph assigned).
  - Enemy tick occurs in `Update()` on owner only; guarantee ownership via `Networking.SetOwner` when spawning.
  - Provide seam triggers so `EnemyNavigator.OnEnterPortal` fires when crossing doorway colliders.
- **Combat & Damage**
  - `WeaponBase.TryFire()` consumes rate-limited hits; ensure limiter references `EconomyConfig`.
  - `Damageable.ApplyDamage` forwards to `Health.Modify`; ensure colliders call `OnHit` with proper target GameObject.
  - `Health` must reside on `Networking.IsOwner` = instance master; call `RequestSerialization()` only on state change (built-in).
- **FX/Audio**
  - Pooled FX prefabs should include a despawn behaviour (timer or animation event) calling `SimpleObjectPool.Despawn`.
  - Audio sources configured for spatialization; `AudioRouter.PlayAt` repositions source before playback.
- **Networking Utilities**
  - Use `OwnershipHelpers.EnsureOwner` prior to modifications on shared objects (toggles, health, etc.).
  - `NetworkedToggle` uses manual sync; call `Set(true/false)` from owner-only scripts to propagate state.

## Sanity Checks & Known Gaps
- ✅ No multi-dimensional arrays remain; `DungeonGraphManager.adj` flattened to 1D with helper index method.
- ⚠️ `SimpleObjectPool` still uses `Instantiate` in `Start()`; ensure pool warm-up occurs outside runtime hot paths.
- ⚠️ `WaypointPortal` trigger wiring TODO remains — add collider + enter events when designing portals.
- ⚠️ `FXRouter` requires pooled prefabs to include a timed despawn script (not yet implemented in repo).
- ⚠️ `Health.OnDeserialization` and visual feedback hooks are placeholders; implement view sync before content lock.
- ⚠️ Ensure `DungeonGraphManager.graphReady` is toggled only after procedural generation completes; otherwise enemies stall.
