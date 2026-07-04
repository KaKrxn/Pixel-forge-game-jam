# Customer Presence In Treatment Room — Design Report

**Project:** Cure Me, Please
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`
**Engine:** Unity 6000.3.17f1, 2D Pixel Art
**Report date:** July 5, 2026
**Module:** Customer Flow / Treatment Room Presence
**Document status:** Technical design (no code yet)

---

## 1. Purpose

Right now the customer only exists in the **Counter Room**. After dialog, the room blink-fades to the
**Treatment Room**, but the customer is left behind (its GameObject lives under the counter room root,
which `RoomTransition` deactivates), so the treatment room shows no patient.

This design makes the customer a real presence in the Treatment Room:

1. After dialog ends, the customer is moved into the Treatment Room and placed at a **set patient point**.
2. On the **Anatomy select screen**, the customer figure is **visible**.
3. When a **mini game** starts, the customer figure is **hidden** (the zoomed body-area prefab represents
   them instead).
4. Returning from a mini game to the **select screen** shows the customer figure again.
5. When the player leaves to the **Counter Room** to refill the candle, the customer **stays in the
   Treatment Room** — it does not follow the player.
6. The customer only returns to the Counter Room **after the case is cured**, then walks out of the shop.

---

## 2. Current Project Context

### 2.1 Systems Involved

| System | Current role |
|---|---|
| `CustomerAgent` | Moves the customer along shop enter/exit paths (`BeginEnter`, `ArriveAtCounter`, `BeginExit`). Position is driven by `transform` + waypoints. Holds `Bubble`, `CustomerLayer`, `Door`, and (via `GetComponent`) `Sanity`. |
| `RoomTransition` | Blink-fade between rooms. `ApplyRoomState` does `counterRoomRoot.SetActive(...)` / `treatmentRoomRoot.SetActive(...)`. |
| `Treatment` | `Begin(customer)` → `RoomTransition.ShowTreatmentRoom(cb)` → `AnatomyController.Begin`. Also `ReturnToCounter` / `ResumeTreatment`. |
| `AnatomyController` | `Begin(customer)`, `ShowAnatomyLevel(...)` (select screen), `EnterInfectedArea` / `FoldForMiniGame` (start a mini game), stops mini games on return. |
| `GameFlow` | Owns `activeCustomer`. `CompleteTreatment` → `RoomTransition.ShowCounterRoom(BeginActiveCustomerExit)` → `activeCustomer.BeginExit()`. |
| `Sanity` | Component on the customer. `GameFlow.TickHiddenStatusSystems` already ticks it even while the customer GameObject is **inactive**, so deactivating the customer does not freeze Sanity. |

### 2.2 Where The Customer Lives Today

- The `Customer` GameObject sits under the **Counter Room root** (`Main RoomRoot`).
- When `RoomTransition` shows the Treatment Room, the counter root deactivates → the customer disappears.
- The Treatment Room has **no patient anchor** yet (confirmed: no `PatientPoint` / patient object in the scene).

### 2.3 Key Insight

Because rooms are toggled by **GameObject active state**, "the customer stays in the Treatment Room and
does not follow the player" comes for free **if the customer is parented under the Treatment Room root**:
leaving to the counter deactivates the treatment root (customer hidden, left behind); returning
reactivates it (customer visible at its point again). `Sanity` keeps ticking via the existing hidden-tick.

---

## 3. Confirmed Design Decisions

| Topic | Decision |
|---|---|
| Where the customer goes | A single **Treatment Patient Point** (Transform) in the Treatment Room. |
| How it gets there | **Teleport during the blink-fade** (rooms are separate; no cross-room walk needed). A short in-room walk to the point is a later polish option. |
| Parenting | **Reparent the customer under the Treatment Room root** while treating; reparent back under the Counter Room root for the cure exit. |
| "Doesn't follow the player" | Handled by parenting: the customer is left in the (now inactive) Treatment Room root when the player goes to the counter. **No reparent on return-to-counter.** |
| Visible on select, hidden in mini game | Driven by `AnatomyController`: show on `ShowAnatomyLevel`, hide on `EnterInfectedArea` / fold. |
| Return to counter room | **Only on cure.** `CompleteTreatment` reparents the customer back to the counter room, then runs the existing `BeginExit` walk-out. |
| Ownership of the behavior | A small dedicated component (`TreatmentPatientPresenter`) rather than bloating `CustomerAgent`. Can be folded into `CustomerAgent` later if preferred. |

---

## 4. Non-Goals For The First Version

- Walking animation between counter and treatment (teleport during fade is enough).
- Multiple patient poses / per-area camera framing.
- Reacting the customer figure to Sanity/aggression here (Patient Aggression is a separate system).
- Replacing the Anatomy body-part UI. The customer figure is the **visual patient**; the anatomy
  buttons remain the interactive picker layered over/near it (art/layout is a separate task).

---

## 5. New Concept: Treatment Patient Point + Presenter

### 5.1 Treatment Patient Point

A single empty `Transform` authored under the **Treatment Room root** (e.g.
`Treatment RoomRoot/03_Gameplay/PatientPoint`). This is where the customer stands while being treated.
Keep it in the gameplay layer so sorting matches other treatment content.

### 5.2 `TreatmentPatientPresenter` (new component)

A small controller that owns the customer's treatment-room presence. Suggested path:

```text
Assets/Core/Script/Customer/TreatmentPatientPresenter.cs
```

Serialized references:

```csharp
[SerializeField] private Transform counterHomeParent;    // Counter Room root (where the customer normally lives)
[SerializeField] private Transform treatmentRoomParent;  // Treatment Room root to reparent under
[SerializeField] private Transform treatmentPatientPoint;// stand position in the Treatment Room
[SerializeField] private GameObject customerVisualRoot;  // the figure to show/hide (keeps CustomerAgent active)
[SerializeField] private CustomerLayer customerLayer;    // optional: set a treatment-room sorting state
```

Public API:

```csharp
public void MoveToTreatment(CustomerAgent customer);  // reparent under treatment root, place at point, show
public void ShowPatient();                            // anatomy select level
public void HidePatient();                            // during a mini game
public void ReturnToCounterForExit(CustomerAgent customer); // reparent back to counter, position for BeginExit
```

Rationale for `customerVisualRoot` over toggling the whole GameObject: the `CustomerAgent`/`Sanity`
components stay active and easy to reason about; only the visible figure is hidden during a mini game.

---

## 6. Visibility & Location Rules

| Context | Customer parent | Figure visible? | Notes |
|---|---|---|---|
| Counter Room (dialog / idle) | Counter Room root | Yes | Existing behavior. |
| Transition → Treatment (blink) | Reparented to Treatment root, placed at PatientPoint | Yes (revealed as fade opens) | Teleport during the closed-eye hold. |
| Anatomy select screen | Treatment root | **Yes** | `ShowPatient()`. |
| Inside a mini game | Treatment root | **No** | `HidePatient()`; body-area prefab represents them. |
| Player returned to Counter to refill | Treatment root (now inactive) | No (left behind) | No reparent; hidden with the deactivated treatment root. |
| Player resumes treatment | Treatment root (active again) | Yes | Back at the select screen → `ShowPatient()`. |
| Cured → exit | Reparented back to Counter root | Yes | `ReturnToCounterForExit` then existing `BeginExit`. |

---

## 7. Flow

### 7.1 Enter Treatment (after dialog)

```
GameFlow.CompleteDialog()
  -> Treatment.Begin(customer)
       -> RoomTransition.ShowTreatmentRoom(callback)      // blink fade
            during the closed-eye hold / callback:
              -> presenter.MoveToTreatment(customer)       // reparent to treatment root + place at PatientPoint
              -> AnatomyController.Begin(customer)
                   -> ShowAnatomyLevel(...)  -> presenter.ShowPatient()
