# Knife Lesion Spawn Randomization Technical Design Report

**Project:** Cure Me, Please  
**Module:** Treatment System - Knife Mini Game Lesion Spawn Randomization  
**Engine:** Unity 6000.3.17f1, 2D, World-space Treatment Mini Games  
**Document status:** Design specification and implementation plan  
**Last updated:** July 3, 2026

---

## 1. Purpose

This report defines the next design step for the Knife mini game: randomized lesion spawning with per-anchor rules.

The goal is to let each Knife spawn point decide what kind of lesion can appear there and what cut direction is valid for that position. The system should support:

- Random lesion type per selected spawn point: `Tumor` or `Bulge`.
- Random cut orientation per selected spawn point: horizontal or vertical.
- Non-duplicated spawn anchors per mini game run.
- Spawn point visuals that only appear for the anchors selected by the randomizer.
- Spawn point visual size or style matching the randomized lesion result.
- Bulge visual states: closed before cutting and opened after the wound is cut open.

This design expands the existing Knife mini game without replacing the current gameplay loop.

---

## 2. Current Project Context

The project already has a Knife mini game built around world-space objects.

Relevant current scripts:

```text
Assets/Core/Script/Treatment/Knife/KnifeMiniGame.cs
Assets/Core/Script/Treatment/Knife/Lesion.cs
Assets/Core/Script/Treatment/Knife/LesionType.cs
Assets/Core/Script/Treatment/Knife/CutGuideLine.cs
Assets/Core/Script/Treatment/BodyPrefab/TreatmentBodyPrefab.cs
Assets/Core/Script/Treatment/BodyPrefab/TreatmentBodyPrefabCatalog.cs
Assets/Core/Script/Treatment/Anatomy/AnatomyController.cs
```

The current `LesionType` already supports:

```csharp
public enum LesionType
{
    Bulge,
    Tumor
}
```

The current `Lesion` already supports the core behavioral difference:

- `Tumor`: cut along the guide path and complete.
- `Bulge`: cut open first, then pull by bare hand.

The current `KnifeMiniGame` already has:

- `lesionPrefabs`
- `spawnAnchors`
- `spawnLesionsOnBegin`
- `minLesions`
- `maxLesions`
- shuffled anchor selection
- randomized prefab bag selection

However, the current `spawnAnchors` list is only a `List<Transform>`. A plain transform can provide position and rotation, but it cannot describe which lesion types or cut orientations are valid for that point.

---

## 3. Problem

The Knife body prefabs need more authored control than a simple list of transforms can provide.

Example requirements:

```text
P1 can spawn Tumor or Bulge, horizontal only.
P2 can spawn Bulge only, horizontal or vertical.
P3 can spawn Tumor only, vertical only.
P4 is disabled for this body prefab.
```

The system also needs the selected spawn points to show visual markers while unused anchors stay hidden.

This becomes especially important when the artist wants to place visual spawn indicators on the body. If all spawn point visuals stay visible, the player sees too many possible points. If the visual size does not match the randomized lesion, the preview becomes misleading.

---

## 4. Design Goals

1. Keep Knife lesions as world-space GameObjects.
2. Preserve the existing `Lesion` cut and pull behavior.
3. Replace plain Knife spawn transform data with richer Knife-specific spawn anchor data.
4. Allow each anchor to control valid lesion types and cut orientations.
5. Keep the randomizer simple and readable.
6. Avoid duplicate spawn points in one mini game run.
7. Support visual variants for selected anchors only.
8. Add Bulge closed/open visual switching without changing Tumor behavior.
9. Keep the system compatible with the future `TreatmentBodyPrefab` catalog routing.

---

## 5. Proposed Data Model

### 5.1 LesionCutOrientation

Add a new enum for the cut direction result.

```csharp
public enum LesionCutOrientation
{
    Horizontal,
    Vertical
}
```

The first implementation can rotate the spawned lesion object:

