# THE CRADLE (HUB) — LORE SUMMARY (v0.4)

## Premise
- Players arrive in the **Hub** (lore name: **The Cradle**), a node inside a **galaxy-spanning Superstructure** abandoned eons ago by its creators and inhabitants.
- Universe is an alternate reality far into the future where unverse has stopped expanding and is slowly collapsing back into the singularity that creates the big bang. This universal collapse is distorting timespace, resulting in the teleporters in the superstructure to malfunction and pull beings from other universes into this one (that's how the player ends up in this world)
- The Superstructure still runs under **Caretaker AIs**—ancient, impartial maintenance processes. Earlier arrivals formed the **Grand Authority (GA)**, a being from a parllel universe was teleported in and was able to adapt to the systems of this universe. Imperfect, but able to coordinate logistics for other survivors and adapt systems for habitation support.

## What You Are
- An **Operator** recognized by the machinery because you bind to a cryptographic **tablet** on arrival, adapted technology left by GA for others.
- The tablet is your passkey, used to identity, interface, and barter with the GA for additional support.

## The Medium: Lumen
- **Lumen** is a viscous white-blue energy gel circulating through the Superstructure.
- Players **harvest** it only from infrastructure—never from enemies—at:
  - **Capillary Taps** (short channel, modest yield)
  - **Reservoir Vats** (long channel, big yield; draws patrols)
  - **Dead Ports** (brief reactivation puzzle, one-shot yield)
- Players **spend** Lumen to:
  - **Fuel weapons** (ammo/charges via tablet-to-weapon ports)
  - **Heal** at devices (Field **Mediports** in layers; hub **Anodyne Crucible**)
  - **Craft/upgrade** at the **Constructor (Printer)** (permanent gear for this save)
  - **Trade** at the **GA Ledger Terminal** (Lumen → Scrip for keys/titles)
- Raw Lumen is **vented** at decontamination if not spent before death/extraction.

## The Tablet (Key)
- Collapsible **stick → tablet** that expands near sanctioned interfaces.
- Shows **Lumen**, **Kill Ledger** (by enemy type), **Clearance** rank, and simple GA/Caretaker text.
- **Owner-locked** when docked (no theft), **visible to all** for immersion.

## Hub devices at a glance

| Device           | Role (source of truth) |
|------------------|------------------------|
| Save Terminal    | First-time registration and post-run debrief/banking (Imprint). |
| Amnion Vat       | Resuscitation if sufficient **Lumen** is banked. |
| Printer          | Craft and upgrade gear. |
| Locker           | Store crafted gear for later runs. |
| Descent Core     | Single-terminal elevator to dungeon. Hub has **no access doors**; only the elevator has a sealing shutter. |


## Arrival & Registration (First-Time or After a Wipe)
- **Spawn:** the **malfunctioning teleporter bay**—a sputtering portal on the Hub’s edge.
- A **depowered tablet** lies on the floor.
- **Bind:** pick up the tablet → cryptographic handshake.
  - Tablet: `[KEY OFFLINE] No record found. Touch to bind.`
  - After bind: `[REGISTRATION REQUIRED] Insert key at the Save Terminal.`
- **Save Terminal:** create your world save and link your tablet to Hub persistence.
  - `No profile detected. Create a save now?`
  - `Save created. Operator linked: {name}.`
  - `Amnion access authorized. Deposit Lumen to insure recovery.`

## Amnion Reserve (Lumen-Backed Recovery)
- The **Amnion Vat** can pull you back from catastrophic injury **only if** you have enough **Amnion Reserve (AR)**—Lumen you’ve **deposited in the Hub** for resuscitation. Deposits are **one-way**.
- **Resuscitation costs (defaults):** See [EconomySpec](./research.md#economyspec) (source of truth).
- Tablet and elevator show your **AR** and a recommended buffer (see [EconomySpec](./research.md#economyspec) for current tuning).

**Death outcomes**
- **Insured (AR ≥ cost):** tablet emits recall pulse → Amnion **debits AR** and restores you at the vat.
  - `Recall accepted. Reserve debited {cost}. Shell integrity restored.`
- **Uninsured (AR < cost):** **recall denied** → **world save wiped** → you re-enter at the **malfunctioning teleporter** with a **depowered tablet**, repeating the bind → Save Terminal intro.
  - `Recall denied. Insufficient reserve. Operator record purged.`

## Progression Without Perks — Clearance & Depth
- **Clearance** is access authority earned by action (shared credit): enemy defeats, objective beats, **layer clears**.
- **Bank** Clearance only by **Imprint at the Save Terminal**.
- Clearance **gates** pressure tiers; it never buffs stats or discounts costs.

### Pressure Tiers (instance setting)
- **SURVEY** (baseline)
- **BREACH** (requires Clearance I)
- **SIEGE** (requires Clearance II)
- **COLLAPSE** (requires Clearance III)

Higher tiers increase **world pressure** (denser spawns, sturdier enemies, richer reservoir yields, miniboss guarantees). Players do **not** gain power from Clearance; only access.

## The Hub Devices (Core Interfaces)
- **Save Terminal (Registration & Debrief):** create your save profile and handle post-run debrief/banking (Imprint).
- **Amnion Vat (Recovery):** resuscitates on death if AR ≥ cost (see [EconomySpec](./research.md#economyspec)).
- **Constructor (Printer):** convert Lumen into **matter** (weapons/upgrades).
- **Locker:** stores crafted gear for this save.
- **Anodyne Crucible:** hub healing for Lumen.
- **GA Ledger Terminal:** optional Lumen→Scrip exchange.

## Elevator — Descent Core (Single Terminal)
- One **central Key Dock**. Whoever docks becomes **Conductor** for that cycle, selects a **pressure tier** allowed by their **banked Clearance**, then launches.
- A **REQUEST CONTROL** plate lets a higher-rank player politely ask for the dock—no forced ejects.
- Launch arms for ~2 s; doors seal; the tablet **auto-ejects** back to the owner. The hub room has no access doors; only the elevator mechanism uses a sealing shutter.

Panel copy:
- `Dock key to select pressure.`
- `Access denied. Required: Clearance II.`
- `Pressure set: SIEGE. Prepare descent.`

## Mixed-World Rules (New + Veteran Together)
- **Unregistered:** can bind tablet and create a save; elevator denies boarding.
- **Registered, no AR:** can board, but see warning: `Reserve {AR} < {cost}. Death will reset your profile.`
- **Registered, insured:** safe to recall (AR debited on death).
- Conductor’s **Clearance** sets the tier; each player sees their **own** AR warning locally. Social handoff via **REQUEST CONTROL**.

## The Dungeon Below
- Seeded **layers** of vats, catwalks, and shafts; each hides an **Uplink** (exit) and 1–2 secrets.
- **Harvest ecology:** taps/vats route Lumen; patrols cluster there.
- **Objectives:** **Dead Ports** (reactivate→siphon), **shortcut lifts**, **Mediport** alcoves.

## Enemies (Face of the Facility)
- **Scav Drone** (common): hovering construct; spline patrols; investigates Reservoir hum; pulse-cone attack; brief stagger on hit; EMP-fragile.
- Enemies contribute **Clearance** when defeated (shared to nearby contributors) but **never drop Lumen**.
- Deaths produce **local ragdoll** shells that dissolve as reclamators reclaim the frame.

## Tone & Voice
- **Caretaker AIs:** neutral, clinical.
  - `Protocol breach allowed. Local override: 180 s.`
- **Grand Authority:** polite, transactional.
  - `Your remittances keep the grid within tolerance.`

## Player Loop (Diegetic Summary)
1) **Arrival/Reset:** teleporter → pick up tablet → **Save Terminal** create save.  
2) **Prep:** craft/print, heal, **deposit Lumen to AR**.  
3) **Descent Core:** dock tablet → tier (by banked Clearance) → launch.  
4) **In layers:** harvest **Lumen** (taps/vats), fight, open shortcuts, find **Uplink**.  
5) **Return:** **Save Terminal (Debrief/Imprint)** → bank Clearance & ledger → print/trade/deposit.
6) **Death:**  
   - **Insured:** recall (AR debited), continue.  
   - **Uninsured:** recall denied → **world save wiped** → back to teleporter, start again.

## Glossary
- **Lumen:** white-blue energy gel used as currency.  
- **Tablet (Key):** visible identity device; owner-locked when bound.
- **Save Terminal:** first-time registration and post-run debrief/banking (Imprint).
- **Amnion Vat / Amnion Reserve (AR):** Lumen-backed resuscitation bank and recall.  
- **Constructor (Printer):** converts Lumen into matter (gear).  
- **Locker:** stores crafted gear (per save).  
- **Anodyne Crucible / Mediport:** healing devices.  
- **GA Ledger Terminal:** Lumen→Scrip exchange.  
- **Clearance:** access progression banked via Imprint; gates **SURVEY → BREACH → SIEGE → COLLAPSE**.  
- **Capillary Tap / Reservoir Vat / Dead Port:** harvest points.  
- **Descent Core:** single-terminal elevator; tablet-keyed; polite REQUEST CONTROL handoff.  
- **Scav Drone:** hovering construct; most common foe; no Lumen drops.

**Core theme:** You’re a displaced operator surviving inside a **galactic machine** that barely acknowledges you. The **tablet** makes you legible, **Lumen** fuels your choices, **Clearance** lets you press deeper, and the **Amnion** will catch you—**only if you’ve banked the charge.**
