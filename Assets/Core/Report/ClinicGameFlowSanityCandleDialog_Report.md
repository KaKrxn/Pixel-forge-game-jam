# Clinic Game Flow, Sanity, Candle Light, and Dialog System Report

## 1. Objective

Design the core gameplay flow for a 2D top-view clinic/shop game where customers enter the shop with hidden parasite symptoms. The player must talk to the customer, observe clues from the dialog, identify the correct treatment area, perform treatment events, and manage candle light to prevent the parasite from transforming the customer.

The target gameplay loop is:

1. A customer randomly spawns outside the shop on the back-most visual layer.
2. The customer walks behind the shop glass, making it look like they are approaching from outside.
3. When the shop door opens, the customer passes the doorway and changes to the inside-shop sorting layer.
4. The customer walks to the counter while rendering above the shop wall but below the counter.
5. The customer stands in front of the counter.
6. A speech bubble appears above the customer.
7. The player clicks or interacts with the bubble.
8. A dialog screen opens.
9. The customer describes what happened before arriving.
10. The player must infer the infected or injured body area from the story.
11. The dialog ends.
12. The game transitions from the counter room to a separate treatment room.
13. The player treats the customer based on visible symptoms and dialog clues.
14. If the customer has more than one infected area, the player treats each required point one by one.
15. Between treatment events, the player can press a return button to go back to the counter room, refill the candle, and then return to the treatment room.
16. When treatment succeeds, the customer thanks the player.
17. The customer leaves the shop using the reverse door and sorting-layer flow.

The main tension systems are:

- Patient Sanity: Indicates how close the customer is to parasite transformation.
- Candle Light: Suppresses the parasite and slows transformation.
- Treatment Room Separation: Hides the real candle state during treatment so the player must remember and estimate risk.
- Treatment Lock: The player cannot leave in the middle of a treatment event.
- Dialog Clues: The player must understand the story instead of being directly told the answer.
- Shop Entrance Layering: Uses visual sorting changes to sell the illusion that customers enter through the glass door and leave the same way.

## 2. Current Design Analysis

The proposed game flow has four major gameplay pillars:

### 2.1 Customer Flow

The customer is not just a static NPC. They move through a full lifecycle:

- Randomly spawn outside or near the shop entrance.
- Walk behind the shop glass on the back-most visual layer.
- Switch visual sorting at the door when entering the shop.
- Walk to the counter above the wall layer but below the counter layer.
- Wait for player interaction.
- Talk through dialog.
- Move into treatment.
- React to treatment success or failure.
- Exit the shop by reversing the door sorting flow.

This means each customer should be controlled by a clear state machine instead of scattered scene logic.

### 2.2 Sanity Pressure

Sanity is the countdown pressure for each customer. It tells the player how close the customer is to losing control and transforming.

Important behavior:

- Sanity changes over time while the customer is inside the shop.
- If sanity reaches the danger threshold, the customer may transform.
- Candle light affects how quickly sanity rises.
- When candle light is strong, parasite growth is suppressed.
- When candle light is out, sanity rises much faster.

For clarity, this report treats Sanity as a danger meter:

- `0` means stable.
- `100` means transformation.
- Higher value means more danger.

### 2.3 Candle Light Pressure

Candle Light is the core shop defense system. It is not decorative. It controls the parasite pressure in the room.

Important behavior:

- Candle light slowly decreases over time.
- The player must refill the candle with magic.
- If the candle goes out, all customers inside the shop become more dangerous.
- When the candle is out, customer sanity rises much faster.
- The player must decide when to stop other activities and return to the candle.

This creates time management:

- Continue treatment and risk transformation.
- Refill the candle before starting a long treatment event.
- Finish the current locked treatment event before being allowed to leave.
- Return from the treatment room to the counter room between treatment events if the player thinks the candle may be close to going out.

### 2.4 Treatment Room Separation

Treatment should not happen as a simple overlay on top of the counter room. It should feel like the player and customer have moved into a separate treatment room.

Purpose:

- The player cannot directly see the candle while treating the customer.
- The player must remember the candle condition from before entering treatment.
- Candle light still drains while the player is in the treatment room.
- Sanity still reacts to the candle state even when the candle is off-screen.
- The player can only return to the counter room after the current treatment event is complete.
- Returning to the counter room is a deliberate action, not an automatic interruption.

This makes the candle a memory and risk-management system instead of a simple meter the player watches at all times.

### 2.5 Shop Entrance and Exit Layering

Customer entrance should use visual layer changes to create the illusion that the customer is outside the shop, walking behind the glass, then entering through the door.

In Unity 2D, this should primarily use `SpriteRenderer.sortingLayerName` and `SpriteRenderer.sortingOrder`. It should not rely only on the GameObject physics layer unless collision filtering is also needed.

Recommended visual order:

```text
OutsideBack        - customer appears behind the shop glass and walls.
ShopWall           - wall and glass visuals.
CustomerInside     - customer appears inside the shop after passing the door.
Counter            - counter renders above the customer.
ForegroundProps    - any top foreground decoration.
UI                 - speech bubble and UI.
```

Entrance flow:

