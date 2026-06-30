# Tongs Mini Game - Project Integrated Technical Design Report

**Project:** Cure Me, Please  
**Module:** Treatment Mini Game - Tongs / Parasite Extraction  
**Engine:** Unity 6000.3.17f1 (Unity 6.3 LTS), 2D, World-space GameObjects  
**Input:** Input System Package (New) — Active Input Handling set to *Input System Package (New)* only  
**Document status:** Adjusted to current project systems — World-space implementation  
**Last updated:** June 30, 2026

> **Implementation space:** The Tongs mini game is built with **World-space GameObjects** (`SpriteRenderer` + `Collider2D`), not UI Canvas elements. The patient body, parasites, and wound channel all live in the scene as world objects. Only secondary readouts (pain meter, progress) may remain on a Canvas overlay. All distances in this document are expressed in **world units** unless explicitly marked as screen pixels.

> **Input System notice (Unity 6.3):** This project uses the **new Input System package only**, so the legacy `Input` class (`Input.mousePosition`, `Input.GetMouseButtonDown`) is **unavailable and will not compile**. All input in this document uses `UnityEngine.InputSystem` — `Mouse.current.position.ReadValue()` for position and `Mouse.current.leftButton` for clicks. Every script that reads input must add `using UnityEngine.InputSystem;`.

---

## 1. Objective

This report defines the Tongs mini game in a way that fits the systems already implemented in the project.

The player uses tongs to pull parasite fragments out of a patient during the Treatment Room sequence. The mini game should create pressure through three layers:

- Extraction progress: pull the parasite out far enough to remove it.
- Pain: rises during a pull and forces the player to pause.
- Sanity: global patient risk that increases from mistakes and existing case pressure.

The design must integrate with the current project instead of creating duplicate systems.

---

## 2. Current Project Systems To Reuse

### 2.1 Existing Treatment Flow

Current relevant scripts:

```text
Assets/Core/Script/Treatment/Treatment.cs
Assets/Core/Script/Treatment/RoomTransition.cs
Assets/Core/Script/GameFlow/GameFlow.cs
Assets/Core/Script/Customer/Sanity.cs
Assets/Core/Script/Candle/Candle.cs
Assets/Core/Script/UI/BasicStatusHud.cs
```

Current flow:

1. Customer enters and reaches the counter.
2. Dialog starts from the interaction bubble.
3. Dialog completes.
4. `GameFlow.CompleteDialog()` starts Treatment.
5. `Treatment.Begin(CustomerAgent customer)` switches to the Treatment Room through `RoomTransition`.
6. Current Treatment UI is placeholder text/buttons.
7. `Treatment.CompletePlaceholderTreatment()` calls `GameFlow.CompleteTreatment(activeCustomer)`.

The Tongs mini game should replace or sit inside the placeholder Treatment content, not replace the whole flow.

### 2.2 Existing Sanity System

The project already has `Sanity`; do not create a separate `SanityManager`.

Use the current API:

```csharp
Sanity.AddSanity(float amount);
Sanity.SetTreatmentStress(bool active);
Sanity.ResetSanity(float value = 0f);
Sanity.BeginMonitoring();
Sanity.StopMonitoring();
```

Mapping from the mini game design:

| Report concept | Current project API |
|---|---|
| `SanityManager.Spike(amount)` | `sanity.AddSanity(amount)` |
| `SanityManager.AddContinuous(perSec)` | `sanity.AddSanity(perSec * Time.deltaTime)` |
| Pain full spike | `sanity.AddSanity(painSpikeAmount)` |
| Edge contact penalty | `sanity.AddSanity(edgePenaltyPerSecond * Time.deltaTime)` |
| Treatment pressure | `GameFlow.SetTreatmentStress(true)` / `false` |

### 2.3 Existing Candle System

`Candle` already drains over time and affects `Sanity` through the current `Sanity.Tick()` logic.

The mini game does not need to own candle behavior. It only needs to respect the existing rule:

- Treatment happens in a separate room.
- Candle can continue draining while hidden.
- If candle is out, `Sanity` rises faster through the existing `Sanity` system.

### 2.4 Existing Scene Baseline