```text
Horizontal -> use anchor rotation
Vertical   -> anchor rotation + 90 degrees
```

Because `Lesion.cutPath` is authored in local space, rotating the lesion transform also rotates the cut path.

### 5.2 LesionSpawnAnchor

Add a Knife-specific anchor component.

```csharp
public sealed class LesionSpawnAnchor : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    [Header("Allowed Lesions")]
    [SerializeField] private bool allowTumor = true;
    [SerializeField] private bool allowBulge = true;

    [Header("Allowed Cut Directions")]
    [SerializeField] private bool allowHorizontal = true;
    [SerializeField] private bool allowVertical = true;

    [Header("Visual")]
    [SerializeField] private GameObject defaultVisualRoot;
    [SerializeField] private List<LesionSpawnVisualVariant> visualVariants;
}
```

Expected responsibilities:

- Return a valid random `LesionType`.
- Return a valid random `LesionCutOrientation`.
- Hide all visuals before randomization.
- Show the visual that matches the selected type and orientation.
- Provide a fallback spawn transform if `spawnPoint` is not assigned.

### 5.3 LesionSpawnVisualVariant

Add a serializable visual entry.

```csharp
[System.Serializable]
public sealed class LesionSpawnVisualVariant
{
    [SerializeField] private LesionType lesionType;
    [SerializeField] private LesionCutOrientation orientation;
    [SerializeField] private GameObject visualRoot;
}
```

This allows authored variants such as:

```text
Tumor + Horizontal -> small yellow horizontal marker
Tumor + Vertical   -> small yellow vertical marker
Bulge + Horizontal -> larger raised horizontal marker
Bulge + Vertical   -> larger raised vertical marker
```

If no exact visual is assigned, the anchor can show `defaultVisualRoot`.

### 5.4 Lesion Spawn Options

The current `KnifeMiniGame` uses `List<Lesion> lesionPrefabs`. It should be changed into typed spawn options.

```csharp
[System.Serializable]
private sealed class LesionSpawnOption
{
    [SerializeField] private LesionType lesionType;
    [SerializeField] private Lesion lesionPrefab;
}
```

This prevents the randomizer from choosing a Bulge prefab when the anchor result requested Tumor, or vice versa.

---

## 6. Bulge Visual State Design

Bulge needs two visual states:

1. **Closed Bulge**
   - Visible before the player cuts the lesion.
   - Represents a raised unopened wound.
   - Player must equip Knife and cut along the guide line.

2. **Opened Bulge**
   - Visible after the cut is complete.
   - Represents the opened wound state.
   - Player should stop holding Knife and use bare hand to pull.

Recommended prefab structure:

```text
BulgeLesion_World
- ClosedVisual
- OpenedVisual
- PullVisualRoot
- CutGuideLine
- Collider2D
- Lesion
```

Add fields to `Lesion`:

```csharp
[Header("State Visuals")]
[SerializeField] private GameObject closedVisualRoot;
[SerializeField] private GameObject openedVisualRoot;
```

Visual state rules:

| Lesion State | ClosedVisual | OpenedVisual |
|---|---:|---:|
| Bulge, not cut open | On | Off |
| Bulge, wound open | Off | On |
| Bulge, completed | Off | Off or completed visual |
| Tumor | Not required | Not required |

The current `woundOpen` boolean in `Lesion` can drive this behavior.

---

## 7. Runtime Flow

### 7.1 Begin Knife Mini Game

When the Knife mini game begins:

1. Clear previously spawned lesions.
2. Hide every `LesionSpawnAnchor` visual.
3. Build a valid anchor list from the configured anchors.
4. Shuffle the valid anchors.
5. Choose a count between `minLesions` and `maxLesions`.
6. Clamp the count to the available valid anchors.

### 7.2 Per Selected Anchor

For each selected anchor:

1. Randomize lesion type from that anchor's allowed lesion types.
2. Randomize cut orientation from that anchor's allowed orientations.
3. Find a matching lesion prefab from `LesionSpawnOption`.
4. Spawn the prefab at the anchor spawn point.
5. Apply rotation:

```text
Horizontal -> anchor rotation
Vertical   -> anchor rotation + 90 degrees around Z
```

6. Add the lesion to the active `lesions` list.
7. Show the anchor visual matching the selected type and orientation.

### 7.3 During Gameplay

Tumor:

```text
Knife equipped -> cut along guide path -> complete
```

Bulge:

```text
Knife equipped -> cut along guide path -> switch to opened visual -> bare hand pull -> complete
```

### 7.4 Completion

The existing completion rule remains:

```text
All active lesions completed -> MiniGameCompleted event
```

If `autoCompleteWhenAllLesionsDone` is true, the mini game can return to Anatomy automatically.

---

## 8. Unity Setup Plan

### 8.1 Knife Body Prefab Hierarchy

Each Knife body prefab should use this structure:

```text
Knife_Arm_BodyPrefab
- BodySpriteRoot
- LesionRoot
- Spawn Points
  - P1
  - P2
  - P3
  - P4
  - P5
- OptionalVFXRoot
```

Each `P#` should have:

```text
LesionSpawnAnchor
```

Optional child visual layout:

```text
P1
- Visual_Tumor_Horizontal
- Visual_Tumor_Vertical
- Visual_Bulge_Horizontal
- Visual_Bulge_Vertical
```

### 8.2 Lesion Prefabs

Minimum required prefabs:

```text
TumorLesion_World
BulgeLesion_World
```

Optional expanded prefabs if art cannot rotate cleanly:

```text
TumorLesion_Horizontal_World
TumorLesion_Vertical_World
BulgeLesion_Horizontal_World
BulgeLesion_Vertical_World
```

The recommended first version is to use two prefabs and rotate them by orientation.

### 8.3 KnifeMiniGame Inspector

Replace:

```text
Lesion Prefabs: List<Lesion>
Spawn Anchors: List<Transform>
```

With:

```text
Lesion Spawn Options:
  - Tumor -> TumorLesion_World
  - Bulge -> BulgeLesion_World

Spawn Anchors:
  - P1 (LesionSpawnAnchor)
  - P2 (LesionSpawnAnchor)
  - P3 (LesionSpawnAnchor)
```

### 8.4 TreatmentBodyPrefab

For Knife body prefabs:

```text
Mini Game Type: Knife
Area: Arm / Head / Torso / Leg
Gameplay Root: prefab root
Body Sprite Root: BodySpriteRoot
Lesion Root: LesionRoot
Lesion Spawn Anchors: all P# transforms or future LesionSpawnAnchor references
```

If `TreatmentBodyPrefab` remains transform-based in the next implementation phase, the Knife mini game can still resolve `LesionSpawnAnchor` components from those transforms.

---

## 9. Implementation Plan

### Step 1 - Add Orientation Enum

Create:

```text
Assets/Core/Script/Treatment/Knife/LesionCutOrientation.cs
```

Add:

```csharp
public enum LesionCutOrientation
{
    Horizontal,
    Vertical
}
```

### Step 2 - Add LesionSpawnAnchor

Create:

```text
Assets/Core/Script/Treatment/Knife/LesionSpawnAnchor.cs
```

Responsibilities:

- Store allowed lesion types.
- Store allowed cut orientations.
- Randomize a valid type.
- Randomize a valid orientation.
- Hide and show spawn visuals.
- Return `SpawnPoint`, falling back to its own transform.

### Step 3 - Update KnifeMiniGame Spawn Data

In `KnifeMiniGame`:

- Add `LesionSpawnOption`.
- Replace or supplement `List<Lesion> lesionPrefabs` with `List<LesionSpawnOption>`.
- Replace or supplement `List<Transform> spawnAnchors` with `List<LesionSpawnAnchor>`.
- Keep backwards compatibility if useful by resolving anchor components from transform entries.

### Step 4 - Update Spawn Logic