1. Pick a random outside spawn point.
2. Spawn the customer on the `OutsideBack` sorting layer.
3. Move the customer along the outside path so they pass behind the shop glass.
4. Open or animate the door when the customer reaches the door trigger.
5. At the door threshold, switch the customer's renderer to the `CustomerInside` sorting layer.
6. Continue the path into the shop toward the counter.
7. Keep the customer above the wall but below the counter.
8. When the customer reaches the counter point, show the speech bubble and continue the normal game flow.

Exit flow:

1. After treatment succeeds, the customer thanks the player.
2. The customer walks from the counter toward the door.
3. The door opens.
4. At the door threshold, switch the customer from `CustomerInside` back to `OutsideBack`.
5. The customer walks behind the glass and exits toward an outside despawn point.
6. Despawn the customer and allow the next customer to enter.

This makes the entrance and exit feel physical without requiring a separate cutscene.

### 2.6 Dialog as Investigation

Dialog is the clue delivery system.

The customer should not say "heal my arm" directly every time. Instead, they describe an event:

- Where they went.
- What they touched.
- What they felt.
- When the pain started.
- What symptom appeared first.

The player must infer the correct treatment target from the story.

Example:

- Customer says they picked up a glowing root, then felt crawling under the left wrist.
- The correct treatment area is likely the left arm or hand.

This turns dialog into gameplay instead of simple story text.

## 3. Recommended System Architecture

Recommended folder layout:

```text
Assets/
  Core/
    Script/
      GameFlow/
        GameFlow.cs
        CustomerQueue.cs
      Customer/
        CustomerAgent.cs
        Sanity.cs
        CustomerData.cs
        CustomerState.cs
        CustomerLayer.cs
      Shop/
        Door.cs
        ShopLayers.cs
      Dialog/
        Dialog.cs
        DialogData.cs
        DialogChoice.cs
        Bubble.cs
      Candle/
        Candle.cs
        CandleUse.cs
      Treatment/
        Treatment.cs
        TreatmentCase.cs
        TreatmentPoint.cs
        TreatmentEvent.cs
        TreatmentLock.cs
        RoomTransition.cs
      Interaction/
        PlayerInteract.cs
        Interactable.cs
      Camera/
        CameraSway.cs
        MouseParallax.cs
      Parallax/
        ParallaxLayer.cs
        ParallaxRig.cs
      VFX/
        RoomAtmosphere.cs
    Data/
      Customers/
      Dialog/
      TreatmentCases/
      Parallax/
    Prefab/
      Customer/
      UI/
      Treatment/
      Parallax/
      VFX/
    Texture/
      Parallax/
    Material/
      VFX/
    Scene/
    Report/
      ClinicGameFlowSanityCandleDialog_Report.md
```

### 3.1 Script Category Rule

When implementing the game systems, every script must be placed inside a clear category folder under:

```text
Assets/Core/Script
```

Do not place new gameplay scripts directly in `Assets/Core/Script`. Create or use a category folder based on the script's job first.

Required script categories:

```text
Assets/Core/Script/GameFlow
Assets/Core/Script/Customer
Assets/Core/Script/Shop
Assets/Core/Script/Dialog
Assets/Core/Script/Candle
Assets/Core/Script/Treatment
Assets/Core/Script/Interaction
Assets/Core/Script/Camera
Assets/Core/Script/Parallax
Assets/Core/Script/VFX
```

Category rules:

- `GameFlow`: overall game state, customer queue, and loop coordination.
- `Customer`: customer movement, sanity, customer data, and customer visual layer changes.
- `Shop`: door behavior, shop sorting layers, counter room objects, and entrance/exit visuals.
- `Dialog`: dialog UI, dialog data, choices, and speech bubbles.
- `Candle`: candle light, candle refill, magic cost, and candle state events.
- `Treatment`: treatment room, treatment cases, treatment points, treatment locks, and room transitions.
- `Interaction`: player interaction checks, interact prompts, and shared interaction interfaces.
- `Camera`: orthographic camera support, pixel-perfect setup helpers, camera sway, and mouse-driven camera offset.
- `Parallax`: layered room depth, parallax layer movement, and shared parallax rig coordination.
- `VFX`: non-gameplay atmosphere effects such as fog, dust, candle flicker visuals, overlays, and room mood effects.

If a script belongs to more than one category, place it in the category that owns the main responsibility. For example, a script that opens the treatment room door but mainly controls treatment flow should stay in `Treatment`, not `Shop`.

## 4. Main Gameplay State Machine

The whole shop flow should be managed by a main game state.

Recommended states:

```text
ShopIdle
CustomerSpawnedOutside
CustomerBehindGlass
CustomerDoorTransitionIn
CustomerEntering
CustomerWaitingAtCounter
DialogActive
TreatmentPreparation
TransitionToTreatmentRoom
TreatmentActive
TreatmentEventLocked
TreatmentReturnAvailable
TransitionToCounterRoom
CandleRefillActive
CustomerResolved
CustomerDoorTransitionOut
CustomerTransforming
CustomerLeaving
```

### 4.1 State Descriptions

`ShopIdle`

- No active customer is at the counter.
- The game can spawn or queue the next customer.

`CustomerSpawnedOutside`