Current authored room layout is protected by:

```text
Assets/Core/Report/SceneBaseline_CurrentLayout.md
```

Tongs implementation should add new objects under current Treatment UI/room roots. It should not reposition existing `Main RoomRoot`, `Treatment RoomRoot`, `Customer`, `Counter`, `Candle`, door visuals, or waypoint objects.

---

## 3. Gameplay Flow

### 3.1 Full Treatment Flow With Tongs

1. `Treatment.Begin(customer)` opens the Treatment Room.
2. The Treatment Room shows the current wound/body area as a **world-space** sprite.
3. Player selects the Tongs tool.
4. Parasites appear as **world-space objects** at predefined wound anchors.
5. Player clicks and holds a parasite.
6. Player pulls upward to extract it.
7. Pain rises while holding.
8. Player can release to let pain drain.
9. If pain fills, Sanity receives a spike.
10. If parasite touches wound edge, Sanity rises continuously.
11. When a parasite reaches full pull progress, it is removed.
12. When all required parasites are removed, Treatment can complete.
13. `GameFlow.CompleteTreatment(activeCustomer)` handles cure, Sanity reset, and customer exit.

### 3.2 First Playable Vertical Slice

The first implementation should be smaller than the full report:

1. One Treatment Room mini game root.
2. One Tongs tool state.
3. One `Small` parasite type.
4. Pull progress.
5. Pain bar.
6. Pain full -> `Sanity.AddSanity(painSpikeAmount)`.
7. Extract one parasite -> enable Complete Treatment.

Do not start with jitter, Big variant, random 3-7 parasite spawning, or heavy VFX. Those are Phase 2+ features.

---

## 4. Core Design Decisions

### 4.1 Parasite Length

Parasite length should be driven by `requiredDistance`, not sprite size.

Each parasite tracks:

```text
pullProgress: 0 to 1
requiredDistance: total drag distance needed to extract (world units)
```

This keeps gameplay tuning independent from art.

Example values (world units — assuming a typical 2D setup where ~1 unit reads as a noticeable on-screen pull; tune to your camera size):

| Variant | Required Distance | Behavior |
|---|---:|---|
| Small | 1.0 | Quick extraction |
| Long | 4.0 | Long extraction, more pain pressure |
| Big | 2.5 | Directional extraction with wider/shifted channel |

> These were originally drafted as screen pixels (100 / 400 / 250 px). In World-space they become world units scaled to your orthographic camera. Use the Section 8.3 Gizmos to confirm the on-screen pull distance feels right, then adjust.

### 4.2 Pain vs Sanity

Pain and Sanity must stay separate.

| Bar | Scope | Lifetime | Purpose |
|---|---|---|---|
| Pain | Local parasite pull | Resets per parasite or per pull context | Controls short rhythm |
| Sanity | Whole patient/case | Persists through the customer case | Fail condition |

Pain should not continuously drain Sanity. Instead:

- Pain rises while the player holds/pulls.
- Pain drains when the player releases.
- If Pain reaches full, it applies one Sanity spike.
- Edge contact applies smaller continuous Sanity damage.

This fits the current game because `Sanity` already tracks the long-term patient risk.

---

## 5. Recommended Script Architecture

### 5.1 Folder

Create the mini game scripts under:

```text
Assets/Core/Script/Treatment/Tongs
```

Recommended first scripts:

```text
ParasiteType.cs
Parasite.cs
TongsMiniGame.cs
TongsTool.cs
PainMeter.cs
TreatmentSanityBridge.cs
```

The bridge can stay small. It exists only to keep mini game code from directly knowing too much about `GameFlow`.

### 5.2 ParasiteType

Use a ScriptableObject for tuning.

