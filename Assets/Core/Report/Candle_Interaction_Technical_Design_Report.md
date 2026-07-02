# Magic Candle — Interaction Technical Design & Implementation Report

**Project:** Cure Me, Please
**Module:** Obstacle / Pressure System — Magic Candle (light upkeep)
**Engine:** Unity 6000.3.17f1 (Unity 6.3 LTS), 2D
**Input:** Input System Package (New) — `UnityEngine.InputSystem`
**Document status:** Design specification, reconciled with the current project systems
**Last updated:** July 2, 2026

> **Project reconciliation note:** This module already exists as `Assets/Core/Script/Candle/Candle.cs`.
> **Extend that class — do NOT add a parallel `MagicCandle`.** The sketches below show the model; map
> them onto the existing `Candle`:
>
> | This report | Existing `Candle.cs` |
> |---|---|
> | `MagicCandle` (new class) | **`Candle`** (extend it) |
> | `lightLevel` (0–100) | `currentLight` (0–100), `NormalizedLight` |
> | `decayPerSecond` | `drainRate` |
> | `LightTier { Bright, Dim, Critical, Out }` | `CandleLightState { Bright, Low, Flickering, Extinguished, Refilling }` (already tiered) |
> | `OnTierChanged` | `StateChanged(CandleLightState)` event (+ `Extinguished` / `Relit`) |
> | Hold refill | `StartRefill()` / `StopRefill()` (continuous `refillRate`) or `AddLight(amount)` |
> | `SetAtCounter(bool)` | derive from `RoomTransition.IsInTreatmentRoom` (no `isAtCounter`/event exists) |
>
> Two hard corrections: (1) **Decay must NOT be driven by a self-contained `Update()`** — the candle
> is hidden during treatment, so `Update()` would stop. Decay already runs everywhere via
> `Candle.Tick(deltaTime)`, which `GameFlow.TickHiddenStatusSystems()` calls while the candle is
> inactive. Route all new logic through `Tick`, not `Update`. (2) The **click-refill with per-second
> cap is the only genuinely new mechanic** to add on top of `AddLight`. See Sections 3 and 6.
>
> **Readout decision (confirmed):** the diegetic room-darkness readout is the final target, but the
> existing `BasicStatusHud` candle bar + label stay for now as a **temporary dev/debug readout**
> (to be hidden/removed at final build). So "no bar" is the end goal, not the current state.

> **Note on scope:** The project already has a base Candle that ticks with `GameFlow` and touches `Sanity` (it continues ticking even while its GameObject is hidden). This document designs the **interaction and light-level model** on top of that base — the refill input, the tiered light level, the counter-only gating, and how other systems read the light — rather than re-inventing the tick plumbing.

---

## 1. Overview

The Magic Candle is an **ambient pressure system**, not a mini game. Its flame slowly decays over real play time. The player must periodically leave treatment, return to the counter, and refill it. If the flame gets low the room darkens, patient Sanity rises faster, and the game becomes harder to read — pushing the player to balance attention between treating patients and keeping the light alive. (Reference: the music-box upkeep pressure in FNAF, per the GDD.)

Unlike the Tongs / Knife / Needle mini games (which are discrete scenes the player enters), the candle runs continuously in the background the whole time, including while a mini game is active.

---

## 2. Confirmed Design Decisions

