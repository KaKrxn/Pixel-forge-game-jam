# Customer Cured Visual — Design Report

**Project:** Cure Me, Please
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`
**Engine:** Unity 6000.3.17f1, 2D Pixel Art
**Report date:** July 5, 2026
**Module:** Customer Visual / Cure Result
**Document status:** Technical design (no code yet)

---

## 1. Purpose

Give each customer a **sick (default) look** and a **cured look**. When the player finishes treating a
customer, swap the customer to its **cured visual** — a different sprite and animation controller — so the
patient visibly looks healed as they thank the player and walk out.

The customer should store the extra **Sprite** and **Animation Controller** for the cured state, and the
game should switch to them at the cure moment.

---

## 2. Current Project Context

| System | Current role | Relevance |
|---|---|---|
| `CustomerAgent` | Owns the customer's movement (enter/counter/exit) and references (`CustomerLayer`, bubble, door). | Where the cure swap can be triggered / exposed. |
| `CustomerLayer` | Holds the customer's main `SpriteRenderer` (`targetRenderer`) and swaps its sorting layer. | The renderer whose `sprite` we swap. |
| `CustomerDefinition` | Data SO: `customerId`, `displayName`, `customerPrefab`, `caseData`. **No visual fields yet.** | Optional home for cured/sick art if data-driven. |
| `CustomerSpawner` | Instantiates a fresh customer from `CustomerDefinition.customerPrefab` per queue entry. | Each customer starts fresh (sick) — no reset needed between customers. |
| `GameFlow.CompleteTreatment` | The cure moment: `SetState(CustomerLeaving)` → `ShowCounterRoom(BeginActiveCustomerExit)` → `activeCustomer.BeginExit()`. | Hook the cured-visual swap here, **before the exit walk**, so the walk-out shows the healed patient. |
| `TreatmentPatientPresenter` | Shows the customer figure in the treatment room; returns it to the counter for the exit. | The cured swap should be applied by the time the figure walks out. |

Each customer already has a `SpriteRenderer` (via `CustomerLayer`). Whether it has an `Animator` today is
per-prefab; this design adds explicit references so both are handled.

---

## 3. Confirmed Design Decisions

| Topic | Decision |
|---|---|
| What to store | A **sick** (default) and a **cured** variant: each a `Sprite` **and** a `RuntimeAnimatorController`. |
| Where to store | On the customer **prefab**, in a small `CustomerVisual` component (art is per-character, and each customer has its own prefab). Optionally sourced from `CustomerDefinition` if the team prefers data-driven. |
| Swap style | **Replace** the visual (sprite + animator controller) — matches "store an extra controller and switch to it". An optional additive cured-effect object (glow/particles) can layer on top. |
| When to swap | At **cure** (`GameFlow.CompleteTreatment`), before the customer starts the exit walk, so the walk-out shows the cured look. |
| Reset | Not needed — the queue spawns a fresh customer each time; it starts sick by default. |
| Animation | Swap `Animator.runtimeAnimatorController` to the cured controller (a whole-controller swap). Alternative: one controller with a `Cured` bool/trigger. |

---

## 4. Non-Goals For The First Version

- Mid-treatment progressive changes (only the final cured swap).
- A transformation/fail visual (that is the lose path, separate).
- Per-body-area visual changes (the mini games / body prefabs handle those).
- Cutscene / animated morph — a clean state swap is enough first (an effect can be added later).

---

## 5. Component Design — `CustomerVisual`

```text
Assets/Core/Script/Customer/CustomerVisual.cs
```

On the customer prefab, next to `CustomerLayer` / the `SpriteRenderer`.

```csharp
[SerializeField] private SpriteRenderer bodyRenderer;        // defaults to the CustomerLayer renderer
[SerializeField] private Animator animator;

[Header("Sick (default)")]
[SerializeField] private Sprite sickSprite;
[SerializeField] private RuntimeAnimatorController sickController;

[Header("Cured")]
[SerializeField] private Sprite curedSprite;
[SerializeField] private RuntimeAnimatorController curedController;