```csharp
[CreateAssetMenu(fileName = "ParasiteType", menuName = "Pixel Forge/Treatment/Parasite Type")]
public sealed class ParasiteType : ScriptableObject
{
    public enum Variant
    {
        Small,
        Long,
        Big
    }

    [SerializeField] private Variant variant;
    [SerializeField] private float requiredDistance = 1f;     // world units
    [SerializeField] private float pullSpeed = 1f;
    [SerializeField] private bool needsDirection;
    [SerializeField] private float painPerSecond = 0.25f;
    [SerializeField] private float painDrainRate = 0.4f;
    [SerializeField] private float painSpikeAmount = 25f;
    [SerializeField] private float edgePenaltyPerSecond = 8f;
    [SerializeField] private float channelHalfWidth = 0.4f;   // world units
    [SerializeField] private float jitterStrength = 0f;       // world units
    [SerializeField] private float jitterFrequency = 1.5f;
}
```

Notes:

- Distances (`requiredDistance`, `channelHalfWidth`, `jitterStrength`) are in **world units**, measured along/across the pull direction in the scene.
- Start with `jitterStrength = 0` for the first playable version.
- Add read-only public properties instead of public fields if following the style of newer scripts.

### 5.3 Parasite

Responsibilities:

- Own one parasite instance.
- Read its `ParasiteType`.
- Track `pullProgress`.
- Track local `painLevel`.
- Report pain/edge/success events to `TongsMiniGame`.

Recommended events:

```csharp
event Action<Parasite> Extracted;
event Action<float> PainChanged;
event Action<float> SanitySpikeRequested;
event Action<float> ContinuousSanityRequested;
```

First pass can be simpler:

```csharp
public void BeginHold(Vector2 worldPointer);   // mouse converted to world space
public void EndHold();
public void TickPull(Vector2 worldPointer, float deltaTime);
```

Each parasite is a World-space GameObject carrying a `SpriteRenderer` and a `Collider2D` (used only for click selection — not for edge detection). The parasite tip position is read from its `Transform`, not from screen coordinates.

### 5.4 TongsMiniGame

Responsibilities:

- Own the current mini game state.
- Start/stop the mini game.
- Connect active `CustomerAgent` to `Sanity`.
- Track remaining parasites.
- Call `GameFlow.SetTreatmentStress(true)` while the player is actively treating.
- Call `GameFlow.SetTreatmentStress(false)` when returning to counter or completing treatment.
- Notify `Treatment` when the mini game is complete.

Recommended references:

```csharp
[SerializeField] private GameFlow flow;
[SerializeField] private Treatment treatment;
[SerializeField] private Transform parasiteRoot;     // world-space root in the Treatment Room
[SerializeField] private List<Parasite> parasites;
[SerializeField] private Camera treatmentCamera;     // for ScreenToWorldPoint conversion
[SerializeField] private Slider painSlider;          // pain readout may stay on a Canvas overlay
```

`parasiteRoot` is a World-space transform placed in the Treatment Room (under the existing room root). Parasites are spawned as world objects under it. `treatmentCamera` is used to convert the mouse position to world space each frame, using the new Input System:

```csharp
using UnityEngine.InputSystem;

Vector2 screenPos   = Mouse.current.position.ReadValue();
Vector3 worldPointer = treatmentCamera.ScreenToWorldPoint(screenPos);
```

Do not hardcode scene positions. Reference the existing Treatment Room root and add new world-space children under it. The pain meter and progress readout can remain as a small Canvas overlay since they are pure UI.

### 5.5 TreatmentSanityBridge

This can be a small helper if direct access becomes messy.

Responsibilities:

- Find `Sanity` from the active customer.
- Apply spike/continuous values.
- Avoid creating a second sanity meter.

Pseudo-code:

```csharp
public sealed class TreatmentSanityBridge
{
    private Sanity activeSanity;

    public void SetCustomer(CustomerAgent customer)
    {
        activeSanity = customer != null ? customer.GetComponent<Sanity>() : null;
    }

    public void AddSpike(float amount)
    {
        activeSanity?.AddSanity(amount);
    }

    public void AddContinuous(float perSecond, float deltaTime)
    {
        activeSanity?.AddSanity(perSecond * deltaTime);
    }
}
```

This may be implemented as a component or folded into `TongsMiniGame` if the first pass is small.

---

## 6. Input Model

### 6.1 First Version

Use the **new Input System** mouse API (`UnityEngine.InputSystem`), converted to world space. The legacy `Input.GetMouseButtonDown` / `Input.mousePosition` will not compile in this project.