- A customer is randomly spawned at one of the outside spawn points.
- The customer's renderer is placed on the back-most outside sorting layer.
- The customer begins walking toward the shop glass or door path.

`CustomerBehindGlass`

- The customer walks behind the shop glass.
- The customer should render behind glass and wall visuals.
- This creates the feeling that the customer is outside the shop.

`CustomerDoorTransitionIn`

- The customer reaches the door threshold.
- The door opens.
- The customer's visual sorting changes from outside/back sorting to inside-shop sorting.
- After the sorting change, the customer can appear above the wall but below the counter.

`CustomerEntering`

- A customer walks from the entrance to the counter.
- The player can move around, but the customer is not ready for dialog yet.

`CustomerWaitingAtCounter`

- Customer reaches the counter.
- Speech bubble appears.
- Player can interact with the bubble.

`DialogActive`

- Dialog UI is open.
- Player reads the story and clue text.
- Customer sanity may continue rising unless the design chooses to pause it.
- Recommended: keep sanity active for tension, but at a slower dialog rate.

`TreatmentPreparation`

- Dialog is complete.
- Treatment case is known internally by the game.
- Player is prepared to move from the counter room into the treatment room.

`TransitionToTreatmentRoom`

- The player and active customer move into the treatment room.
- Counter room visuals and candle visibility are hidden or disabled.
- Treatment UI becomes active after the transition.

`TreatmentActive`

- Player can inspect the customer and choose treatment actions inside the treatment room.
- Candle light continues to drain in the counter room, but the player cannot directly see it.
- Sanity continues to rise.
- If the case has multiple treatment points, only incomplete points remain selectable.

`TreatmentEventLocked`

- Player started a specific treatment event, such as pulling parasite fragments out.
- Player cannot leave until the event ends.
- If the candle goes out during this time, sanity increases faster, but the player will not directly see the candle go out.
- The player must finish the current treatment event first.

`TreatmentReturnAvailable`

- The current treatment event is complete.
- The player is still in the treatment room.
- If more treatment points remain, the player can either continue treatment or press a return button to go back to the counter room.
- Returning allows the player to check and refill the candle before continuing.

`TransitionToCounterRoom`

- The treatment UI closes or fades out.
- The player returns to the counter room.
- The candle becomes visible again.
- The player may refill the candle, then return to the treatment room to continue unfinished treatment points.

`CandleRefillActive`

- Player is interacting with the candle.
- Magic is being restored into the candle.
- Candle level rises until full or until the player stops.

`CustomerResolved`

- Treatment is complete.
- Customer is safe.
- Customer thanks the player.

`CustomerDoorTransitionOut`

- The cured customer walks from the counter to the door.
- The door opens.
- The customer's visual sorting changes back from inside-shop sorting to outside/back sorting.
- This mirrors the entrance setup in reverse.

`CustomerTransforming`

- Sanity reached transformation threshold.
- Customer changes into a dangerous form or triggers a failure event.

`CustomerLeaving`

- Customer exits the shop.
- Queue advances to the next customer.

## 5. Customer State Machine

Each customer should have their own state.

Recommended customer states:

```text
None
SpawnedOutside
WalkingBehindGlass
EnteringThroughDoor
EnteringShop
WalkingToCounter
WaitingForDialog
InDialog
WaitingForTreatment
MovingToTreatmentRoom
InTreatment
WaitingBetweenTreatmentEvents
ReturningToDoor
LeavingThroughDoor
WalkingBehindGlassToExit
Transforming
Cured
LeavingShop
Exited
```

This allows the game to support multiple customers later if needed.

## 6. Patient Sanity System

### 6.1 Purpose

Sanity controls the parasite transformation risk.

It should communicate urgency to the player through:

- UI meter.
- Character animation.
- Sound effects.
- Visual effects.
- Dialog tone changes.
- Warning flashes near high danger.

### 6.2 Recommended Values

Each customer should have these values:

```text
currentSanity
maxSanity
baseSanityIncreaseRate
candleProtectedIncreaseRate
candleOutIncreaseRate
treatmentStressIncreaseRate
transformationThreshold
warningThreshold
criticalThreshold
```

Recommended starting values:

```text
currentSanity = 0
maxSanity = 100
baseSanityIncreaseRate = 1 per second
candleProtectedIncreaseRate = 0.35 per second
candleOutIncreaseRate = 5 per second
treatmentStressIncreaseRate = 1.5 per second
warningThreshold = 50
criticalThreshold = 80
transformationThreshold = 100
```

These numbers should be tuned after playtesting.

### 6.3 Sanity Rate Logic

The sanity increase rate should depend on the current situation.

Recommended priority:

1. If customer is cured, sanity stops.
2. If customer is transforming, sanity no longer matters.
3. If candle is out, use a high danger rate.
4. If treatment is active, add treatment stress.
5. If candle is lit, use protected rate.

Example logic:

```csharp
float rate = candle.IsLit
    ? candleProtectedIncreaseRate
    : candleOutIncreaseRate;

if (treatment.IsTreatmentActive)
{
    rate += treatmentStressIncreaseRate;
}

currentSanity += rate * Time.deltaTime;
```

### 6.4 Sanity Milestones

