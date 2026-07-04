# Customer Spawn & Queue System Design Report

**Project:** Cure Me, Please  
**Module:** Game Loop - Customer Spawn, Queue, and Multi-Patient Run  
**Engine:** Unity 6000.3.17f1, 2D  
**Document status:** Adjusted design specification for the current project  
**Last updated:** July 4, 2026

---

## 1. Purpose

This report adapts the Customer Spawn & Queue system design to the current project implementation.

The original design goal is still valid:

```text
Turn the current single-customer treatment loop into a full multi-patient run.
```

However, the current project has evolved. It now has:

- A working `GameFlow` state machine for one active customer.
- `CustomerAgent` enter, counter, dialog, and exit movement.
- `CustomerLayer` sorting changes for outside/inside door flow.
- `CustomerCaseProvider` for attaching a `TreatmentCaseData` to a customer.
- `TreatmentCaseData` with `caseId`, customer display name, `DialogData`, and required treatment areas.
- `Dialog` resolving active dialog from the customer's assigned case.
- `AnatomyController` routing required body areas to Tongs, Knife, or Needle.
- Tongs, Knife, and Needle mini games at prototype/runtime level.
- Basic Candle and Sanity systems connected to `GameFlow`.

The queue system should build on these existing systems instead of replacing them.

---

## 2. Current Project Reality

### 2.1 Current GameFlow

Current `ClinicFlowState` values:

```text
Idle
CustomerEntering
WaitingForDialog
DialogActive
TransitionToTreatment
TreatmentReady
TreatmentActive
CustomerLeaving
Complete
```

Current runtime shape:

```text
StartFirstCustomer
-> CustomerEntering
-> WaitingForDialog
-> DialogActive
-> TreatmentActive
-> CustomerLeaving
-> Complete
```

Important current behavior:

- `GameFlow` has one serialized `firstCustomer`.
- `activeCustomer` is a single runtime reference.
- `CompleteTreatment()` resets Sanity, returns to the counter room, and starts customer exit.
- `CustomerLeft()` currently ends the flow by setting state to `Complete`.
- Transformation currently logs a placeholder and moves to `Complete`, not a full game-over screen yet.

### 2.2 Current CustomerAgent

`CustomerAgent` already supports the entrance and exit illusion:

```text
spawnPoint
outsideDoorPoint
insideDoorPoint
counterPoint
exitPoint
Bubble
Door
CustomerLayer
```

Entry flow:

```text
Spawn point
-> outside door point
-> inside door point
-> counter point
-> show bubble
```

Exit flow:

```text
counter / inside
-> inside door point
-> outside door point
-> exit point
-> notify GameFlow.CustomerLeft
```

This should remain the movement layer for queued customers.

### 2.3 Current Case System

`TreatmentCaseData` is the project source of truth for a patient case:

```text
caseId
customerDisplayName
dialogData
requiredTreatments
```

Each `TreatmentAreaRequirement` stores:

```text
BodyArea
TreatmentMiniGameType
designerNote
```

This means a queued customer does not need a separate `DialogSet` field. Dialog should stay inside `TreatmentCaseData.DialogData`, because `Dialog` already resolves it through `CustomerCaseProvider`.

### 2.4 Current Gap

The project does not yet have:

- A real patient roster.
- A queue runner.
- A multi-customer loop.
- Counter breather state between customers.
- Save/resume for a run.
- Real game-over and ending states.

The queue system is therefore the next layer above the existing single-customer vertical slice.

---

## 3. Updated Design Decisions

| Topic | Adjusted Decision |
|---|---|
| Run structure | Multi-patient run, target 7 patients for ACT 1, but implementation should support any list size for testing. |
| First patient | Optional fixed starter, recommended for tutorial/onboarding. |
| Customer identity | Use `CustomerDefinition` assets to connect prefab + case + stable id. |
| Case data | Reuse existing `TreatmentCaseData`; do not create a separate dialog/case format. |
| Dialog | Keep dialog inside `TreatmentCaseData.DialogData`. |
| Spawn movement | Reuse `CustomerAgent.BeginEnter()` and `BeginExit()`. |
| Layer/door flow | Reuse `CustomerLayer` and `Door`; do not duplicate entrance logic in the queue. |
| Between patients | Add a `CounterBreather` state after customer exit, before spawning the next customer. |
| Failure | Transformation should become `GameOver`, but current implementation only has a placeholder. |
| Save | Save/resume is a later phase. There is no current save manager to plug into yet. |
| Testing | Queue must work with 1-3 test customers before requiring all 7 real GDD patients. |

---

## 4. Proposed CustomerDefinition

Add a ScriptableObject that describes one patient in the run.

