# Needle Mini Game — Technical Design & Implementation Report

**Project:** Cure Me, Please
**Module:** Treatment — Needle Mini Game (pierce, then squeeze or drain pus)
**Engine:** Unity 6000.3.17f1 (Unity 6.3 LTS), 2D, World-space GameObjects
**Input:** Input System Package (New) — `UnityEngine.InputSystem`
**Document status:** Design specification, reconciled with the current project systems
**Last updated:** July 2, 2026

> **Project reconciliation note:** Aligned with the live project. Three things to know while
> implementing: (1) `Sanity` is wired **directly** by each mini game (`customer.GetComponent<Sanity>()`
> then `Sanity.AddSanity`) — there is no bridge class. (2) Tool selection goes through the shared
> `MiniGameOverlay` (a `Toggle` or `Button` per tool + a `HandleToolSelected(string)` handler on the
> mini game); the old per-tool `TongsTool` component is legacy and no longer used. (3) Area→mini-game
> routing runs through `TreatmentMiniGameType` + `TreatmentCaseData` and `AnatomyController`, which
> today only knows `Tongs` and `Knife` — adding Needle requires the enum value, an `AnatomyController`
> field, and case-data support. See Section 8.2.

> **Implementation space:** Like the Tongs and Knife mini games, the Needle mini game is built with **World-space GameObjects** (`SpriteRenderer` + `Collider2D`), not UI Canvas. The patient body, pustules, and needle live in the scene as world objects; only the shared meters stay on the Canvas overlay. All distances are in **world units** unless marked otherwise.

> **Input System notice (Unity 6.3):** Active Input Handling is *Input System Package (New)* only, so the legacy `Input` class will not compile. All input uses `Mouse.current` from `UnityEngine.InputSystem`. `OnMouseDown` does not fire — select pustules with `Physics2D.OverlapPoint`.

---

## 1. Overview

The Needle mini game is the third and final core treatment mini game, completing the GDD's tool set (Tongs, Knife, Needle). The player uses a needle to **pierce** a pus pustule, then either **squeezes it out by hand** (small pustule) or **drains it with the needle** (large pustule), all while managing patient pain and keeping the needle on target.

It reuses almost every pressure system already built for Tongs and Knife. Its one unique detection rule is the simplest of the three: a **point-in-circle** check (needle tip distance from the pustule center) rather than an offset-from-line (Tongs) or distance-from-path (Knife).

---

## 2. Confirmed Design Decisions

| Topic | Decision |
|---|---|
| Pustule types | **Two:** Small (pierce → bare-hand squeeze) and Big (pierce → needle drain). |
| Tool model | `ToolState` is **None (bare hand)** or **Needle**. Player starts every mini game **bare-handed** and clicks the Needle to hold it. |
| Phase 1 — pierce | A **short click-hold** (`pierceProgress`) on the pustule. **No pain, no jitter** during piercing. |
| Phase 2 — Small (squeeze) | Put the needle down (bare hand), click-hold to squeeze pus out. **Pain: yes. Jitter: no.** |
| Phase 2 — Big (drain) | Keep holding the needle, click-hold to drain pus out. **Pain: yes. Jitter: yes** + needle-off-pustule check. |
| Off-target detection | Only for Big drain: needle tip distance from the pustule **center** exceeds the pustule **radius** → Sanity increases. |
| Pain bar | Present only during phase 2 (squeeze/drain). Piercing does not hurt. |
| Pustules per case | Random **3–7** at predefined anchors (shuffle bag, no duplicate anchors). |
| Reused systems | Local pain bar + Sanity spike, Perlin jitter (Big drain only), world-space, Input System, gizmos, shuffle bag, shared MiniGameOverlay. |

---

## 3. Relationship To Tongs & Knife

### 3.1 Reused (already implemented)

