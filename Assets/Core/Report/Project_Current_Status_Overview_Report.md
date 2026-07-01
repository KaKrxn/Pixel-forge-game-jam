# Project Current Status Overview Report

**Project:** Cure Me, Please  
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`  
**Engine:** Unity 6000.3.17f1, 2D Pixel Art  
**Report date:** July 2, 2026  
**Report purpose:** Maintain the current project snapshot, implemented systems, active gaps, setup notes, and script inventory.

---

## 1. High-Level Project Overview

`Cure Me, Please` is a 2D pixel-art parasite treatment clinic prototype. The current vertical slice is built around a customer entering the clinic, talking to the player through clue-based dialog, moving into a separated Treatment Room, selecting body areas on an Anatomy screen, and playing the correct mini game for each required treatment area.

The project now has these major implemented pillars:

- Clinic game flow and first-pass customer enter / exit behavior.
- Dialog system using ScriptableObject data, speaker names, typewriter text, and text blip SFX.
- Treatment Case ScriptableObject pipeline for defining each customer's dialog and required treatment areas.
- Candle and Sanity pressure systems.
- Counter Room to Treatment Room transition with blink-style fade.
- Anatomy Selection Screen that can read required treatments from the active customer case.
- Shared MiniGameOverlay with progress / pain meters, Complete button, and toggle-based tool selection.
- Tongs parasite extraction mini game.
- Knife lesion treatment mini game with slice and pull phases.
- Basic Main Menu controller, audio settings, button hover animation, and SFX.
- Camera sway and parallax support for pixel-art presentation.
- Editor setup tools for clinic, Tongs, Knife, Anatomy, and Treatment Case setup.

The prototype is no longer only a room mockup. It now has a playable treatment structure with multiple required treatment areas per case. The main remaining work is content production, balancing, final art, scene polish, and expanding the patient roster.

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
8. `Treatment` begins the treatment flow.
9. Anatomy Selection Screen appears.
10. Player selects a body area based on dialog memory.
11. The selected body area is checked against the active `TreatmentCaseData`.
12. If the area is required, Anatomy starts the configured mini game for that area.
13. Current sample case routes Arm to Tongs and Torso to Knife.
14. Completing one mini game marks only that body area as treated.
15. Player returns to Anatomy and selects the next required treatment area.
16. When all required areas are treated, the Treatment screen shows Complete.
17. Player completes the case.
18. `GameFlow` resets Sanity, returns to the Counter Room, and starts customer exit.

Current implemented fallback:

- If no customer case is found, Anatomy can fall back to Manual or Random infection setup.
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

Implemented through `CustomerAgent.cs`, `CustomerLayer.cs`, `Door.cs`, and `CustomerCaseProvider.cs`.

Current responsibilities:

- Customer enters from spawn / outside points.
- Door visuals can switch between open and closed.
- Customer walks to the counter.
- Bubble appears when the customer is ready for dialog.
- Customer can exit after treatment completion.
- Customer sorting layer changes support the illusion of entering through the shop / door area.
- Customer can hold a `TreatmentCaseData` reference through `CustomerCaseProvider`.

Status: **Basic customer enter / counter / exit flow is implemented, with case data support.**

Remaining work:

- Multi-customer queue / random patient spawning is not finalized.
- Patient selection and case assignment beyond the current test case is not finalized.

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
- Dialog can resolve its active `DialogData` from the active customer's `TreatmentCaseData`.

Status: **Implemented and integrated with GameFlow and Treatment Case data.**

Current data:

- `Assets/Core/Data/Dialog/DialogDataTest.asset`

Remaining work:

- Final dialog writing for real patient cases.
- More patient-specific dialog assets.

---

### 3.4 Treatment Case Data

Implemented through `TreatmentCaseData.cs`, `TreatmentAreaRequirement.cs`, `TreatmentCaseRuntime.cs`, `TreatmentCaseSource.cs`, `TreatmentMiniGameType.cs`, `CustomerCaseProvider.cs`, and `TreatmentCaseSetup.cs`.

Current features:

- Each customer can reference a `TreatmentCaseData` asset.
- Case data stores a `caseId`, customer display name, dialog data, and a list of required treatments.
- Each required treatment defines a body area and the mini game type needed for that area.
- Runtime case state tracks which required areas are treated.
- Anatomy can read the active customer's case and only complete treatment after all required areas are treated.
- Current sample case requires more than one treatment area.

Current sample case:

- `Assets/Core/Data/Treatment/Cases/Case_Test_MixedTreatment.asset`
- Case id: `test_mixed_treatment`
- Customer display name: `Test Customer`
- Required treatments:
  - Arm -> Tongs
  - Torso -> Knife

Unity setup tool:

- `Tools > Pixel Forge > Treatment > Assign Test Treatment Case To Selected Customer`

Status: **Implemented as the current source of truth for patient treatment requirements.**

Remaining work:

- Build the real patient case library.
- Decide how customers are selected/spawned and which case each customer receives.
- Expand case data if future design needs rewards, time limits, symptoms, or transformation tuning per patient.

---

### 3.5 Candle System

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

### 3.6 Sanity System

Implemented through `Sanity.cs`.

Current features:

- Tracks patient Sanity.
- Increases based on protected / candle-out / treatment-stress conditions.
- Has stable, warning, and critical style state support.
- Can trigger transformation placeholder when maximum is reached.
- Resets after treatment completion.
- Can tick while hidden through `GameFlow.TickHiddenStatusSystems`.
- Mini games can add Sanity pressure through pain or mistakes.

Status: **Basic system implemented and connected to GameFlow and mini games.**

Remaining work:

- Final transformation gameplay / fail state is placeholder.
- Balancing values need playtesting.

---

### 3.7 Room Transition

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

### 3.8 Treatment Flow

Implemented through `Treatment.cs`.

Current features:

- Owns Treatment UI / treatment root visibility.
- Starts treatment after room transition.
- Supports returning to the Counter Room from Anatomy level.
- Supports resuming Treatment Room.
- Can route through `AnatomyController` first.
- Falls back to direct `TongsMiniGame` if Anatomy is not assigned.
- Shows Complete only when Anatomy reports all required areas are treated.
- Completes the case by calling `GameFlow.CompleteTreatment(customer)`.

Status: **Core treatment flow is implemented and now supports multi-area case completion.**

Remaining work:

- Final treatment UI layout.
- Final candle refill UI/interaction in Counter return state.
- Better in-game feedback for which treatment steps are complete without revealing hidden diagnosis too early.

---

### 3.9 Anatomy Selection Screen

Implemented through `AnatomyController.cs`, `BodyPartButton.cs`, `BodyArea.cs`, `PartState.cs`, and `InfectionMode.cs`.

Current features:

- Supports logical body areas: `Head`, `Torso`, `Arm`, `Leg`.
- Supports separate UI buttons for left / right limbs while mapping them to the same area.
- Supports Manual infection setup.
- Supports Random infection setup.
- Supports Customer Case setup through `TreatmentCaseData`.
- Does not visually reveal infected parts by default.
- Allows free selection order.
- Healthy area shows a "nothing to treat here" style message and allows immediate exit.
- Required areas route to the mini game type defined by the active case.
- Current fallback routes Arm to Tongs and Head/Torso/Leg to Knife when no case overrides are available.
- Completing one mini game marks only that selected body area treated.
- Complete treatment becomes available only after all required areas are treated.
- Counter return is only available at Anatomy level, not inside a selected part / active mini game.

Status: **Implemented and integrated with Treatment Case data and both current mini games.**

Unity setup tool:

- `Tools > Pixel Forge > Treatment > Create Anatomy Selection Screen`

Remaining work:

- Replace placeholder body part Images with final cut sprites.
- Enable Read/Write on body part textures if using `alphaHitTestMinimumThreshold`.
- Verify final Anatomy scene references after art/layout changes.
- Add final feedback for treated/checked states.

---

### 3.10 Shared Mini Game Overlay

Implemented through `MiniGameOverlay.cs`.

Current features:

- Shared HUD for treatment mini games.
- Reuses one canvas/content root for progress slider, pain slider, Complete button, and tool controls.
- Mini games activate the overlay with their own Complete and tool-selection handlers.
- Tool controls are toggle-based.
- Turning a tool toggle on equips/selects the tool.
- Turning a selected tool toggle off clears the held tool.
- Existing Button controls can still work as a fallback by clicking the selected tool again.
- Selection indicators are updated from the current selected tool id.
- Overlay ownership protection prevents a late mini game deactivation from wiping a newer active mini game.

Unity setup tool:

- `Tools > Pixel Forge > Treatment > Convert MiniGame Tool Buttons To Toggles`

Status: **Implemented and shared by Tongs and Knife.**

Remaining work:

- Final icon art for Tongs / Knife.
- Final tool tray layout and SFX feedback.

---

### 3.11 Tongs Mini Game

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
- Can use the shared MiniGameOverlay tool selection flow.

Current parasite data:

- `SmallParasiteType.asset`
- `LongParasiteType.asset`
- `BigParasiteType.asset`

Current prefabs:

- `SmallParasite_World.prefab`
- `LongParasite_World.prefab`
- `BigParasite_World.prefab`

Status: **Playable prototype implemented.**

Remaining work:

- Replace placeholder parasite visuals with final art.
- Tune pull distance, pain, channel width, and parasite behavior.
- Finalize UI / SFX / VFX feedback.

---

### 3.12 Knife Mini Game

Implemented through `KnifeMiniGame.cs`, `Lesion.cs`, `LesionType.cs`, `KnifeToolState.cs`, and `KnifeMiniGameSetup.cs`.

Current features:

- World-space lesion treatment mini game.
- Uses Input System pointer position and supports an assigned input camera / world input plane.
- Uses the shared MiniGameOverlay with a `Knife` tool id.
- Ignores world actions when the pointer is over UI.
- Supports lesions placed manually under a lesion root.
- Supports optional lesion spawning from prefabs and spawn anchors.
- Uses a shuffled lesion prefab bag when spawning is enabled.
- Supports two lesion types:
  - `Tumor`: slice path completes the lesion.
  - `Bulge`: slice path opens the wound, then the player puts the knife down and pulls the bulge by hand.
- Tracks progress and pain per lesion.
- Pain can add Sanity pressure.
- Auto-completes when all lesions are done if enabled.
- Knife action mode locks whether the current action is Slice or Pull when the press begins.
- Knife tip preview follows the cursor while the Knife tool is equipped, making the slice check point easier to read.
- Slice can begin from the lesion collider or near the configured cut path/start radius.
- Bulge pull visual can follow the cursor while being pulled.

Unity setup tool:

- `Tools > Pixel Forge > Treatment > Setup Knife MiniGame In Open Scene`

Status: **Playable prototype implemented, currently still using sample lesion visuals.**

Remaining work:

- Final lesion art.
- Final cut guide visual design.
- Better wound opening / pull animation.
- Tune path width, pain, rollback, and pull speed.
- Build final lesion prefabs if spawning will be used in production cases.

---

### 3.13 Main Menu

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

### 3.14 Camera, Parallax, and Pixel Presentation

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

### 3.15 Interaction and HUD

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
| `CustomerCaseProvider.cs` | Holds the customer's assigned `TreatmentCaseData`. |
| `CustomerLayer.cs` | Customer sorting/layer state changes. |
| `Sanity.cs` | Patient sanity, stress, transformation trigger. |

### Dialog

| Script | Purpose |
|---|---|
| `Bubble.cs` | Clickable customer interaction bubble. |
| `Dialog.cs` | Dialog runtime, typewriter, speaker display, blip SFX, case-dialog resolution, flow completion. |
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
| `MiniGameOverlay.cs` | Shared mini game HUD, meters, Complete button, and toggle-based tool selection. |
| `RoomTransition.cs` | Counter/Treatment room switching with blink fade. |
| `Treatment.cs` | Treatment flow owner, room return/resume, Anatomy/Tongs handoff, case completion. |

### Treatment / Anatomy

| Script | Purpose |
|---|---|
| `AnatomyController.cs` | Body-area selection, case/manual/random infection setup, mini-game routing, multi-area completion. |
| `BodyArea.cs` | Logical body area enum. |
| `BodyPartButton.cs` | UI body-part hover/click behavior and part tinting. |
| `InfectionMode.cs` | Manual/Random infection setup enum. |
| `PartState.cs` | Untouched/Healthy/Infected/Treated state enum. |
| `AnatomySelectionSetup.cs` | Editor setup tool for creating Anatomy Selection UI. |

### Treatment / Case

| Script | Purpose |
|---|---|
| `CustomerCaseProvider.cs` | Component attached to customers to provide a treatment case. |
| `TreatmentAreaRequirement.cs` | Serializable area + mini game requirement entry. |
| `TreatmentCaseData.cs` | ScriptableObject defining a customer case, dialog, and required treatments. |
| `TreatmentCaseRuntime.cs` | Runtime tracker for required and treated areas. |
| `TreatmentCaseSource.cs` | Anatomy case source enum. |
| `TreatmentMiniGameType.cs` | Mini game type enum used by treatment requirements. |
| `TreatmentCaseSetup.cs` | Editor setup tool for assigning the test case to a customer. |

### Treatment / Knife

| Script | Purpose |
|---|---|
| `KnifeMiniGame.cs` | Knife mini game runtime, input, spawning, slice/pull actions, completion, sanity stress. |
| `KnifeToolState.cs` | Knife held/not-held state enum. |
| `Lesion.cs` | Lesion behavior, cut path, pain, pull phase, visual state, cursor/gizmo preview. |
| `LesionType.cs` | Lesion type enum: Bulge or Tumor. |
| `KnifeMiniGameSetup.cs` | Editor setup tool for Knife mini game, sample lesions, anchors, overlay assignment. |

### Treatment / Tongs

| Script | Purpose |
|---|---|
| `Parasite.cs` | Parasite runtime behavior, pull progress, pain, extraction, visuals. |
| `ParasiteType.cs` | Parasite type data asset definition. |
| `TongsMiniGame.cs` | Tongs mini game runtime, spawn, input, completion, sanity stress. |
| `TongsTool.cs` | Tongs equip button/tool behavior. |
| `TongsMiniGameSetup.cs` | Editor setup tools for Tongs mini game, parasite options, overlay toggle conversion. |

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
| `Assets/Core/Data/Treatment/Cases/Case_Test_MixedTreatment.asset` | Test customer case requiring Arm/Tongs and Torso/Knife. |
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
| `Knife_MiniGame_Technical_Design_Report.md` | Knife mini game technical design. |
| `PatientTreatmentCaseSO_Design_Report.md` | Treatment Case ScriptableObject design. |
| `SharedMiniGameOverlay_Refactor_Report.md` | Shared mini game overlay/refactor design. |
| `SceneBaseline_CurrentLayout.md` | Preserved current scene baseline and setup notes. |
| `Project_Current_Status_Overview_Report.md` | Current maintained project overview snapshot. |

---

## 6. Current Completion Level

### Solid / Usable Prototype Systems

- GameFlow state machine.
- Customer first-pass enter and exit.
- Dialog SO and typewriter pipeline.
- Treatment Case SO pipeline.
- Candle and Sanity basic pressure loop.
- Room transition.
- Anatomy Selection with multi-area treatment completion.
- Shared MiniGameOverlay with toggle tool controls.
- Tongs mini game core loop.
- Knife mini game core loop.
- Main Menu scripts.

### Partially Complete / Needs Scene or Content Setup

- Candle refill interaction is not finalized.
- Real patient data / case library is not finalized.
- Final treatment room art is still placeholder / in-progress.
- Final parasite, lesion, wound, and tool art are not complete.
- Final treatment SFX/VFX are not complete.
- Head and Leg do not have unique final mini game content yet.
- Multi-customer flow is not finalized.
- Anatomy and mini game UI need final art pass.

### Placeholder / Future Work

- Transformation fail state.
- Full patient roster.
- Case-specific rewards / difficulty / tuning.
- Final cut guide visual for Knife.
- Final UI art pass.
- Final balancing for Candle, Sanity, Tongs, and Knife.
- Save/settings polish beyond basic menu audio settings.

---

## 7. Current Workspace Notes

This report was updated from the current workspace state on July 2, 2026. It includes both committed work and local in-progress work.

Notable current local / in-progress areas:

- `Assets/Core/Script/Treatment/Case/`
- `Assets/Core/Script/Treatment/Knife/`
- `Assets/Core/Script/Treatment/MiniGameOverlay.cs`
- `Assets/Core/Data/Treatment/Cases/Case_Test_MixedTreatment.asset`
- `Assets/Core/Report/Knife_MiniGame_Technical_Design_Report.md`
- `Assets/Core/Report/PatientTreatmentCaseSO_Design_Report.md`
- `Assets/Core/Report/SharedMiniGameOverlay_Refactor_Report.md`
- `Assets/Core/Scene/GameScene.unity`

Important project rule:

- Preserve the existing manually arranged Unity hierarchy positions, sprite assignments, sorting values, and active states unless a task explicitly asks to move or replace them.

---

## 8. Recommended Next Steps

1. Verify the current full sample case in Play Mode:
   - Customer enters.
   - Dialog opens from the assigned case.
   - Dialog completes.
   - Treatment Room opens.
   - Anatomy screen appears.
   - Arm starts Tongs and returns to Anatomy when complete.
   - Torso starts Knife and returns to Anatomy when complete.
   - Complete appears only after both required areas are treated.
   - Customer exits after treatment completion.
2. Polish Knife mini game feedback:
   - Design and implement the final cut guide visual.
   - Replace sample lesion art.
   - Tune path width, pain, rollback, and pull speed.
3. Finish the Candle refill interaction at the Counter return state.
4. Create real patient case assets beyond `Case_Test_MixedTreatment`.
5. Decide the patient spawning / case assignment flow for multiple customers.
6. Replace Anatomy placeholder body parts with final sprites and verify hit testing.
7. Finalize shared MiniGameOverlay visuals, tool icons, and SFX.
8. Balance Sanity, Candle drain/refill, Tongs pain, and Knife pain.

---

## 9. Summary

The project now has a stronger vertical-slice foundation than the previous snapshot. The clinic loop, dialog system, sanity/candle pressure, room transition, Anatomy Selection, Tongs mini game, Knife mini game, shared mini game HUD, and Treatment Case ScriptableObject pipeline are all implemented at prototype level. The current sample case can represent more than one treatment requirement and can route different body areas to different mini games.

The main remaining work is no longer basic architecture. The next phase should focus on playtesting the full multi-treatment case, polishing Knife and Tongs feedback, finalizing Candle refill, creating real patient content, and replacing placeholder art with final pixel-art assets.
