# Needle Pustule Spawn & Visual State Technical Design Report

**Project:** Cure Me, Please  
**Module:** Treatment System - Needle Mini Game Pustule Spawn and Visual State  
**Engine:** Unity 6000.3.17f1, 2D, World-space Treatment Mini Games  
**Document status:** Design specification and implementation plan  
**Last updated:** July 3, 2026

---

## 1. Purpose

This report defines the next design step for the Needle mini game: randomized pustule spawning with per-anchor rules and a clearer two-state pustule visual system.

The Needle mini game already supports the core gameplay flow:

```text
Small Pustule: Needle pierce -> bare-hand squeeze
Big Pustule: Needle pierce -> needle drain
```

The new design focuses on two improvements:

1. Let each spawn point decide whether it can spawn `Small`, `Big`, or both pustule types.
2. Let each pustule prefab store two main visual states:

```text
FullVisual  -> pus is still inside
EmptyVisual -> pus has been squeezed or drained out
```

This keeps the Needle system consistent with the new Knife and Tongs direction: spawn points should carry local authoring rules instead of being only raw transforms.

---

## 2. Current Project Context

Relevant current scripts:

```text
Assets/Core/Script/Treatment/Needle/NeedleMiniGame.cs
Assets/Core/Script/Treatment/Needle/Pustule.cs
Assets/Core/Script/Treatment/Needle/PustuleType.cs
Assets/Core/Script/Treatment/Needle/NeedleToolState.cs
Assets/Core/Script/Treatment/Needle/NeedleActionMode.cs
Assets/Core/Script/Treatment/BodyPrefab/TreatmentBodyPrefab.cs
Assets/Core/Script/Treatment/BodyPrefab/TreatmentBodyPrefabCatalog.cs
```

The current `PustuleType` supports:

```csharp
public enum PustuleType
{
    Small,
    Big
}
```

The current `Pustule` already tracks:

- `pierceProgress`
- `drainProgress`
- `painLevel`
- `isPierced`
- `completed`
- `pustuleRadius`
- jitter and off-radius checks for Big pustules

The current `NeedleMiniGame` already has:

- `pustulePrefabs`
- `spawnAnchors`
- `spawnPustulesOnBegin`
- `minPustules`
- `maxPustules`
- shuffled anchor selection
- randomized prefab bag selection

However, the current spawn anchors are only `List<Transform>`. A transform can provide position and rotation, but it cannot describe whether that location is valid for a small pustule, a big pustule, or both.

---

## 3. Problem

Needle spawn points need designer-authored rules.

Example requirements:

```text
P1 can spawn Small only.
P2 can spawn Big only.
P3 can spawn Small or Big.
P4 is disabled for this body prefab.
```

The system also needs selected spawn points to show visual feedback while unused anchors remain hidden. This is useful when a body prefab has many authored positions but only a subset is active in one treatment case.

Pustule prefabs also need clearer visual states. The current color-based state is useful for debug, but the final art should be able to swap between two authored visuals:

```text
pus still inside
pus removed
```

---

## 4. Design Goals

1. Keep Needle gameplay as world-space GameObjects.
2. Preserve the existing `Pustule` behavior and state machine.
3. Replace plain transform spawn data with Needle-specific spawn anchor data.
4. Allow each anchor to restrict valid pustule types.
5. Prevent duplicate spawn anchors in one mini game run.
6. Show spawn point visuals only for selected anchors.
7. Support visual variants by randomized pustule type.
8. Add two main pustule visual roots: `FullVisual` and `EmptyVisual`.
9. Keep the design compatible with future `TreatmentBodyPrefab` catalog routing.

---

## 5. Proposed Data Model

### 5.1 PustuleSpawnAnchor

Add a Needle-specific anchor component.

```csharp
public sealed class PustuleSpawnAnchor : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    [Header("Allowed Pustules")]
    [SerializeField] private bool allowSmall = true;
    [SerializeField] private bool allowBig = true;

    [Header("Optional Overrides")]
    [SerializeField] private bool overrideRadius;
    [SerializeField] private float radiusOverride = 0.4f;

    [Header("Visual")]
    [SerializeField] private GameObject defaultVisualRoot;
    [SerializeField] private List<PustuleSpawnVisualVariant> visualVariants;
}
```

Expected responsibilities:

- Return a valid random `PustuleType`.
- Hide all spawn visuals before randomization.
- Show the visual that matches the selected pustule type.
- Provide a fallback spawn transform if `spawnPoint` is not assigned.
- Optionally provide local difficulty data such as radius override.

### 5.2 PustuleSpawnVisualVariant

Add a serializable visual entry.

```csharp
[System.Serializable]
public sealed class PustuleSpawnVisualVariant
{
    [SerializeField] private PustuleType pustuleType;
    [SerializeField] private GameObject visualRoot;
}
```

Possible visual setup:

```text
Small -> small marker
Big   -> larger marker
```