- **Local pain bar** that fills while acting and drains while paused; full bar applies a large one-time Sanity spike.
- **Perlin-noise mouse jitter** added to the tool tip (never overriding the OS cursor); player counter-steers. Used **only** during the Big-pustule drain.
- **Sanity pressure** routed through the existing `Sanity` component using the **same direct wiring** Tongs/Knife use — the mini game holds the active `Sanity` (`customer.GetComponent<Sanity>()`) and calls `Sanity.AddSanity(...)` itself. There is **no** bridge class and no new manager.
- **World-space** GameObjects, `Mouse.current` input, **debug gizmos**, **shuffle spawn bag**, and the **shared MiniGameOverlay** (progress + pain meters, Complete button, tool toggle).
- **Two-phase "act then put the tool down" pattern** is identical to the Knife **Bulge** flow: pierce, then (for Small) drop the needle and use the bare hand.

### 3.2 New To Needle

- **Point-in-circle** off-target detection (needle tip vs. pustule center + radius). This is the simplest of the three detection models.
- **Tool-dependent phase 2**: Small uses the bare hand, Big keeps the needle — a per-type branch on which tool must be held.

---

## 4. Pustule Types & State Machine

```csharp
public enum PustuleType { Small, Big }
public enum ToolState   { None, Needle }
```

Both types share one state machine; only phase 2 differs.

```
Idle
  -> (hold Needle, click pustule) -> Piercing        // short hold, no pain, no jitter
  -> Pierced
       -> [Small] put needle down (bare hand) -> Squeezing  // pain, no jitter
       -> [Big]  keep needle                   -> Draining   // pain, jitter, off-target check
  -> Done (drainProgress reaches 1)
```

### 4.1 Small Pustule (pierce → bare-hand squeeze)

1. Hold the Needle, click the pustule, hold briefly until `pierceProgress` fills. Pierced.
2. **Put the needle down** → `ToolState.None` (bare hand).
3. Click-hold to squeeze: `drainProgress` rises and pain rises. Pause periodically to drain pain.
4. `drainProgress` reaches 1 → pus out. Done.

### 4.2 Big Pustule (pierce → needle drain)

1. Hold the Needle, click the pustule, hold briefly until `pierceProgress` fills. Pierced.
2. **Keep holding the needle** (`ToolState.Needle`).
3. Click-hold to drain: `drainProgress` rises, pain rises, and **the mouse jitters**.
4. While draining, if the needle tip leaves the pustule radius, Sanity rises continuously and drain progress rolls back slightly.
5. `drainProgress` reaches 1 → pus out. Done.

> Phase 2 advances only with the correct tool: Small squeeze only while bare-handed, Big drain only while holding the needle. This gating makes the "put the needle down" action meaningful for Small pustules.

---

## 5. Off-Target Detection (Big Drain Only)

The simplest detection of the three mini games: a point-in-circle test.

```csharp
float distance = Vector2.Distance(needleTip.position, pustuleCenter);
bool outside   = distance > pustuleRadius;

if (outside)
{
    Sanity.AddSanity(strayPenaltyPerSecond * Time.deltaTime);   // continuous
    drainProgress -= rollbackPerSecond * Time.deltaTime;        // clamp >= 0
}
```

The Perlin jitter pushes the needle tip off the center; the player counter-steers to keep it inside the circle. Same principle as Tongs/Knife, with a circle instead of a line or path.

Small pustules have **no** off-target check because squeezing is bare-handed and does not jitter.

---

## 6. Pain, Jitter & Sanity Integration

### 6.1 Pain (reused)

- Pain rises only during phase 2 (squeeze or drain), never during piercing.
- Pain is **local** to the current pustule and resets when the pustule is done.
- Full pain bar → single large Sanity spike (`painSpikeAmount`) + reset. Player must pause to drain pain.

### 6.2 Jitter (reused, Big drain only)

- Big pustule drain: Perlin jitter on the needle tip; player counter-steers to stay in the circle.
- Small pustule squeeze: **no jitter**.
- Piercing: **no jitter**.

### 6.3 Sanity (reused component)

- Needle off pustule (Big drain) → continuous Sanity increase.
- Pain bar full → large Sanity spike.
- All through the existing `Sanity` component.

---

## 7. Input Model (Unity 6.3, Input System)

Player starts bare-handed. Clicking the Needle sets `ToolState.Needle`; putting it down returns to `None`.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

// Per frame:
Mouse mouse = Mouse.current;
if (mouse == null) return;

