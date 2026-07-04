# Room Lighting / Diegetic Darkness Technical Design Report

**Project:** Cure Me, Please  
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`  
**Engine:** Unity 6000.3.17f1, 2D Pixel Art  
**Render pipeline:** Universal Render Pipeline 17.3.0, 2D Renderer / Light2D  
**Report date:** July 4, 2026  
**Module:** Candle Readout / Room Lighting / Diegetic Darkness  
**Document status:** Technical design and implementation plan  

---

## 1. Purpose

The Room Lighting / Diegetic Darkness system makes the Magic Candle readable through the room itself.

Instead of relying only on a debug UI candle bar, the room should become darker as the candle loses light. When the candle is low, the player should understand the risk by looking at the environment:

- Global room brightness drops.
- Candle point light becomes weaker.
- A darkness overlay or vignette becomes stronger.
- The candle flickers when it is near failure.
- The room becomes nearly black when the candle is extinguished, but the player can still recover by refilling it at the counter.

The first pass should keep the current `BasicStatusHud` candle meter as a dev/debug readout. The final game can later hide the meter once the diegetic lighting is readable enough.

---

## 2. Current Project Context

### 2.1 Existing Candle System

`Assets/Core/Script/Candle/Candle.cs` already provides the runtime data this system needs:

| Candle API | Use |
|---|---|
| `CurrentLight` | Raw candle value. |
| `MaxLight` | Max candle value. |
| `NormalizedLight` | 0 to 1 value for lighting curves. |
| `CurrentState` | `Bright`, `Low`, `Flickering`, `Extinguished`, or `Refilling`. |
| `LightChanged(float current, float max)` | Event for light amount changes. |
| `StateChanged(CandleLightState state)` | Event for tier changes. |
| `Extinguished` | Event when the flame reaches 0. |
| `Relit` | Event when the candle recovers from 0. |

The lighting system should subscribe to these values. It should not create a parallel candle value.

### 2.2 Existing Lighting Objects

The current scene baseline already includes:

- `Global Light 2D`
- `Global Volume`
- `CandleGlow`
- `CandleGlow (1)`
- `Spot Light 2D`
- `Spot Light 2D (1)`
- Treatment room candle helper objects

The report design should preserve these authored objects. Implementation should add components and helper objects around them rather than resetting transforms, replacing sprites, or rerunning setup logic that disturbs the scene.

### 2.3 Existing Render Pipeline

The project uses URP:

```text
com.unity.render-pipelines.universal: 17.3.0
```

Scenes already contain serialized URP 2D `Light2D` components:

```text
UnityEngine.Rendering.Universal.Light2D
```

This means the first implementation can use `Light2D` directly.

---

## 3. Design Goals

1. Use `Candle.NormalizedLight` as the single lighting input.
2. Make candle state readable without opening a UI panel.
3. Keep the current HUD candle meter during development.
4. Support both Counter Room and Treatment Room.
5. Support `Global Light 2D`, candle point lights, and optional darkness overlay.
6. Add flicker only when the candle is in `Flickering` state.
7. Keep scene edits additive and preserve current authored room objects.
8. Keep the system data-driven enough to tune in Inspector.

---

## 4. Non-Goals For The First Version

Do not implement these in the first pass:

- Candle audio layers.
- Low-light tool jitter.
- Sanity fail tips.
- Final vignette art.
- Full post-processing stack management.
- Per-patient lighting variants.
- New candle gameplay rules.
- Replacement of `BasicStatusHud`.

Those are future layers that should read the same candle state after the visual readout works.

---

## 5. Core System Design

### 5.1 Main Script

Add:

```text
Assets/Core/Script/Candle/RoomLightController.cs
```

Responsibilities:

- Reference the active `Candle`.
- Reference one or more URP 2D `Light2D` objects.
- Reference an optional darkness overlay.
- Subscribe to `Candle.LightChanged`.
- Subscribe to `Candle.StateChanged`.
- Convert candle normalized value to target lighting values.
- Smoothly interpolate current lighting toward target values.
- Add flicker when the candle is in `Flickering` state.

### 5.2 Optional Data Profile

The first version can tune values directly in `RoomLightController`.

If the values become too large, add:

```text
Assets/Core/Script/Candle/RoomLightProfile.cs
Assets/Core/Data/Candle/RoomLightProfile.asset
```

This is optional for the first pass.

---

## 6. Recommended Component Model

### 6.1 Scene Object Layout

Recommended additive object:

```text
RoomLightingRoot
|-- RoomLightController
|-- DarknessOverlay (optional UI Image or SpriteRenderer)
`-- Debug helpers if needed
```

