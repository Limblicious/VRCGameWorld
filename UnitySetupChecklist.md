# Unity Setup Checklist — L2.005-GA MVP

**TL;DR Quick Start**
1. Drop `_Managers` prefab group into the scene and place `DungeonGraphManager`, `EconomyConfig`, `FXRouter`, `AudioRouter`, and `OwnershipHelpers` under it.
2. Lay out `_TilesAndPortals` tiles (10m x 10m) and add `WaypointPortal` objects with primitive trigger colliders on every doorway.
3. Assign each portal's `graph` to the shared `DungeonGraphManager`, then link seams and call `DungeonGraphManager.SealAndMarkReady()` immediately after the runtime layout finishes and **before** enabling any enemies or spawners.
4. Spawn `_Enemies/Enemy_Drone_00`, set its `EnemyNavigator.graph` reference, call `OnSpawnAtPortal(startIdx)`, and wire target portal indices to validate traversal.
5. Verify `_UI` and `_Test` helpers (BillboardText, DevWeapon + RateLimiter) are wired, ensure pooled FX/audio assets resolve, and confirm the navgraph returns valid paths between at least two portals.

## ✅ Checkbox Scene Hierarchy
```
- [ ] VRDefaultWorldScene
  - [ ] _Managers
    - [ ] DungeonGraphManager (component)
    - [ ] EconomyConfig (component)
    - [ ] OwnershipHelpers (component)
    - [ ] FXRouter (component)
      - [ ] FXPool (SimpleObjectPool)
    - [ ] AudioRouter (component)
  - [ ] _TilesAndPortals
    - [ ] Tile_00 (10m x 10m)
      - [ ] Portal_A (WaypointPortal + BoxCollider isTrigger)
      - [ ] Portal_B (WaypointPortal + BoxCollider isTrigger)
    - [ ] Tile_01 (10m x 10m)
      - [ ] Portal_C (WaypointPortal + BoxCollider isTrigger)
      - [ ] Portal_D (WaypointPortal + BoxCollider isTrigger)
  - [ ] _Enemies
    - [ ] Enemy_Drone_00 (EnemyNavigator + VRCObjectSync + Damageable + Health)
  - [ ] _UI
    - [ ] HintText (BillboardText)
  - [ ] _Test (optional)
    - [ ] DevWeapon (WeaponBase + RateLimiter)
  - [ ] _PhaseB (placeholders for later)
    - [ ] SaveTerminal
    - [ ] AmnionVat
    - [ ] PrinterConsole
    - [ ] LockerBay
    - [ ] ElevatorRoot (DescentCoreController)
    - [ ] ElevatorShutter_L / ElevatorShutter_R
    - [ ] ElevatorTerminal
    - [ ] TabletDevice
```

### Component Loadout
| [ ] GameObject | Required Components | Serialized fields to wire | Notes |
| --- | --- | --- | --- |
| [ ] DungeonGraphManager | `DungeonGraphManager` | `maxNodes` (capacity), assign in-scene portals as they register | Call `SealAndMarkReady()` once generation finishes. Arrays auto-allocate in `Start()`. |
| [ ] EconomyConfig | `EconomyConfig` | (none) | Scene singleton read by `RateLimiter`. |
| [ ] OwnershipHelpers | `OwnershipHelpers` | (none) | Utility for ensuring network ownership; optional helper object. |
| [ ] FXRouter | `FXRouter` | `pool`, `fxIdToPoolIndex[]` | `pool` references `FXPool` `SimpleObjectPool`. Pooled prefabs must self-despawn. |
| [ ] FXPool | `SimpleObjectPool` | `prefab`, `parentForSpawned`, `size` | Warmed automatically in `Start()`. Prefab should be disabled by default. |
| [ ] AudioRouter | `AudioRouter` | `sources[]` (array of `AudioSource`) | Provide at least one one-shot source per routed cue; transforms repositioned on `PlayAt`. |
| [ ] Portal_* | `WaypointPortal`, `BoxCollider (IsTrigger)` | `graph`, optional `prelinkedNeighbors[]` | Register portals on `Start`; ensure collider faces doorway seam. |
| [ ] Enemy_Drone_* | `EnemyNavigator`, `VRCObjectSync`, `Damageable`, `Health` | `EnemyNavigator.graph`, `EnemyNavigator.objectSync`, `Health.maxValue` | `EnemyNavigator` allocates path buffer on `Start`; ensure VRCObjectSync owns the drone. |
| [ ] Enemy_Drone_* (Health wiring) | `Health` (component already above) | none beyond defaults; optional FX hooks | Health is `[UdonSynced]`; ensure ownership transfer allowed before Modify. |
| [ ] DevWeapon | `WeaponBase` | `limiter`, optional `fx`, `audio` | Place child `RateLimiter` component and reference it. |
| [ ] RateLimiter (child of DevWeapon) | `RateLimiter` | `economy` | Reference the scene `EconomyConfig`. Verifies ≤8 hits/s, ≤10 charge/s. |
| [ ] HintText | `BillboardText` | `face` (optional override) | Defaults to `transform`; faces `Camera.main` during `LateUpdate`. |
| [ ] ElevatorShutter_* | `NetworkedToggle` | `targets[]` (panels) | Placeholder until DescentCore scripts arrive; toggle panels via synced bool. |

