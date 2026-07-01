# Patient Treatment Case ScriptableObject Design Report

**Project:** Cure Me, Please  
**Module:** Patient case data, Anatomy routing, and mini game selection  
**Engine:** Unity 6000.3.17f1, 2D Pixel Art  
**Document status:** Design and implementation plan  
**Report date:** July 1, 2026  

---

## 1. Purpose

The current treatment flow can already route from the Anatomy Selection Screen into the Tongs or Knife mini games. However, the route is currently configured directly inside `AnatomyController` through inspector fields such as `tongsArea`, `knifeAreas`, and manual infected-area booleans.

The next design goal is to move patient-specific treatment data into ScriptableObject assets. Each patient case should define:

- Which body areas must be treated.
- Which mini game each area uses.
- Which dialog clue set belongs to the patient.
- Optional per-area setup data for future mini game variations.

This allows the designer to create a patient case as data instead of changing scene objects or controller code for every customer.

---

## 2. Current System Analysis

### 2.1 Current Dialog Data

`DialogData` is already a ScriptableObject. It stores:

- Player and customer display names.
- Dialog lines.
- Player/customer speaker type per line.
- Customer text blip clip variations.

This means dialog content is already data-driven, but it is not yet connected to treatment requirements.

### 2.2 Current Customer Flow

`CustomerAgent` controls movement, door flow, bubble display, and enter/exit behavior. It does not currently hold a patient case asset.

Current limitation:

- Every customer uses the same scene-wired dialog/treatment setup unless another component or scene reference changes it.

### 2.3 Current Anatomy Routing

`AnatomyController` currently owns the treatment decision data:

- Manual infection flags: `headInfected`, `torsoInfected`, `armInfected`, `legInfected`.
- Random infection mode: `minInfected`, `maxInfected`.
- Mini game routing:
  - `tongsArea`
  - `tongsMiniGame`
  - `knifeMiniGame`
  - `knifeAreas`

Current limitation:

- This is useful for testing, but it is not ideal for real patients.
- A patient case cannot say "Head uses Knife, Arm uses Tongs" unless the scene controller is manually changed.
- Dialog clues and real treatment targets can drift apart.

### 2.4 Current Mini Game Entry Points

The project currently has these mini game APIs:

- `TongsMiniGame.Begin(CustomerAgent customer)`
- `KnifeMiniGame.Begin(CustomerAgent customer)`

Both mini games use the active customer to read the current `Sanity` component.

This is a good foundation. The new case system should keep this entry contract and only change how `AnatomyController` decides which mini game to start.

---

## 3. Proposed Data Model

### 3.1 New Enum: TreatmentMiniGameType

Create a lightweight enum to describe which mini game should be used by a body area.

```csharp
public enum TreatmentMiniGameType
{
    None,
    Tongs,
    Knife
}
```

Future values can be added later, for example:

- `Needle`
- `CandleRitual`
- `Bandage`
- `Puzzle`

### 3.2 New Serializable Data: TreatmentAreaRequirement

This class represents one required treatment area inside a patient case.

```csharp
[Serializable]
public sealed class TreatmentAreaRequirement
{
    [SerializeField] private BodyArea area;
    [SerializeField] private TreatmentMiniGameType miniGameType;
    [SerializeField, TextArea(2, 4)] private string designerNote;

    public BodyArea Area => area;
    public TreatmentMiniGameType MiniGameType => miniGameType;
    public string DesignerNote => designerNote;
}
```

Recommended fields:

| Field | Purpose |
|---|---|
| `area` | The body area the player must inspect and treat. |
| `miniGameType` | Which mini game should launch for this area. |
| `designerNote` | Optional note for symptom clues, wound idea, or setup reminder. |

### 3.3 New ScriptableObject: TreatmentCaseData

This is the main patient case asset.

```csharp
[CreateAssetMenu(fileName = "TreatmentCase", menuName = "Pixel Forge/Treatment Case")]
public sealed class TreatmentCaseData : ScriptableObject
{
    [SerializeField] private string caseId;
    [SerializeField] private string customerDisplayName = "Customer";
    [SerializeField] private DialogData dialogData;
    [SerializeField] private List<TreatmentAreaRequirement> requiredTreatments;

    public string CaseId => caseId;
    public string CustomerDisplayName => customerDisplayName;
    public DialogData DialogData => dialogData;
    public IReadOnlyList<TreatmentAreaRequirement> RequiredTreatments => requiredTreatments;
}
```

Recommended case examples:

| Case Asset | DialogData | Required Treatments |
|---|---|---|
| `Case_VillagerGirl_ArmParasite` | `Dialog_VillagerGirl_ArmPain` | Arm = Tongs |
| `Case_Miner_HeadTumor` | `Dialog_Miner_Headache` | Head = Knife |
| `Case_Traveler_MixedParasite` | `Dialog_Traveler_MixedSymptoms` | Arm = Tongs, Torso = Knife |

