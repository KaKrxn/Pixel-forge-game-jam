# Anatomy Selection Screen — Design & Implementation Report

**Project:** Cure Me, Please
**Module:** Treatment — Anatomy Selection Screen (body-part picker before mini games)
**Engine:** Unity 6000.3.17f1 (Unity 6.3 LTS), 2D
**Input:** Input System Package (New) — `UnityEngine.InputSystem`
**Document status:** Design specification, reconciled with the current project systems
**Last updated:** July 1, 2026

> **Project reconciliation note:** Terminology and API references in this document were aligned with the live project. The room is the **Treatment Room** (`Treatment RoomRoot` / `TreatmentRoot`, driven by `Treatment.cs` + `RoomTransition.cs`) — the project has no separate "Operating Room". Room changes go through `RoomTransition.ShowCounterRoom()` / `ShowTreatmentRoom()`, the mini game handoff uses the existing `TongsMiniGame` API, and case completion routes through `Treatment` → `GameFlow.CompleteTreatment(customer)`. See Section 8.5.

---

## 1. Overview

The Anatomy Selection Screen is the screen the player sees after finishing a patient's dialog and entering the Treatment Room. It shows the patient's body as a standing figure (T-pose). The player clicks a body part (Head, Torso, Arm, Leg) to zoom into that part's treatment mini game (e.g. the Tongs extraction mini game).

The twist that drives the gameplay: the screen does **not** reveal which parts are infected. The player must remember the symptoms from the dialog and choose the correct parts from memory. Entering a healthy part simply wastes time; entering an infected part starts a mini game that must be completed before the player can leave that part.

This document analyzes the screen, locks in the design decisions confirmed with the designer, lays out a phased work plan, and specifies the implementation in detail.

---

## 2. Confirmed Design Decisions

These were decided up front and the rest of the document is built around them.

| Topic | Decision |
|---|---|
| Sprite assembly | **Cut sprites per part** (head, torso, arm, leg as separate sprites) and assemble them in the Canvas. |
| Hit detection on irregular shapes | Use `Image.alphaHitTestMinimumThreshold` so only opaque pixels of each part receive pointer events (no rectangular overlap). |
| Left/right limbs | **Two separate sprites/objects** (e.g. ArmLeft + ArmRight) that report the **same** `BodyArea` value. Hover scales each side independently. |
| Part selection order | **Free** — the player may pick any part in any order. |
| Parts per case | **Two modes, selectable in the Inspector:** (1) Manual — designer ticks exactly which parts are infected; (2) Random — randomly pick 1–4 infected parts. |
| Wrong guess | Entering a non-infected part shows it has nothing; the player can leave immediately (time lost only, no penalty). |
| Infection hint | **None.** All parts look identical at first; the player must remember from the dialog. |
| Treated part feedback | The part **changes color** once its mini game is completed. |
| Room transition | Only the **Counter ↔ Treatment Room** transition exists. Rules in Section 6. |
| Hover feedback | On hover, the part **highlights and scales up slightly**, as if it is about to be selected. |

---

## 3. Body Parts

Four logical treatment areas:

```csharp
public enum BodyArea { Head, Torso, Arm, Leg }
```

Visual breakdown into sprites/objects (left/right limbs are separate objects but map to one area):

| Object | Maps to BodyArea |
|---|---|
| HeadPart | Head |
| TorsoPart | Torso |
| ArmLeftPart | Arm |
| ArmRightPart | Arm |
| LegLeftPart | Leg |
| LegRightPart | Leg |

So there are **6 clickable objects** but only **4 logical areas**. Clicking ArmLeft or ArmRight both resolve to `BodyArea.Arm` — the same infection state and the same mini game entry.

---

## 4. Screen States & Part States

### 4.1 Per-Part State

Each part tracks one of these states:

```csharp
public enum PartState
{
    Untouched,   // not yet visited
    Healthy,     // visited, had no parasite (or never infected) — informational
    Infected,    // has a mini game waiting; must be completed
    Treated      // mini game completed — shown in a changed color
}
```

> Note: because the screen gives no hint, `Untouched` and `Infected` look identical to the player. The distinction is internal until the player enters the part.