`0 - 49`

- Customer is uncomfortable but controlled.
- Light visual parasite effects.

`50 - 79`

- Warning state.
- Customer breathing changes.
- UI starts warning the player.

`80 - 99`

- Critical state.
- Strong parasite visual effects.
- Audio warning.
- Customer may twitch or speak distorted lines.

`100`

- Transformation triggers.
- Treatment fails or enters emergency mode.

## 7. Candle Light System

### 7.1 Purpose

Candle Light is the main environmental resource. It prevents the parasite from transforming customers too quickly.

It should make the player think:

- Can I finish this treatment before the candle goes out?
- Should I refill before starting a risky event?
- Can I survive the candle being out during a locked treatment action?
- Do I remember enough about the candle state to continue treatment without returning to check?

### 7.2 Recommended Values

```text
currentLight
maxLight
drainRate
refillRate
lowLightThreshold
extinguishedThreshold
magicCostPerSecond
```

Recommended starting values:

```text
currentLight = 100
maxLight = 100
drainRate = 2 per second
refillRate = 30 per second
lowLightThreshold = 30
extinguishedThreshold = 0
magicCostPerSecond = 10
```

### 7.3 Candle States

```text
Bright
Low
Flickering
Extinguished
Refilling
```

`Bright`

- Safe state.
- Sanity rises slowly.

`Low`

- Warning state.
- UI should show candle danger.
- Audio/visual effects should suggest urgency.

`Flickering`

- Very low light.
- The player has only a short time before danger spikes.

`Extinguished`

- Candle is out.
- Customer sanity rises much faster.
- The room should look visually unsafe.

`Refilling`

- Player channels magic into the candle.
- Candle light rises.

### 7.4 Candle Interaction

The candle exists in the counter room, not in the treatment room. The player should only be able to interact with the candle after returning to the counter room.

Rules:

- If the player is in the counter room and free, they can go to the candle and refill it.
- If the player is in dialog, candle interaction is blocked.
- If the player is in the treatment room, the candle is not visible and cannot be interacted with.
- If the player is in a locked treatment event, returning to the counter room is blocked.
- If the player finishes the current treatment event and more treatment points remain, a return button becomes available.
- Pressing the return button moves the player back to the counter room, where they can check and refill the candle.
- After refilling, the player can return to the treatment room and continue the remaining treatment points.

### 7.5 Candle Feedback

Recommended feedback:

- Light radius shrinks as candle decreases.
- Room color becomes darker or more hostile.
- Candle flame animation becomes unstable.
- In the counter room, the UI meter can show the candle state clearly.
- In the treatment room, the candle meter should be hidden, disabled, or shown only as vague indirect feedback.
- Sound becomes muffled or distorted when extinguished.
- If the candle goes out while the player is in the treatment room, the player may hear a distant audio cue or see parasite pressure increase, but should not get a perfect candle readout.

## 8. Dialog System

### 8.1 Purpose

Dialog gives the player clues. It should not always directly reveal the correct treatment.

The player must observe:

- Location mentioned in the story.
- Object or creature encountered.
- Body sensation.
- Time of symptom onset.
- Emotional state.
- Repeated words.
- Contradictions or gaps.

### 8.2 Dialog Flow

Recommended flow:

```text
Speech bubble appears
Player interacts
Dialog UI opens
Customer introduction line
Customer event story
Symptom clue lines
Optional player question
Final clue line
Dialog closes
Transition to treatment room starts
Treatment starts in the separate treatment room
```

### 8.3 Speech Bubble

Speech bubble behavior:

- Appears only when customer is waiting at counter.
- Can be clicked by mouse or selected by player interaction.
- Should pulse lightly to attract attention.
- Hides when dialog starts.
- Does not appear during treatment.

### 8.4 Dialog Data Structure

Recommended `DialogData` fields:

```text
dialogId
customerName
openingLines
storyLines
clueLines
optionalQuestionLines
closingLines
linkedTreatmentCaseId
```

### 8.5 Example Dialog Case

Case: parasite fragment in left hand.

Customer story:

```text
I was closing the old storage room behind the market.
There was a cracked jar on the floor, still warm for some reason.
I picked it up with my left hand.
Something inside it moved, like a thorn crawling under my skin.
Now my fingers feel cold, but my wrist burns whenever I hear the candle flame.
```

Player inference:

- Object touched: cracked jar.
- Contact point: left hand.
- Main symptom: wrist burn and cold fingers.
- Treatment target: left hand or wrist.

## 9. Treatment System

### 9.1 Purpose

Treatment is the action phase after dialog.

The player must:

- Inspect symptoms.
- Choose the correct area.
- Perform treatment actions.
- Complete timed or precision events.
- Manage candle risk between actions.

### 9.2 Treatment Case Data

Recommended fields:

```text
caseId
displayName
targetBodyAreas
visibleSymptoms
dialogClues
requiredTools
requiredTreatmentEvents
requiredEventsPerBodyArea
sanityStressModifier
successDialog
failureDialog
```

### 9.3 Treatment Flow

