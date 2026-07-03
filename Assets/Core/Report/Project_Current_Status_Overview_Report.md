# Project Current Status Overview Report

**Project:** Cure Me, Please  
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`  
**Engine:** Unity 6000.3.17f1, 2D Pixel Art  
**Report date:** July 3, 2026  
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
- Needle pustule treatment mini game with pierce, squeeze, and drain phases.
- Treatment Body Prefab Catalog support for selecting different body prefabs by mini game and body area.
- Anchor-driven spawn authoring for parasites, lesions, and pustules.
- Basic Main Menu controller, audio settings, button hover animation, and SFX.
- Camera sway and parallax support for pixel-art presentation.
- Editor setup tools for clinic, Tongs, Knife, Needle, Anatomy, and Treatment Case setup.

The prototype is no longer only a room mockup. It now has a playable treatment structure with multiple required treatment areas per case and three core treatment mini games. The main remaining work is content production, balancing, final art, scene polish, and implementing the multi-customer queue/runtime flow.

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
13. Current sample case routes Arm to Tongs and Torso to Knife. Additional cases can route areas to Needle.
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
- Current focused playtest case routes Arm to Tongs and completes the full customer exit flow.
- A mixed sample case remains available for multi-area treatment testing.

Current focused playtest case:

- `Assets/Core/Data/Treatment/Cases/Case_Test_Arm_Tongs.asset`
- Case id: `test_arm_tongs`
- Customer display name: `Tongs Test Customer`
- Required treatments:
  - Arm -> Tongs

Mixed sample case:

- `Assets/Core/Data/Treatment/Cases/Case_Test_MixedTreatment.asset`
- Case id: `test_mixed_treatment`
- Customer display name: `Test Customer`
- Required treatments:
  - Arm -> Tongs
  - Torso -> Knife
  - Leg -> Needle

Unity setup tool:

- `Tools > Pixel Forge > Treatment > Assign Arm Tongs Case To Selected Customer`
- `Tools > Pixel Forge > Treatment > Assign Mixed Treatment Case To Selected Customer`
- `Tools > Pixel Forge > Treatment > Setup Dynamic Body Prefab Spawner In Open Scene`

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

Status: **Implemented and shared by Tongs, Knife, and Needle.**

Remaining work:

- Final icon art for Tongs / Knife / Needle.
- Final tool tray layout and SFX feedback.

---

### 3.11 Tongs Mini Game

Implemented through `TongsMiniGame.cs`, `Parasite.cs`, `ParasiteType.cs`, `ParasiteSpawnAnchor.cs`, `ParasiteReveal.cs`, and `TongsTool.cs`.

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
- Parasite reveal masking can hide the body under the skin and reveal more of the parasite as it is pulled.
- Spawn anchor debug logging is available for diagnosing anchor selection and prefab alignment.
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
- Finish tuning parasite mask/head reveal setup on final body prefabs.
- Tune pull distance, pain, channel width, and parasite behavior.
- Finalize UI / SFX / VFX feedback.

---

### 3.12 Knife Mini Game

Implemented through `KnifeMiniGame.cs`, `Lesion.cs`, `LesionType.cs`, `LesionCutOrientation.cs`, `LesionSpawnAnchor.cs`, `KnifeToolState.cs`, `CutGuideLine.cs`, and `KnifeMiniGameSetup.cs`.

Current features:

- World-space lesion treatment mini game.
- Uses Input System pointer position and supports an assigned input camera / world input plane.
- Uses the shared MiniGameOverlay with a `Knife` tool id.
- Ignores world actions when the pointer is over UI.
- Supports lesions placed manually under a lesion root.
- Supports optional lesion spawning from prefabs and spawn anchors.
- Supports typed lesion spawn options so Tumor and Bulge can use separate prefabs.
- Supports `LesionSpawnAnchor` rules for allowed lesion types and allowed cut orientations.
- Randomizes selected spawn anchors without duplicates.
- Randomizes the lesion as Tumor or Bulge per selected anchor when allowed.
- Randomizes horizontal or vertical cut orientation per selected anchor when allowed.
- Shows only the selected spawn point visual that matches the randomized lesion type and orientation.
- Uses a shuffled lesion prefab bag when spawning is enabled.
- Supports two lesion types:
  - `Tumor`: slice path completes the lesion.
  - `Bulge`: slice path opens the wound, then the player puts the knife down and pulls the bulge by hand.
- Bulge supports closed and opened visual roots so the wound can change after being cut open.
- Tracks progress and pain per lesion.
- Pain can add Sanity pressure.
- Auto-completes when all lesions are done if enabled.
- Knife action mode locks whether the current action is Slice or Pull when the press begins.
- Knife tip preview follows the cursor while the Knife tool is equipped, making the slice check point easier to read.
- Slice can begin from the lesion collider or near the configured cut path/start radius.
- Bulge pull visual can follow the cursor while being pulled.

Unity setup tool:

- `Tools > Pixel Forge > Treatment > Setup Knife MiniGame In Open Scene`

Status: **Playable prototype implemented, with anchor-driven random lesion spawning and visual state support.**

Remaining work:

- Final lesion art.
- Final cut guide visual design.
- Better wound opening / pull animation.
- Configure final Tumor/Bulge prefabs and selected-anchor visuals for each body prefab.
- Tune path width, pain, rollback, and pull speed.

---

### 3.13 Needle Mini Game

Implemented through `NeedleMiniGame.cs`, `Pustule.cs`, `PustuleType.cs`, `PustuleSpawnAnchor.cs`, `NeedleActionMode.cs`, `NeedleToolState.cs`, and `NeedleMiniGameSetup.cs`.

Current features:

- World-space pustule treatment mini game.
- Uses Input System pointer position and ignores world actions when the pointer is over UI.
- Uses the shared MiniGameOverlay with a `Needle` tool id.
- Supports two pustule types:
  - `Small`: pierce with Needle, put the Needle down, then squeeze by hand.
  - `Big`: pierce with Needle, then keep the Needle equipped and drain.
- Supports typed pustule spawn options so Small and Big pustules can use separate prefabs.
- Supports `PustuleSpawnAnchor` rules for allowed pustule types.
- Randomizes selected spawn anchors without duplicates.
- Shows only the selected spawn point visual that matches the randomized pustule type.
- Pustules support full and empty visual roots so the visual can change after the pus is removed.
- Tracks progress, pain, target radius, jitter, and Sanity stress hooks.
- Auto-completes when all pustules are done if enabled.

Unity setup tool:

- `Tools > Pixel Forge > Treatment > Setup Needle MiniGame In Open Scene`

Status: **Playable prototype implemented, with anchor-driven random pustule spawning and visual state support.**

Remaining work:

- Final pustule art.
- Configure final Small/Big pustule prefabs and selected-anchor visuals for each body prefab.
- Tune pierce time, squeeze speed, drain speed, radius, jitter, and pain.
- Final Needle SFX/VFX feedback.

---

### 3.14 Treatment Body Prefab Catalog

Implemented through `TreatmentBodyPrefab.cs`, `TreatmentBodyPrefabEntry.cs`, `TreatmentBodyPrefabCatalog.cs`, and `TreatmentBodyPrefabSpawner.cs`.

Current features:

- Body prefabs can be authored per `TreatmentMiniGameType` and `BodyArea`.
- Catalog entries map a mini game + body area pair to a specific `TreatmentBodyPrefab`.
- The spawner can instantiate the correct body prefab and clear the previous active body.
- A body prefab can expose separate gameplay roots and spawn anchor lists for Tongs, Knife, and Needle.
- If no catalog or matching entry is assigned, the spawner logs a warning and existing scene references can still be used.

Status: **Runtime support implemented, content setup still in progress.**

Remaining work:

- Finish final prefab setup for Head, Torso, Arm, and Leg across Tongs, Knife, and Needle.
- Wire the catalog/spawner into the final treatment scene flow where needed.
- Verify that each mini game receives the correct root and anchor list from the spawned body prefab.

---

### 3.15 Main Menu

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

### 3.16 Camera, Parallax, and Pixel Presentation

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

### 3.17 Interaction and HUD

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

### Treatment / Body Prefab

| Script | Purpose |
|---|---|
| `TreatmentBodyPrefab.cs` | Per-mini-game body prefab component, body area metadata, and anchor/root references. |
| `TreatmentBodyPrefabEntry.cs` | Serializable catalog entry mapping mini game + body area to a body prefab. |
| `TreatmentBodyPrefabCatalog.cs` | ScriptableObject catalog for resolving the correct body prefab by mini game and body area. |
| `TreatmentBodyPrefabSpawner.cs` | Runtime spawner that instantiates and clears active treatment body prefabs. |

### Treatment / Knife

| Script | Purpose |
|---|---|
| `KnifeMiniGame.cs` | Knife mini game runtime, input, spawning, slice/pull actions, completion, sanity stress. |
| `KnifeToolState.cs` | Knife held/not-held state enum. |
| `Lesion.cs` | Lesion behavior, cut path, pain, pull phase, visual state, cursor/gizmo preview. |
| `LesionCutOrientation.cs` | Horizontal/Vertical cut orientation enum for randomized Knife spawn points. |
| `LesionSpawnAnchor.cs` | Knife spawn anchor rules, selected visual variants, and orientation/type randomization. |
| `LesionType.cs` | Lesion type enum: Bulge or Tumor. |
| `CutGuideLine.cs` | Optional visual guide helper for Knife cut paths. |
| `KnifeMiniGameSetup.cs` | Editor setup tool for Knife mini game, sample lesions, anchors, overlay assignment. |

### Treatment / Needle

| Script | Purpose |
|---|---|
| `NeedleMiniGame.cs` | Needle mini game runtime, input, spawning, pierce/squeeze/drain actions, completion, sanity stress. |
| `NeedleActionMode.cs` | Current Needle action mode enum: Pierce, Squeeze, or Drain. |
| `NeedleToolState.cs` | Needle held/not-held state enum. |
| `Pustule.cs` | Pustule behavior, pierce state, squeeze/drain state, visual state, radius/jitter handling. |
| `PustuleSpawnAnchor.cs` | Needle spawn anchor rules, selected visual variants, and type randomization. |
| `PustuleType.cs` | Pustule type enum: Small or Big. |
| `NeedleMiniGameSetup.cs` | Editor setup tool for Needle mini game, sample pustules, anchors, overlay assignment. |

### Treatment / Tongs

| Script | Purpose |
|---|---|
| `Parasite.cs` | Parasite runtime behavior, pull progress, pain, extraction, visuals. |
| `ParasiteReveal.cs` | Parasite body reveal/mask helper for showing hidden body portions as the parasite is pulled. |
| `ParasiteSpawnAnchor.cs` | Tongs spawn anchor helper for spawn point, pull direction, wound mask, and required direction data. |
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
| `Assets/Core/Data/Treatment/Cases/Case_Test_Arm_Tongs.asset` | Focused test customer case requiring Arm/Tongs only. |
| `Assets/Core/Data/Treatment/Cases/Case_Test_MixedTreatment.asset` | Test customer case requiring Arm/Tongs and Torso/Knife. |
| `Assets/Core/Data/Treatment/BodyPrefabs/TreatmentBodyPrefabCatalog.asset` | Current catalog asset for mini game + body area body prefab lookup. |
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
| `Knife_Lesion_Spawn_Randomization_Technical_Design_Report.md` | Knife lesion type/orientation spawn randomization and Bulge visual-state design. |
| `Needle_MiniGame_Technical_Design_Report.md` | Needle mini game technical design. |
| `Needle_Pustule_Spawn_Visual_State_Technical_Design_Report.md` | Needle pustule spawn randomization and full/empty visual-state design. |
| `PatientTreatmentCaseSO_Design_Report.md` | Treatment Case ScriptableObject design. |
| `Treatment_Body_Prefab_Catalog_Technical_Design_Report.md` | Body prefab catalog design for mini-game-specific body prefabs. |
| `Parasite_Mask_Head_Reveal_Technical_Design_Report.md` | Parasite mask/head reveal design for Tongs. |
| `Candle_Interaction_Technical_Design_Report.md` | Candle refill interaction design. |
| `Customer_Spawn_Queue_System_Design_Report.md` | Multi-customer spawn queue design adapted to the current project. |
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
- Needle mini game core loop.
- Anchor-driven parasite / lesion / pustule spawning support.
- Knife Tumor/Bulge and Needle Small/Big visual-state support.
- Treatment Body Prefab Catalog runtime support.
- Main Menu scripts.

### Partially Complete / Needs Scene or Content Setup

- Candle refill interaction is not finalized.
- Real patient data / case library is not finalized.
- Final treatment room art is still placeholder / in-progress.
- Final parasite, lesion, pustule, wound, body, and tool art are not complete.
- Final treatment SFX/VFX are not complete.
- Head and Leg do not have unique final mini game content yet.
- Multi-customer queue flow has a design report but is not implemented yet.
- Anatomy and mini game UI need final art pass.
- Body prefab catalog content setup is still in progress.
- Selected spawn point visuals need final art/scale setup per body prefab.

### Placeholder / Future Work

- Transformation fail state.
- Full patient roster.
- Customer queue runtime, counter breather pacing, and case assignment beyond the current test setup.
- Case-specific rewards / difficulty / tuning.
- Final cut guide visual for Knife.
- Final UI art pass.
- Final balancing for Candle, Sanity, Tongs, Knife, and Needle.
- Save/settings polish beyond basic menu audio settings.

---

## 7. Current Workspace Notes

This report was updated from the current workspace state on July 3, 2026. It includes both committed work and local in-progress work.

Recent verification:

- `dotnet build Assembly-CSharp.csproj` passed after the latest mini game spawn randomization work.
- `dotnet build Assembly-CSharp-Editor.csproj` passed after the latest mini game setup tool updates.
- Latest relevant commit before this report update: `ae4d409 Add lesion and pustule spawn randomization`.

Notable current local / in-progress areas:

- `Assets/Core/Script/Treatment/Case/`
- `Assets/Core/Script/Treatment/BodyPrefab/`
- `Assets/Core/Script/Treatment/Knife/`
- `Assets/Core/Script/Treatment/Needle/`
- `Assets/Core/Script/Treatment/Tongs/`
- `Assets/Core/Script/Treatment/MiniGameOverlay.cs`
- `Assets/Core/Data/Treatment/Cases/Case_Test_MixedTreatment.asset`
- `Assets/Core/Data/Treatment/BodyPrefabs/TreatmentBodyPrefabCatalog.asset`
- `Assets/Core/Report/Knife_MiniGame_Technical_Design_Report.md`
- `Assets/Core/Report/Knife_Lesion_Spawn_Randomization_Technical_Design_Report.md`
- `Assets/Core/Report/Needle_MiniGame_Technical_Design_Report.md`
- `Assets/Core/Report/Needle_Pustule_Spawn_Visual_State_Technical_Design_Report.md`
- `Assets/Core/Report/Treatment_Body_Prefab_Catalog_Technical_Design_Report.md`
- `Assets/Core/Report/Customer_Spawn_Queue_System_Design_Report.md`
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
2. Finish and test body prefab setup:
   - Verify `Tongs_Arm_BodyPrefab` anchor visuals, parasite reveal masks, and spawn alignment.
   - Configure Knife body prefabs with `LesionSpawnAnchor` type/orientation rules and selected visuals.
   - Configure Needle body prefabs with `PustuleSpawnAnchor` type rules and selected visuals.
3. Playtest Knife and Needle random spawning:
   - Knife should select unique anchors, randomize Tumor/Bulge, randomize horizontal/vertical cut orientation, and show only selected visuals.
   - Needle should select unique anchors, randomize Small/Big pustules, and show only selected visuals.
4. Wire or verify final `TreatmentBodyPrefabCatalog` usage in the treatment flow.
5. Finish the Candle refill interaction at the Counter return state.
6. Create real patient case assets beyond `Case_Test_MixedTreatment`, including cases that route to Needle.
7. Implement the Customer Spawn Queue runtime from `Customer_Spawn_Queue_System_Design_Report.md` when the current single-customer loop is stable.
8. Replace Anatomy placeholder body parts with final sprites and verify hit testing.
9. Finalize shared MiniGameOverlay visuals, tool icons, and SFX.
10. Balance Sanity, Candle drain/refill, Tongs pain, Knife pain, and Needle pain.

---

## 9. Summary

The project now has a stronger vertical-slice foundation than the previous snapshot. The clinic loop, dialog system, sanity/candle pressure, room transition, Anatomy Selection, Tongs mini game, Knife mini game, Needle mini game, shared mini game HUD, Treatment Case ScriptableObject pipeline, and Treatment Body Prefab Catalog support are all implemented at prototype level. The current sample case can represent more than one treatment requirement and can route different body areas to different mini games.

The main remaining work is no longer basic architecture. The next phase should focus on playtesting the full multi-treatment case, completing body prefab setup, polishing Tongs/Knife/Needle feedback, finalizing Candle refill, creating real patient content, implementing the customer queue runtime, and replacing placeholder art with final pixel-art assets.