### 4.2 Visual Feedback Per State

| State | Appearance |
|---|---|
| Untouched | Normal sprite |
| Healthy | Normal sprite (optionally a subtle "checked, nothing here" tint) |
| Infected | Normal sprite — deliberately indistinguishable from Untouched |
| Treated | **Color-changed** sprite (e.g. desaturated / tinted to read as "done") |
| Hover (any selectable state) | Highlight + slight scale-up |

---

## 5. Infection Setup (Two Inspector Modes)

The screen must support both a designer-authored layout and a randomized one, chosen by a toggle in the Inspector.

```csharp
public enum InfectionMode { Manual, Random }

[SerializeField] private InfectionMode infectionMode = InfectionMode.Manual;

// Manual mode: designer ticks exactly which areas are infected
[SerializeField] private bool headInfected;
[SerializeField] private bool torsoInfected;
[SerializeField] private bool armInfected;
[SerializeField] private bool legInfected;

// Random mode: pick a random count in [minInfected, maxInfected]
[SerializeField] private int minInfected = 1;
[SerializeField] private int maxInfected = 4;
```

At case start:

- **Manual** — read the four bools and mark those areas `Infected`, the rest `Untouched`.
- **Random** — choose a random integer N in `[minInfected, maxInfected]`, then randomly select N of the four areas to mark `Infected`.

This keeps both workflows available without code changes: designers can hand-craft specific patients (e.g. the Elf Archer infected in Arm + Head) or let the screen randomize for variety and testing.

> Later, this same data can be sourced from the `CustomerAgent` / patient ScriptableObject so each of the 7 patients carries its own infection layout. The Inspector toggle remains useful for prototyping and one-off tests.

---

## 6. Room Transition Rules

There is one transition: **Counter ↔ Treatment Room** (per the GDD). The Anatomy screen lives in the Treatment Room context. The rules:

1. While viewing the **Anatomy screen** (the full body, no part entered): the Counter ↔ Treatment Room transition button **is available**. The player can go back to the counter (e.g. to refill the candle) and return.
2. After the player **enters a part**:
   - If the part is **Infected** (has a mini game): the exit/transition button is **hidden** until the mini game is completed. The player is committed to finishing the treatment.
   - If the part is **Healthy / not infected** (no mini game): an exit button **is available immediately** so the player can back out right away.
3. The Counter ↔ Treatment Room transition is **only** offered at the Anatomy-screen level, never from inside a part.

State summary:

| Context | Counter↔Treatment button | Exit-part button |
|---|---|---|
| Anatomy screen (no part entered) | Visible | n/a |
| Inside an Infected part (mini game running) | Hidden | Hidden until complete |
| Inside a Healthy part (no mini game) | Hidden | Visible immediately |

---

## 7. Interaction Flow

```
Dialog ends
  -> Enter Treatment Room
  -> Anatomy Selection Screen shows the patient body (no hints)
  -> Infection layout is resolved (Manual or Random)
  -> Player recalls symptoms from dialog and clicks a part
       - Hover: part highlights + scales up slightly
       - Click ArmLeft/ArmRight -> resolves to BodyArea.Arm
  -> Enter the chosen part:
       - Infected  -> start that part's mini game; exit hidden until done
                       on completion -> mark Treated (color change) -> return to Anatomy
       - Healthy   -> show "nothing here"; exit available -> return to Anatomy
  -> Repeat until all Infected parts are Treated
  -> When no Infected parts remain -> treatment can complete
  -> GameFlow.CompleteTreatment(customer) handles cure, Sanity reset, patient exit
```

The Counter ↔ Treatment Room transition may be used any time the player is at the Anatomy-screen level (not inside a part).

---

## 8. Implementation Detail

### 8.1 Component Structure