If no exact visual is assigned, the anchor can show `defaultVisualRoot`.

### 5.3 Pustule Spawn Options

The current `NeedleMiniGame` uses `List<Pustule> pustulePrefabs`. It should be changed into typed spawn options.

```csharp
[System.Serializable]
private sealed class PustuleSpawnOption
{
    [SerializeField] private PustuleType pustuleType;
    [SerializeField] private Pustule pustulePrefab;
}
```

This prevents the randomizer from spawning a Big prefab when the anchor requested Small, or spawning Small when the anchor is authored for Big only.

---

## 6. Pustule Visual State Design

Each pustule prefab should store two primary visual states.

```text
Pustule_World
- FullVisual
- EmptyVisual
- Collider2D
- Pustule
```

### 6.1 FullVisual

`FullVisual` means the pustule still contains pus.

It should be visible during:

```text
Spawned
Piercing
Pierced but not drained/squeezed
Active drain/squeeze phase
```

This means piercing does not automatically change the main visual into an empty state. The pustule is still full until the player completes the squeeze or drain phase.

### 6.2 EmptyVisual

`EmptyVisual` means the pus has been removed.

It should be visible when:

```text
drainProgress >= 1
completed == true
```

If the designer wants the empty state to remain visible after completion, set:

```text
hideWhenCompleted = false
```

If `hideWhenCompleted` remains true, the completed pustule may disappear immediately and the player may barely see the empty visual.

### 6.3 Recommended Pustule Fields

Add fields to `Pustule`:

```csharp
[Header("State Visuals")]
[SerializeField] private GameObject fullVisualRoot;
[SerializeField] private GameObject emptyVisualRoot;
```

Visual state rules:

| State | FullVisual | EmptyVisual |
|---|---:|---:|
| Not pierced | On | Off |
| Piercing | On | Off |
| Pierced | On | Off |
| Squeezing / Draining | On | Off |
| Completed | Off | On |
| Completed + hideWhenCompleted | Object hidden | Object hidden |

---

## 7. Runtime Flow

### 7.1 Begin Needle Mini Game

When Needle begins:

1. Clear previously spawned pustules.
2. Hide every `PustuleSpawnAnchor` visual.
3. Build a valid anchor list from the configured anchors.
4. Shuffle the valid anchors.
5. Choose a count between `minPustules` and `maxPustules`.
6. Clamp the count to available valid anchors.

### 7.2 Per Selected Anchor

For each selected anchor:

1. Randomize `PustuleType` from the anchor's allowed types.
2. Find a matching prefab from `PustuleSpawnOption`.
3. Spawn the prefab at the anchor spawn point.
4. Apply optional radius or difficulty override if enabled.
5. Add the spawned pustule to the active `pustules` list.
6. Show the anchor visual matching the selected type.

### 7.3 During Gameplay

Small:

```text
Needle equipped -> pierce -> bare hand squeeze -> complete
```

Big:

```text
Needle equipped -> pierce -> needle drain with radius check and jitter -> complete
```

### 7.4 Completion

The existing completion rule remains:

```text
All active pustules completed -> MiniGameCompleted event
```

If `autoCompleteWhenAllPustulesDone` is true, the mini game can return to the Anatomy screen automatically.

---

## 8. Unity Setup Plan

### 8.1 Needle Body Prefab Hierarchy

Each Needle body prefab should use this structure:

```text
Needle_Arm_BodyPrefab
- BodySpriteRoot
- PustuleRoot
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
PustuleSpawnAnchor
```

Optional visual layout:

```text
P1
- Visual_Small
- Visual_Big
```

### 8.2 Pustule Prefabs

Minimum required prefabs:

```text
SmallPustule_World
BigPustule_World
```

Recommended child layout:

```text
SmallPustule_World
- FullVisual
- EmptyVisual
- Collider2D
- Pustule
```

```text
BigPustule_World
- FullVisual
- EmptyVisual
- Collider2D
- Pustule
```

### 8.3 NeedleMiniGame Inspector

Replace:

```text
Pustule Prefabs: List<Pustule>
Spawn Anchors: List<Transform>
```

With:

```text
Pustule Spawn Options:
  - Small -> SmallPustule_World
  - Big   -> BigPustule_World

Spawn Anchors:
  - P1 (PustuleSpawnAnchor)
  - P2 (PustuleSpawnAnchor)
  - P3 (PustuleSpawnAnchor)
```

### 8.4 TreatmentBodyPrefab

For Needle body prefabs:

```text
Mini Game Type: Needle
Area: Arm / Head / Torso / Leg
Gameplay Root: prefab root
Body Sprite Root: BodySpriteRoot
Pustule Root: PustuleRoot
Pustule Spawn Anchors: all P# transforms or future PustuleSpawnAnchor references
```

If `TreatmentBodyPrefab` remains transform-based during the first implementation pass, `NeedleMiniGame` can resolve `PustuleSpawnAnchor` components from those transforms.