The controller should reference existing lights:

```text
Global Light 2D
CandleGlow
CandleGlow (1)
Spot Light 2D
Spot Light 2D (1)
```

Do not move these lights unless the user explicitly asks.

### 6.2 Single Controller vs Per-Room Controllers

There are two valid setup styles.

#### Option A: One RoomLightController

One controller references all relevant lights in both room roots.

Pros:

- Simple.
- One place to tune candle readout.
- Good for first pass.

Cons:

- Counter Room and Treatment Room have less independent tuning.

#### Option B: One Controller Per Room

Use separate controllers:

```text
MainRoomLightController
TreatmentRoomLightController
```

Pros:

- Better room-specific mood.
- Treatment Room can be darker and more threatening.

Cons:

- More setup.
- More chances for references to drift.

#### Recommendation

Start with one controller that supports multiple light groups:

- Main room global / candle lights.
- Treatment room global / candle lights.
- Optional per-group multipliers.

This keeps the first pass simple while still supporting both rooms.

---

## 7. Light Mapping

Use `Candle.NormalizedLight` as the input.

Recommended starting values:

| Candle state | Candle range | Global intensity | Candle point intensity | Overlay alpha |
|---|---:|---:|---:|---:|
| Bright | 1.00 to 0.60 | 1.00 | 1.20 | 0.00 |
| Low | 0.60 to 0.25 | 0.65 | 0.80 | 0.25 |
| Flickering | 0.25 to 0.01 | 0.35 | 0.45 plus flicker | 0.55 |
| Extinguished | 0.00 | 0.10 | 0.00 | 0.80 |
| Refilling | any | Use current light value, optional warm pulse | Based on current light | Based on current light |

The controller should expose Inspector fields for these values.

### 7.1 Curves

Use `AnimationCurve` for smooth artistic control:

```csharp
[SerializeField] private AnimationCurve globalIntensityByLight;
[SerializeField] private AnimationCurve pointIntensityByLight;
[SerializeField] private AnimationCurve overlayAlphaByLight;
```

Default curve intent:

```text
NormalizedLight 1.00 -> bright
NormalizedLight 0.60 -> slightly dim
NormalizedLight 0.25 -> dangerous
NormalizedLight 0.00 -> nearly black
```

Curves are better than hard tiers because they avoid sudden visual jumps.

---

## 8. Flicker Design

Flicker only applies when:

```text
Candle.CurrentState == CandleLightState.Flickering
```

Optional subtle flicker can also apply during `Low`, but it should be much weaker.

Recommended flicker properties:

```csharp
[SerializeField] private bool enableFlicker = true;
[SerializeField] private float flickerAmplitude = 0.12f;
[SerializeField] private float flickerSpeed = 18f;
[SerializeField] private float flickerNoiseScale = 0.4f;
```

Flicker output:

```text
flicker = PerlinNoise(time * speed, noiseScale) - 0.5
pointIntensity += flicker * amplitude
overlayAlpha += small inverse flicker
```

Rules:

- Flicker should not make gameplay unreadable.
- Flicker should communicate "the candle is failing".
- Extinguished should not flicker; point light should be off.

---

## 9. Darkness Overlay

There are two practical overlay choices.

### 9.1 UI Image Overlay

Use a full-screen `Image` on the gameplay Canvas.

Pros:

- Easy setup.
- Always covers camera view.
- Easy alpha control.

Cons:

- Can cover UI unless sorting/canvas order is planned carefully.
- Needs to sit behind gameplay UI if UI should remain readable.

### 9.2 World Sprite Overlay

Use a large black `SpriteRenderer` in front of room art but behind UI.

Pros:

- Does not affect UI.
- Easier to fit the room.

Cons:

- Needs sizing per camera / room.
- Can conflict with sorting layers.

### Recommendation