```
TreatmentRoom (existing room context — `Treatment RoomRoot` / `TreatmentRoot`, driven by `Treatment.cs`)
└── AnatomyScreen (Canvas, Screen Space)
    ├── AnatomyController        // owns infection setup + completion tracking
    ├── BodyRoot
    │   ├── HeadPart   (BodyPartButton, area = Head)
    │   ├── TorsoPart  (BodyPartButton, area = Torso)
    │   ├── ArmLeftPart  (BodyPartButton, area = Arm)
    │   ├── ArmRightPart (BodyPartButton, area = Arm)
    │   ├── LegLeftPart  (BodyPartButton, area = Leg)
    │   └── LegRightPart (BodyPartButton, area = Leg)
    ├── TransitionButton         // Counter <-> Treatment Room (visibility per Section 6)
    └── ExitPartButton           // shown only inside a healthy part
```

### 8.2 BodyPartButton

Each clickable part. Uses the new Input System UI pipeline (pointer events still work through `EventSystem` as long as the scene uses `InputSystemUIInputModule`).

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class BodyPartButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private BodyArea area;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private Color treatedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private Image image;
    private Vector3 baseScale;
    private AnatomyController controller;

    void Awake()
    {
        image = GetComponent<Image>();
        // Only opaque pixels receive pointer events (irregular body shapes)
        image.alphaHitTestMinimumThreshold = 0.1f;
        baseScale = transform.localScale;
        controller = GetComponentInParent<AnatomyController>();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!IsSelectable()) return;
        transform.localScale = baseScale * hoverScale;   // highlight + scale up
        // optional: swap to a highlighted sprite or raise an outline
    }

    public void OnPointerExit(PointerEventData e)
    {
        transform.localScale = baseScale;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!IsSelectable()) return;
        controller.EnterArea(area);   // both left & right resolve to the same area
    }

    public void MarkTreated()
    {
        image.color = treatedColor;   // color change on completion
    }

    private bool IsSelectable()
    {
        // not selectable once the area is treated
        return controller != null && !controller.IsAreaTreated(area);
    }
}
```

`alphaHitTestMinimumThreshold` requires the sprite's texture to have **Read/Write Enabled** in its import settings; otherwise the alpha test throws. Note this in the asset import step.

### 8.3 AnatomyController

Owns infection resolution, area state, and completion tracking. Drives the transition/exit button visibility.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class AnatomyController : MonoBehaviour
{
    [SerializeField] private InfectionMode infectionMode = InfectionMode.Manual;
    [SerializeField] private bool headInfected, torsoInfected, armInfected, legInfected;
    [SerializeField] private int minInfected = 1;
    [SerializeField] private int maxInfected = 4;

    [SerializeField] private List<BodyPartButton> partButtons; // all 6 objects
    [SerializeField] private GameObject transitionButton;       // Counter <-> Treatment
    [SerializeField] private GameObject exitPartButton;         // healthy-part exit

    private readonly Dictionary<BodyArea, PartState> areaStates = new();

    public void Begin(/* CustomerAgent customer */)
    {
        ResolveInfection();
        ShowAnatomyLevel();   // transition visible, no part entered
    }

    private void ResolveInfection()
    {
        // default everything to Untouched
        foreach (BodyArea a in System.Enum.GetValues(typeof(BodyArea)))
            areaStates[a] = PartState.Untouched;

        if (infectionMode == InfectionMode.Manual)
        {
            if (headInfected)  areaStates[BodyArea.Head]  = PartState.Infected;
            if (torsoInfected) areaStates[BodyArea.Torso] = PartState.Infected;
            if (armInfected)   areaStates[BodyArea.Arm]   = PartState.Infected;
            if (legInfected)   areaStates[BodyArea.Leg]   = PartState.Infected;
        }
        else // Random
        {
            var all = new List<BodyArea>
                { BodyArea.Head, BodyArea.Torso, BodyArea.Arm, BodyArea.Leg };
            int n = Random.Range(minInfected, maxInfected + 1);
            for (int i = 0; i < n && all.Count > 0; i++)
            {
                int idx = Random.Range(0, all.Count);
                areaStates[all[idx]] = PartState.Infected;
                all.RemoveAt(idx);
            }
        }
    }

    public void EnterArea(BodyArea area)
    {
        if (areaStates[area] == PartState.Infected)
        {
            HideTransition();
            HideExitPart();              // committed until mini game done
            StartMiniGame(area);
        }
        else
        {
            HideTransition();
            ShowExitPart();              // healthy: can leave immediately
            ShowNothingHere(area);
        }
    }

    public void OnMiniGameComplete(BodyArea area)
    {
        areaStates[area] = PartState.Treated;
        foreach (var b in partButtons)
            // MarkTreated on the buttons whose area matches (both limb halves)
            // (a small helper on BodyPartButton can expose its area)
            ;
        ShowAnatomyLevel();
        if (AllTreated())
            CompleteTreatment();
    }

    public bool IsAreaTreated(BodyArea area) => areaStates[area] == PartState.Treated;

    private bool AllTreated()
    {
        foreach (var kv in areaStates)
            if (kv.Value == PartState.Infected) return false;
        return true;
    }

    private void ShowAnatomyLevel() { transitionButton.SetActive(true);  exitPartButton.SetActive(false); }
    private void HideTransition()   { transitionButton.SetActive(false); }
    private void ShowExitPart()     { exitPartButton.SetActive(true); }
    private void HideExitPart()     { exitPartButton.SetActive(false); }

    private void StartMiniGame(BodyArea area)   { /* hand off to the part's mini game */ }
    private void ShowNothingHere(BodyArea area) { /* show empty/clean part view */ }
    private void CompleteTreatment()            { /* GameFlow.CompleteTreatment(customer) */ }
}
```

