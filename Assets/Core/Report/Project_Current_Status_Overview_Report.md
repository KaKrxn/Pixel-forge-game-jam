# Project Current Status Overview Report

**Project:** Cure Me, Please  
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`  
**Engine:** Unity 6000.3.17f1, 2D Pixel Art  
**Report date:** July 1, 2026  
**Report purpose:** Summarize the current project state, implemented systems, remaining gaps, and the current script inventory.

---

## 1. High-Level Project Overview

`Cure Me, Please` is currently a 2D pixel-art parasite treatment clinic prototype. The core gameplay loop is built around a customer entering the clinic, speaking through a clue-based dialog, moving into the Treatment Room, selecting or treating infected body areas, and completing the case so the customer can leave.

The project currently has these major implemented pillars:

- Clinic game flow and customer enter / exit flow.
- Dialog system using ScriptableObject data, speaker names, typewriter text, and text blip SFX.
- Candle and Sanity systems as the pressure mechanics.
- Counter Room to Treatment Room transition with blink-style fade.
- World-space Tongs parasite extraction mini game.
- Anatomy Selection Screen code layer for choosing body parts before mini games.
- Basic Main Menu controller, audio settings, button hover animation, and SFX.
- Camera sway and parallax support for pixel-art presentation.
- Editor setup tools for clinic, Tongs, and Anatomy setup.

The prototype is no longer just a static room mockup. It now has a playable vertical-slice structure, but some content and scene setup work is still placeholder or needs final art integration.

---

## 2. Current Gameplay Flow

Current intended flow:

1. Customer enters the shop.
2. Customer reaches the counter and the interaction bubble appears.
3. Player clicks the bubble.
4. Dialog opens.
5. Customer describes symptoms through dialog clues.
6. Dialog completes and `GameFlow` starts treatment.
7. Room transition moves from Counter Room to Treatment Room.
8. Treatment flow begins.
9. Anatomy Selection Screen is intended to appear first.
10. Player selects a body area based on dialog memory.
11. If the selected area is infected, the matching mini game starts.
12. Arm currently routes to the Tongs mini game.
13. Completing the mini game marks the area as treated.
14. When all infected areas are treated, `Treatment` completes the case.
15. `GameFlow` resets Sanity, returns to the Counter Room, and starts customer exit.

Current implemented fallback:

- If no `AnatomyController` is assigned to `Treatment`, `Treatment` can still start `TongsMiniGame` directly.

---

## 3. Completed / Implemented Systems

### 3.1 Game Flow

Implemented through `GameFlow.cs`.

Current state machine:

- `Idle`
- `CustomerEntering`
- `WaitingForDialog`
- `DialogActive`
- `TransitionToTreatment`
- `TreatmentReady`
- `TreatmentActive`
- `CustomerLeaving`
- `Complete`

Current responsibilities:

- Starts the first customer on play.
- Connects active customer and active Sanity component.
- Opens dialog when the customer reaches the counter.
- Starts treatment after dialog completion.
- Completes treatment and routes customer exit.
- Resets Sanity after a cured customer.
- Ticks Candle and Sanity even if their GameObjects are hidden.

Status: **Functional vertical-slice flow exists.**

---

### 3.2 Customer Flow

Implemented through `CustomerAgent.cs`, `CustomerLayer.cs`, and `Door.cs`.

Current responsibilities:

- Customer enters from spawn / outside points.
- Door visuals can switch between open and closed.
- Customer walks to the counter.
- Bubble appears when the customer is ready for dialog.
- Customer can exit after treatment completion.
- Customer sorting layer changes support the illusion of entering through the shop / door area.

Status: **Basic customer enter / counter / exit flow is implemented.**

Remaining work:

- Multi-customer queue / random patient spawning is not finalized.
- Per-patient data assets are not connected yet.

---

### 3.3 Dialog System

Implemented through `Dialog.cs`, `DialogData.cs`, and `Bubble.cs`.

Current features:

- Dialog content is stored in a ScriptableObject.
- Lines support `Player` or `Customer` speaker type.
- Display names exist for player and customer.
- TMP speaker and body text are supported.
- Typewriter effect reveals text gradually.
- Next button only appears after the line finishes typing.
- Text blip SFX plays while text appears.
- Customer blip variation list is supported.
- Customer blip selection can avoid repeating the previous clip.
- Blip playback stops when a line completes.

Status: **Implemented and integrated with GameFlow.**

Current data:

- `Assets/Core/Data/Dialog/DialogDataTest.asset`
- Test dialog currently exists, but the asset has local uncommitted changes.

Remaining work:

- Final dialog writing for real patient cases.
- Link each patient case to infected body areas.

---

### 3.4 Candle System

Implemented through `Candle.cs`.

Current features:

- Tracks current candle light value.
- Supports drain and refill behavior.
- Has light state values such as bright / low / out.
- Can keep ticking while hidden through `GameFlow.TickHiddenStatusSystems`.
- Intended to pressure the player to return to the counter and maintain the candle.

Status: **Basic system implemented.**

Remaining work:

- Final candle refill interaction is not fully designed / connected.
- Final VFX / audio feedback still needs polish.

---

### 3.5 Sanity System

Implemented through `Sanity.cs`.

Current features:

- Tracks patient Sanity.
- Increases based on protected / candle-out / treatment-stress conditions.
- Has stable, warning, and critical style state support.
- Can trigger transformation placeholder when maximum is reached.
- Resets after treatment completion.
- Can tick while hidden through `GameFlow.TickHiddenStatusSystems`.

Status: **Basic system implemented and connected to GameFlow.**

Remaining work:

- Final transformation gameplay / fail state is placeholder.
- Balancing values need playtesting.

---

### 3.6 Room Transition

Implemented through `RoomTransition.cs`.

Current features:

- Handles Counter Room to Treatment Room switching.
- Uses blink-style fade timing.
- Supports callback after room transition.
- Keeps the Treatment Room separate so the player cannot directly watch the candle during treatment.

Status: **Implemented.**

Remaining work:

- Final transition polish / animation timing can still be tuned.

---

### 3.7 Treatment Flow

Implemented through `Treatment.cs`.

Current features:

- Owns Treatment UI / treatment root visibility.
- Starts treatment after room transition.
- Supports returning to the Counter Room.
- Supports resuming Treatment Room.
- Can route through `AnatomyController` first.
- Falls back to direct `TongsMiniGame` if Anatomy is not assigned.
- Completes the case by calling `GameFlow.CompleteTreatment(customer)`.

Status: **Core treatment flow is implemented, but scene-level Anatomy setup may still need to be created / assigned.**

Remaining work:

- Final treatment UI layout.
- Final body-part screens and mini games beyond Arm / Tongs.

---

### 3.8 Anatomy Selection Screen

Implemented through `AnatomyController.cs`, `BodyPartButton.cs`, `BodyArea.cs`, `PartState.cs`, and `InfectionMode.cs`.

Current features:

- Supports logical body areas: `Head`, `Torso`, `Arm`, `Leg`.
- Supports separate UI buttons for left / right limbs while mapping them to the same area.
- Supports Manual infection setup.
- Supports Random infection setup.
- Does not visually reveal infected parts.
- Allows free selection order.
- Healthy area shows a "nothing to treat here" style message and allows immediate exit.
- Infected Arm can route to Tongs mini game.
- Treated parts change color.
- Counter return is only available at Anatomy level, not inside a selected part / active mini game.

Status: **Code implemented. Scene setup still needs to be run or manually assigned in Unity.**

Unity setup tool:

- `Tools > Pixel Forge > Treatment > Create Anatomy Selection Screen`

Remaining work:

- Replace placeholder body part Images with final cut sprites.
- Enable Read/Write on body part textures if using `alphaHitTestMinimumThreshold`.
- Assign / verify Anatomy screen references in `Treatment`.
- Add future mini games for Head, Torso, and Leg.
- Connect real patient case data to infected body areas.

---

### 3.9 Tongs Mini Game

Implemented through `TongsMiniGame.cs`, `Parasite.cs`, `ParasiteType.cs`, and `TongsTool.cs`.

Current features:

- World-space parasite extraction mini game.
- Uses Input System pointer position.
- Requires Tongs equip if enabled.
- Supports parasite hold, pull, pain, extraction completion, and Sanity stress hooks.
- Supports parasite spawn anchors.
- Prevents duplicate spawn positions by shuffling anchors.
- Supports multiple parasite spawn options.
- Uses a shuffled spawn bag so Small / Long / Big variants are distributed before repeating.
- Parasite prefabs can align their spawn anchor / pivot to a spawn point.
- Parasites can visually follow pointer pull direction and tilt while being pulled.
- Complete button appears when all parasites are extracted.

Current parasite data:

- `SmallParasiteType.asset`
- `LongParasiteType.asset`
- `BigParasiteType.asset`

Current prefabs:

- `SmallParasite_World.prefab`
- `LongParasite_World.prefab`
- `BigParasite_World.prefab`

Status: **Playable prototype implemented and committed previously as `Add Tongs minigame`.**

Remaining work:

- Replace placeholder parasite visuals with final art.
- Tune pull distance, pain, channel width, and parasite behavior.
- Finalize UI / SFX / VFX feedback.

---

### 3.10 Main Menu

Implemented through `MainMenuController.cs`, `MenuAudioSettings.cs`, and `MenuButtonSfx.cs`.

Current features:

- Main panel and settings panel switching.
- Play button loads the configured game scene.
- Credit button loads the configured credit scene.
- Quit button supports editor-safe quit behavior.
- Music and Game volume sliders control AudioMixer exposed parameters.
- Volumes are saved with PlayerPrefs.
- Buttons can play hover and click SFX.
- Buttons scale and randomly tilt left / right on hover.

Status: **Script controller side implemented.**

Remaining work:

- Final scene wiring verification.
- Final audio clips and mixer tuning.

---

### 3.11 Camera, Parallax, and Pixel Presentation

Implemented through `CameraSway.cs`, `MouseParallax.cs`, and `ParallaxLayer.cs`.

Current features:

- Camera sway support.
- Mouse-driven parallax layer support.
- Pixel Perfect Camera exists in the current GameScene setup.

Status: **Support scripts implemented.**

Remaining work:

- Final parallax art slicing and layer tuning.
- Confirm all layers move visibly with intended sensitivity.

---

### 3.12 Interaction and HUD

Implemented through `PlayerInteract.cs`, `Interactable.cs`, and `BasicStatusHud.cs`.

Current features:

- Player interaction supports Input System key reading.
- Generic `Interactable` interface exists.
- Basic status HUD can display Candle and Sanity state.

Status: **Basic support implemented.**

Remaining work:

- Final player movement / interaction polish.
- Final HUD visual style.

---

## 4. Current Script Inventory

### Camera

| Script | Purpose |
|---|---|
| `CameraSway.cs` | Camera motion / sway support. |
| `MouseParallax.cs` | Mouse-driven parallax movement. |

### Candle

| Script | Purpose |
|---|---|
| `Candle.cs` | Candle light value, drain/refill, candle state. |

### Customer

| Script | Purpose |
|---|---|
| `CustomerAgent.cs` | Customer movement, entering, counter arrival, exit behavior. |
| `CustomerLayer.cs` | Customer sorting/layer state changes. |
| `Sanity.cs` | Patient sanity, stress, transformation trigger. |

### Dialog

| Script | Purpose |
|---|---|
| `Bubble.cs` | Clickable customer interaction bubble. |
| `Dialog.cs` | Dialog runtime, typewriter, speaker display, blip SFX, flow completion. |
| `DialogData.cs` | Dialog ScriptableObject, speaker enum, dialog lines, blip clip selection. |

### Game Flow

| Script | Purpose |
|---|---|
| `GameFlow.cs` | Main clinic flow state machine and system coordinator. |
| `ClinicSceneSetup.cs` | Editor setup helper for scene structure. |
| `ClinicAdditiveStatusSetup.cs` | Editor setup helper for additive HUD/status setup. |

### Interaction

| Script | Purpose |
|---|---|
| `Interactable.cs` | Generic interaction interface. |
| `PlayerInteract.cs` | Player interaction input using the Input System. |

### Main Menu

| Script | Purpose |
|---|---|
| `MainMenuController.cs` | Main menu navigation, play, settings, quit, credits. |
| `MenuAudioSettings.cs` | AudioMixer volume sliders and saved settings. |
| `MenuButtonSfx.cs` | Button hover/click SFX and hover scale/tilt animation. |

### Parallax

| Script | Purpose |
|---|---|
| `ParallaxLayer.cs` | Per-layer parallax response. |

### Shop

| Script | Purpose |
|---|---|
| `Door.cs` | Door open/close visual logic. |

### Treatment

| Script | Purpose |
|---|---|
| `RoomTransition.cs` | Counter/Treatment room switching with blink fade. |
| `Treatment.cs` | Treatment flow owner, room return/resume, Anatomy/Tongs handoff, case completion. |

### Treatment / Anatomy

| Script | Purpose |
|---|---|
| `AnatomyController.cs` | Body-area selection, infection setup, healthy/infected logic, mini-game handoff. |
| `BodyArea.cs` | Logical body area enum. |
| `BodyPartButton.cs` | UI body-part hover/click behavior and part tinting. |
| `InfectionMode.cs` | Manual/Random infection setup enum. |
| `PartState.cs` | Untouched/Healthy/Infected/Treated state enum. |
| `AnatomySelectionSetup.cs` | Editor setup tool for creating Anatomy Selection UI. |

### Treatment / Tongs

| Script | Purpose |
|---|---|
| `Parasite.cs` | Parasite runtime behavior, pull progress, pain, extraction, visuals. |
| `ParasiteType.cs` | Parasite type data asset definition. |
| `TongsMiniGame.cs` | Tongs mini game runtime, spawn, input, completion, sanity stress. |
| `TongsTool.cs` | Tongs equip button/tool behavior. |
| `TongsMiniGameSetup.cs` | Editor setup tools for Tongs mini game and parasite options. |

### UI

| Script | Purpose |
|---|---|
| `BasicStatusHud.cs` | Candle and Sanity HUD display. |

---

## 5. Current Data, Prefabs, and Reports

### Data Assets

| Path | Purpose |
|---|---|
| `Assets/Core/Data/Dialog/DialogDataTest.asset` | Test dialog data. |
| `Assets/Core/Data/Treatment/Tongs/SmallParasiteType.asset` | Small parasite type. |
| `Assets/Core/Data/Treatment/Tongs/LongParasiteType.asset` | Long parasite type. |
| `Assets/Core/Data/Treatment/Tongs/BigParasiteType.asset` | Big parasite type. |
| `Assets/Core/Data/Treatment/Tongs/TongsParasitePlaceholder.asset` | Placeholder parasite sprite/texture asset. |

### Prefabs

| Path | Purpose |
|---|---|
| `Assets/Core/Prefab/Parasites/SmallParasite_World.prefab` | Small parasite world prefab. |
| `Assets/Core/Prefab/Parasites/LongParasite_World.prefab` | Long parasite world prefab. |
| `Assets/Core/Prefab/Parasites/BigParasite_World.prefab` | Big parasite world prefab. |

### Existing Reports

| Report | Purpose |
|---|---|
| `EightDirectionPlayerControllerCamera_Report.md` | Early player/camera movement plan. |
| `ClinicGameFlowSanityCandleDialog_Report.md` | Clinic flow, Sanity, Candle, Dialog design. |
| `MainMenuUIImplementation_Report.md` | Main menu implementation plan. |
| `DialogSpeakerNameTypewriterSfx_Report.md` | Dialog speaker/typewriter/SFX plan. |
| `Tongs_MiniGame_Technical_Design_Report.md` | Tongs mini game technical design. |
| `Anatomy_Selection_Screen_Design_Report.md` | Anatomy Selection Screen design. |
| `SceneBaseline_CurrentLayout.md` | Preserved current scene baseline and setup notes. |

---

## 6. Current Completion Level

### Solid / Usable Prototype Systems

- GameFlow state machine.
- Customer first-pass enter and exit.
- Dialog SO and typewriter pipeline.
- Candle and Sanity basic pressure loop.
- Room transition.
- Tongs mini game core loop.
- Main Menu scripts.
- Anatomy Selection code layer.

### Partially Complete / Needs Scene or Content Setup

- Anatomy Selection Screen needs final Unity scene setup and final body-part sprites.
- Candle refill interaction is not finalized.
- Real patient data / case data is not finalized.
- Final treatment room art is still placeholder / in-progress.
- Final parasite art and treatment SFX/VFX are not complete.
- Head, Torso, and Leg mini games are not implemented.
- Multi-customer flow is not finalized.

### Placeholder / Future Work

- Transformation fail state.
- Full patient roster.
- Case-specific infection layouts.
- Final UI art pass.
- Final balancing for Candle, Sanity, and Tongs.
- Save/settings polish beyond basic menu audio settings.

---

## 7. Current Workspace Notes

The workspace currently has local uncommitted changes and untracked files outside the last committed Tongs work. The notable current local items are:

- `Assets/Core/Data/Dialog/DialogDataTest.asset`
- `Assets/Core/Report/SceneBaseline_CurrentLayout.md`
- `Assets/Core/Script/Treatment/Treatment.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/Core/Data/Parasite.meta`
- `Assets/Core/Report/Anatomy_Selection_Screen_Design_Report.md`
- `Assets/Core/Script/Treatment/Anatomy/`
- `Assets/Core/Texture/Char1_VillagerGirlHealed.aseprite.meta`

This report was generated from the current workspace state, so it includes both committed work and currently local in-progress work.

---

## 8. Recommended Next Steps

1. Run the Anatomy setup tool in Unity:
   - `Tools > Pixel Forge > Treatment > Create Anatomy Selection Screen`
2. Replace placeholder Anatomy body-part images with final cut sprites.
3. Enable Read/Write on textures used by Anatomy part alpha hit testing.
4. Verify `Treatment` has `AnatomyController` and `TongsMiniGame` references assigned.
5. Test this flow:
   - Customer enters.
   - Dialog opens.
   - Dialog completes.
   - Treatment Room opens.
   - Anatomy screen appears.
   - Arm selection starts Tongs.
   - Completing Tongs marks Arm treated.
   - Treatment completes and customer exits.
6. Design the next mini game or expand patient case data.

---

## 9. Summary

The project currently has a strong vertical-slice foundation. The clinic loop, dialog system, sanity/candle pressure, room transition, Tongs mini game, and main menu scripts are in place. The newest layer is the Anatomy Selection Screen, which is implemented in code and ready for scene setup. The main remaining work is content integration: final art, final patient cases, final body-part sprites, real infection layouts, candle refill interaction, and additional mini games beyond the Arm/Tongs path.