Update `SpawnLesions()`:

- Hide all anchor visuals.
- Build valid anchor list.
- Shuffle anchors.
- Select unique anchors.
- Randomize type and orientation per selected anchor.
- Find a matching prefab by type.
- Instantiate lesion.
- Apply rotation by orientation.
- Show matching anchor visual.

### Step 5 - Add Bulge State Visuals

Update `Lesion`:

- Add `closedVisualRoot`.
- Add `openedVisualRoot`.
- Update `ApplyVisualState()` to switch them based on `type`, `woundOpen`, and `completed`.

### Step 6 - Editor Setup Support

Update `KnifeMiniGameSetup` if needed:

- Add `LesionSpawnAnchor` to generated anchor objects.
- Fill default allowed values.
- Assign generated anchors to the Knife mini game.
- Keep generated scene objects additive and avoid moving manually authored objects.

### Step 7 - Testing

Test cases:

| Test | Expected Result |
|---|---|
| Spawn count 2-5 | Spawned lesions match configured min/max. |
| Duplicate prevention | No two lesions use the same anchor in one run. |
| Tumor-only anchor | Only Tumor can spawn there. |
| Bulge-only anchor | Only Bulge can spawn there. |
| Horizontal-only anchor | Spawned lesion keeps horizontal orientation. |
| Vertical-only anchor | Spawned lesion rotates vertically. |
| Selected anchor visual | Only selected anchors show visuals. |
| Visual type match | Tumor/Bulge visual matches randomized type. |
| Bulge cut open | Closed visual hides, opened visual shows. |
| Bulge pull complete | Bulge completes after pull phase. |

---

## 10. Risk Notes

### 10.1 Prefab Rotation

Rotating the lesion is the simplest solution because `cutPath` is local. However, if the art, collider, or pull visual does not look correct after rotation, the system may need separate horizontal and vertical prefabs.

### 10.2 Existing Serialized Fields

Changing `lesionPrefabs` and `spawnAnchors` directly can break existing scene assignments. To reduce risk, the first implementation can keep old fields temporarily and add new fields beside them.

Recommended migration:

```text
Old Transform anchors -> resolve LesionSpawnAnchor if present.
Old Lesion prefab list -> fallback random prefab list if no typed options are assigned.
```

After Unity setup is stable, old fields can be hidden or removed later.

### 10.3 Body Prefab Catalog Integration

The body prefab catalog system is currently a separate routing layer. This Knife spawn randomization design should be compatible with it, but full automatic body prefab swapping requires a later hook:

```text
AnatomyController -> TreatmentBodyPrefabSpawner -> KnifeMiniGame
```

This report only defines the Knife lesion randomization design.

---

## 11. Recommended Implementation Order

1. Add `LesionCutOrientation`.
2. Add `LesionSpawnAnchor`.
3. Add typed `LesionSpawnOption` to `KnifeMiniGame`.
4. Update `KnifeMiniGame.SpawnLesions()`.
5. Add Bulge closed/open visuals to `Lesion`.
6. Update editor setup helper.
7. Configure one test prefab: `Knife_Arm_BodyPrefab`.
8. Run Unity play test.
9. Expand setup to Head, Torso, and Leg Knife body prefabs.

---

## 12. Final Design Summary

The Knife mini game should move from simple transform-based spawning to anchor-driven randomization.

Each Knife spawn point will control:

```text
Allowed lesion type
Allowed cut orientation
Spawn point transform
Selected-state visual
```

The runtime randomizer will choose unique anchors, randomize `Tumor` or `Bulge`, randomize horizontal or vertical cutting, spawn the matching lesion prefab, rotate it when needed, and show only the selected spawn point visuals.

Bulge will become visually clearer by switching from `ClosedVisual` to `OpenedVisual` after the cut phase, then completing after the pull phase.

This keeps the current Knife mini game behavior intact while giving the designer more control over where lesions appear and how each cut should read visually.