### 8.4 Left/Right Limb Mapping

Both `ArmLeftPart` and `ArmRightPart` carry `area = Arm`. When `OnMiniGameComplete(BodyArea.Arm)` runs, **both** arm objects must change color so the whole arm reads as treated. The controller therefore iterates all part buttons and recolors any whose area matches the completed area (a small `public BodyArea Area` getter on `BodyPartButton` makes this clean).

### 8.5 Wiring Into the Existing Project (real APIs)

The stubs above (`StartMiniGame`, `transitionButton.SetActive`, `CompleteTreatment`) map onto systems that already exist. The Anatomy screen should slot **between** the room transition and the mini game inside the current `Treatment.cs` flow, not replace it.

Current flow (already implemented):

```
GameFlow.CompleteDialog()
  -> Treatment.Begin(customer)
       -> RoomTransition.ShowTreatmentRoom(callback)   // blink fade
            -> Treatment.BeginTreatmentContent()
                 -> TongsMiniGame.Begin(customer)        // today: starts Tongs directly
```

With the Anatomy screen inserted, `Treatment.BeginTreatmentContent()` shows the `AnatomyController` instead of starting Tongs immediately. The mapping:

| Report stub | Real hook in the project |
|---|---|
| `AnatomyController.Begin(customer)` | Called from `Treatment.cs` after `RoomTransition.ShowTreatmentRoom` completes (replaces the direct `TongsMiniGame.Begin` call). |
| `StartMiniGame(area)` | For `BodyArea.Arm` → `tongsMiniGame.Begin(customer)`. Other areas hand off to their own (future) mini games; reuse the same pattern. |
| mini game finished | Subscribe to the existing `TongsMiniGame.MiniGameCompleted` event → `AnatomyController.OnMiniGameComplete(area)`. (`Treatment.cs` already listens to this event today.) |
| `transitionButton` (Counter ↔ Treatment) | `RoomTransition.ShowCounterRoom()` / `RoomTransition.ShowTreatmentRoom()`. While at the counter, `TongsMiniGame.Pause()` is already used by `Treatment.ReturnToCounter()`; resume with `TongsMiniGame.Resume()`. |
| `CompleteTreatment()` | Do **not** call `GameFlow` from `AnatomyController`. Raise an event/callback that `Treatment.cs` handles, and let `Treatment` call `flow.CompleteTreatment(customer)` — keeping `Treatment` the single owner of the flow reference (matches the current code). |
| treatment stress | `TongsMiniGame.Begin/Stop` already toggle `GameFlow.SetTreatmentStress(true/false)`. The Anatomy screen does not need its own Sanity hooks — reuse the existing `Sanity` component via the mini game, per the Tongs report. |