Vector2 screenPos    = mouse.position.ReadValue();
Vector2 worldPointer = treatmentCamera.ScreenToWorldPoint(screenPos);

if (mouse.leftButton.wasPressedThisFrame)  { /* select pustule / begin pierce or phase 2 */ }
if (mouse.leftButton.isPressed)            { /* continue pierce / squeeze / drain by state */ }
if (mouse.leftButton.wasReleasedThisFrame) { /* stop; pain begins draining */ }
```

Notes:
- Add `using UnityEngine.InputSystem;` to every input-reading script.
- Select pustules with `Physics2D.OverlapPoint` against their `Collider2D`; ignore world actions when the pointer is over UI (same guard the Knife mini game uses).
- Needle equip / put-down goes through the shared `MiniGameOverlay` tool tray, exactly like Tongs and Knife do today: register a tool entry with id `"Needle"` (a `Toggle` or `Button`), and give `NeedleMiniGame` an `overlayToolId = "Needle"` plus a `HandleToolSelected(string toolId)` method that equips the needle when `toolId == overlayToolId` and drops it (bare hand) otherwise. The old `TongsTool` component is legacy and not used by this pattern.

---

## 8. Component Structure

```
NeedleMiniGame (world-space controller)
├── ToolState (None / Needle) — player switches manually
├── spawns 3–7 pustules at predefined anchors (shuffle bag, no duplicate anchors)
└── Pustule (per spawn)
    ├── PustuleType (Small / Big)
    ├── center + radius            // point-in-circle off-target check (Big)
    ├── pierceProgress : 0..1      // phase 1 short hold
    ├── drainProgress  : 0..1      // phase 2 squeeze/drain
    ├── painLevel      : 0..1      // local, phase 2 only
    └── isPierced      : bool
```

### 8.1 Pustule Sketch

```csharp
using UnityEngine;

public class Pustule : MonoBehaviour
{
    [SerializeField] private PustuleType type;
    [SerializeField] private float pustuleRadius = 0.4f;      // world units (Big check)
    [SerializeField] private float pierceTime = 0.4f;         // seconds to pierce
    [SerializeField] private float drainSpeed = 0.5f;         // progress/sec in phase 2
    [SerializeField] private float painPerSecond = 0.22f;
    [SerializeField] private float painDrainRate = 0.40f;
    [SerializeField] private float painSpikeAmount = 25f;
    [SerializeField] private float strayPenaltyPerSecond = 8f;
    [SerializeField] private float rollbackPerSecond = 0.3f;  // Big off-target
    [SerializeField] private float jitterStrength = 0f;       // Big drain only (0 first)
    [SerializeField] private float jitterFrequency = 1.8f;

    private float pierceProgress;
    private float drainProgress;
    private float painLevel;
    private bool  isPierced;

    // TickPierce(dt): raise pierceProgress; no pain, no jitter. On full -> isPierced = true.
    // TickSqueeze(dt) [Small, bare hand]: raise drainProgress + pain. No jitter.
    // TickDrain(worldTip, dt) [Big, needle]: raise drainProgress + pain + jitter;
    //     if outside radius -> Sanity + rollback.
    // On drainProgress >= 1 -> Done.
    // On painLevel >= 1 -> Sanity spike + reset pain.
}
```

### 8.2 Integration Contract (match Tongs & Knife exactly)

`AnatomyController` already drives mini games, but only knows `Tongs` and `Knife`. Wiring Needle in
has **three required pieces**:

**A. `NeedleMiniGame` must expose the same public surface as `TongsMiniGame` / `KnifeMiniGame`:**

| Member | Purpose |
|---|---|
| `void Begin(CustomerAgent customer)` | Start; find `Sanity` from the customer; `flow.SetTreatmentStress(true)`; `overlay.Activate(CompleteMiniGame, HandleToolSelected)`. |
| `void Stop()` | End and hide; `flow.SetTreatmentStress(false)`; `overlay.Deactivate(CompleteMiniGame)`. |
| `void Pause()` / `void Resume()` | Used when the player leaves to the counter and returns. |
| `event Action MiniGameCompleted` | **No argument** — the controller tracks which area is active and marks it treated. |
| meters | Drive `overlay.SetMeters(progress, pain)`; show the shared complete button via `overlay.SetCompleteVisible(...)`. |
| tool | `overlayToolId = "Needle"` + `HandleToolSelected(string)` (see Section 7). |

**B. Add the enum value.** `Assets/Core/Script/Treatment/Case/TreatmentMiniGameType.cs` currently has
`None, Tongs, Knife` — add `Needle`. `TreatmentCaseData` can then map body areas to `Needle`.

**C. Extend `AnatomyController`** (do not fork the flow):

```csharp
[SerializeField] private NeedleMiniGame needleMiniGame;