- Click down (`Mouse.current.leftButton.wasPressedThisFrame`): convert the mouse to world space and use `Physics2D.OverlapPoint` (or `Physics2D.Raycast`) against the parasite's `Collider2D` to select and hold it.
- Hold (`Mouse.current.leftButton.isPressed`): keep pulling upward in world space to increase pull progress.
- Release (`Mouse.current.leftButton.wasReleasedThisFrame`): stop holding and drain pain.

For the first version:

- Ignore Tongs tool selection until core pull is proven.
- Or use a simple `bool tongsEquipped` toggle from a UI button.

Full first-pass input example:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class TongsInput : MonoBehaviour
{
    [SerializeField] private Camera treatmentCamera;
    private Parasite held;

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;   // no mouse device present

        Vector2 screenPos = mouse.position.ReadValue();
        Vector2 worldPointer = treatmentCamera.ScreenToWorldPoint(screenPos);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPointer);
            if (hit != null && hit.TryGetComponent(out Parasite parasite))
            {
                held = parasite;
                held.BeginHold(worldPointer);
            }
        }
        else if (mouse.leftButton.isPressed && held != null)
        {
            held.TickPull(worldPointer, Time.deltaTime);
        }
        else if (mouse.leftButton.wasReleasedThisFrame && held != null)
        {
            held.EndHold();
            held = null;
        }
    }
}
```

### 6.2 Pull Progress

Use the world-space pointer delta while held (Y axis = pull-up direction):

```text
upwardDelta = max(0, currentWorldPointerY - previousWorldPointerY)
pullProgress += upwardDelta * pullSpeed / requiredDistance
```

Because both the delta and `requiredDistance` are now in world units, the ratio stays consistent regardless of screen resolution.

Clamp:

```text
pullProgress = 0 to 1
```

Extraction succeeds at:

```text
pullProgress >= 1
```

### 6.3 Big Variant Later

For Big parasite:

- `requiredDirection = -1` or `+1`.
- Pull progress only advances if lateral input matches required direction.
- Wound channel can shift toward the required side.

Do not implement Big until Small and Long feel good.

### 6.4 Input System Setup Notes (Unity 6.3)

Because this project uses the new Input System only, confirm the following before writing input code:

- The **Input System package** is installed (`com.unity.inputsystem`, v1.19.0 ships with Unity 6000.3) and **Active Input Handling** is set to *Input System Package (New)* in Project Settings → Player.
- Add `using UnityEngine.InputSystem;` to every script that reads input.
- The polling approach above (`Mouse.current.*`) needs **no** EventSystem, Input Action asset, or PlayerInput component. It reads the device directly, which is the simplest path for a first slice.
- Always null-check `Mouse.current` — it is null if no mouse device is present.
- Note: legacy `MonoBehaviour` mouse callbacks like `OnMouseDown` do **not** fire under the new Input System. That is why selection uses an explicit `Physics2D.OverlapPoint` against the parasite collider instead.
- If the team later wants rebindable controls, migrate this polling code to an **Input Action asset** with a "Click" (Button) and "Point" (Vector2/Pointer position) action. Not needed for the first slice.

---

## 7. Pain And Sanity Integration

### 7.1 Pain

While held:

```text
pain += painPerSecond * deltaTime
```

While released:

```text
pain -= painDrainRate * deltaTime
```

Clamp:

```text
pain = 0 to 1
```

If pain reaches 1:

```text
Sanity.AddSanity(painSpikeAmount)
pain = 0 or partial reset
```

Recommendation:

- First pass: reset pain to 0 after spike.
- Later: reset to 0.35 if the spike should remain stressful.

### 7.2 Sanity

Use existing `Sanity` component.

Do not create:

```text
SanityManager
```

Use:

```csharp
activeSanity.AddSanity(amount);
```

For edge contact:

```csharp
activeSanity.AddSanity(edgePenaltyPerSecond * Time.deltaTime);
```

### 7.3 Treatment Stress

Use existing `GameFlow.SetTreatmentStress`.

Recommended rule:

- `SetTreatmentStress(true)` while the mini game is active in Treatment Room.
- `SetTreatmentStress(false)` when the player returns to counter.
- `SetTreatmentStress(false)` when treatment completes.

This preserves the current Sanity model where treatment stress is added on top of candle pressure.

---

## 8. Edge And Jitter Design

Edge and jitter are Phase 2 or Phase 3 systems. All coordinates here are **world-space** values.

### 8.1 Edge Contact

Use lateral (world X) offset from the channel center line. `channelCenterX` is a world X coordinate in the scene:

```text
offset = abs(parasiteTip.position.x - channelCenterX)
edgeContact = offset > channelHalfWidth
```

If edge contact:

```csharp
activeSanity.AddSanity(edgePenaltyPerSecond * Time.deltaTime);
```

For the Big variant, shift `channelCenterX` toward the required direction (in world units) and/or widen `channelHalfWidth`.

### 8.2 Jitter

Do not move the OS cursor.

Instead:

- Add a Perlin-noise lateral offset (in world units) to the parasite tip's world position or its visual child transform.
- Let the player counter-steer with mouse movement.

```csharp
float noise = Mathf.PerlinNoise(Time.time * jitterFrequency, jitterSeed);
float jitterOffset = (noise - 0.5f) * 2f * jitterStrength;   // world units
// applied to the parasite tip's world X (or a visual offset transform)
```

First implementation should keep jitter disabled until core pulling is playable.

### 8.3 Debug Gizmos

Add gizmos after edge detection exists. Because the mini game is World-space, gizmos draw directly at scene world coordinates with no conversion — they line up exactly with the parasite and channel.

Show:

- Center line (at world X = `channelCenterX`).
- Left/right channel bounds (`channelCenterX ± channelHalfWidth`), green inside / red on contact.
- Jitter range band (`playerWorldX ± jitterStrength`) in yellow.
- Current parasite tip as a small sphere at its world position.

```csharp
void OnDrawGizmos()
{
    Vector3 basePos = transform.position;       // world-space, no conversion needed
    float topY    = basePos.y;
    float bottomY = topY + gizmoHeight;         // along the pull (world units)

    bool overEdge = Mathf.Abs(currentParasiteX - channelCenterX) > channelHalfWidth;
    Gizmos.color = overEdge ? Color.red : Color.green;
    float l = channelCenterX - channelHalfWidth;
    float r = channelCenterX + channelHalfWidth;
    Gizmos.DrawLine(new Vector3(l, topY), new Vector3(l, bottomY));
    Gizmos.DrawLine(new Vector3(r, topY), new Vector3(r, bottomY));

    Gizmos.color = Color.cyan;
    Gizmos.DrawLine(new Vector3(channelCenterX, topY), new Vector3(channelCenterX, bottomY));

    Gizmos.color = Color.yellow;
    float jl = currentPlayerX - jitterStrength;
    float jr = currentPlayerX + jitterStrength;
    Gizmos.DrawLine(new Vector3(jl, topY), new Vector3(jl, bottomY));
    Gizmos.DrawLine(new Vector3(jr, topY), new Vector3(jr, bottomY));

    Gizmos.color = Color.white;
    Gizmos.DrawSphere(new Vector3(currentParasiteX, topY, 0f), gizmoTipRadius);
}
```

This is useful for balancing and should not affect runtime builds.

---

## 9. UI Integration

### 9.1 Existing UI To Keep

Do not replace the current `BasicStatusHud`.

The mini game itself (patient body, parasites, wound channel) lives in **World-space** under the Treatment Room root. Only the lightweight readouts stay as a Canvas overlay:

- Pain meter (Canvas `Slider`).
- Pull progress indicator (Canvas).
- Optional tool selected indicator (Canvas).
- Optional parasite remaining count (Canvas).

So there are two layers: a world-space gameplay layer (sprites + colliders) and a thin UI overlay for meters. Keep them separate.

### 9.2 Treatment Placeholder Replacement

Current `Treatment` text says the minigame is not designed yet. Once Tongs starts:

- Keep `ReturnCounterButton`.
- Keep `ResumeTreatmentButton`.
- Replace placeholder body text by activating the world-space mini game root.
- Keep `CompleteTreatmentButton` hidden until all parasites are extracted.

Recommended minimal approach:

- Place the `TongsMiniGame` world-space root under the existing **Treatment Room root** (not the UI Canvas). Its meters can be a separate child Canvas.
- Let `Treatment.Begin(customer)` call `tongsMiniGame.Begin(customer)`.
- Let `TongsMiniGame` call back when complete.

---

## 10. Phased Work Plan Adjusted To Current Project

### Phase 1 - First Playable Tongs Slice

Goal:

Prove the mini game inside the current Treatment Room.

Tasks:

1. Confirm the Input System package is active (`using UnityEngine.InputSystem;` compiles; `Mouse.current` resolves). See Section 6.4.
2. Create `Assets/Core/Script/Treatment/Tongs`.
3. Add `ParasiteType`.
4. Add `Parasite` as a world-space GameObject (`SpriteRenderer` + `Collider2D` for click selection).
5. Add `TongsMiniGame` with a world-space `parasiteRoot` under the Treatment Room root, plus a `treatmentCamera` reference for `ScreenToWorldPoint`.
6. Place one parasite as a world object in the scene.
7. Read input via `Mouse.current` (new Input System), click it via `Physics2D.OverlapPoint` in world space, and pull upward to fill progress.
8. Pain rises while holding and drains while released.
9. Pain full calls `Sanity.AddSanity(painSpikeAmount)`.
10. Extracted parasite enables treatment completion.
11. Run `dotnet build`.

Validation:

- Treatment Room opens normally.
- The world-space Tongs mini game root appears in the scene (not as a Canvas element).
- Clicking the parasite in world space selects it; pulling changes progress.
- Pain meter (Canvas overlay) changes.
- Pain spike changes existing Sanity HUD.
- Completing extraction can complete treatment through existing `GameFlow`.

### Phase 2 - Multiple Parasites

Goal:

Turn one parasite into a basic case.

Tasks:

1. Add spawn anchors.
2. Add parasite list.
3. Support 3-7 parasites from data or serialized count.
4. Track remaining parasites.
5. Complete treatment only when all required parasites are removed.

Validation:

- Multiple parasites can be removed one by one.
- Pain resets per parasite.
- Sanity persists across the case.

### Phase 3 - Edge Contact

Goal:

Add mistake pressure.

Tasks:

1. Add channel center and half-width.
2. Detect edge contact from lateral offset.
3. Apply `Sanity.AddSanity(edgePenaltyPerSecond * Time.deltaTime)`.
4. Add simple feedback when touching edge.

Validation:

- Edge contact visibly/clearly punishes.
- Sanity rises continuously during contact.
- Player can avoid contact with careful control.

### Phase 4 - Long And Big Variants

Goal:

Add variant variety.

Tasks:

1. Add Long parasite values.
2. Add Big parasite direction.
3. Add Big visual hint for left/right pull.
4. Tune channel width for Big.

Validation:

- Small is quick.
- Long tests endurance.
- Big requires clear directional control.

### Phase 5 - Jitter And Polish

Goal:

Increase tension and presentation.

Tasks:

1. Add Perlin jitter.
2. Scale jitter with pain or Sanity.
3. Add parasite emerge visual.
4. Add SFX.
5. Add color/shake/blood feedback.
6. Add debug gizmos.

Validation:

- Jitter feels fair.
- Feedback communicates mistakes.
- No system fights the player's actual cursor.

---

## 11. Starting Balance Values

Distance parameters are in **world units** (converted from the earlier pixel draft by ÷100 as a starting ratio — re-tune to your camera).

| Parameter | Unit | Small | Long | Big |
|---|---|---:|---:|---:|
| `requiredDistance` | world units | 1.0 | 4.0 | 2.5 |
| `pullSpeed` | multiplier | 1.5 | 1.0 | 1.0 |
| `needsDirection` | bool | false | false | true |
| `painPerSecond` | per sec | 0.25 | 0.18 | 0.30 |
| `painDrainRate` | per sec | 0.40 | 0.40 | 0.40 |
| `painSpikeAmount` | sanity | 25 | 25 | 25 |
| `edgePenaltyPerSecond` | sanity/sec | 8 | 8 | 8 |
| `channelHalfWidth` | world units | 0.40 | 0.35 | 0.70 |
| `jitterStrength` | world units | 0 first, later 0.25 | 0 first, later 0.30 | 0 first, later 0.45 |
| `jitterFrequency` | — | 1.5 | 1.8 | 2.2 |

Important:

- World-unit values assume ~1 unit is a meaningful on-screen pull. Verify with the Section 8.3 gizmos against your actual camera size and adjust the ratio if pulls feel too short or too long.
- Start with jitter off.
- Confirm the basic pull rhythm first.
- Tune pain before adding edge and jitter.

---

## 12. Risks And Adjustments

### Risk: Legacy Input Will Not Compile

Because Active Input Handling is *Input System Package (New)* only, any copied snippet using `Input.mousePosition`, `Input.GetMouseButtonDown`, or `OnMouseDown` will fail to compile or silently never fire. Always use `Mouse.current` (Section 6.1 / 6.4) and add `using UnityEngine.InputSystem;`. If older project scripts still use legacy `Input`, they were either already migrated or the project would not be building — verify during Phase 1 task 1.

### Risk: Duplicate Sanity Logic

Do not create `SanityManager`.

Use the current `Sanity` component. This keeps HUD, transformation state, candle interaction, and GameFlow behavior unified.

### Risk: Treatment.cs Becomes Too Large

Do not put all mini game logic inside `Treatment.cs`.

`Treatment` should remain a room/flow presenter. `TongsMiniGame` should own mini game state.

### Risk: Jitter Feels Unfair

Delay jitter until core pull and pain are fun.

When adding jitter:

- Keep the OS cursor untouched.
- Move parasite visual/tip offset only.
- Use debug gizmos to tune the channel.

### Risk: Player Cannot See Candle In Treatment

This is intended. The current design says the player should finish the event in front of them before returning to counter.

Keep return-to-counter behavior through existing `Treatment.ReturnToCounter()`.

### Risk: Too Many Parasites Too Early

Start with one parasite. Add 3-7 random parasites only after the single parasite loop is stable.

---

## 13. Acceptance Criteria For First Implementation

The first implementation is complete when:

- `Treatment.Begin(customer)` can show the Tongs mini game.
- Input is read through the **new Input System** (`Mouse.current`), with no legacy `Input` calls.
- The mini game renders as **world-space GameObjects** (sprites + colliders), with only meters on a Canvas overlay.
- A parasite can be clicked in world space, held, and pulled upward.
- Pull progress reaches 1 and removes the parasite.
- Pain rises while holding.
- Pain drains while released.
- Pain full calls `Sanity.AddSanity`.
- Existing Sanity HUD updates.
- Treatment can complete through existing `GameFlow.CompleteTreatment`.
- Returning to counter still works.
- No existing room layout or sprite setup is reset.
- `dotnet build "Pixel-forge-game-jam.slnx"` passes.

---

## 14. Summary

The Tongs mini game should be implemented as a Treatment Room module that reuses the current project systems instead of introducing parallel managers.

This revision targets **Unity 6000.3.17f1** with a **World-space GameObject** implementation and the **new Input System** (`Mouse.current`): the patient body, parasites, and wound channel are scene objects (`SpriteRenderer` + `Collider2D`), mouse input is read via `UnityEngine.InputSystem` and converted with `Camera.ScreenToWorldPoint`, all distances are in world units, and only the pain/progress meters remain on a Canvas overlay. This makes physics, particles, lighting, and blood/horror VFX straightforward to add later, and lets the debug gizmos line up exactly with scene coordinates.

The most important adjustment from the earliest design is replacing the proposed `SanityManager` with the existing `Sanity` component and connecting mini game mistakes through `Sanity.AddSanity`. The second important adjustment is scope: build a small vertical slice first, then add multiple parasites, edge contact, Long/Big variants, jitter, and polish.

This approach keeps the current `GameFlow`, `Treatment`, `RoomTransition`, `Candle`, `Sanity`, and HUD systems intact while giving the project a clear path toward the first real treatment mini game.