[Header("Optional")]
[SerializeField] private GameObject curedEffectRoot;         // optional glow/particles shown when cured
```

API:

```csharp
public bool IsCured { get; }
public void ShowSick();     // apply sick sprite + controller (default on spawn)
public void ShowCured();    // apply cured sprite + controller (+ enable curedEffectRoot)
public void SetCured(bool cured);
```

Behavior:
- `Awake` resolves `bodyRenderer` (from `CustomerLayer`/`GetComponent`) and `animator`, and applies the
  sick state so a freshly spawned customer looks sick.
- `ShowCured()` sets `bodyRenderer.sprite = curedSprite` (if assigned) and
  `animator.runtimeAnimatorController = curedController` (if assigned), and enables `curedEffectRoot`.
- Guard each swap with a null check so a customer that only defines one field still works.

> If the team prefers **data-driven** art, add `curedSprite` / `curedController` (and sick) to
> `CustomerDefinition`, and have `CustomerSpawner` push them into `CustomerVisual` after instantiation
> (like it already pushes `caseData` into `CustomerCaseProvider`). Recommend starting on the prefab.

---

## 6. Cure Trigger & Flow

```
Player completes the case
  -> Treatment.CompleteTreatmentCase -> GameFlow.CompleteTreatment(customer)
       -> apply cured visual:  customerVisual.ShowCured()          // NEW, before the exit
       -> SetState(CustomerLeaving)
       -> RoomTransition.ShowCounterRoom(BeginActiveCustomerExit)
       -> activeCustomer.BeginExit()                                // walks out looking cured
```

- Do the swap **before** `ShowCounterRoom` / `BeginExit`, so the healed look is in place when the customer
  becomes visible in the counter room and walks to the door.
- Expose it cleanly: `CustomerAgent.MarkCured()` → forwards to its `CustomerVisual.ShowCured()`, so
  `GameFlow`/`Treatment` calls the agent (not the visual component directly).

---

## 7. Animation Approach

| Approach | How | Notes |
|---|---|---|
| **Controller swap (recommended)** | `animator.runtimeAnimatorController = curedController` | Matches "store an extra controller". Clean separation of sick vs cured animation sets. Rebinding resets animator params/state — fine for a full swap. |
| Single controller + parameter | one controller with a `Cured` bool/trigger and transitions | Keeps a shared state machine; needs both animation sets authored in one controller. Use if a smooth transition between sick↔cured is wanted. |

For a static cured pose (no idle animation), the sprite swap alone is enough — leave the controller field
empty.

---

## 8. Integration Points

| Where | Change |
|---|---|
| New `CustomerVisual` on the customer prefab | Holds sick/cured sprite + controller; `Awake` applies sick. |
| `CustomerAgent` | Add `MarkCured()` that calls its `CustomerVisual.ShowCured()` (resolve the component in `Awake`). |
| `GameFlow.CompleteTreatment` | Call `customer.MarkCured()` before `SetState(CustomerLeaving)` / `ShowCounterRoom`. |
| (optional) `CustomerDefinition` + `CustomerSpawner` | If data-driven: add art fields to the SO and push them into `CustomerVisual` on spawn. |

---

## 9. Implementation Plan (phased)

### Phase 1 — CustomerVisual + Sick Default
- Add `CustomerVisual` (sick/cured sprite + controller, resolve renderer/animator, apply sick on Awake).
- Add `CustomerAgent.MarkCured()` forwarding to it.
- Run `dotnet build "Pixel-forge-game-jam.slnx"`.

### Phase 2 — Swap On Cure
- Call `customer.MarkCured()` in `GameFlow.CompleteTreatment` before the exit.
- Verify the customer looks cured while thanking / walking out.

### Phase 3 — Polish (optional)
- `curedEffectRoot` (glow / sparkle) on cure.
- Data-driven art via `CustomerDefinition` if the team wants it centralized.
- A short cross-fade or cured SFX at the swap.

---

## 10. Risks & Notes

- **Animator rebind resets state:** swapping `runtimeAnimatorController` restarts the animator; ensure the
  cured controller's default state is the intended cured idle. Fine for a full swap.
- **Sprite pivot / size mismatch:** the cured sprite should share the same pivot/scale as the sick sprite,
  or the customer will jump/shift when swapped (keep both authored to the same rig).
- **Sorting unaffected:** `CustomerLayer` still controls the sorting layer; swapping only the `sprite` keeps
  sorting intact (do not create a second renderer that fights it).
- **Timing:** swap before the customer is shown walking out; doing it after `BeginExit` risks a one-frame
  sick flash.
- **Fresh per customer:** queue-spawned customers start from the prefab (sick) each time, so no reset is
  needed — but if a customer instance is ever reused, call `ShowSick()` on (re)spawn.
- **Treatment-room figure:** the customer figure is hidden during mini games and shown on the select
  screen; applying the cured swap at `CompleteTreatment` (when leaving) means the select screen always
  shows the sick look, which is correct.

---

## 11. Summary

Add a small `CustomerVisual` component to the customer prefab that stores a **sick** and a **cured**
`Sprite` + `RuntimeAnimatorController`, applies the sick look on spawn, and exposes `ShowCured()`. Expose
`CustomerAgent.MarkCured()` and call it from `GameFlow.CompleteTreatment` **before the exit walk**, so the
healed patient is shown thanking the player and leaving the shop. Store the art on the per-character prefab
(or optionally in `CustomerDefinition` for a data-driven pipeline), swap the sprite and animator controller
together, keep pivots consistent, and let `CustomerLayer` continue to own sorting.