// GetMiniGameTypeForArea already checks activeCaseRuntime first, then falls back to
// tongsArea / knifeAreas. Add a needleAreas fallback (or rely on TreatmentCaseData).

// EnterInfectedArea(area): switch on GetMiniGameTypeForArea(area):
//   Tongs  -> tongsMiniGame.Begin(customer)
//   Knife  -> knifeMiniGame.Begin(customer)
//   Needle -> needleMiniGame.Begin(customer)   // new branch

// SubscribeMiniGames(): also  needleMiniGame.MiniGameCompleted += HandleNeedleCompleted;
// HandleNeedleCompleted(): if (isMiniGameRunning && activeMiniGameType == Needle) MarkAreaTreated(activeArea);
```

All completion handlers converge on the existing `MarkAreaTreated(area)` path, so case completion and
`Sanity` reset through `Treatment` / `GameFlow.CompleteTreatment` stay unchanged. Subscription lives in
`Awake`/`OnDestroy` so completion still fires while the Anatomy screen is folded away during the mini game.

---

## 9. Debug Gizmos (Editor Visualization)

World-space, so gizmos draw at scene coordinates with no conversion:

- The **pustule circle** at `center` with `pustuleRadius` — green when the needle tip is inside, red when outside (Big only).
- The **needle tip** as a small sphere at its live world position.
- The **jitter range** as a circle of radius `jitterStrength` around the player's steer point (Big drain), so you can see whether jitter alone can push the tip outside the pustule.

```csharp
void OnDrawGizmos()
{
    // pustule radius
    bool outside = Application.isPlaying &&
                   Vector2.Distance(currentTip, transform.position) > pustuleRadius;
    Gizmos.color = outside ? Color.red : Color.green;
    DrawCircle(transform.position, pustuleRadius);

    // jitter range (Big drain)
    Gizmos.color = Color.yellow;
    DrawCircle(currentSteerPoint, jitterStrength);

    // needle tip
    Gizmos.color = Color.white;
    Gizmos.DrawSphere(currentTip, 0.05f);
}