```text
Enter treatment screen
Move from counter room to treatment room
Show patient body or symptom view
Player inspects body areas
Player selects suspected treatment area
Game validates selected area
Player performs treatment event
Treatment event locks the player temporarily
Event succeeds or fails
Sanity changes based on result
If more treatment points remain, player chooses to continue or return to counter room
If returning, player can refill candle and then re-enter treatment room
Repeat until all required treatment points are complete
Customer is cured or transforms
```

### 9.4 Treatment Lock Rule

This is one of the most important design rules.

If the player starts a treatment event, they cannot leave halfway.

Example:

- Player starts removing parasite fragments.
- Candle goes out during the event.
- Player cannot cancel immediately.
- Sanity begins rising much faster because the candle is out.
- Player must finish the fragment removal event.
- After the event ends, the return button becomes available.
- Player presses the return button to go back to the counter room.
- Player checks and refills the candle.
- Player returns to the treatment room to continue the remaining treatment points.

This creates meaningful tension without feeling unfair, as long as the player receives enough warning before starting the event.

### 9.5 Multiple Treatment Points

A single customer can have more than one area that must be treated.

Example:

```text
Treatment case: Root Parasite Infection
Required treatment points:
- Left wrist: remove parasite fragment.
- Neck: clean spreading parasite veins.
- Chest: apply magical seal.
```

Rules:

- The treatment case is complete only when all required treatment points are cured.
- Each treatment point can have its own event type, duration, and failure penalty.
- The player can return to the counter room between completed treatment events.
- The player cannot return while a treatment event is currently locked.
- Wrong body area selections may increase sanity or waste time.
- Returning too often is safer for candle management but costs time.

### 9.6 Treatment Event Types

Possible event types:

- Pull parasite fragment.
- Clean infected wound.
- Burn parasite root.
- Apply magical seal.
- Cut corrupted tissue.
- Stabilize heartbeat or breathing rhythm.
- Match symbol pattern.
- Hold cursor steady while parasite moves.

Each event should have:

```text
duration
difficulty
canCancel
sanityPenaltyOnFail
sanityRewardOnSuccess
requiredTool
```

For the described design, most critical events should have `canCancel = false`.

## 10. Full Game Flow Implementation Plan

### Phase 1: Build Core Game Flow

Create `GameFlow`.

Responsibilities:

- Track the current game state.
- Start the customer flow.
- Listen for dialog completion.
- Start treatment.
- Resolve success or transformation.

Initial methods:

```text
StartNextCustomer()
BeginDialog(CustomerAgent customer)
EndDialog()
BeginTreatment(CustomerAgent customer)
TransitionToTreatmentRoom()
RequestReturnToCounter()
TransitionToCounterRoom()
ReturnToTreatmentRoom()
CompleteTreatment()
TriggerTransformation()
CustomerLeave()
```

### Phase 2: Build Customer Agent

Create `CustomerAgent`.

Responsibilities:

- Randomly choose an outside spawn point.
- Move customer behind the shop glass.
- Trigger the door entrance transition.
- Move customer to the counter.
- Show or hide speech bubble.
- Hold reference to customer data.
- Connect to sanity component.
- Play enter, wait, talk, cured, transform, and leave states.
- Reverse the entrance flow after treatment success so the customer exits through the same door illusion.

Required references:

```text
CustomerData
Sanity
Bubble
CustomerLayer
Door
Transform[] outsideSpawnPoints
Transform outsideDoorPoint
Transform insideDoorPoint
Transform counterPoint
Transform exitPoint
```

### Phase 2.5: Build Shop Entrance Layering

Create `CustomerLayer`.

Responsibilities:

- Set the customer's visual sorting layer while outside.
- Set the customer's visual sorting layer after entering the shop.
- Keep the customer below the counter while inside.
- Restore outside sorting when the customer leaves.

Create `Door`.

Responsibilities:

- Open the door when the customer reaches the outside door point.
- Notify the customer when the sorting-layer switch should happen.
- Close the door after the customer passes through.
- Reuse the same behavior in reverse when the customer leaves.

Recommended methods:

```text
SetOutsideLayer()
SetInsideLayer()
OpenDoor()
CloseDoor()
PlayEnterDoorSequence()
PlayExitDoorSequence()
```

### Phase 3: Build Sanity System

Create `Sanity`.

Responsibilities:

- Store current sanity.
- Increase sanity over time.
- Read candle state.
- Read treatment state.
- Trigger warning and critical events.
- Trigger transformation at threshold.

Important events:

```text
OnSanityChanged
OnWarningReached
OnCriticalReached
OnTransformationReached
```

### Phase 4: Build Candle Light System

Create `Candle`.

Responsibilities:

- Drain candle light over time.
- Refill when player channels magic.
- Broadcast candle state changes.
- Tell sanity systems whether the candle is lit.

Important events:

```text
OnLightChanged
OnLowLight
OnExtinguished
OnRelit
```

### Phase 5: Build Dialog System

Create `Dialog`.

Responsibilities:

- Open and close dialog UI.
- Display lines one by one.
- Read from `DialogData`.
- Send event when dialog completes.
- Hide speech bubble during dialog.

Important events:

```text
OnDialogStarted
OnLineChanged
OnDialogCompleted
```

### Phase 6: Build Treatment System

