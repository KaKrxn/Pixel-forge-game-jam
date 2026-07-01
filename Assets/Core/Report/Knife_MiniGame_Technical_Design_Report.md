# Knife Mini Game — Technical Design & Implementation Report

**Project:** Cure Me, Please
**Module:** Treatment — Knife Mini Game (open wounds & cut out growths)
**Engine:** Unity 6000.3.17f1 (Unity 6.3 LTS), 2D, World-space GameObjects
**Input:** Input System Package (New) — `UnityEngine.InputSystem`
**Document status:** Design specification, reconciled with the current project systems
**Last updated:** July 1, 2026

> **Project reconciliation note:** This document was aligned with the live project. Two things to keep in mind while implementing: (1) `TongsMiniGame` wires the existing `Sanity` component **directly** (it holds `activeSanity` and calls `Sanity.AddSanity` from its event handlers) — there is no separate bridge class, so "reuse the bridge" means reuse that direct wiring. (2) The handoff target `AnatomyController` **already exists** but currently hard-codes a **single** mini game (`tongsMiniGame` + `tongsArea`); wiring Knife in requires extending it and matching the `TongsMiniGame` public contract. See Sections 3.1 and 8.2.

> **Implementation space:** Like the Tongs mini game, the Knife mini game is built with **World-space GameObjects** (`SpriteRenderer` + `Collider2D`), not UI Canvas. The patient body, lesions, and cut guide lines live in the scene as world objects. Only secondary readouts (pain meter, progress) may remain on a Canvas overlay. All distances are in **world units** unless marked otherwise.

> **Input System notice (Unity 6.3):** Active Input Handling is *Input System Package (New)* only, so the legacy `Input` class will not compile. All input uses `Mouse.current` from `UnityEngine.InputSystem`.

---

## 1. Overview

The Knife mini game is the second treatment mini game, alongside the already-implemented Tongs mini game. The player uses a scalpel to either **open a wound** (bulge) or **cut out a growth** (tumor) by slicing along a curved dotted guide line, while managing patient pain and avoiding straying outside the cut path.

It shares its core pressure systems with the Tongs mini game (local pain bar, mouse jitter, straying penalty feeding into the global Sanity meter) but introduces three new systems: a **curved path-following cut mechanic**, **two lesion sub-modes**, and a **bare-hand phase** for pulling flesh out after opening a wound.

---

## 2. Confirmed Design Decisions

| Topic | Decision |
|---|---|
| Guide line shape | **Curved** — defined as an ordered path of waypoints (supports curves, e.g. around a tumor). A thin dotted line shows the player where to cut. |
| Cut direction | Player must **start at one end of the line and slice to the other end** — the path has a defined start and end, cut in order. |
| Straying outside the path mid-cut | Cut progress **rolls back slightly** (not a full reset) **and** Sanity increases while outside. |
| Lesion sub-modes | **Two:** Bulge (open wound, then pull flesh by hand) and Tumor (slice around to cut it out). |
| Tool model | `ToolState` is **None (bare hand)** or **Knife**. The player starts every mini game **bare-handed** and must click the Knife to hold it. |
| Bulge phase 2 (bare-hand pull) | Has a **pain bar** but **no mouse jitter** (jitter only applies while slicing with the knife). |
| Reused pressure systems | Local pain bar + Sanity spike, Perlin mouse jitter (slicing only), Sanity-on-stray, world-space, Input System, debug gizmos — all shared with Tongs. |
| Lesions per case | Random **2–5** lesions per case at predefined anchors. |

---

## 3. Relationship To The Tongs Mini Game

### 3.1 Reused (already designed/implemented for Tongs)

- **Local pain bar** that fills while acting and drains while paused; filling it fully applies a large one-time Sanity spike.
- **Perlin-noise mouse jitter** added to the tool position (never overriding the OS cursor); player counter-steers.
- **Straying penalty** feeding the existing `Sanity` component via `Sanity.AddSanity`.
- **World-space** GameObjects, `Mouse.current` input, and **debug gizmos** that line up with scene coordinates.
- Reuse the existing `Sanity` component with the **same direct wiring** Tongs uses — the mini game holds the active `Sanity` (from `customer.GetComponent<Sanity>()`) and calls `Sanity.AddSanity(...)` from its own handlers. There is **no** separate bridge class in the codebase, and do **not** create a new sanity manager.