---

## 4. Runtime Ownership

### 4.1 New Component: CustomerCaseProvider

Attach this component to each customer prefab or scene customer object.

```csharp
public sealed class CustomerCaseProvider : MonoBehaviour
{
    [SerializeField] private TreatmentCaseData caseData;

    public TreatmentCaseData CaseData => caseData;
}
```

Why use a separate component instead of placing the field directly inside `CustomerAgent`?

- Keeps `CustomerAgent` focused on movement and door flow.
- Avoids mixing patient data with movement logic.
- Allows case assignment to be swapped independently.

### 4.2 Runtime Case State

Do not mutate the ScriptableObject during gameplay. ScriptableObjects are shared assets, so runtime progress should live in memory.

Recommended runtime state:

```csharp
public sealed class TreatmentCaseRuntime
{
    private readonly Dictionary<BodyArea, TreatmentAreaRequirement> requirements;
    private readonly HashSet<BodyArea> treatedAreas;
}
```

Runtime state should track:

- Which areas are required.
- Which areas are already treated.
- Which mini game belongs to each required area.

---

## 5. Updated Flow

### 5.1 Customer Enters

1. `GameFlow` starts the active customer.
2. Customer has `CustomerCaseProvider`.
3. `CustomerCaseProvider.CaseData` becomes the active patient case.

### 5.2 Dialog Starts

Current behavior:

- `Dialog` uses its own serialized `DialogData`.

Target behavior:

- `Dialog.Open(customer)` asks the customer for `CustomerCaseProvider`.
- If a case exists and has `DialogData`, use that dialog.
- Otherwise, fall back to the serialized test `DialogData`.

This preserves current test setup while enabling case-specific dialog.

### 5.3 Treatment Starts

1. `Treatment.Begin(customer)` calls `AnatomyController.Begin(customer)`.
2. `AnatomyController` reads `CustomerCaseProvider.CaseData`.
3. Required treatment areas from the case are marked internally as `PartState.Infected`.
4. Non-required areas remain `PartState.Untouched`.

### 5.4 Player Selects A Body Area

1. Player clicks Head, Torso, Arm, or Leg.
2. `AnatomyController` checks the active runtime case.
3. If the area is not required:
   - Mark it as `Healthy`.
   - Show the "nothing to treat" message.
4. If the area is required:
   - Look up `TreatmentMiniGameType`.
   - Start the matching mini game.

### 5.5 Mini Game Completes

1. Mini game raises `MiniGameCompleted`.
2. `AnatomyController` marks the active area as `Treated`.
3. If every required area is treated, raise `TreatmentCompleted`.
4. `Treatment` calls `GameFlow.CompleteTreatment(customer)`.

---

## 6. AnatomyController Changes

### 6.1 Keep Existing Test Mode

Keep the current manual/random setup as fallback mode for testing.

Recommended modes:

```csharp
public enum TreatmentCaseSource
{
    CustomerCase,
    Manual,
    Random
}
```

Default should become `CustomerCase`.

If no case data is found:

- Use Manual mode fallback, or
- Log a clear warning and use the existing manual inspector fields.

### 6.2 Replace Hard-Coded Mini Game Mapping

Current logic:

```csharp
if (area == tongsArea) start Tongs
if (knifeAreas.Contains(area)) start Knife
```

Target logic:

```csharp
TreatmentMiniGameType miniGameType = activeCaseRuntime.GetMiniGameType(area);

switch (miniGameType)
{
    case TreatmentMiniGameType.Tongs:
        StartTongs(area);
        break;

    case TreatmentMiniGameType.Knife:
        StartKnife(area);
        break;

    default:
        HandleMissingMiniGame(area);
        break;
}
```

### 6.3 Completion Handling

Current completion checks:

- Tongs checks `activeArea == tongsArea`.
- Knife checks `knifeAreas.Contains(activeArea)`.

Target completion checks:

- Any active mini game completion marks `activeArea` as treated.
- The active area's mini game type was already validated before entering.

This makes completion logic independent of hard-coded area lists.

---

## 7. Dialog Integration

### 7.1 Recommended Change

Add a runtime active dialog source to `Dialog`.

Behavior:

1. `Dialog.Open(CustomerAgent customer)` tries to find `CustomerCaseProvider`.
2. If `provider.CaseData.DialogData` exists, use it as the current dialog.
3. If not, use the serialized fallback `dialogData`.

Important:

- Keep the serialized `dialogData` field for testing.
- Do not remove current DialogData workflow.
- Case-specific dialog should only override the fallback when available.

### 7.2 Benefit

The patient case becomes a single source of truth:

- The story clues live in `DialogData`.
- The real treatment targets live in `TreatmentCaseData`.
- Both are assigned together in one asset.

This directly supports clue-based gameplay where the player must infer the correct treatment areas from dialog.

---

## 8. Unity Setup Plan

### 8.1 Folder Structure

Recommended script folders:

```text
Assets/Core/Script/Treatment/Case
Assets/Core/Data/Treatment/Cases
```

Recommended files:

```text
Assets/Core/Script/Treatment/Case/TreatmentMiniGameType.cs
Assets/Core/Script/Treatment/Case/TreatmentAreaRequirement.cs
Assets/Core/Script/Treatment/Case/TreatmentCaseData.cs
Assets/Core/Script/Treatment/Case/CustomerCaseProvider.cs
Assets/Core/Script/Treatment/Case/TreatmentCaseRuntime.cs
```

### 8.2 Example Asset Setup

Create a case asset:

```text
Assets/Core/Data/Treatment/Cases/Case_Test_MixedTreatment.asset
```

Set:

- `Case Id`: `test_mixed_treatment`
- `Customer Display Name`: `Test Customer`
- `Dialog Data`: `DialogDataTest`
- `Required Treatments`:
  - Element 0: `Arm`, `Tongs`
  - Element 1: `Torso`, `Knife`

### 8.3 Customer Setup

On the customer object:

1. Add `CustomerCaseProvider`.
2. Assign `Case_Test_MixedTreatment`.
3. Keep existing `CustomerAgent`, `Sanity`, `Bubble`, and movement setup.

### 8.4 Anatomy Setup

On `AnatomyController`:

- Keep references to `TongsMiniGame`.
- Keep references to `KnifeMiniGame`.
- Set case source to `CustomerCase`.
- Keep manual/random settings for test fallback.

---

## 9. Implementation Phases

### Phase 1: Data Types Only

Create:

- `TreatmentMiniGameType`
- `TreatmentAreaRequirement`
- `TreatmentCaseData`
- `CustomerCaseProvider`

No gameplay behavior changes yet.

Validation:

- Unity compiles.
- Designer can create a Treatment Case asset.
- Customer object can reference the asset.

### Phase 2: Dialog Uses Case Data

Update `Dialog.Open(customer)`:

- Get `CustomerCaseProvider`.
- Use `caseData.DialogData` when present.
- Fall back to current serialized `dialogData`.

Validation:

- Existing dialog still works without a case.
- A customer with case data uses the case's dialog.

### Phase 3: Anatomy Reads Case Data

Update `AnatomyController.Begin(customer)`:

- Build runtime treatment state from `TreatmentCaseData`.
- Mark required areas as internally infected.
- Preserve manual/random fallback.

Validation:

- Selecting a non-required area shows healthy message.
- Selecting a required area starts the correct mini game.

### Phase 4: Mini Game Routing By Type

Replace area-list routing with mini game type routing:

- `TreatmentMiniGameType.Tongs` starts `TongsMiniGame`.
- `TreatmentMiniGameType.Knife` starts `KnifeMiniGame`.
- `None` or missing type shows warning/fallback.

Validation:

- Arm can use Tongs if the SO says so.
- Arm can use Knife if the SO says so.
- Head/Torso/Leg can use either mini game if the SO says so.

### Phase 5: Editor/Setup Support

Optional but recommended:

- Add a menu item that creates a sample Treatment Case asset.
- Add validation warnings for duplicate body areas.
- Add warnings when an area has `None` as mini game type.

Validation:

- Designers can create test cases quickly.
- Invalid case assets are easy to detect.

---

## 10. Risks And Safeguards

| Risk | Safeguard |
|---|---|
| ScriptableObject gets modified during play mode | Keep progress in `TreatmentCaseRuntime`, not inside `TreatmentCaseData`. |
| Dialog and treatment data drift apart | Reference `DialogData` from `TreatmentCaseData`. |
| Missing case on customer breaks flow | Keep existing serialized fallback data. |
| Duplicate area entries in one case | Validate and warn in `OnValidate` or editor tooling. |
| Area has no mini game assigned | Show warning and optionally auto-treat only in debug/fallback mode. |
| Existing scene setup breaks | Keep `AnatomyController` mini game references and current fallback modes. |

---

## 11. Recommended First Implementation Step

Start with Phase 1 only:

1. Add the new case data scripts.
2. Create one test case asset.
3. Add `CustomerCaseProvider` to the current test customer.
4. Do not change runtime routing yet.

This gives the project a safe data foundation before touching active flow logic.

After Phase 1 is stable, implement Dialog override first, then Anatomy routing.

---

## 12. Expected Final Result

After this system is implemented, the designer can create a patient case like this:

```text
Case: Villager Girl
Dialog: Dialog_VillagerGirl

Required Treatments:
- Arm   -> Tongs
- Torso -> Knife
```

At runtime:

1. The customer enters.
2. The customer's dialog comes from the case asset.
3. The player listens for clues.
4. The Anatomy screen gives no direct infection hint.
5. The player chooses an area.
6. The mini game is selected from the case asset.
7. The case completes only after all required treatment areas are cured.

This turns patient design into a data workflow and makes the clinic loop scalable for multiple customers.