Create `Treatment`.

Responsibilities:

- Transition into the treatment room after dialog.
- Open treatment UI only after the room transition completes.
- Load the correct treatment case.
- Let the player choose treatment areas.
- Track multiple required treatment points.
- Start treatment events.
- Lock or unlock player actions.
- Enable the return button only between treatment events.
- Let the player return to the counter room to refill the candle, then resume the same unfinished treatment case.
- Report success or failure.

Important events:

```text
OnTreatmentStarted
OnTreatmentEventStarted
OnTreatmentEventCompleted
OnReturnToCounterAvailable
OnReturnedToCounter
OnTreatmentResumed
OnTreatmentCompleted
OnTreatmentFailed
```

### Phase 7: Connect UI

Recommended UI:

- Customer sanity meter.
- Candle light meter in the counter room.
- Speech bubble prompt.
- Dialog box.
- Treatment room screen.
- Return to counter button, visible only between treatment events.
- Warning overlay when candle is low.
- Transformation warning when sanity is critical.

### Phase 8: Tune and Test

Test these situations:

- Customer randomly spawns at an outside spawn point.
- Customer appears behind the shop glass before entering.
- Customer sorting changes at the door threshold.
- Customer renders above the shop wall but below the counter after entering.
- Customer enters and stops at counter.
- Speech bubble appears only at the correct time.
- Dialog opens when player interacts.
- Dialog gives enough clue to identify treatment area.
- Treatment room transition starts after dialog.
- Candle meter is not directly visible in the treatment room.
- Treatment can contain more than one required treatment point.
- Sanity increases over time.
- Candle drain affects sanity speed.
- Candle out makes sanity rise much faster.
- Player cannot leave during locked treatment event.
- Player can press a return button between treatment events to go back to the counter room and refill the candle.
- Player can return to treatment after refilling and continue unfinished treatment points.
- Customer thanks player and leaves after successful treatment.
- Customer exit reverses the entrance sorting sequence.
- Customer appears behind the shop glass again when leaving.
- Customer transforms if sanity reaches maximum.

## 11. Example Runtime Sequence

This is the expected player experience:

```text
Customer randomly spawns outside the shop on the back-most layer.
Customer walks behind the shop glass.
Door opens when the customer reaches the entrance.
Customer sorting changes to the inside-shop layer at the door threshold.
Customer walks above the wall layer but below the counter layer.
Customer stops.
Speech bubble appears.
Player clicks the bubble.
Dialog UI opens.
Customer tells a story about an incident.
Player identifies the likely infected area from clues.
Dialog ends.
Room transition plays.
Treatment room opens.
Player inspects the customer.
Player selects the suspected area.
Player starts a parasite removal event.
Treatment lock begins.
Candle light continues draining in the counter room, but the player cannot see it directly.
If candle goes out, sanity rises faster and parasite pressure increases.
Player finishes the current event.
Treatment lock ends.
Return button appears because more treatment points remain.
Player chooses whether to continue or return to the counter room.
Player returns to the counter room.
Player checks and refills the candle.
Player goes back into the treatment room.
Player completes the remaining treatment points.
Customer thanks player.
Customer walks back to the door.
Door opens.
Customer sorting changes back to the outside/back layer.
Customer walks behind the shop glass toward the exit point.
Customer exits shop and despawns.
Next customer can enter.
```

## 12. Data-Driven Case Design

The game should use ScriptableObjects for customer and treatment content.

Recommended assets:

```text
CustomerData.asset
DialogData.asset
TreatmentCase.asset
TreatmentPoint.asset
```

Benefits:

- Designers can create new cases without changing code.
- Dialog clues and treatment requirements stay linked.
- Balancing sanity rates per customer becomes easier.
- The same system can support many customers.

### 12.1 CustomerData

Suggested fields:

```text
customerId
customerName
portrait
prefab
dialogData
treatmentCase
initialSanity
sanityIncreaseMultiplier
walkSpeed
thankYouLine
transformationPrefab
outsideSpawnGroup
outsideDoorPoint
insideDoorPoint
counterPoint
exitPoint
outsideSortingLayer
insideSortingLayer
insideSortingOrder
```

### 12.2 TreatmentCase

Suggested fields:

```text
caseId
targetBodyAreas
wrongAreaPenalty
requiredEventIds
requiredEventsPerBodyArea
visibleSymptomSprites
toolRequirements
successSanityReduction
failureSanityIncrease
```

### 12.3 TreatmentPoint

Suggested fields:

```text
pointId
bodyArea
visibleSymptom
requiredTreatmentEvent
isCompleted
sanityPenaltyOnFail
sanityRewardOnSuccess
estimatedDuration
```

### 12.4 DialogData

Suggested fields:

```text
dialogId
lines
speakerNames
linePortraits
clueTags
linkedCaseId
```

## 13. Player Decision Points

The design should create repeated small decisions:

- Talk now or refill candle first?
- Trust the first dialog clue or inspect more?
- Start a long treatment event while candle is low?
- Return from the treatment room between events to refill?
- Continue treating without seeing the candle, based only on memory and risk judgment?
- Risk a wrong treatment area for speed?

These decisions support the horror-clinic tension of the game.