## Assets & Prefabs Checklist
- [ ] Create a prefab for **Enemy_Drone** with `EnemyNavigator` + `VRCObjectSync` + primitive collider + `Damageable` + `Health`.
- [ ] Create FX prefab(s) (e.g., `FX_Puff`) compatible with `SimpleObjectPool`, each with a timed despawn script invoking `pool.Despawn(gameObject)`.
- [ ] Create/assign `AudioSource` set for `AudioRouter` (one per routed cue, spatialized as needed).
- [ ] Configure a **test weapon** prefab using `WeaponBase` + `RateLimiter` (+ optional FX/audio) to validate hit rate throttling.
- [ ] Author any additional prefabs referenced by pools/routers (e.g., damage numbers, impact markers) with pooling hooks.

## **3D Assets to Model (Blender)**
### Phase A (now)
- [ ] **DungeonTile_10x10m** — Floor quad with doorway cutouts; exact footprint 10m × 10m × ≤0.5m. Pivot at tile center (0,0,0).  
  Colliders: BoxColliders for floor/walls only (no MeshColliders).  
  Child anchors: `[ ] PortalAnchor_A/B/C/D` empties at doorway centers (1m wide openings at mid-edges).
- [ ] **PortalFrame** — Door surround sized to 3m tall × 1m thick × 1.5m wide. Pivot at floor centerline.  
  Child: `[ ] PortalTrigger` (GameObject) with BoxCollider (isTrigger) to host `WaypointPortal`.
- [ ] **EnemyDrone_Capsule** — 1.2m tall capsule or cube body, pivot at base center.  
  Collider: CapsuleCollider or BoxCollider aligned with mesh.  
  Attach points: `[ ] NavAnchor` (transform for AI), `[ ] FX_Hardpoint` (optional for VFX).
- [ ] **FX_Puff** — 0.5m particle mesh or quad billboard. Pivot at center.  
  Collider: none.  
  Include script hook or animation event to call pooled despawn.
- [ ] **WorldUI_HintText** — Flat card or text mesh sized ~0.5m wide. Pivot at center bottom.  
  Child anchor: `[ ] TextRoot` (assign to `BillboardText.face`).

### Phase B (placeholders for later)
- [ ] **SaveTerminal** — 2m tall kiosk with control panel. Pivot at base center.  
  Colliders: BoxCollider covering cabinet; `[ ] TabletDock` anchor front-center.
- [ ] **AmnionVat** — 2.5m tall cylinder + lid. Pivot at floor center.  
  Colliders: CapsuleCollider for vat, BoxCollider for base.  
  Anchors: `[ ] VatLid`, `[ ] FX_Bubbles`.
- [ ] **PrinterConsole** — 1.2m tall console. Pivot at base center.  
  Collider: BoxCollider; `[ ] PrintSlot` anchor for spawned items.
- [ ] **LockerBay** — 4m wide wall segment with doors. Pivot at base center.  
  Colliders: BoxColliders per door; anchors `[ ] Door_L`, `[ ] Door_R`.
- [ ] **ElevatorCabin** — 3m × 3m × 3m cab. Pivot at floor center.  
  Colliders: BoxCollider walls/floor.  
  Anchors: `[ ] DescentCoreAnchor`, `[ ] PlayerSpawnPoints` (array of empties).
- [ ] **ElevatorShutters** — Two 3m × 1.5m panels. Pivot at hinge edge.  
  Colliders: BoxColliders per panel; anchors `[ ] TogglePivot` for `NetworkedToggle` movement.
- [ ] **ElevatorTerminal** — Wall-mounted interface. Pivot at mount point.  
  Collider: BoxCollider; `[ ] TabletDock` anchor.
- [ ] **TabletDevice** — Collapsible baton + tablet (~0.7m). Pivot at grip center.  
  Anchors: `[ ] DockInsert`, `[ ] ScreenRoot` for future tablet scripts.

*(All measurements in meters; ensure pivots align with Unity's (0,0,0) when dropped into scenes.)*

## Per-system Wiring
- [ ] BFS graph: Place `DungeonGraphManager`; assign every `WaypointPortal.graph`; link seams; call `SealAndMarkReady()` after generation; verify `GetPath(start, goal, buf)` returns ≥2 entries.
- [ ] Enemy nav: Spawn `Enemy_Drone_00` at a portal; call `OnSpawnAtPortal(idx)`; set `SetTargetPortalIndex(goalIdx)`; confirm traversal obeys seam cooldown.
- [ ] Economy/rates: Drop `EconomyConfig` in scene; assign it to any `RateLimiter`; fire `DevWeapon` to confirm ≤8 hits per second.
- [ ] FX/Audio: Warm `SimpleObjectPool` via `Start`; ensure pooled FX auto-despawn; assign `AudioRouter.sources[]` to scene `AudioSource` objects.
- [ ] UI: Confirm `BillboardText` rotates toward `Camera.main` and text remains legible from multiple positions.

## Sanity Checks
- [ ] Profile runtime for **no per-frame GC allocations**.
- [ ] Confirm owner-only ticks (remote clients idle while master simulates enemies).
- [ ] Ensure enemies traverse portals only after the graph is sealed.
- [ ] Use primitive colliders only (Box/Capsule/Sphere); no MeshColliders in phase A assets.
- [ ] Call `RequestSerialization()` only when `Health` values change.

## Known Gaps / Next
- [ ] Implement Interactables (SaveTerminal, AmnionVat, PrinterConsole, LockerBay).
- [ ] Implement Elevator controllers (DescentCoreController, ElevatorShutter, ElevatorTerminal).
- [ ] Implement Weapons (RangedEmitter, MeleeBlade) with pooled projectiles/FX.
- [ ] Tablet suite (BindOnPickup, Wallet, ChargeLink, HoloDisplay).
- [ ] EnemySpawner + DroneController behavior layers.