// DrawCircle: draw a ring with a short loop of Gizmos.DrawLine segments.
```

Reading the gizmos: keep the yellow jitter circle inside the green pustule circle. If yellow is larger than the pustule, jitter alone can push the needle out — the player must counter-steer.

---

## 10. Phased Work Plan

### Phase 1 — Pierce + Small Squeeze
- `NeedleMiniGame`, `Pustule`, `PustuleType`, `ToolState`.
- Needle tool select (bare hand → needle).
- One Small pustule: click-hold to pierce (short), then put needle down, bare-hand squeeze to full.
- Draw the pustule gizmo. Run `dotnet build "Pixel-forge-game-jam.slnx"`.

### Phase 2 — Pain
- Local pain bar in phase 2 only (squeeze), draining while paused.
- Pain full → `Sanity.AddSanity(painSpikeAmount)` + reset.

### Phase 3 — Big Drain + Off-Target
- Add Big pustule: pierce, then needle drain.
- Point-in-circle off-target check → Sanity + rollback.

### Phase 4 — Jitter (Big only)
- Perlin jitter on the needle tip during Big drain (off for pierce and Small squeeze).
- Tune `pustuleRadius`, `jitterStrength`, `rollbackPerSecond` with gizmos.

### Phase 5 — Multi-Pustule & Integration
- Spawn 3–7 pustules at anchors (shuffle bag).
- Complete when all are done; raise `MiniGameCompleted` so `AnatomyController` marks the area treated (see Section 8.2 for the required contract, enum value, and controller changes).
- Route the appropriate body areas to Needle via `TreatmentMiniGameType.Needle` in `TreatmentCaseData` (add the enum value first).

### Phase 6 — Feedback
- Final art, pierce/squeeze/drain SFX, pus VFX, pain heartbeat tie-in, success/fail feedback.

---

## 11. Starting Balance Values

Distances in world units — tune against the actual camera with the gizmos.

| Parameter | Small | Big |
|---|---:|---:|
| `pierceTime` (sec) | 0.40 | 0.50 |
| `drainSpeed` (progress/sec) | 0.6 | 0.4 |
| `painPerSecond` (phase 2) | 0.22 | 0.28 |
| `painDrainRate` (per sec) | 0.40 | 0.40 |
| `painSpikeAmount` (sanity) | 25 | 25 |
| `pustuleRadius` (world units) | — | 0.40 |
| `strayPenaltyPerSecond` (sanity/sec) | — | 8 |
| `rollbackPerSecond` (Big off-target) | — | 0.30 |
| `jitterStrength` (world units) | 0 (never) | 0 first, later 0.35 |
| `jitterFrequency` | — | 1.8 |

Case-level:

| Parameter | Value |
|---|---|
| Pustules per case | 3–7 (random) |
| Sanity spike (pain full) | +25 |
| Sanity per second (needle off pustule) | +8 / sec |

Tuning order: pierce + squeeze feel first, then pain, then Big drain, then jitter last.

---

## 12. Open Questions (Default Choices Applied)

These had reasonable defaults baked into the values above; revisit during playtesting:

- **Big drain off-target:** default is *roll back slightly + add Sanity* (matching Knife). Alternative: Sanity only, no rollback.
- **Releasing during pierce:** default is *pierceProgress holds* (does not reset) so a brief slip doesn't restart the pierce. Alternative: reset to 0.
- **Small squeeze rhythm:** default is a *single long hold* to full. Alternative: rhythmic click-pulses if a squeezing "beat" feels better in testing.

---

## 13. Risks & Notes

- **Legacy input will not compile:** use `Mouse.current`; add `using UnityEngine.InputSystem;`. `OnMouseDown` does not fire — select via `Physics2D.OverlapPoint`.
- **Simplest detection, but tune the radius:** a `pustuleRadius` that is too large makes the Big drain trivial; too small makes jitter feel unfair. Use the gizmo to balance jitter vs. radius.
- **Tool-switch clarity:** for Small pustules, make it obvious the needle is down and the bare hand is active, so players understand why the squeeze won't start while holding the needle.
- **Pierce should feel quick:** piercing has no pain and no jitter by design; keep `pierceTime` short so it reads as a quick action, not a second challenge.
- **Reuse, don't duplicate:** share the pain/jitter/Sanity/shuffle-bag/overlay code paths with Tongs and Knife rather than copying, to keep balancing unified. Needle is largely the Knife Bulge two-phase pattern with a circle check.
- **Phase-specific rules (by design):** pain only in phase 2; jitter only on Big drain. Confirm these read as intentional and not inconsistent.

---

## 14. Summary

The Needle mini game completes the tool set and is the lightest of the three to build because it reuses almost everything from Tongs and Knife: the local pain bar + Sanity spike, Perlin jitter, world-space setup, Input System, gizmos, shuffle bag, and the shared MiniGameOverlay. Its structure mirrors the Knife **Bulge** two-phase flow — **pierce first, then act** — with two pustule types: **Small** (pierce, put the needle down, squeeze by hand — pain, no jitter) and **Big** (pierce, keep the needle, drain — pain, jitter, and a point-in-circle off-target check that adds Sanity and rolls progress back). The player always starts bare-handed and must pick up the needle; pain applies only in phase 2, and jitter only during the Big drain. Built for Unity 6000.3.17f1 with the new Input System from the start, the plan moves pierce + squeeze → pain → Big drain + off-target → jitter → multi-pustule integration → feedback, keeping all tunable values in data and reusing the existing `Sanity` component rather than adding a parallel system.