## 14. Failure and Recovery Design

### 14.1 Soft Failure

Soft failure should punish but not immediately end the run.

Examples:

- Wrong treatment area increases sanity.
- Failed treatment event damages candle stability.
- Customer becomes harder to treat.
- Dialog clue becomes distorted at high sanity.

### 14.2 Hard Failure

Hard failure occurs when sanity reaches transformation.

Possible results:

- Customer transforms and attacks.
- Customer escapes the shop.
- Shop reputation decreases.
- Current treatment case fails.
- Player must survive an emergency event.

The exact punishment depends on the intended game genre.

## 15. Implementation Risks

### 15.1 Too Many Systems at Once

The full design includes customer AI, dialog, sanity, candle, treatment, UI, and game flow. Build it in small vertical slices.

Recommended first playable slice:

```text
One customer
One dialog
One treatment case
One candle
One sanity meter
One locked treatment event
One success ending
One transformation failure
```

### 15.2 Hidden Candle May Feel Unfair

If the candle can go out while the player is in a separate treatment room, the player needs enough information before committing to treatment.

Solutions:

- Show estimated event duration.
- Show candle meter clearly only in the counter room.
- Warn when candle is low before entering the treatment room.
- Add indirect audio or parasite pressure cues in the treatment room.
- Make the return button clearly available between treatment events.
- Let early treatment events be short.

### 15.3 Dialog Clues May Be Too Vague

If the player cannot infer the treatment area, the system may feel random.

Solutions:

- Include at least three clue types:
  - Contact object.
  - Body sensation.
  - Location or gesture.
- Add optional player questions.
- Add visual symptoms that confirm the dialog.

### 15.4 Sanity Needs Clear Feedback

If sanity rises silently, the player will not understand failure.

Solutions:

- Use meter changes.
- Add patient animation changes.
- Add audio warnings.
- Add candle color shifts.
- Add distorted dialog or breathing.

## 16. Recommended Development Order

1. Create script category folders under `Assets/Core/Script` before writing gameplay scripts.
2. Create one test scene for the clinic loop.
3. Create a placeholder Player and movement.
4. Create a placeholder Customer prefab.
5. Add outside spawn points and an outside-to-door path.
6. Add shop visual sorting layers for outside, wall, customer-inside, counter, and UI.
7. Add Customer movement behind the glass.
8. Add door threshold sorting change.
9. Add Customer movement from inside door point to counter.
10. Add Speech Bubble interaction.
11. Add Dialog UI with one hardcoded test dialog.
12. Convert dialog to `DialogData`.
13. Add room transition from counter room to treatment room.
14. Add Treatment UI with one body area.
15. Add one locked treatment event.
16. Add return-to-counter button after the locked event.
17. Add Candle Light drain and refill in the counter room.
18. Hide direct candle status while in the treatment room.
19. Add Customer Sanity meter.
20. Connect Candle Light to Sanity rate.
21. Add transformation event at max sanity.
22. Add a second treatment point to prove the return/resume loop.
23. Add success flow where the customer thanks the player and exits.
24. Reverse the entrance sorting sequence when the customer leaves.
25. Convert the full flow to data-driven customer cases.

## 17. Acceptance Criteria

The system is working when:

- A customer randomly spawns outside the shop.
- The customer initially renders behind the glass/wall visuals.
- The customer changes sorting layer at the door when entering.
- The customer renders above the wall but below the counter after entering.
- A customer can enter the shop and walk to the counter.
- A speech bubble appears at the counter.
- Clicking the bubble opens dialog.
- Dialog provides clues about the treatment target.
- Dialog completion transitions the player and customer into a separate treatment room.
- Player can perform treatment events.
- Treatment events can temporarily lock player exit.
- The player cannot directly see the candle state while in the treatment room.
- Candle light drains over time.
- Player can return to the counter room between treatment events.
- Player can refill the candle only after returning to the counter room.
- Player can resume the same treatment case after refilling.
- A customer can require more than one treatment point.
- Candle outage increases sanity speed.
- Sanity reaches warning, critical, and transformation states.
- Successful treatment makes the customer thank the player.
- The customer exits after being cured.
- The exit uses the reverse sorting flow: inside-shop layer to outside/back layer at the door.

## 18. Current Direction Update: Pixel Art Parallax Room

This project should use the GDD as an in-progress direction document, not as a final locked specification. The current stable design direction is a fantasy horror clinic game where the player treats parasite-infected patients, reads dialog clues, manages sanity pressure, and protects the shop with candle light.

The latest visual direction adds a pixel-art layered room setup inspired by a 2D parallax diorama. The goal is not to turn the game into a side-scrolling parallax scene. The goal is to make the shop and treatment room feel deeper, more atmospheric, and more alive while staying readable as a 2D pixel-art clinic.

### 18.1 Camera Direction

Use an Orthographic camera for the main game camera.

Recommended setup:

```text
Projection: Orthographic
Position Z: -10
Rotation: 0, 0, 0
Pixel Perfect Camera: Enabled
Assets Pixels Per Unit: Match the sprite import setting, usually 16 or 32
Reference Resolution: 320x180 or 640x360
Sprite Filter Mode: Point
Sprite Compression: None or low enough to avoid pixel artifacts
```

