# BGM, Room Transition, and Door SFX Technical Design Report

## Purpose

This report defines the first audio implementation slice for **Cure Me, Please**. The scope is limited to three audio groups from the GDD:

1. BGM
2. Room Transition SFX
3. Door SFX

The goal is to add a clear audio foundation without disrupting the current gameplay systems, scene layout, or manually authored object positions.

## Current Project Context

The project already has several systems that can be used as audio trigger points:

| System | Current Role | Audio Opportunity |
| --- | --- | --- |
| `MenuAudioSettings` | Stores and applies Music/Game volume through `AudioMixer` exposed parameters. | BGM should use the existing `MusicVolume` mixer route. |
| `RoomTransition` | Handles blink-style transition between Counter Room and Treatment Room. | Transition SFX should play during close-eye, room switch, and optional open-eye timing. |
| `Door` | Switches open/closed visuals. | Door open/close SFX can be triggered directly from `Open()` and `Close()`. |
| `CustomerAgent` | Calls `door.Open()` and `door.Close()` during enter/exit flow. | Door SFX will automatically work for both entering and leaving customers. |
| `MainMenuController` | Loads game scene from Main Menu. | Main Menu BGM can start in the menu scene. |

## Design Goals

- Keep audio modular and easy to assign in the Unity Inspector.
- Reuse the existing mixer split:
  - `MusicVolume` for BGM.
  - `GameVolume` for SFX.
- Avoid changing scene-authored transforms, sprites, or room layout.
- Allow missing clips without breaking gameplay.
- Support quick placeholder setup now and polish later.
- Keep the implementation small enough to test in Unity immediately.

## Out of Scope

The following GDD audio groups are not part of this report:

- Tool SFX.
- Sanity heartbeat audio.
- Candle flame audio.
- Dialog blip upgrades.
- Patient expression voices.
- Footstep SFX.
- Parasite SFX.

Those should be handled in separate focused reports because they need different trigger logic and balancing.

## Audio Architecture

### Recommended Folder Layout

```text
Assets/Core/Script/Audio/
Assets/Core/Sound/BGM/
Assets/Core/Sound/SFX/Transition/
Assets/Core/Sound/SFX/Door/
```

### Mixer Routing

The project already uses:

```text
MusicVolume
GameVolume
```

Recommended mixer groups:

```text
Master
  Music
  Game
```

| Audio Type | Mixer Group | Volume Parameter |
| --- | --- | --- |
| BGM | Music | `MusicVolume` |
| Room Transition SFX | Game | `GameVolume` |
| Door SFX | Game | `GameVolume` |

## System 1: BGM

### Purpose

BGM gives each major scene a stable mood:

- Main Menu: quiet, ominous, lonely.
- Gameplay: tense clinic ambience with low fantasy horror pressure.

### Required Audio Assets

| Clip Name | Usage |
| --- | --- |
| `BGM_MainMenu` | Main Menu background music. |
| `BGM_Gameplay` | Gameplay scene background music. |

### Proposed Script

```text
BgmPlayer
```

### Responsibility

`BgmPlayer` owns one looping `AudioSource` and plays one selected track.

It should support:

- Play track on start.
- Looping playback.
- Optional fade in.
- Optional fade out / crossfade later.
- Inspector-assigned clips.
- Safe behavior when no clip is assigned.

### Suggested Inspector Fields

```text
Audio
- AudioSource musicSource
- AudioClip mainMenuClip
- AudioClip gameplayClip

Startup
- BgmTrack startTrack
- bool playOnStart

Fade
- float fadeDuration
- bool useUnscaledTime
```

### Suggested Enum

```csharp
public enum BgmTrack
{
    None,
    MainMenu,
    Gameplay
}
```

### Runtime Flow

```text
MainMenuScene starts
-> BgmPlayer.Start()
-> Play(MainMenu)
-> musicSource loops through Music mixer group

GameScene starts
-> BgmPlayer.Start()
-> Play(Gameplay)
-> musicSource loops through Music mixer group
```

### Implementation Notes

- `BgmPlayer` can exist per scene at first.
- A persistent global audio manager is not required for the first implementation slice.
- If seamless music across scene loads is needed later, `DontDestroyOnLoad` can be added after the basic scene-local version is stable.

## System 2: Room Transition SFX

### Purpose

Room transition SFX should make the blink-style transition feel physical and tense. The player should feel the room change even when the screen is black.

### Current Transition Flow

`RoomTransition` currently performs:

```text
Set fade to visible blocker
Fade 0 -> 1
Apply room state and camera position
Hold black
Fade 1 -> 0
Release blocker
Invoke completed callback
```

### Required Audio Assets

Minimum version:

| Clip Name | Usage |
| --- | --- |
| `SFX_Transition_ToTreatment` | Played when going from Counter Room to Treatment Room. |
| `SFX_Transition_ToCounter` | Played when returning from Treatment Room to Counter Room. |

Polish version:

| Clip Name | Usage |
| --- | --- |
| `SFX_Transition_CloseEye` | Played when fade starts closing. |
| `SFX_Transition_RoomSwitch` | Played while screen is fully black and room changes. |
| `SFX_Transition_OpenEye` | Optional soft open sound after fade opens. |

### Proposed Script

```text
RoomTransitionSfx
```

### Responsibility

`RoomTransitionSfx` plays one-shot clips at specific transition moments. It should not own room activation or camera movement.

### Suggested Inspector Fields

```text
Audio
- AudioSource sfxSource
- float volume

To Treatment
- AudioClip toTreatmentCloseClip
- AudioClip toTreatmentSwitchClip
- AudioClip toTreatmentOpenClip

To Counter
- AudioClip toCounterCloseClip
- AudioClip toCounterSwitchClip
- AudioClip toCounterOpenClip
```

### Required Hook Points

`RoomTransition` should expose events:

```csharp
public event Action<RoomTransitionDirection> TransitionStarted;
public event Action<RoomTransitionDirection> RoomSwitched;
public event Action<RoomTransitionDirection> TransitionFinished;
```

Suggested direction enum:

```csharp
public enum RoomTransitionDirection
{
    ToCounter,
    ToTreatment
}
```

### Runtime Flow

```text
ShowTreatmentRoom()
-> TransitionStarted(ToTreatment)
-> Fade closes
-> ApplyImmediate(treatment)
-> RoomSwitched(ToTreatment)
-> Fade opens
-> TransitionFinished(ToTreatment)

ShowCounterRoom()
-> TransitionStarted(ToCounter)
-> Fade closes
-> ApplyImmediate(counter)
-> RoomSwitched(ToCounter)
-> Fade opens
-> TransitionFinished(ToCounter)
```

### Why Use Events

Events keep `RoomTransition` focused on visual transition logic. Audio can be added, removed, or replaced without changing the room switching behavior.

This also makes future effects easier:

- Camera shake on room switch.
- Subtitle or warning text.
- Candle audio ducking.
- Treatment room ambience fade.

## System 3: Door SFX

### Purpose

Door audio reinforces the customer enter/exit loop. The player should hear the shop door open and close when patients arrive or leave.

### Current Door Flow

`CustomerAgent` already calls:

```csharp
door?.Open();
door?.Close();
```

This happens during:

- Customer entering the shop.
- Customer leaving the shop.

### Required Audio Assets

Minimum version:

| Clip Name | Usage |
| --- | --- |
| `SFX_Door_Open` | Played when the door opens. |
| `SFX_Door_Close` | Played when the door closes. |

Polish version:

| Clip Group | Usage |
| --- | --- |
| `SFX_Door_Open_01..03` | Random open variations. |
| `SFX_Door_Close_01..03` | Random close variations. |

### Proposed Script Change

Extend the existing:

```text
Door
```

### Suggested Inspector Fields

```text
Visual
- GameObject openVisual
- GameObject closedVisual
- bool startClosed

Audio
- AudioSource sfxSource
- AudioClip[] openClips
- AudioClip[] closeClips
- float volume
- Vector2 randomPitchRange
- bool playOnlyOnStateChanged
```

### Runtime Flow

```text
Customer reaches outside door while entering
-> door.Open()
-> open visual enabled
-> play open SFX

Customer crosses inside door point
-> door.Close()
-> closed visual enabled
-> play close SFX

Customer leaves after treatment
-> door.Open()
-> play open SFX

Customer exits outside
-> door.Close()
-> play close SFX
```

### Implementation Notes

- `playOnlyOnStateChanged` should default to `true`.
- If `Open()` is called while already open, no extra sound should play.
- If `Close()` is called while already closed, no extra sound should play.
- Missing `AudioSource` should not block visual changes.
- Missing clips should not log every frame or break gameplay.

## Recommended Implementation Plan

### Step 1: Create Audio Folder

Create:

```text
Assets/Core/Script/Audio/
```

Add:

```text
BgmTrack.cs
BgmPlayer.cs
RoomTransitionDirection.cs
RoomTransitionSfx.cs
```

### Step 2: Implement BGM

Add `BgmPlayer` with:

- Scene-local playback.
- `Play(BgmTrack track)`.
- Optional fade coroutine.
- `Stop()` or `FadeOut()` helper.

Test:

- Main Menu scene plays Main Menu BGM.
- Game scene plays Gameplay BGM.
- Music volume slider still controls playback through mixer routing.

### Step 3: Add Transition Events

Modify `RoomTransition` to expose:

```csharp
TransitionStarted
RoomSwitched
TransitionFinished
```

Event timing:

- `TransitionStarted`: before fade closes.
- `RoomSwitched`: immediately after `ApplyImmediate`.
- `TransitionFinished`: after fade opens and blocker is released.

### Step 4: Implement RoomTransitionSfx

Add `RoomTransitionSfx` to the same object as `RoomTransition`.

It should:

- Subscribe to `RoomTransition` events.
- Play direction-specific clips.
- Use `AudioSource.PlayOneShot`.
- Ignore missing clips.

Test:

- Counter -> Treatment plays the To Treatment clips.
- Treatment -> Counter plays the To Counter clips.
- Audio timing matches the blink transition.

### Step 5: Extend Door SFX

Modify `Door` to:

- Store open/close clip arrays.
- Play a random clip on state change.
- Apply optional random pitch.
- Use one-shot playback.

Test:

- Door open visual and sound trigger together.
- Door close visual and sound trigger together.
- Repeated `Open()` while already open does not spam sound.
- Repeated `Close()` while already closed does not spam sound.

## Unity Setup Guide

### Audio Mixer

Confirm exposed parameters exist:

```text
MusicVolume
GameVolume
```

Recommended groups:

```text
Master
  Music
  Game
```

### Main Menu Scene

1. Create an object named `BgmPlayer` or reuse an audio manager object.
2. Add `AudioSource`.
3. Route the AudioSource output to the `Music` mixer group.
4. Add `BgmPlayer`.
5. Assign `BGM_MainMenu`.
6. Set `Start Track = MainMenu`.
7. Enable `Play On Start`.

### Game Scene

1. Create an object named `BgmPlayer` or reuse an audio manager object.
2. Add `AudioSource`.
3. Route the AudioSource output to the `Music` mixer group.
4. Add `BgmPlayer`.
5. Assign `BGM_Gameplay`.
6. Set `Start Track = Gameplay`.
7. Enable `Play On Start`.

### Room Transition

1. Select the object with `RoomTransition`.
2. Add `AudioSource` or assign an existing SFX AudioSource.
3. Route the AudioSource output to the `Game` mixer group.
4. Add `RoomTransitionSfx`.
5. Assign `RoomTransition`.
6. Assign transition clips.

### Door

1. Select the `Door` object.
2. Add or assign an `AudioSource`.
3. Route the AudioSource output to the `Game` mixer group.
4. Assign open clips.
5. Assign close clips.
6. Keep `Play Only On State Changed` enabled.

## Test Checklist

### BGM

- Main Menu BGM starts when entering Main Menu.
- Gameplay BGM starts when entering Game Scene.
- BGM loops.
- Music slider affects BGM volume.
- Missing clip does not throw an error.

### Room Transition SFX

- Counter -> Treatment plays transition audio.
- Treatment -> Counter plays transition audio.
- Room switch sound happens while the screen is black.
- Game volume slider affects transition SFX.
- Transition still works if no clip is assigned.

### Door SFX

- Door open sound plays when the customer enters.
- Door close sound plays after the customer crosses the door.
- Door open sound plays when the customer leaves.
- Door close sound plays after the customer exits.
- Door sound does not repeat when the door is already in that state.
- Game volume slider affects door SFX.

## Risk Notes

| Risk | Mitigation |
| --- | --- |
| AudioSource missing in scene | Scripts should skip playback safely. |
| Door sound repeats too often | Use state-change guard. |
| Room transition SFX timing feels early/late | Use three event points instead of one hardcoded clip. |
| BGM restarts abruptly between scenes | Accept for first slice; add persistent cross-scene music later if needed. |
| Mixer parameter mismatch | Reuse existing `MusicVolume` and `GameVolume` names. |

## Recommended First Implementation Scope

The first implementation should include:

- `BgmPlayer` with scene-local looping playback.
- `RoomTransition` events.
- `RoomTransitionSfx` event listener.
- Door open/close one-shot SFX with random variation support.

Do not add a large global audio manager yet. The project is still changing quickly, and a simple scene-local approach is easier to test and adjust.

## Future Extensions

After this slice is stable, the next audio systems should be:

1. Tool SFX for Tongs, Knife, Needle, Magic, and Medicine.
2. Sanity heartbeat layers.
3. Candle flame intensity audio.
4. Footstep SFX during customer movement.
5. Patient expression voice sets.
6. Parasite grab, cut, and removal SFX.

## Summary

This audio slice should establish the project's core sound routing and event timing. BGM should reinforce scene mood, transition SFX should make the blink room switch feel intentional, and door SFX should strengthen the patient arrival/departure loop. The design intentionally stays small, inspector-driven, and compatible with the existing `MenuAudioSettings`, `RoomTransition`, `Door`, and `CustomerAgent` systems.
