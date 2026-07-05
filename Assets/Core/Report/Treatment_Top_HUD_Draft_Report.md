# Treatment Top HUD Draft Report

Date: 2026-07-05  
Branch: feature/addvisual

## Summary

Treatment Top HUD is a draft UI layer for the Treatment Room. It adds a top-screen patient information panel, treatment dialogue panel, sanity display, and a conditional character visual slot for the selected body-part treatment context.

This feature is intended to become the shared Treatment Room HUD for Anatomy, Knife, Tongs, and Needle flows.

## Implemented

- Added `TreatmentTopHud` runtime component.
- Added `TreatmentTopHudSetup` editor setup tool.
- Hooked `Treatment` into the HUD lifecycle.
- Added scene layout for `TreatmentTopHUD` in `Game_K`.
- Added a character visual panel that appears only after entering a body part / mini game.
- Fixed character visual sprite display so it supports both:
  - `TreatmentTopHud.Character Visual Sprite`
  - `CharacterVisualImage.Source Image`
- Pushed implementation and scene layout to `feature/addvisual`.

## Current Scene Layout

Scene: `Assets/Core/Scene/Game_K.unity`

Main objects:

- `TreatmentTopHUD`
- `PatientPanel`
- `TreatmentDialogPanel`
- `CharacterVisualPanel`
- `CharacterVisualImage`

Current behavior:

- HUD is hidden during counter/dialog setup.
- HUD is bound when entering Treatment Room.
- Character visual panel stays hidden at Anatomy level.
- Character visual panel appears after selecting a body part / entering a mini game.

## Related Files

- `Assets/Core/Script/UI/TreatmentTopHud.cs`
- `Assets/Core/Script/UI/Editor/TreatmentTopHudSetup.cs`
- `Assets/Core/Script/Treatment/Treatment.cs`
- `Assets/Core/Scene/Game_K.unity`

## Editor Tool

Menu path:

```text
Tools > Pixel Forge > Create Treatment Top HUD
```

The tool creates or repairs the HUD in the open scene. It targets the main `Canvas`, avoids utility canvases such as `MiniOverlayCanvas`, and binds the HUD reference back to `Treatment`.

## Feature Integration Plan

### Customer System

Current customer queue work already provides:

- `CustomerDefinition`
- `CustomerSpawner`
- `CustomerQueueBuilder`
- `CustomerCaseProvider`

Next step:

- Add customer-specific HUD visual data.
- Preferred data source is `CustomerDefinition` if the sprite belongs to the character identity.
- `TreatmentCaseData` can also hold case-specific treatment visuals if the sprite depends on the medical case.

Suggested future fields:

```csharp
Sprite treatmentHudSprite;
Sprite treatmentHudPortrait;
```

Recommended resolve order:

```text
CustomerDefinition treatment HUD sprite
TreatmentCaseData treatment HUD sprite
Manual TreatmentTopHud characterVisualSprite
Customer SpriteRenderer fallback
```

### Dialog System

Current support:

- `TreatmentTopHud` already reads `TreatmentCaseData.DialogData`.
- It can display patient name from `TreatmentCaseData.CustomerDisplayName` or `DialogData.CustomerDisplayName`.
- It can pull pain/sanity dialogue lines from `DialogData`.

Next step:

- Trigger treatment dialogue from actual mini game events:
  - pain mistake
  - high sanity
  - aggression burst
  - tool misuse
  - successful body-part completion

### Anatomy / Mini Game Flow

Current support:

- `Treatment` checks `AnatomyController.IsInsidePart`.
- Character visual slot appears when inside a selected body part or mini game.

Next step:

- If visual timing needs more precision, add an explicit event to `AnatomyController`, for example:

```csharp
event Action<BodyArea, TreatmentMiniGameType> BodyPartEntered;
event Action BodyPartExited;
```

This would be cleaner than polling `IsInsidePart` through `Treatment.RefreshText`.

### Patient Aggression

Future integration:

- During aggression burst, HUD can show an aggressive dialogue line or visual warning.
- The character visual panel can swap to an aggressive/stressed sprite.
- `PatientAggressionController.AggressionStarted` and `AggressionEnded` are good hooks.

### Dynamic Tool Tray

Related branch work includes hover tray / tool tray layout. HUD must avoid covering the tool tray.

Current layout places HUD at the top and leaves the tray area free. If the tray changes size or anchor, re-check overlap in `Game_K`.

## Not Included In This Work

These local dirty files were not part of the HUD commit and should be reviewed separately:

- `Assets/Core/Material/VFX/CutGuideLine_Dotted.mat`
- `Assets/K/Ref/KnifeSampleLesion_Bulge.prefab`
- `Assets/K/Ref/KnifeSampleLesion_Tumor.prefab`

They were intentionally not committed with the HUD layout because they are unrelated to the Treatment Top HUD.

## Next Recommended Tasks

1. Confirm HUD visibility in Unity:
   - Counter/dialog: hidden
   - Treatment Room / Anatomy: top HUD visible, character visual hidden
   - Inside body part / mini game: character visual visible
2. Add data-driven customer HUD sprite support.
3. Connect treatment dialogue triggers to mini game pain/mistake events.
4. Add aggression visual/dialog feedback.
5. Replace draft panel visuals with final art UI once available.