```csharp
[CreateAssetMenu(
    fileName = "CustomerDefinition",
    menuName = "Pixel Forge/Customer Definition")]
public sealed class CustomerDefinition : ScriptableObject
{
    [SerializeField] private string customerId;
    [SerializeField] private string displayName;
    [SerializeField] private CustomerAgent customerPrefab;
    [SerializeField] private TreatmentCaseData caseData;

    public string CustomerId => customerId;
    public string DisplayName => displayName;
    public CustomerAgent CustomerPrefab => customerPrefab;
    public TreatmentCaseData CaseData => caseData;
}
```

### Why this matches the current project

The current project already places patient-specific dialog and treatment requirements inside `TreatmentCaseData`. `CustomerDefinition` should not duplicate that data.

It only answers:

```text
Which character prefab should spawn?
Which TreatmentCaseData should that character use?
What stable id is saved in queue order?
```

---

## 5. Customer Prefab Requirements

Each queued customer prefab should include:

```text
CustomerAgent
CustomerLayer
Sanity
CustomerCaseProvider
Bubble reference
Door/waypoint references or runtime assignment support
```

The current `CustomerCaseProvider` only exposes a read-only serialized `caseData`. There are two valid setup paths:

### Option A - Preconfigured Customer Prefabs

Each customer prefab already has its own `CustomerCaseProvider.caseData` assigned in the prefab.

Pros:

- Simple.
- Requires less runtime mutation.
- Good for the first implementation pass.

Cons:

- `CustomerDefinition.caseData` can duplicate the prefab assignment unless carefully managed.

### Option B - Runtime Case Assignment

Add a small method to `CustomerCaseProvider`:

```csharp
public void SetCase(TreatmentCaseData nextCase)
{
    caseData = nextCase;
}
```

Then the queue spawner can instantiate any compatible customer prefab and assign the case from `CustomerDefinition`.

Pros:

- Cleaner data ownership.
- `CustomerDefinition` becomes the single queue source.

Cons:

- Requires a small code change.

Recommended path: **Option B**, because the queue system should decide which case belongs to the spawned customer.

---

## 6. Queue Construction

The queue should support:

- Optional fixed starter.
- Shuffled remaining pool.
- No duplicate customers.
- Any queue size.

```csharp
public sealed class CustomerQueueBuilder : MonoBehaviour
{
    [SerializeField] private CustomerDefinition fixedStarter;
    [SerializeField] private List<CustomerDefinition> customerPool;
    [SerializeField] private bool useFixedStarter = true;

    public List<CustomerDefinition> BuildQueue()
    {
        List<CustomerDefinition> queue = new List<CustomerDefinition>();
        List<CustomerDefinition> pool = new List<CustomerDefinition>();

        if (useFixedStarter && fixedStarter != null)
        {
            queue.Add(fixedStarter);
        }

        for (int i = 0; i < customerPool.Count; i++)
        {
            CustomerDefinition definition = customerPool[i];
            if (definition != null && definition != fixedStarter)
            {
                pool.Add(definition);
            }
        }

        Shuffle(pool);
        queue.AddRange(pool);
        return queue;
    }
}
```

Use the same Fisher-Yates shuffle pattern already used by mini-game spawn systems.

---

## 7. Queue Runtime

Use a small runtime class to track queue progress.

```csharp
public sealed class CustomerQueueRuntime
{
    private readonly List<CustomerDefinition> queue = new List<CustomerDefinition>();
    private int currentIndex;

    public CustomerDefinition Current =>
        currentIndex >= 0 && currentIndex < queue.Count ? queue[currentIndex] : null;

    public bool IsComplete => currentIndex >= queue.Count;
    public int CurrentIndex => currentIndex;
    public IReadOnlyList<CustomerDefinition> Queue => queue;

    public void SetQueue(IReadOnlyList<CustomerDefinition> definitions, int startIndex = 0)
    {
        queue.Clear();
        if (definitions != null)
        {
            queue.AddRange(definitions);
        }

        currentIndex = Mathf.Clamp(startIndex, 0, queue.Count);
    }

    public bool Advance()
    {
        currentIndex++;
        return !IsComplete;
    }
}
```

This class should not know about scenes, doors, or dialog. It only tracks which definition is active.

---

## 8. GameFlow Integration

### 8.1 New States

Add states after the current vertical slice:

```text
CounterBreather
AllCustomersComplete
GameOver
```

Updated enum:

```csharp
public enum ClinicFlowState
{
    Idle,
    CustomerEntering,
    WaitingForDialog,
    DialogActive,
    TransitionToTreatment,
    TreatmentReady,
    TreatmentActive,
    CustomerLeaving,
    CounterBreather,
    AllCustomersComplete,
    GameOver,
    Complete
}
```