```

### 7.2 Enter / Leave A Mini Game

```
AnatomyController.EnterInfectedArea(area)  -> presenter.HidePatient()   // figure hidden, body prefab shown
mini game completes / ExitCurrentPart
  -> AnatomyController.ShowAnatomyLevel(...) -> presenter.ShowPatient()  // figure back
```

### 7.3 Return To Counter To Refill (customer stays)

```
Treatment.ReturnToCounter()
  -> RoomTransition.ShowCounterRoom(...)   // treatment root deactivates -> customer hidden, left in place
     (no presenter call; customer is NOT reparented)
Treatment.ResumeTreatment()
  -> RoomTransition.ShowTreatmentRoom(...) // treatment root reactivates -> customer visible at PatientPoint
     -> AnatomyController.ResumeAtAnatomyLevel() -> ShowAnatomyLevel -> presenter.ShowPatient()
```

### 7.4 Cure → Walk Out

```
GameFlow.CompleteTreatment(customer)
  -> presenter.ReturnToCounterForExit(customer)   // reparent back to counter root, place near counter/door
  -> RoomTransition.ShowCounterRoom(BeginActiveCustomerExit)
  -> activeCustomer.BeginExit()                   // existing walk-to-door-and-leave
```

---

## 8. Integration Points (minimal edits)

| Where | Change |
|---|---|
| `Treatment.Begin` (inside the `ShowTreatmentRoom` callback) | Call `presenter.MoveToTreatment(activeCustomer)` before `AnatomyController.Begin`. |
| `AnatomyController.ShowAnatomyLevel` | Call `presenter.ShowPatient()` (or raise an event the presenter subscribes to). |
| `AnatomyController.EnterInfectedArea` / `FoldForMiniGame` | Call `presenter.HidePatient()`. |
| `Treatment.ReturnToCounter` / `ResumeTreatment` | **No reparent.** Room active-state already handles hide/show. |
| `GameFlow.CompleteTreatment` | Call `presenter.ReturnToCounterForExit(customer)` before `ShowCounterRoom`. |

To keep systems decoupled, prefer having the presenter **subscribe to `AnatomyController.NavigationStateChanged`**
(already exists) and derive show/hide from `IsInsidePart`, instead of adding direct calls inside AnatomyController.

---

## 9. Implementation Plan (phased)

### Phase 1 — Patient Point + Move Into Treatment
- Author `PatientPoint` under the Treatment Room root.
- Add `TreatmentPatientPresenter` with `MoveToTreatment` (reparent + place) and `ShowPatient`/`HidePatient`.
- Hook `Treatment.Begin` to call `MoveToTreatment` in the transition callback.
- Result: customer appears at the point on the select screen. Run `dotnet build "Pixel-forge-game-jam.slnx"`.

### Phase 2 — Show/Hide With Anatomy
- Drive `ShowPatient`/`HidePatient` from `AnatomyController` state (subscribe to `NavigationStateChanged` /
  `IsInsidePart`, or add the two calls).
- Verify: visible on select, hidden in mini game, visible again on return.

### Phase 3 — Stays In Treatment On Counter Refill
- Confirm return-to-counter does **not** reparent; customer stays under the (now inactive) treatment root.
- Confirm `Sanity` still ticks via `GameFlow.TickHiddenStatusSystems` while the customer is inactive.

### Phase 4 — Cure Exit
- `GameFlow.CompleteTreatment` calls `ReturnToCounterForExit` (reparent to counter root + position) before
  `ShowCounterRoom`, so `BeginExit` walks out from a valid counter position.

### Phase 5 — Polish (optional)
- Short in-room walk to the point instead of teleport.
- Treatment-room sorting state on `CustomerLayer`.
- Idle/patient pose, subtle reaction hooks (later, alongside Aggression/Sanity).

---

## 10. Risks & Notes

- **Reparent + sorting:** moving the customer under the treatment root can change its sorting context. Set a
  treatment-room sorting order (via `CustomerLayer` or a serialized order) so the figure sits correctly
  behind foreground props and the anatomy UI.
- **Original counter home:** cache the customer's original counter parent (and a counter stand position)
  so `ReturnToCounterForExit` restores it before `BeginExit`; otherwise the walk-out may start off-screen.
- **Exit waypoints are in the counter room:** `BeginExit` uses `insideDoorPoint`/`outsideDoorPoint` — the
  customer must be back under the counter root before exit, or the path will be wrong.
- **Sanity while inactive:** already handled by the hidden-tick; do not add a second ticking path.
- **World figure vs Anatomy UI:** the customer figure and the body-part buttons must be laid out so the
  player can still read/click the parts. Decide whether buttons overlay the figure or sit beside it
  (art/layout task, out of scope here).
- **Teleport timing:** do the reparent/reposition during the blink's closed-eye hold so the jump is unseen.

---

## 11. Summary

Make the customer a real occupant of the Treatment Room by **reparenting it under the Treatment Room root
and placing it at a single Patient Point** after dialog. A small `TreatmentPatientPresenter` owns this:
it moves the customer in during the blink-fade, **shows** the figure on the Anatomy select screen, **hides**
it during a mini game, and — crucially — does nothing special on return-to-counter, so the customer is
simply left in the (now inactive) Treatment Room and does not follow the player. Only on **cure** does the
presenter reparent the customer back to the Counter Room so the existing `BeginExit` walk-out plays. This
reuses the current room-active-state model and the existing hidden-tick for `Sanity`, keeping the change
small and the flow readable.