Do not use a Perspective camera for the first playable version. Perspective can make pixel sprites scale unevenly, complicate sorting, and fight the top-view / room-diorama presentation.

### 18.2 Camera Movement Direction

Camera movement should stay subtle because this is a pixel-art game.

Use three separate concepts:

```text
CameraSway      - automatic small camera drift for atmosphere.
MouseParallax   - small camera or layer offset based on mouse position.
ParallaxLayer   - each visual layer reacts to camera movement with different strength.
```

Recommended first implementation:

1. Start with `MouseParallax` because the player can feel the depth immediately by moving the mouse.
2. Add `ParallaxLayer` to make background, midground, and foreground move at different strengths.
3. Add `CameraSway` only after the pixel snapping feels stable.

For pixel art, all camera and parallax movement should be very small and should avoid visible sub-pixel blur. If movement creates shimmering, snap the camera or layer positions to the pixel grid.

### 18.3 Parallax Sorting Layers

The project should add dedicated sorting layers for room depth:

```text
BG_Far
BG_Mid
Main
Foreground
VFX
UI
```

Recommended purpose:

| Sorting Layer | Purpose |
|---|---|
| `BG_Far` | Back wall, window silhouettes, far room shadows, exterior shapes behind glass. |
| `BG_Mid` | Cabinets, shelves, wall props, back furniture, hanging tools. |
| `Main` | Treatment table, candle, main gameplay objects, patient body view. |
| `Foreground` | Curtains, close props, counter edge overlays, near silhouettes. |
| `VFX` | Fog, dust, candle glow, parasite particles, screen atmosphere effects. |
| `UI` | Dialog, speech bubbles, meters, treatment buttons, prompts. |

The existing customer entrance sorting flow should still work, but it should be mapped into this deeper visual structure. Customers outside the shop should render behind the wall/glass layer, customers inside should render above wall/background elements, and the counter should still render above the customer.

### 18.4 Recommended Room Hierarchy

Use a clear hierarchy for both the counter room and treatment room:

```text
RoomRoot
|-- 00_Background
|-- 01_Midground
|-- 02_MainArea
|-- 03_Gameplay
|-- 04_Foreground
|-- 05_VFX
|-- UI
`-- Main Camera
```

The treatment room should be a real room transition, not only a UI overlay. This supports the candle design because the player should not directly see the candle state while treating the patient.

### 18.5 Updated Next Implementation Sequence

The next work should be implemented in small testable steps:

1. Add script category folders for `Camera`, `Parallax`, and `VFX`.
2. Add the new sorting layers for layered pixel-art room depth.
3. Add `ParallaxLayer` and test it with three placeholder sprite layers.
4. Add `MouseParallax` with very low strength and pixel-safe movement.
5. Add `CameraSway` only after the mouse parallax and pixel snapping are stable.
6. Set the main camera to Orthographic and add Pixel Perfect Camera in Unity.
7. Build a placeholder layered room hierarchy for the counter room.
8. Build a placeholder layered room hierarchy for the treatment room.
9. Keep the current customer entrance/dialog/exit test flow working while the visual layer system is added.
10. After the parallax room is stable, continue with `PatientData`, `TreatmentData`, `Sanity`, `Candle`, and the treatment room flow.

### 18.6 Updated Acceptance Criteria for Parallax

The parallax update is working when:

- The main camera is Orthographic.
- Pixel Perfect Camera is active and configured for the project's sprite pixel density.
- Background, midground, main, foreground, VFX, and UI elements render in the correct visual order.
- Moving the mouse creates a subtle depth effect without making pixel art blurry.
- Camera sway, if enabled, does not create obvious sub-pixel shimmer.
- The customer entrance layer change still works after adding the new room sorting layers.
- The treatment room can use the same layered depth structure without showing the counter-room candle directly.

## 19. Final Recommendation

Build the game flow as a connected set of small systems:

- `GameFlow` controls the overall state.
- `CustomerAgent` controls each customer's lifecycle, including outside spawn, door entry, counter wait, and reverse exit.
- `CustomerLayer` controls the customer's sorting layer changes while entering and leaving the shop.
- `Door` controls door open/close timing for entry and exit.
- `Sanity` controls transformation pressure.
- `Candle` controls the shop safety resource.
- `Dialog` delivers story clues.
- `Treatment` runs the separate treatment room, multiple treatment points, locked events, and return-to-counter loop.
- `CameraSway` and `MouseParallax` create subtle room movement while preserving pixel-art readability.
- `ParallaxLayer` gives the counter room and treatment room layered depth without changing the core gameplay flow.

The most important design rule is that Candle Light and Sanity must affect every phase of the customer visit, even when the candle is not visible. The separate treatment room makes the player commit to treatment under uncertainty, then decide between continuing treatment or returning to the counter room to refill the candle.

For the first prototype, keep the scope narrow: one customer, one dialog, one treatment case with two treatment points, one candle, one sanity meter, one return-to-counter button, one transformation failure, and one layered pixel-art room test. Once that full loop feels good, expand by adding more customer cases, more treatment event types, stronger clue variety, and more polished parallax room art.