---

## 9. Implementation Plan

### Step 1 - Add PustuleSpawnAnchor

Create:

```text
Assets/Core/Script/Treatment/Needle/PustuleSpawnAnchor.cs
```

Responsibilities:

- Store allowed pustule types.
- Randomize a valid type.
- Hide and show spawn visuals.
- Return a spawn transform.
- Provide optional local setup overrides.

### Step 2 - Add Typed Spawn Options

In `NeedleMiniGame`:

- Add `PustuleSpawnOption`.
- Add a typed list for Small/Big prefab mapping.
- Keep the old `pustulePrefabs` list temporarily as a fallback if needed.

### Step 3 - Update Spawn Logic

Update `SpawnPustules()`:

- Hide all anchor visuals.
- Build valid anchor list.
- Shuffle anchors.
- Select unique anchors.
- Randomize type per selected anchor.
- Find a matching prefab by type.
- Instantiate the pustule.
- Show matching anchor visual.

### Step 4 - Add Full/Empty Visuals To Pustule

Update `Pustule`:

- Add `fullVisualRoot`.
- Add `emptyVisualRoot`.
- Update `ApplyVisualState()` so completed state switches to empty visual.

### Step 5 - Optional Override Support

If needed, add a public method to `Pustule`:

```csharp
public void ApplySpawnSettings(float? radiusOverride)
```

This allows an anchor to tune difficulty without making extra prefabs for every size.

### Step 6 - Editor Setup Support

Update `NeedleMiniGameSetup` if needed:

- Add `PustuleSpawnAnchor` to generated anchor objects.
- Fill default allowed values.
- Assign generated anchors to the Needle mini game.
- Preserve authored scene layout and avoid resetting manually arranged objects.

### Step 7 - Testing

Test cases:

| Test | Expected Result |
|---|---|
| Spawn count 3-7 | Spawned pustules match configured min/max. |
| Duplicate prevention | No two pustules use the same anchor in one run. |
| Small-only anchor | Only Small pustule can spawn there. |
| Big-only anchor | Only Big pustule can spawn there. |
| Mixed anchor | Small or Big can spawn there. |
| Selected anchor visual | Only selected anchors show visuals. |
| Visual type match | Small/Big visual matches randomized type. |
| Pustule starts full | `FullVisual` is visible when spawned. |
| Pierced pustule remains full | `FullVisual` stays visible after pierce. |
| Completed pustule becomes empty | `EmptyVisual` appears after squeeze/drain. |
| hideWhenCompleted false | Empty visual remains visible after completion. |

---

## 10. Risk Notes

### 10.1 Immediate Hide On Completion

If `hideWhenCompleted` is true, the pustule object may be hidden immediately after completion. This can make `EmptyVisual` hard to see.

Recommended first setup:

```text
hideWhenCompleted = false
```

for final art prefabs that should leave an empty mark on the body.

### 10.2 Existing Serialized Fields

Changing `pustulePrefabs` and `spawnAnchors` directly can break current scene assignments.

Recommended migration:

```text
Old Transform anchors -> resolve PustuleSpawnAnchor if present.
Old Pustule prefab list -> fallback random prefab list if typed options are empty.
```

After setup is stable, old fields can be hidden or removed later.

### 10.3 Body Prefab Catalog Integration

This design is compatible with the body prefab catalog system, but full automatic body prefab swapping still requires a later hook:

```text
AnatomyController -> TreatmentBodyPrefabSpawner -> NeedleMiniGame
```

This report only defines Needle pustule randomization and visual state.

---

## 11. Recommended Implementation Order

1. Add `PustuleSpawnAnchor`.
2. Add `PustuleSpawnVisualVariant`.
3. Add typed `PustuleSpawnOption` to `NeedleMiniGame`.
4. Update `NeedleMiniGame.SpawnPustules()`.
5. Add `FullVisual` and `EmptyVisual` fields to `Pustule`.
6. Update `Pustule.ApplyVisualState()`.
7. Update editor setup helper if needed.
8. Configure one test prefab: `Needle_Arm_BodyPrefab`.
9. Run Unity play test.
10. Expand setup to Head, Torso, and Leg Needle body prefabs.

---

## 12. Final Design Summary

The Needle mini game should move from simple transform-based spawning to anchor-driven pustule randomization.

Each Needle spawn point will control:

```text
Allowed pustule type
Spawn point transform
Selected-state visual
Optional local difficulty settings
```

The runtime randomizer will choose unique anchors, randomize `Small` or `Big` according to each anchor's rules, spawn the matching pustule prefab, and show only the selected spawn point visuals.

Each pustule prefab will store two main visuals:

```text
FullVisual  -> pus is still inside
EmptyVisual -> pus has been removed
```

This keeps the current Needle interaction intact while making the prefab setup clearer for final art and designer-authored spawn layouts.
