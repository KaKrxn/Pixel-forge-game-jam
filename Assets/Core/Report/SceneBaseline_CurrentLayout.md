# Current Game Snapshot - Scene Baseline and Implemented Systems

Snapshot date: 2026-06-30

This file records the current saved state of `Assets/Core/Scene/GameScene.unity` and the major implemented systems in `Assets/Core/Script`.

Use this document as the current baseline when making future Unity scene or gameplay changes. Existing hierarchy positions, sprite references, room roots, and authored object setup should be preserved unless the user explicitly asks to change them.

## Baseline Rule

- Do not overwrite the user's current room composition.
- Do not reset existing transform positions or sprite assignments.
- Do not rerun editor setup logic in a way that replaces manually arranged objects.
- Add new objects as children or separate helpers when more functionality is needed.
- If a required change affects an existing object below, explain the reason before changing it.

## Current Root-Level Scene State

| Object | Active | Current Notes |
|---|---:|---|
| `=== Manager ===` | 1 | Manager grouping object in the current scene. |
| `GameFlowManager` | 1 | Holds game flow references, dialog, room transition, treatment, candle, and first customer flow links. |
| `Main Camera` | 1 | Main camera at `{x: 0, y: 0, z: -10}` with Pixel Perfect Camera and CameraSway. |
| `DialogCanvas` | 1 | Main dialog/fade UI canvas. Contains inactive `DialogRoot` and inactive `BlinkFadeOverlay`. |
| `EventSystem` | 1 | UI event input. |
| `=== Canvas ===  ` | 1 | Current authored UI grouping object. |
| `=== Room === ` | 1 | Current authored room grouping object. |
| `Main RoomRoot` | 1 | Counter/main shop room. This is the visible starting room. |
| `Treatment RoomRoot` | 0 | Treatment room root. Inactive by default and shown through room transition. |
| `Global Light 2D` | 1 | Global 2D lighting. |
| `Global Volume` | 1 | Current post-processing/global volume object. |

## Core Room Roots

| Object | Active | Local Position | Preserve |
|---|---:|---|---|
| `Main RoomRoot` | 1 | `{x: 0, y: 0, z: 0}` | Yes |
| `Treatment RoomRoot` | 0 | `{x: 0, y: 0, z: 0}` | Yes |
| `Main Camera` | 1 | `{x: 0, y: 0, z: -10}` | Yes |
| `DialogCanvas` | 1 | `{x: 0, y: 0, z: 0}` | Yes |
| `GameFlowManager` | 1 | `{x: 0, y: 0, z: 0}` | Yes |

## Main Room Authored Layout

Current important main room objects that should not be repositioned or have sprites replaced without approval:

| Object | Active | Local Position | Sorting / Sprite Notes |
|---|---:|---|---|
| `Customer` | 1 | `{x: -5.9300003, y: -2, z: 0}` | Sorting layer `Customer`, order `-10`; has `CustomerAgent` and `CustomerLayer`. |
| `Door` | 1 | `{x: 4.51, y: -2, z: 0}` | Parent for door visuals. |
| `Door Open` | 0 | `{x: -12.0354, y: 1.9457, z: 0}` | Door open visual. Preserve sprite reference. |
| `Door Close` | 1 | `{x: -14.18, y: 1.966, z: 0}` | Door closed visual. Preserve sprite reference. |
| `Counter` | 1 | `{x: -3.66, y: -4.53, z: 0}` | Sorting layer id `23284193`, order `0`; preserve counter sprite. |
| `Candle` | 1 | `{x: 4.8, y: -4.37, z: 0}` | Sorting layer id `23284193`, order `1`; has `Candle` script. |
| `CandleFlame` | 1 | `{x: 4.798, y: -3.39, z: 0}` | Current flame helper/VFX. |
| `CandleGlow` | 1 | `{x: 4.804, y: -3.613, z: 0}` | Current glow helper/VFX. |
| `Bubble` | 0 | `{x: 0, y: 0, z: 0}` | Customer interaction bubble. |
| `Wall` | 1 | `{x: 0.3, y: 0.0125, z: 2.42}` | Room wall sprite object. |
| `Wall (1)` | 1 | `{x: 0.09, y: 0.0125, z: 2.42}` | Additional wall/current authored room object. |

## Treatment Room Current State

| Object | Active | Current Notes |
|---|---:|---|
| `Treatment RoomRoot` | 0 | Room root inactive by default. |
| `TreatmentRoot` | 0 | Treatment UI/state root. |
| `ReturnCounterButton` | 1 | Button used to return to counter room during treatment flow. |
| `CompleteTreatmentButton` | 1 | Current placeholder treatment completion button. |
| `ResumeTreatmentButton` | 1 | Current placeholder resume treatment button. |
| `TitleText` | 1 | Treatment title text. |
| `BodyText` | 1 | Treatment body/description text. |
| `Candle (2)` | 0 | Treatment room candle sprite helper, inactive in saved scene. |
| `CandleFlame (1)` | 0 | Treatment room flame helper, inactive in saved scene. |
| `CandleGlow (1)` | 0 | Treatment room glow helper, inactive in saved scene. |

## Waypoints and Customer Flow Points