### 3.2 New To Knife

- **Curved path-following** cut: measuring distance from the knife tip to a multi-segment path, and tracking ordered progress along it.
- **Two lesion sub-modes** (Bulge vs Tumor) with different completion logic.
- **Tool state / bare-hand phase**: opening a wound with the knife, then putting the knife down (bare hand) to pull flesh out.

---

## 4. Lesion Types & Flow

```csharp
public enum LesionType { Bulge, Tumor }
public enum ToolState  { None, Knife }
```

### 4.1 Tumor (single phase)

1. Player clicks the Knife to hold it (`ToolState.Knife`).
2. Player click-holds the tumor and slices along the curved guide line from start to end.
3. Watch the pain bar; counter-steer against jitter; stay inside the path.
4. When cut progress reaches 1.0, the tumor is cut out. Done.

### 4.2 Bulge (two phases)

**Phase 1 — open the wound (knife):**
1. Player holds the Knife.
2. Slice along the guide line from start to end (same cut mechanic as Tumor).
3. When cut progress reaches 1.0, the wound is open.

**Phase 2 — pull the flesh (bare hand):**
4. Player **puts the knife down** → `ToolState.None` (bare hand).
5. Player click-holds on the exposed flesh and pulls it out.
6. Phase 2 has a **pain bar** but **no jitter**.
7. When pull progress reaches 1.0, the flesh is removed. Done.

> Phase 2 pull only advances while bare-handed; phase 1 slice only advances while holding the knife. This gating is what makes the tool switch meaningful.

---

## 5. Cut Path System (New Core Mechanic)

### 5.1 Path Definition

The guide line is an ordered list of world-space points. Curves are approximated by enough waypoints.

```csharp
[SerializeField] private List<Vector2> cutPath;   // ordered start -> end, world space
[SerializeField] private float pathHalfWidth;     // max distance from path before "outside"
```

The dotted guide line is rendered from this same path (e.g. a `LineRenderer` or repeated dot sprites), so the visual and the logic always match.

### 5.2 Straying Check (Outside The Frame)

Each frame while slicing, find the distance from the knife tip to the **nearest segment** of the path. If it exceeds `pathHalfWidth`, the knife is outside.

```text
distance = min over each segment S in cutPath of DistancePointToSegment(knifeTip, S)
outside  = distance > pathHalfWidth
```

While outside:
- `Sanity.AddSanity(strayPenaltyPerSecond * Time.deltaTime)` (continuous)
- cut progress rolls back slightly: `cutProgress -= rollbackPerSecond * Time.deltaTime` (clamped to ≥ 0)

### 5.3 Ordered Progress

Cutting must proceed in order from the start of the path. Track the index of the furthest waypoint the knife has passed while staying inside.

```text
- Player must begin near cutPath[0] (the start end).
- As the knife tip moves inside the path past the next waypoint, advance the index.
- cutProgress = passedWaypoints / totalWaypoints
- Moving backward or skipping ahead does not increase progress.
```

This enforces the "start at one end, slice to the other" rule and prevents scribbling across the shape to cheat progress.

### 5.4 Distance-To-Segment Helper

```csharp
static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
{
    Vector2 ab = b - a;
    float t = Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f);
    t = Mathf.Clamp01(t);
    Vector2 proj = a + t * ab;
    return Vector2.Distance(p, proj);
}
```

---

## 6. Pain, Jitter & Sanity Integration

### 6.1 Pain (reused from Tongs)

- Pain rises while actively slicing (phase 1) or pulling (phase 2) and drains while paused.
- Pain is **local** to the current lesion and resets when the lesion is completed.
- If the pain bar fills completely, apply a single large Sanity spike (`painSpikeAmount`) and reset the pain bar. The player must pause periodically to let pain drain.

### 6.2 Jitter (reused, slicing only)