For first pass, use a UI Image overlay placed behind Dialog / Result / MiniGame UI but above the world camera render.

If UI readability becomes a problem, move to a world sprite overlay or split world darkness from UI darkness.

---

## 10. Room And State Rules

### 10.1 Counter Room

Counter Room should show candle state clearly because the player can refill there.

Suggested behavior:

- Candle point light should be warm and readable.
- Darkness should be slightly softer than Treatment Room.
- Refill can add a warm pulse or increased candle glow.

### 10.2 Treatment Room

Treatment Room should feel more threatening because the player cannot directly refill while inside an active treatment step.

Suggested behavior:

- Global light can be darker than Counter Room.
- Flicker can be more obvious.
- Overlay can be stronger.
- Debug HUD can remain during development.

### 10.3 Refilling

When the candle is refilling:

- Use current light level as the base.
- Optionally add a short warm pulse to the point light.
- Do not hide the darkness instantly unless the candle is actually restored.

---

## 11. Integration With Existing Systems

### 11.1 Candle

The controller subscribes to:

```csharp
candle.LightChanged += HandleLightChanged;
candle.StateChanged += HandleStateChanged;
```

On enable:

- Subscribe.
- Read current `candle.NormalizedLight`.
- Apply initial target values.

On disable:

- Unsubscribe.

### 11.2 GameFlow

No direct GameFlow changes are required for the first pass.

`GameFlow` already tells Candle when the player is at the counter:

```text
GameFlow.SetCandleAtCounter(bool)
-> Candle.SetAtCounter(bool)
```

Lighting should read Candle only.

### 11.3 Sanity

No Sanity changes are required for this lighting pass.

Sanity already reads candle tiers for pressure. The lighting controller should not duplicate that logic.

### 11.4 Future Low-Light Tool Modifier

Future tool difficulty should read:

```text
Candle.CurrentState
Candle.NormalizedLight
```

It should not read room light intensity directly.

---

## 12. Implementation Plan

### Step 1: Add RoomLightController

Create:

```text
Assets/Core/Script/Candle/RoomLightController.cs
```

Required namespaces:

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
```

Serialized fields:

```csharp
[SerializeField] private Candle candle;
[SerializeField] private Light2D[] globalLights;
[SerializeField] private Light2D[] candlePointLights;
[SerializeField] private Graphic darknessOverlay;
[SerializeField] private AnimationCurve globalIntensityByLight;
[SerializeField] private AnimationCurve pointIntensityByLight;
[SerializeField] private AnimationCurve overlayAlphaByLight;
[SerializeField] private float smoothingSpeed = 8f;
[SerializeField] private bool enableFlicker = true;
```

### Step 2: Subscribe To Candle Events

Implement:

```text
OnEnable
-> Resolve candle if missing
-> Subscribe events
-> Refresh target values