| Object | Active | Local Position | Notes |
|---|---:|---|---|
| `Point Manager` | 1 | `{x: -11.84, y: -2.89, z: 0}` | Parent/manager for current points. |
| `Door Point` | 1 | `{x: 0, y: 0, z: 0}` | Door waypoint parent. |
| `OutsideDoorPoint` | 1 | `{x: 0.8500004, y: -0.75, z: 0}` | Outside door waypoint. |
| `InsideDoorPoint` | 1 | `{x: 2.2, y: -0.75, z: 0}` | Inside door waypoint. |
| `CounterPoint` | 1 | `{x: 10.9, y: -0.83, z: 0}` | Counter stop waypoint. |
| `Customer point` | 1 | `{x: 14.950001, y: 2.02, z: 0}` | Customer point parent. |
| `CustomerSpawnPoint` | 1 | `{x: 0.63, y: -1.02, z: 0}` | Spawn waypoint. |
| `CustomerExitPoint` | 1 | `{x: -0.03999996, y: -1.02, z: 0}` | Exit waypoint. |

## UI Snapshot

| UI Object | Active | Current Notes |
|---|---:|---|
| `DialogRoot` | 0 | Dialog panel root, hidden by default. |
| `SpeakerText` | 1 | TMP speaker name field. |
| `BodyText` | 1 | TMP dialog body/typewriter text field. |
| `NextButton` | 1 | Hidden/shown by dialog logic depending on typewriter completion. |
| `BlinkFadeOverlay` | 0 | Fade overlay for blink-style room transition. |
| `BasicStatusHud` | 1 | Current candle/sanity HUD root. |
| `CandleMeter` | 1 | Candle slider/meter. |
| `CandleLabel` | 1 | Current label text starts as `Candle: Bright`. |
| `SanityMeter` | 1 | Sanity slider/meter. |
| `SanityLabel` | 1 | Current sanity state label. |
| `BubbleButton` | 1 | Button under bubble UI. |

## Implemented Gameplay Systems

### Customer Flow

- `CustomerAgent` supports spawn, outside door, inside door, counter, and exit point movement.
- `CustomerLayer` switches customer sorting state for outside/inside visual flow.
- `Door` controls open/closed visuals.
- Current post-dialog behavior can send customer through flow depending on `GameFlow`.

### Dialog

- `DialogData` is a ScriptableObject with:
  - `playerDisplayName`
  - `customerDisplayName`
  - `customerBlipClip`
  - `customerBlipClips`
  - `List<DialogLine> lines`
- `Dialog` now supports:
  - TMP speaker names.
  - Typewriter reveal.
  - Next button hidden while text is typing.
  - Text blip SFX.
  - Customer blip variation list.
  - Customer clip randomized once per Customer line, then reused for every blip in that line.
  - Stop blip playback when the line completes.
- `DialogDataTest.asset` currently contains 20 English test lines:
  - 10 Customer lines.
  - 10 Player lines.
  - Customer name `Rinna`.

### Candle

- `Candle` has max/current light, drain, refill, low light, flickering, and drain-on-play fields.
- `CandleFlame` and `CandleGlow` exist as helper/VFX objects in the main room.
- Candle is part of the game loop and affects sanity pacing.

### Sanity

- `Sanity` has protected increase rate, candle-out increase rate, treatment stress rate, warning threshold, critical threshold, and monitoring state.
- Sanity is designed to reset after a cured customer and increase again for a new customer.

### Treatment and Room Transition

- `RoomTransition` controls:
  - Counter room root.
  - Treatment room root.
  - Target camera.
  - Fade canvas group.
  - Blink-style close/open timing.
- `Treatment` currently has placeholder treatment UI, complete button, return button, and resume button.
- Treatment room is intentionally separate so the player cannot directly watch the counter candle while treating.

### Camera and Parallax

- `Main Camera` has Pixel Perfect Camera.
- `CameraSway` exists on the main camera.
- `MouseParallax` and `ParallaxLayer` scripts exist for pixel-art-friendly parallax motion.

### Main Menu

Current Main Menu scripts exist under `Assets/Core/Script/MainMenu`:

- `MainMenuController`
- `MenuAudioSettings`
- `MenuButtonSfx`

These support play/settings/quit/credit flow, audio sliders through AudioMixer parameters, button hover/click sounds, hover scale, and random left/right tilt.

## Current Dialog Data Snapshot

`Assets/Core/Data/Dialog/DialogDataTest.asset`

| Field | Current Value |
|---|---|
| `playerDisplayName` | `Player` |
| `customerDisplayName` | `Rinna` |
| `customerBlipClip` | Assigned |
| `customerBlipClips` | Empty list in saved asset |
| `lines` | 20 total lines |

## Current Reports

Important planning/report files in `Assets/Core/Report` include:

- `SceneBaseline_CurrentLayout.md`
- `DialogSpeakerNameTypewriterSfx_Report.md`
- `MainMenuUIImplementation_Report.md`

## Future Work Note

When adding Sanity tuning, Candle refill interaction, treatment minigames, customer data, or additional room art, keep the existing authored objects stable and add new helper objects/components around them.

Recommended safe additions:

- Add new candle interaction helpers under the existing candle/VFX setup.
- Add new treatment minigame UI under treatment UI roots.
- Add new customer case data as assets instead of hardcoding it into scene objects.
- Add new room art under `Treatment RoomRoot` without moving or replacing `Main RoomRoot`.
- Extend dialog and customer data without resetting `DialogDataTest.asset` unless requested.