### 8.2 Updated Loop

Current:

```text
CustomerLeft -> Complete
```

New:

```text
CustomerLeft
-> Despawn or deactivate old customer
-> Queue advance
   -> next customer exists: CounterBreather
   -> no next customer: AllCustomersComplete
```

Counter breather:

```text
CounterBreather
-> candle can be refilled
-> player confirms next customer
-> SpawnCurrentCustomer
-> CustomerEntering
```

### 8.3 Active Customer Creation

Add a customer spawner service or make `GameFlow` own a small spawn method.

Recommended service:

```csharp
public sealed class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private Transform customerParent;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform outsideDoorPoint;
    [SerializeField] private Transform insideDoorPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Door door;

    public CustomerAgent Spawn(CustomerDefinition definition)
    {
        CustomerAgent customer = Instantiate(definition.CustomerPrefab, customerParent);
        CustomerCaseProvider provider = customer.GetComponent<CustomerCaseProvider>();
        provider?.SetCase(definition.CaseData);

        // CustomerAgent waypoint assignment needs either serialized prefab references
        // or a new runtime ConfigureWaypoints method.
        return customer;
    }
}
```

The current `CustomerAgent` does not have runtime waypoint assignment methods. Implementation needs one of these:

1. Keep waypoint references on every prefab.
2. Add `CustomerAgent.ConfigurePath(...)` so the spawner can assign shared scene waypoints after instantiate.

Recommended path: **add `CustomerAgent.ConfigurePath(...)`**, because all customers use the same scene waypoints.

---

## 9. Counter Breather Design

Counter breather should be a real state, not just a delay.

Purpose:

- Let the previous customer finish leaving.
- Give the player time to refill candle.
- Save the run later.
- Start the next customer when the player is ready.

First implementation can be simple:

```text
Customer exits
-> CounterBreather
-> show "Next Customer" interaction/button
-> player clicks
-> next customer enters
```

This should connect to the Candle interaction system later. It should not force immediate spawn unless the design intentionally wants pressure between patients.

---

## 10. Failure / Transformation

Current `GameFlow.TriggerTransformation()`:

```text
SetTreatmentStress(false)
SetState(Complete)
Debug.Log("Transformation placeholder triggered.")
```

Queue version should become:

```text
SetTreatmentStress(false)
Stop active treatment/minigame if needed
SetState(GameOver)
Show GameOver UI
Restart creates a new queue
```

Important: the original design says transformation is a full game over. That is not implemented yet, so this must be a dedicated phase.

---

## 11. Save / Resume Scope

The original report includes save/resume. That remains valid, but it should not be Phase 1 because the current project does not have a save service yet.

When implemented, save at `CounterBreather`:

```csharp
[Serializable]
public sealed class RunSaveData
{
    public string[] queueOrder;
    public int currentIndex;
    public float candleLightLevel;
}
```

New game:

```text
Build fresh queue.
Save queue ids.
Start index 0.
```

Resume:

```text
Load queue ids.
Resolve ids to CustomerDefinition assets.
Do not reshuffle.
Set currentIndex from save.
```

Mid-treatment save should stay out of scope for now.

---

## 12. Implementation Plan

Current implementation checkpoint:

- Phase 1 data/runtime scripts have been added:
  - `CustomerDefinition.cs`
  - `CustomerQueueBuilder.cs`
  - `CustomerQueueRuntime.cs`
- `CustomerCaseProvider.SetCase(TreatmentCaseData nextCase)` has been added for runtime case assignment.
- Phase 2 spawner script has been added:
  - `CustomerSpawner.cs`
- `CustomerAgent` now exposes runtime setup methods:
  - `ConfigurePath(...)`
  - `ConfigureBubble(...)`
  - `ConfigureDoor(...)`
- Phase 3 `GameFlow` queue hook has started:
  - Optional `useCustomerQueue` path.
  - `firstCustomer` fallback remains intact.
  - New states: `CounterBreather`, `AllCustomersComplete`, and `GameOver`.
  - `CustomerLeft` can advance the queue instead of always ending at `Complete`.
  - `StartNextQueuedCustomer()` exists for a future button/interactable.
- Queue building now skips invalid `CustomerDefinition` assets so unfinished customer data does not stop the runtime flow.
- Code build verification passed for `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj`.

Unity scene setup and Play Mode validation are still required before this should be treated as a finished runtime feature.

### Phase 1 - Data Layer

Create:

```text
CustomerDefinition.cs
CustomerQueueBuilder.cs
CustomerQueueRuntime.cs
```

Update:

```text
CustomerCaseProvider.cs
```

Add:

```csharp
SetCase(TreatmentCaseData nextCase)
```

Verification:

- Create 2-3 test customer definitions.
- Log the queue order.
- Confirm fixed starter and shuffle work.

### Phase 2 - Customer Spawner

Create:

```text
CustomerSpawner.cs
```

Update `CustomerAgent` if needed:

```text
ConfigurePath(...)
ConfigureBubble(...)
ConfigureDoor(...)
```

Verification:

- Spawn one customer prefab at runtime.
- Confirm it follows the existing enter path.
- Confirm bubble opens dialog.

### Phase 3 - GameFlow Queue Hook

Update `GameFlow`:

- Replace direct `firstCustomer` start path with optional queue start.
- Keep `firstCustomer` as fallback for current scene testing.
- On `CustomerLeft`, advance queue instead of always going to `Complete`.
- Add `CounterBreather`.
- Add next-customer trigger method.

Verification:

- Customer 1 enters, talks, gets treated, exits.
- CounterBreather starts.
- Customer 2 enters after trigger.

### Phase 4 - Case Integration

Ensure spawned customer has:

```text
CustomerCaseProvider.CaseData
Sanity
CustomerLayer
Bubble
```

Verification:

- Dialog resolves from the active customer's case.
- Anatomy reads the active customer's case.
- Required treatments route to Tongs, Knife, or Needle.

### Phase 5 - Failure And Ending

Add:

```text
GameOver
AllCustomersComplete
```

Verification:

- Transformation enters GameOver.
- Last customer complete enters AllCustomersComplete / ending placeholder.

### Phase 6 - Save / Resume

Add save only after queue loop is stable.

Verification:

- Save at CounterBreather.
- Resume keeps the same queue order and current index.

---

## 13. Unity Setup Plan

### 13.1 Data Folders

Recommended folders:

```text
Assets/Core/Data/Customer
Assets/Core/Data/Treatment/Cases
Assets/Core/Data/Dialog
Assets/Core/Prefab/Customers
```

### 13.2 Customer Prefab

Each customer prefab should contain:

```text
CustomerAgent
CustomerLayer
Sanity
CustomerCaseProvider
SpriteRenderer / visual children
```

If using runtime path assignment, leave scene-specific waypoint references empty and let `CustomerSpawner` assign them.

### 13.3 CustomerDefinition Assets

Create one asset per patient:

```text
Customer_VillageWoman.asset
Customer_ElfArcher.asset
Customer_VillageMan.asset
...
```

Each asset assigns:

```text
Customer Id
Display Name
Customer Prefab
Treatment Case Data
```

### 13.4 GameFlow Scene References

Add to scene:

```text
CustomerQueueBuilder
CustomerSpawner
NextCustomer interaction/button
```

Assign:

```text
Fixed Starter
Customer Pool
Customer Parent
Shared waypoints
Door
```

Keep the existing `firstCustomer` path until the queue path is confirmed.

---

## 14. Risks

### 14.1 Duplicating Case Data

Avoid storing dialog separately in `CustomerDefinition`. The current project already stores dialog in `TreatmentCaseData`.

### 14.2 Prefab Scene References

Customer prefabs cannot reliably keep references to scene-specific waypoints. Runtime configuration is cleaner.

### 14.3 Save Too Early

Save/resume should wait until the queue loop is stable. Otherwise save data may encode temporary state names or placeholder assumptions.

### 14.4 GameOver Scope

Transformation currently only logs a placeholder. Full game-over behavior needs UI and reset rules.

### 14.5 Existing Single-Customer Testing

Do not remove `firstCustomer` immediately. It is still useful for quick scene tests while queue work is being built.

---

## 15. Current-Compatible Summary

The Customer Spawn & Queue system is now partially implemented as a layer above the existing single-customer loop.

It should use:

```text
CustomerDefinition -> customer prefab + TreatmentCaseData
CustomerQueueBuilder -> fixed starter + shuffled pool
CustomerQueueRuntime -> current index and queue progress
CustomerSpawner -> instantiate/configure CustomerAgent
GameFlow -> loop customers with CounterBreather
```

The current `TreatmentCaseData`, `Dialog`, `CustomerCaseProvider`, `CustomerAgent`, `CustomerLayer`, Candle, Sanity, Anatomy, and mini-game systems should remain the core runtime path.

The next validation should prove:

```text
Customer A enters -> dialog/case/treatment -> exits
CounterBreather
Customer B enters -> dialog/case/treatment -> exits
AllCustomersComplete placeholder
```

Save/resume, final 7-patient content, ACT 1 ending, and full game-over UI should come after that loop is stable.