OnDisable
-> Unsubscribe events
```

### Step 3: Calculate Target Values

Use:

```text
normalized = candle.NormalizedLight
targetGlobalIntensity = globalCurve.Evaluate(normalized)
targetPointIntensity = pointCurve.Evaluate(normalized)
targetOverlayAlpha = overlayCurve.Evaluate(normalized)
```

### Step 4: Smooth Runtime Values

Use `Update()` for visual smoothing only:

```text
current = Lerp(current, target, deltaTime * smoothingSpeed)
```

This is acceptable because lighting is visual. Candle gameplay logic still stays in `Candle.Tick`.

### Step 5: Add Flicker

If state is `Flickering`:

```text
pointIntensity += flickerOffset
overlayAlpha += small inverse flicker
```

Clamp final values.

### Step 6: Scene Setup

Add `RoomLightController` to a helper object.

Assign:

- Candle.
- Existing `Global Light 2D`.
- Existing candle point lights / glow lights.
- Optional darkness overlay.

Keep object transforms and sprites unchanged.

### Step 7: Test

Test:

1. Candle drains from Bright to Low.
2. Room visibly dims.
3. Candle drains to Flickering.
4. Candle point light flickers.
5. Candle reaches Extinguished.
6. Room becomes near-black but not unusable.
7. Refill candle at counter.
8. Room brightens smoothly.
9. Treatment room receives the same candle readout.

---

## 13. Setup Checklist

1. Create `RoomLightingRoot` or use an existing manager object.
2. Add `RoomLightController`.
3. Assign the scene `Candle`.
4. Assign `Global Light 2D`.
5. Assign candle glow / point lights:
   - `CandleGlow`
   - `CandleGlow (1)`
   - any current room candle point lights
6. Create a darkness overlay if needed.
7. Assign the overlay to `RoomLightController`.
8. Tune curves and intensities.
9. Keep `BasicStatusHud` enabled for debug validation.
10. Enter Play Mode and compare visual darkness against the candle HUD value.

---

## 14. Recommended Starting Values

### Global intensity curve

```text
0.00 -> 0.10
0.25 -> 0.35
0.60 -> 0.65
1.00 -> 1.00
```

### Candle point intensity curve

```text
0.00 -> 0.00
0.25 -> 0.45
0.60 -> 0.80
1.00 -> 1.20
```

### Overlay alpha curve

```text
0.00 -> 0.80
0.25 -> 0.55
0.60 -> 0.25
1.00 -> 0.00
```

### Flicker

```text
flickerAmplitude = 0.12
flickerSpeed = 18
overlayFlickerAmount = 0.05
smoothingSpeed = 8
```

These are first-pass values only. Final values should be tuned in Play Mode with the actual room art.

---

## 15. Definition Of Done

The first pass is complete when:

- A `RoomLightController` reads the current Candle.
- Room brightness follows `Candle.NormalizedLight`.
- Candle point lights weaken as candle light drops.
- Darkness overlay alpha increases as candle light drops.
- Flickering state adds visible candle flicker.
- Extinguished state turns off candle point light or reduces it to near zero.
- Refilling the candle brightens the room again.
- Counter Room and Treatment Room both communicate candle state.
- Current debug HUD can remain active.
- No existing scene object positions, sprites, or room roots are reset.

---

## 16. Future Extensions

### 16.1 Candle Audio Layer

Add:

```text
CandleAudioController
```

It should subscribe to `Candle.StateChanged` and play:

- Bright loop.
- Low loop.
- Flickering loop.
- Extinguished hit.
- Relit hit.
- Refill loop.

### 16.2 Low-Light Tool Modifier

Add:

```text
LowLightToolModifier
```

It should read `Candle.NormalizedLight` and apply:

- Tool drift.
- Cursor jitter.
- Reduced control precision.

### 16.3 Post-Processing Integration

If needed, connect `Global Volume` values:

- Vignette intensity.
- Color adjustments.
- Exposure.
- Saturation.

This should be a later pass after Light2D and overlay are readable.

### 16.4 Room-Specific Profiles

If Counter Room and Treatment Room need different values, add:

```text
RoomLightProfile
```

Then each room can use its own curve set.

---

## 17. Risks

| Risk | Mitigation |
|---|---|
| Darkness looks like static art instead of gameplay feedback | Make dimming visibly change over time and add flicker near failure. |
| UI becomes hard to read | Keep overlay behind gameplay UI or use a world overlay instead. |
| Treatment room becomes too dark to play | Clamp minimum global intensity above full black. |
| Light changes feel jumpy | Use curves and smoothing. |
| Current scene lighting gets overwritten | Add controller references to existing objects; do not reset transforms or sprites. |
| Flicker is uncomfortable | Keep flicker amplitude small and expose Inspector tuning. |

---

## 18. Recommended First Implementation Scope

Implement now:

- `RoomLightController`
- Light2D intensity mapping.
- Optional UI darkness overlay mapping.
- Flicker during `CandleLightState.Flickering`.
- Setup references in the active gameplay scene.

Do later:

- Candle audio.
- Tool difficulty from darkness.
- Post-processing volume control.
- Room-specific profiles.
- Removal of debug candle HUD.

---

## 19. Summary

The Room Lighting / Diegetic Darkness system should turn the existing Candle value into a visible environmental readout. The Candle already exposes `NormalizedLight`, `CurrentState`, and events, and the project already contains URP 2D lights. The first implementation should add a `RoomLightController` that maps candle light to Global Light 2D intensity, candle point light intensity, and darkness overlay alpha. Flickering should only occur near failure. This makes the candle readable through the world and prepares the project for later Candle audio and low-light tool difficulty systems.