Note the mini game itself (Tongs) is **World-space** (sprites + colliders), while this Anatomy picker is a **UI Canvas**. That split is intentional: the picker is flat UI; entering an infected part hands off to the world-space mini game. Keep the two layers separate, exactly as the Tongs design report specifies.

---

## 9. Input System Notes (Unity 6.3)

- The screen is **UI Canvas**, so part clicks come through the `EventSystem` + pointer interfaces (`IPointerEnterHandler`, etc.).
- The scene's `EventSystem` must use **`InputSystemUIInputModule`**, not the legacy `StandaloneInputModule`. With Active Input Handling set to *Input System Package (New)* only, the legacy module will not feed pointer events and **buttons will silently not respond**. **Current status:** `GameScene.unity` already uses `InputSystemUIInputModule`, so this is satisfied today — just confirm it still holds if the Anatomy screen is built in a new scene or a re-created EventSystem.
- `alphaHitTestMinimumThreshold` works with UI `Image` raycasting and needs the sprite texture's **Read/Write Enabled** import flag.

---

## 10. Phased Work Plan

### Phase 1 — Static Anatomy Screen
- Assemble the 6 cut sprites in a Canvas into a full body.
- Add `BodyPartButton` to each, set `area`, enable `alphaHitTestMinimumThreshold`.
- Confirm hover highlight + slight scale-up works per part.
- Verify the `EventSystem` uses `InputSystemUIInputModule`.

### Phase 2 — Infection Setup
- Add `AnatomyController` with the Manual/Random Inspector toggle.
- Resolve infection at `Begin()`.
- Log which areas are infected (debug only — no on-screen hint).

### Phase 3 — Enter / Exit Logic
- Implement `EnterArea`: Infected vs Healthy branch.
- Wire transition button + exit-part button visibility per Section 6.
- Healthy part: "nothing here" view + immediate exit.

### Phase 4 — Mini Game Handoff
- Connect Infected parts to the matching mini game (Tongs first).
- On mini game complete: mark `Treated`, recolor both limb halves, return to Anatomy.
- When all infected parts are treated, allow `GameFlow.CompleteTreatment`.

### Phase 5 — Polish
- Treated color/tint, healthy tint, hover sprite swap or outline.
- Optional transition animations between Anatomy and a zoomed part.

---

## 11. Risks & Notes

- **EventSystem input module:** with the new Input System only, a legacy `StandaloneInputModule` will break all clicks. Use `InputSystemUIInputModule`. `GameScene.unity` is already set up this way; the risk only returns if a new scene/EventSystem is created. (Section 9)
- **Read/Write Enabled textures:** required for `alphaHitTestMinimumThreshold`; forgetting it throws at runtime on hover.
- **Limb sync:** both halves of an arm/leg must recolor together on completion; drive it from the area, not the individual object.
- **Hover pivot:** set each part's pivot so the scale-up grows naturally (e.g. head grows from the neck) rather than drifting off position.
- **No-hint design depends on dialog quality:** since the screen never reveals infected parts, the dialog must give the player enough to remember. If players guess blindly too often, revisit the dialog clarity rather than adding hints here.
- **Future data source:** the Manual bools are a stand-in. Later, pull the infection layout from each patient's data so the 7 patients are authored once and reused.

---

## 12. Summary

The Anatomy Selection Screen is a **UI Canvas** picker built from **per-part cut sprites** assembled into a body, using `alphaHitTestMinimumThreshold` for accurate clicks on irregular shapes. Left and right limbs are separate objects that resolve to one `BodyArea`. Infection is set up in one of two Inspector-selectable modes — **Manual** (designer ticks parts) or **Random** (1–4 parts) — and the screen gives **no hint**, so the player must recall symptoms from the dialog. Selection is free-order; a wrong guess just costs time. Entering an infected part commits the player (exit hidden until the mini game is done); entering a healthy part allows an immediate exit. The Counter ↔ Treatment Room transition is available only at the Anatomy-screen level. Completed parts change color, and when all infected parts are treated, the case completes through the existing `GameFlow`.

The most important setup detail under Unity 6.3 is using `InputSystemUIInputModule` on the EventSystem; without it, no part button will respond.