- While slicing with the knife, add a Perlin-noise lateral offset to the knife tip; the player counter-steers to keep the tip on the path.
- **No jitter during the bare-hand pull phase** (phase 2 of Bulge).

### 6.3 Sanity (reused component)

- Straying outside the path → continuous Sanity increase.
- Pain bar full → large Sanity spike.
- All routed through the existing `Sanity` component, never a new manager.

---

## 7. Input Model (Unity 6.3, Input System)

The player starts bare-handed. Clicking the Knife tool sets `ToolState.Knife`; putting it down returns to `ToolState.None`.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

// Per frame:
Mouse mouse = Mouse.current;
if (mouse == null) return;

Vector2 screenPos    = mouse.position.ReadValue();
Vector2 worldPointer = treatmentCamera.ScreenToWorldPoint(screenPos);

if (mouse.leftButton.wasPressedThisFrame)  { /* begin slice or begin pull, depending on ToolState */ }
if (mouse.leftButton.isPressed)            { /* continue slice (Knife) or pull (None) */ }
if (mouse.leftButton.wasReleasedThisFrame) { /* stop; pain begins draining */ }
```

Notes:
- Add `using UnityEngine.InputSystem;` to every input-reading script.
- `OnMouseDown` does not fire under the new Input System; select lesions with `Physics2D.OverlapPoint` against their `Collider2D`.
- Tool selection (click Knife / put down) can be simple world-space clickable objects or a small UI toggle, mirroring the Tongs `TongsTool` approach.

---

## 8. Component Structure

```
KnifeMiniGame (world-space controller)
├── ToolState (None / Knife) — player switches manually
├── spawns 2–5 lesions at predefined anchors (shuffle bag, no duplicate anchors)
└── Lesion (per spawn)
    ├── LesionType (Bulge / Tumor)
    ├── cutPath : List<Vector2>      // curved guide line
    ├── pathHalfWidth                // stray threshold
    ├── cutProgress : 0..1           // ordered slice progress
    ├── painLevel : 0..1             // local, reused from Tongs
    └── [Bulge only] pullProgress : 0..1   // phase-2 bare-hand pull
```

### 8.1 Lesion Sketch

```csharp
using UnityEngine;
using System.Collections.Generic;

public class Lesion : MonoBehaviour
{
    [SerializeField] private LesionType type;
    [SerializeField] private List<Vector2> cutPath;
    [SerializeField] private float pathHalfWidth = 0.35f;
    [SerializeField] private float rollbackPerSecond = 0.3f;
    [SerializeField] private float strayPenaltyPerSecond = 8f;

    private float cutProgress;   // 0..1
    private float pullProgress;  // 0..1 (Bulge phase 2)
    private float painLevel;     // 0..1 (local)
    private int   passedWaypoints;
    private bool  woundOpen;     // Bulge: phase 1 complete

    // TickSlice(worldPointer, dt): advance cutProgress in order while inside path,
    //   roll back + add Sanity while outside, raise pain, apply jitter.
    // TickPull(worldPointer, dt):  Bulge only, only after woundOpen and bare-handed,
    //   raise pullProgress, raise pain, NO jitter.
    // On cutProgress >= 1: Tumor -> removed; Bulge -> woundOpen = true.
    // On pullProgress >= 1 (Bulge): flesh removed.
    // On painLevel >= 1: Sanity spike + reset pain.
}
```

### 8.2 Integration Contract (must match TongsMiniGame)

`AnatomyController` already exists and drives mini games, but it is **not generic** yet — it hard-codes one mini game:

```csharp
// AnatomyController.cs (current)
[SerializeField] private BodyArea tongsArea = BodyArea.Arm;
[SerializeField] private TongsMiniGame tongsMiniGame;
// EnterInfectedArea: if (area == tongsArea && tongsMiniGame != null) tongsMiniGame.Begin(customer);
// SubscribeMiniGames: tongsMiniGame.MiniGameCompleted += HandleTongsCompleted;
```

So Knife integration has **two required pieces**:

**A. `KnifeMiniGame` must expose the same public surface as `TongsMiniGame`** so the controller can drive it identically:

| Member | Purpose |
|---|---|
| `void Begin(CustomerAgent customer)` | Start the mini game; find `Sanity` from the customer; `flow.SetTreatmentStress(true)`. |
| `void Stop()` | End and hide; `flow.SetTreatmentStress(false)`. |
| `void Pause()` / `void Resume()` | Used when the player leaves to the counter and returns. |
| `event Action MiniGameCompleted` | **No argument** — the controller tracks which area is active and marks it treated. |

Note `TongsMiniGame.Begin/Stop` already toggle `GameFlow.SetTreatmentStress`; Knife must do the same (hold a serialized `GameFlow flow` reference) so treatment-stress Sanity pressure stays consistent.

**B. Extend `AnatomyController`** to route non-Tongs areas to Knife (do not fork the flow):

```csharp
[SerializeField] private KnifeMiniGame knifeMiniGame;
[SerializeField] private List<BodyArea> knifeAreas = new() { BodyArea.Torso, BodyArea.Head, BodyArea.Leg };