| Topic | Decision |
|---|---|
| System type | Ambient/background pressure, always running — **not** a mini game. |
| Flame decay | Continuous over time, and **keeps decaying even while inside a mini game** (this is the core pressure). |
| Decay rate | **Constant across all cases** (difficulty comes from case content, not from a faster candle). |
| Refill location | **Counter only.** The player must transition back to the counter to refill. |
| Refill — Hold | Slow gain, low effort. For casual players. |
| Refill — Click | Fast gain when spammed, higher effort. For players who want to clear the light quickly. **Capped per second** to defeat autoclickers. |
| Partial refill | Every tap helps immediately — no need to fill to full before it counts. |
| Flame out (0%) | **Recoverable** — the player can relight by refilling. Not a hard fail state. |
| Status readout | **Final target: no on-screen bar** — flame state read **diegetically** through room darkness (plus the GDD's 3-tier candle audio). **For now**, the existing `BasicStatusHud` candle bar + label stay as a temporary dev/debug readout, hidden/removed at final build. |
| Light level model | A single continuous value 0–100% exposed as shared state, broadcasting **tier changes** so other systems (Sanity, room lighting, future difficulty) react. |

---

## 3. Core Model

### 3.1 Light Level (Shared Value)

The candle owns one continuous value that everything else reads:

```csharp
public float lightLevel;   // 0..100, decays over time, refilled at the counter
```

This is the single source of truth. Sanity, room lighting, and (later) mini-game difficulty subscribe to it rather than each tracking their own copy.

### 3.2 Decay

```csharp
// Inside Candle.Tick(float deltaTime) — the existing entry point, NOT Update():
currentLight = Mathf.Clamp(currentLight - drainRate * deltaTime, 0f, maxLight);
```

Decay runs continuously, in every context (counter, operating room, Anatomy, mini game). This is what makes time spent treating a patient cost something: while the player focuses on a mini game, the light quietly drops.

> **Reconciliation — this is the critical one.** `Candle.Tick(deltaTime)` already does this, and
> `GameFlow.TickHiddenStatusSystems()` calls `Tick` while the candle GameObject is **inactive** (hidden
> during treatment). A self-contained `MonoBehaviour.Update()` would **stop** while the candle is hidden,
> which breaks the whole "decay during a mini game" pressure. Keep decay in `Tick`; do not move it to `Update`.

### 3.3 Tiers (Diegetic, No Bar)

When `lightLevel` crosses a threshold, the tier changes and the room's darkness + candle audio change with it. There is **no** meter — the player reads the state from how dark the room is.

```csharp
public enum LightTier { Bright, Dim, Critical, Out }
```

> **Reconciliation:** the existing `Candle` already has this concept as
> `CandleLightState { Bright, Low, Flickering, Extinguished, Refilling }`, broadcast via the existing
> `StateChanged(CandleLightState)` event (plus `Extinguished` / `Relit`). Reuse those rather than adding
> a second `LightTier` enum + `OnTierChanged` event — just re-tune the thresholds (`lowLightThreshold`,
> `flickeringThreshold`) to the 60% / 25% breakpoints below if you want a wider Dim band. Consumers
> subscribe to `StateChanged`.

| Tier | lightLevel | Room / Feedback | Sanity effect |
|---|---|---|---|
| Bright | 100–60% | Normal lighting | Normal Sanity decay |
| Dim | 60–25% | Room noticeably darker; candle audio tier 2 | Sanity rises somewhat faster |
| Critical | 25–0% | Very dark; vignette / subtle shake; candle audio tier 3 | Sanity rises much faster |
| Out | 0% | Near-black room | Sanity rises fastest |

Tier changes are broadcast via an event so consumers stay decoupled:

```csharp
public event System.Action<LightTier> OnTierChanged;
```

> Because the readout is the darkness itself, the darkness must read as *deteriorating*, not just as art-style ambience. See Risks (onboarding).

### 3.4 Refill (Counter Only)

```csharp
using UnityEngine.InputSystem;

// Only when the player is at the counter:
if (!isAtCounter) return;

Mouse mouse = Mouse.current;
if (mouse == null) return;

// Hold: slow, low-effort
if (mouse.leftButton.isPressed)
    lightLevel += refillPerSecondHold * Time.deltaTime;

// Click: fast when spammed, but capped per second
if (mouse.leftButton.wasPressedThisFrame && clicksThisSecond < clickCapPerSecond)
{
    lightLevel += refillPerClick;
    clicksThisSecond++;
}
// clicksThisSecond resets every 1 second
```

Both paths add to the same `lightLevel`. Partial refills count immediately. The click cap keeps spam-clicking meaningfully faster than holding without letting an autoclicker instantly max the flame.

### 3.5 Counter Gating

`isAtCounter` is driven by the existing `RoomTransition` state:

- At the counter → refill enabled.
- In the operating room / Anatomy / mini game → refill disabled, but **decay continues**.

This is the central trade-off: the longer the player stays in the operating room, the more the flame drops, and refilling means spending time away from treatment.

---

## 4. Component Structure

```
MagicCandle (extends the existing base candle)
├── lightLevel : float 0..100        // shared source of truth
├── decayPerSecond                   // constant
├── currentTier : LightTier
├── OnTierChanged (event)
├── refillPerSecondHold              // Hold profile
├── refillPerClick                   // Click profile
├── clickCapPerSecond                // anti-autoclicker
└── isAtCounter : bool               // from RoomTransition

Consumers (subscribe, decoupled)
├── Sanity            // adjusts decay rate by tier
├── RoomLighting      // maps lightLevel -> global light / vignette (the readout)
└── [future] MiniGameDifficulty  // e.g. stronger jitter in the dark
```

### 4.1 Sketch

> **Reconciliation:** this sketch is illustrative. In the project, fold these members into the existing
> `Candle` class instead of a new `MagicCandle`. Keep decay inside `Candle.Tick(deltaTime)` (called by
> `GameFlow` even while hidden) — the `void Update()` below is only a sketch convenience. The new pieces
> to actually add to `Candle` are: the click-refill counter + per-second cap, and the counter gating
> (`isAtCounter`). Tier/state, decay, and hold-refill already exist.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

// Illustrative only — merge into Candle.cs; run decay/refill through Candle.Tick, not Update.
public class MagicCandle : MonoBehaviour
{
    [SerializeField] private float decayPerSecond = 2.0f;
    [SerializeField] private float refillPerSecondHold = 8f;
    [SerializeField] private float refillPerClick = 3f;
    [SerializeField] private int   clickCapPerSecond = 8;

    [SerializeField] private float dimThreshold = 60f;
    [SerializeField] private float criticalThreshold = 25f;

    public float lightLevel { get; private set; } = 100f;
    public LightTier CurrentTier { get; private set; } = LightTier.Bright;
    public event System.Action<LightTier> OnTierChanged;

    private bool isAtCounter;
    private int  clicksThisSecond;
    private float clickWindowTimer;

    void Update()
    {
        // decay everywhere
        lightLevel = Mathf.Clamp(lightLevel - decayPerSecond * Time.deltaTime, 0f, 100f);

        HandleRefill();
        UpdateTier();
    }

    private void HandleRefill()
    {
        clickWindowTimer += Time.deltaTime;
        if (clickWindowTimer >= 1f) { clickWindowTimer = 0f; clicksThisSecond = 0; }

        if (!isAtCounter) return;
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.isPressed)
            lightLevel = Mathf.Min(100f, lightLevel + refillPerSecondHold * Time.deltaTime);

        if (mouse.leftButton.wasPressedThisFrame && clicksThisSecond < clickCapPerSecond)
        {
            lightLevel = Mathf.Min(100f, lightLevel + refillPerClick);
            clicksThisSecond++;
        }
    }

    private void UpdateTier()
    {
        LightTier t =
            lightLevel <= 0f               ? LightTier.Out :
            lightLevel < criticalThreshold ? LightTier.Critical :
            lightLevel < dimThreshold      ? LightTier.Dim :
                                             LightTier.Bright;

        if (t != CurrentTier)
        {
            CurrentTier = t;
            OnTierChanged?.Invoke(t);
        }
    }

    public void SetAtCounter(bool value) => isAtCounter = value;
}
```

---

## 5. Input Model (Unity 6.3, Input System)

- Refill reads `Mouse.current` (Hold = `isPressed`, Click = `wasPressedThisFrame`). Add `using UnityEngine.InputSystem;`.
- If the candle is a world object clicked directly, select it with `Physics2D.OverlapPoint`; `OnMouseDown` does not fire under the new Input System. If it is a UI button at the counter, ensure the EventSystem uses `InputSystemUIInputModule`.
- Null-check `Mouse.current`.

---

## 6. Integration With Existing Systems

- **Sanity:** the existing `Sanity` currently reads the candle **binary**: `candle.IsLit ? candleProtectedIncreaseRate : candleOutIncreaseRate` (two rates, not a 4-tier multiplier). To get the tiered pressure in the balance table, extend `Sanity` to read `Candle.CurrentState` (or `NormalizedLight`) and pick a rate per tier — subscribe to the existing `StateChanged` event. Still the same `Sanity` component; no parallel manager.
- **RoomTransition:** there is no `isAtCounter`/`SetAtCounter` yet. Derive it from `RoomTransition.IsInTreatmentRoom` (`isAtCounter = !IsInTreatmentRoom`), or have `Treatment` (which already tracks its own `isAtCounter` and drives `ShowCounterRoom`/`ShowTreatmentRoom`) tell the candle when to enable refill.
- **RoomLighting (the readout):** map `lightLevel` to global light intensity / vignette so darkness *is* the meter. Tie the GDD's 3-tier candle audio to `OnTierChanged`.
- **GameFlow:** the base candle already ticks with GameFlow even while hidden; keep that. This document only adds the interaction + tier layer on top.

---

## 7. Phased Work Plan

### Phase 1 — Light Level + Decay + Tiers
- These already exist on `Candle` (`currentLight`, `drainRate`, `CandleLightState`, `StateChanged`). Re-tune thresholds to the 60% / 25% breakpoints if needed; do not add a parallel class.
- Log state changes. Run `dotnet build "Pixel-forge-game-jam.slnx"`.

### Phase 2 — Refill At Counter
- `SetAtCounter` from `RoomTransition`.
- Hold refill + Click refill with per-second cap.
- Partial refills count immediately; relight from 0 works.

### Phase 3 — Diegetic Readout
- Map `lightLevel` → room darkness / vignette (no bar).
- Hook the 3-tier candle audio to `OnTierChanged`.

### Phase 4 — Sanity Coupling
- Scale Sanity decay by tier through the existing `Sanity` component.

### Phase 5 — Polish & Onboarding
- First-time cue so players learn darkness = candle (see Risks).
- Optional: reserve a `lightLevel` hook for future mini-game difficulty in the dark.

---

## 8. Starting Balance Values

| Parameter | Value | Notes |
|---|---|---|
| `decayPerSecond` | 2.0 | Constant; ~50s from full to empty if untouched. Tune to case length. |
| `refillPerSecondHold` | 8 | Hold ≈ 12.5s of holding to fully refill from empty. |
| `refillPerClick` | 3 | Per counted click. |
| `clickCapPerSecond` | 8 | Max counted clicks/sec → ≤ 24/sec via clicking. |
| `dimThreshold` | 60 | Bright → Dim. |
| `criticalThreshold` | 25 | Dim → Critical. |
| Sanity decay ×, Bright | 1.0 | Baseline. |
| Sanity decay ×, Dim | ~1.5 | Faster. |
| Sanity decay ×, Critical | ~2.5 | Much faster. |
| Sanity decay ×, Out | ~4.0 | Fastest. |

> Key balance question: `decayPerSecond` vs. the time a player spends in a mini game. Too fast and the player runs back constantly and can't play mini games; too slow and the candle is meaningless. Playtest the ratio first.

---

## 9. Risks & Notes

- **Decay runs during mini games (by design):** confirm the ratio is fair. If players feel forced to abandon mini games too often, lower `decayPerSecond` rather than pausing decay.
- **No bar → onboarding risk:** since the only readout is darkness, new players may read a dark room as art style, not as a failing candle. Add a first-time cue (e.g. the first time the flame reaches Dim, a one-off hint or a clear flicker) and make the darkening read as *deterioration*, not static ambience.
- **Click cap tuning:** `clickCapPerSecond` must keep clicking faster than holding (to reward effort) without letting an autoclicker trivialize it. Verify both feel right.
- **Constant decay across cases:** difficulty scales with case content (more parasites/pustules/lesions = more time in the operating room = more candle pressure), not with a faster candle. Keep it constant unless playtests say otherwise.
- **Recoverable flame-out:** 0% is not a fail; the room goes near-black and Sanity spikes, but the player can relight. Make sure relighting from 0 is readable and responsive so it feels recoverable, not broken.
- **Decouple via events:** keep Sanity/lighting/difficulty as subscribers to `lightLevel` / `OnTierChanged`, so future systems can react to the dark without editing the candle.

---

## 10. Summary

The Magic Candle is an always-on pressure system built around a single `lightLevel` (0–100%) that **decays continuously — including during mini games — at a constant rate**, and is refilled **only at the counter** via two input profiles: **Hold** (slow, low-effort) and **Click** (fast when spammed, capped per second to stop autoclickers). Partial refills count immediately and a fully extinguished flame is recoverable. Crucially, there is **no UI bar**: the flame's state is shown **diegetically** through room darkness across four tiers (Bright / Dim / Critical / Out), backed by the GDD's 3-tier candle audio, and the same `lightLevel` drives faster Sanity decay as the room darkens. Everything reads the candle through a shared value and an `OnTierChanged` event, keeping Sanity, room lighting, and future difficulty systems decoupled. Built for Unity 6000.3.17f1 with the new Input System, the plan moves light/decay/tiers → counter refill → diegetic darkness readout → Sanity coupling → polish/onboarding, layering on top of the existing candle tick rather than replacing it. The main open balance point is the decay-vs-mini-game-time ratio, and the main design risk is teaching players that darkness means the candle — both to be resolved in playtesting.
