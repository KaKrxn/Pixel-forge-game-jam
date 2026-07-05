# Customer Treatment Scale — Design Report

**Project:** Cure Me, Please
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`
**Engine:** Unity 6000.3.17f1, 2D Pixel Art
**Report date:** July 5, 2026
**Module:** Customer Flow / Treatment Room Presence (scale)
**Document status:** Technical design (no code yet)

---

## 1. Purpose

When the customer enters the **Treatment Room**, it should **scale up** to a defined size (a close-up
patient view). When the customer **leaves the shop**, it should **scale back down** to a defined size
(the normal shop size). This is a visual add-on to the existing treatment-room presence, not a new system.

---

## 2. Current Project Context

The customer's treatment-room presence already exists:

| System | Current role |
|---|---|
| `TreatmentPatientPresenter` | Reparents the customer under the Treatment Room root and places it at the patient point on `MoveToTreatment(customer)`; reparents it back to the counter home on `ReturnToCounterForExit(customer)`. Shows/hides the figure with `AnatomyController` navigation. |
| `Treatment.Begin` → `BeginTreatmentContent` | Calls `patientPresenter.MoveToTreatment(activeCustomer)` (during the blink fade). |
| `Treatment.CompleteTreatmentCase` | Calls `patientPresenter.ReturnToCounterForExit(activeCustomer)` before `GameFlow.CompleteTreatment` → the customer walks out. |
| `CustomerAgent` | Walks the shop enter/exit paths at the customer's authored (shop) scale. |

**Key point:** the two moments the user wants — "enters treatment" and "leaves the shop" — are exactly
`MoveToTreatment` and `ReturnToCounterForExit`, which already exist. Scaling is added there.

---

## 3. Confirmed Design Decisions

| Topic | Decision |
|---|---|
| Two defined sizes | `treatmentScale` (enlarged, in the treatment room) and `shopScale` (normal, at the counter / walking in & out). Both serialized so the designer sets them. |
| When to enlarge | On `MoveToTreatment` (entering the treatment room). |
| When to shrink | On `ReturnToCounterForExit` (heading back to the counter to walk out). |
| Transition | **Smooth scale animation** (lerp over a short duration) by default, so it reads as "the patient looms closer / steps back". An **instant** option (applied during the blink fade) is available for a snappier feel. |
| Ownership | Extend `TreatmentPatientPresenter` — it already owns the move-in / return-for-exit and the customer reference. No new component needed. |
| Pivot | Scale should grow from the **feet/base** so the patient stays grounded (see Risks). |
| Restore safety | Cache the customer's original local scale on first move; `shopScale` defaults to that if left unset. |

---

## 4. Component Changes — `TreatmentPatientPresenter`

Add scale fields and apply them in the two existing entry points.

```csharp
[Header("Scale")]
[SerializeField] private Vector3 treatmentScale = new Vector3(2f, 2f, 1f); // enlarged in the treatment room
[SerializeField] private Vector3 shopScale = Vector3.one;                  // normal shop size (0 = use cached original)
[SerializeField] private bool animateScale = true;
[SerializeField, Min(0f)] private float scaleDuration = 0.35f;
[SerializeField] private bool useUnscaledTime = true;
```

Behavior:

```text
MoveToTreatment(customer):
    ... (existing reparent + place at patient point) ...
    cache original localScale on first move
    ApplyScale(treatmentScale)          // enlarge

ReturnToCounterForExit(customer):
    ... (existing reparent back to counter) ...
    ApplyScale(shopScale or cachedOriginal)   // shrink back before the walk-out
```

`ApplyScale(target)`:
- If `animateScale`: lerp `customer.localScale` from current → target over `scaleDuration` (a small
  coroutine, unscaled time), then finish exactly on target.
- Else: set `customer.localScale = target` instantly.

> Because reparenting can bake parent scale into the child's `localScale`, **set the scale explicitly
> after the reparent** (the presenter already sets position after reparent — scale is set the same way).

---

## 5. Flow

```
Customer walks into shop / dialog          -> shopScale (authored)
Dialog ends -> Treatment.Begin (blink)     -> MoveToTreatment -> scale UP to treatmentScale
  Anatomy select / mini games              -> stays at treatmentScale
Cured -> CompleteTreatmentCase             -> ReturnToCounterForExit -> scale DOWN to shopScale
  BeginExit walks out of the shop          -> at shopScale
```

- The enlarge happens as the treatment room opens (during / just after the blink fade).
- The shrink happens as the customer is sent back to the counter for the exit walk, so it is already the
  correct size while walking out of the shop.

---

## 6. Transition Options

| Mode | When | Look |
|---|---|---|
| **Instant** | Apply during the blink's closed-eye hold | Size just "is" correct when the fade opens — snappy, no motion. |
| **Animated (default)** | Lerp over `scaleDuration` after the room shows | Patient visibly grows when entering / shrinks when leaving — more dramatic, fits the horror-clinic mood. |

Recommend **animated** for the enter (patient looms in) and either mode for the exit. Keep `scaleDuration`
short (~0.3s) so it does not delay the flow.

---

## 7. Implementation Plan (phased)

### Phase 1 — Instant Scale
- Add `treatmentScale` / `shopScale` to `TreatmentPatientPresenter`.
- Set the scale instantly in `MoveToTreatment` (up) and `ReturnToCounterForExit` (down), after the reparent.
- Cache the original scale on first move; default `shopScale` to it if unset. Run `dotnet build`.

### Phase 2 — Animated Scale
- Add `animateScale` + `scaleDuration`; lerp on unscaled time via a small coroutine, snapping to the
  exact target at the end. Cancel any in-flight scale tween when a new one starts.

### Phase 3 — Polish (optional)
- Slight ease-out curve; a subtle "settle" overshoot on enter.
- Tie the scale target to per-patient data later (different patients could be authored at different sizes).

---

## 8. Risks & Notes

- **Pivot / grounding:** if the customer sprite's pivot is at the **feet/bottom**, scaling grows it
  upward and it stays standing on the floor. If the pivot is centered, enlarging will also push it down
  through the floor — set the pivot at the base, or offset the patient point by the size change.
- **Reparent bakes scale:** setting `SetParent(..., worldPositionStays:true)` can change `localScale` if
  the parent has a non-1 scale. Always set `localScale` explicitly after reparenting (as the presenter
  already does for position).
- **Pixel-art crispness:** scaling pixel sprites by **non-integer** factors (or mid-animation) can shimmer.
  Prefer integer target factors (2×, 3×) where possible, keep sprites point-filtered, and keep the tween
  short; if shimmer is bad, use the instant mode.
- **Hidden during mini game:** the figure is hidden while a mini game runs, so mid-treatment scale is only
  seen on the select screen — that is fine; keep the scale applied so it is correct when shown again.
- **Restore correctness:** cache the original scale once; restoring to `shopScale` (or the cached value)
  guarantees the walk-out and any future re-entry use the right size.
- **Position vs scale order:** set position first, then scale, so the patient-point placement is not
  thrown off by the scale change (or place from a base anchored at the feet).

---

## 9. Summary

Add a `treatmentScale` and a `shopScale` to `TreatmentPatientPresenter` and apply them at the two moments
that already exist: **enlarge on `MoveToTreatment`** (entering the treatment room) and **shrink on
`ReturnToCounterForExit`** (heading out of the shop). Do it as a short, unscaled-time **smooth lerp** by
default (with an instant option for the blink), setting `localScale` explicitly after the reparent so the
parent's scale never interferes. Grounding the sprite pivot at the feet keeps the patient standing while it
grows, and caching the original scale guarantees the walk-out returns to the correct shop size.