// EnterInfectedArea(area):
//   if (area == tongsArea && tongsMiniGame != null)  -> tongsMiniGame.Begin(customer)
//   else if (knifeAreas.Contains(area) && knifeMiniGame != null) -> knifeMiniGame.Begin(customer)

// SubscribeMiniGames(): also  knifeMiniGame.MiniGameCompleted += HandleKnifeCompleted;
// HandleKnifeCompleted(): if (isMiniGameRunning && knifeAreas.Contains(activeArea)) MarkAreaTreated(activeArea);
```

Both completion handlers converge on the existing `MarkAreaTreated(area)` → `RefreshAllPartVisuals()` → `TreatmentCompleted` path, so the case-completion and Sanity-reset flow through `Treatment` / `GameFlow.CompleteTreatment` is unchanged. (Longer term, a small `ITreatmentMiniGame` interface with `Begin/Stop/Pause/Resume/MiniGameCompleted` would let `AnatomyController` hold one list instead of one field per tool — optional, not required for the first Knife slice.)

---

## 9. Debug Gizmos (Editor Visualization)

Because the mini game is world-space, gizmos draw at scene coordinates with no conversion. Draw:

- The **cut path** as connected segments (the guide line), cyan.
- The **path corridor** at `pathHalfWidth` on each side (green inside / red when the knife tip is outside).
- The **jitter range** band around the player's steer position, yellow (slicing only).
- The **knife tip** as a small sphere; the **current furthest waypoint** as a marker so you can see ordered progress.

```csharp
void OnDrawGizmos()
{
    if (cutPath == null || cutPath.Count < 2) return;

    Gizmos.color = Color.cyan;
    for (int i = 0; i < cutPath.Count - 1; i++)
        Gizmos.DrawLine(cutPath[i], cutPath[i + 1]);

    // knife tip + corridor state can be drawn here using the live knife position,
    // colored red when DistancePointToSegment(...) > pathHalfWidth.
}
```

Reading the gizmos: keep the yellow jitter band roughly within the green corridor. If the yellow band is wider than the corridor, jitter alone can push the knife outside — the player must actively counter-steer.

---

## 10. Phased Work Plan

### Phase 1 — Cut Path Core (Tumor)
- `KnifeMiniGame`, `Lesion`, `LesionType`, `ToolState`.
- One Tumor lesion with a curved `cutPath`.
- Knife tool select (bare hand → knife).
- Slice along the path in order; `cutProgress` reaches 1 → removed.
- Distance-to-path stray check → roll back + `Sanity.AddSanity`.
- Draw the path gizmo. Run `dotnet build "Pixel-forge-game-jam.slnx"`.

### Phase 2 — Pain
- Local pain bar rising while slicing, draining while paused.
- Pain full → `Sanity.AddSanity(painSpikeAmount)` + reset.

### Phase 3 — Bulge Two-Phase
- Add Bulge: phase 1 slice (reuse cut), then bare-hand phase 2 pull.
- Gate phase 2 behind `woundOpen` and `ToolState.None`.
- Phase 2: pain bar on, jitter off.

### Phase 4 — Jitter & Stray Polish
- Perlin jitter while slicing (off during pull).
- Tune `pathHalfWidth`, `rollbackPerSecond`, jitter values with gizmos.

### Phase 5 — Multi-Lesion & Integration
- Spawn 2–5 lesions at anchors (shuffle bag, no duplicate positions).
- Complete when all lesions are done; raise `MiniGameCompleted` so `AnatomyController` marks the area treated (see Section 8.2 for the required contract and controller changes).
- Extend `AnatomyController` with `knifeMiniGame` + `knifeAreas` (e.g. Torso / Head / Leg) alongside the existing `tongsMiniGame` field.

### Phase 6 — Feedback
- Final art, cut SFX, blood VFX, pain heartbeat tie-in, success/fail feedback.

---

## 11. Starting Balance Values

Distances in world units — tune against the actual camera with the gizmos.

| Parameter | Bulge | Tumor |
|---|---:|---:|
| `pathHalfWidth` (world units) | 0.35 | 0.30 |
| `rollbackPerSecond` (progress/sec outside) | 0.30 | 0.35 |
| `strayPenaltyPerSecond` (sanity/sec) | 8 | 8 |
| `painPerSecond` (slicing) | 0.22 | 0.26 |
| `painDrainRate` (per sec) | 0.40 | 0.40 |
| `painSpikeAmount` (sanity) | 25 | 25 |
| `jitterStrength` (world units, slicing) | 0 first, later 0.30 | 0 first, later 0.35 |
| `jitterFrequency` | 1.6 | 2.0 |
| Bulge phase-2 `pullPerSecond` | 0.5 | — |
| Bulge phase-2 pain | on (no jitter) | — |

Case-level:

| Parameter | Value |
|---|---|
| Lesions per case | 2–5 (random) |
| Sanity spike (pain full) | +25 |
| Sanity per second (outside path) | +8 / sec |

Order of tuning: cut feel first, then pain, then Bulge phase 2, then jitter last.

---

## 12. Risks & Notes

- **Legacy input will not compile:** use `Mouse.current`; add `using UnityEngine.InputSystem;`. `OnMouseDown` does not fire — select via `Physics2D.OverlapPoint`.
- **Path authoring effort:** curved paths need enough waypoints to feel smooth but not so many that authoring is tedious. Consider a small editor tool to place/curve points (similar to the existing Tongs/Anatomy setup tools).
- **Ordered progress vs. player freedom:** strict ordering prevents cheating but can feel punishing if `pathHalfWidth` is too tight around curves. Widen the corridor on sharp bends.
- **Rollback severity:** `rollbackPerSecond` that is too high makes straying feel like a full reset; too low makes the corridor meaningless. Tune with playtests.
- **Tool-switch clarity:** since bare hand = "no tool," make it obvious when the knife is down vs held (cursor art, tool highlight) so players understand why the pull won't start.
- **Reuse, don't duplicate:** share the pain/jitter/Sanity code paths with Tongs where practical rather than copying, to keep balancing unified.
- **Phase 2 has pain but no jitter (by design):** confirm this reads as intended and doesn't feel inconsistent; it is a deliberate difficulty choice.

---

## 13. Summary

The Knife mini game reuses the Tongs pressure model (local pain bar + Sanity spike, Perlin jitter, stray-into-Sanity, world-space, Input System, gizmos) and adds a curved **cut-path system**: an ordered waypoint path with a corridor width, where the player slices from one end to the other, straying rolls progress back slightly and raises Sanity, and progress only counts in order. It supports two lesion types — **Tumor** (slice to cut out, one phase) and **Bulge** (slice to open, then put the knife down and pull the flesh out bare-handed, two phases). The player always starts bare-handed and must pick up the knife; the bare-hand pull phase keeps a pain bar but drops the jitter. Built for Unity 6000.3.17f1 with the new Input System from the start, the plan moves cut-path core → pain → Bulge two-phase → jitter/stray polish → multi-lesion integration → feedback, keeping all tunable values in data and reusing the existing `Sanity` component rather than adding a parallel system.
