# Knife Lesion Spawn Validation Technical Design Report

## 1. Objective

This report defines a safer spawn system for the Knife MiniGame lesions.

The current Knife MiniGame can spawn lesions in visually bad positions:

- Two or more lesions can appear too close together and overlap.
- A lesion can randomly spawn as a vertical cut even when the selected body part, such as an arm or leg, does not have enough visual height.
- Vertical lesions can extend outside the body sprite because the current spawn check only validates the anchor itself, not the final rotated lesion footprint.

The goal is to keep the authored random spawn feeling while preventing unfair or visually broken lesion placement.

## Implementation Status (reconciled 2026-07-06)

This document is a **design proposal**. As of this update, the validation layer it proposes is
**NOT implemented**. Only the anchor-based random spawn described in Section 2 exists in code.

**Already in the project (Section 2 is accurate):**

- `LesionSpawnAnchor` per-anchor flags: `allowTumor`, `allowBulge` (`Allowed Lesions`) and
  `allowHorizontal`, `allowVertical` (`Allowed Cut Directions`), plus `IsUsable`,
  `TryGetRandomLesionType`, `TryGetRandomOrientation`, and `GetSpawnRotation` (vertical = base + 90°).
- Per-anchor visual variants keyed by lesion type + orientation (`ShowSpawnVisual`).
- `KnifeMiniGame.SpawnLesions()` two-phase-free flow: shuffle anchors → clamp count to anchor count →
  instantiate at `anchor.SpawnPoint.position` with `anchor.GetRotation(orientation)`.
- Duplicate spawn-point avoidance (in `GetShuffledAnchors`) and spawned-count already clamped to the
  number of usable anchors.
- Runtime anchor wrapper type already exists: `RuntimeLesionAnchor` (nested in `KnifeMiniGame`).

**NOT implemented yet (everything below in Sections 4, 6, 7, 11 is still a proposal):**

- `Lesion.spawnFootprint` / `Lesion.spawnPadding` and any footprint bounds/corner API.
- `TreatmentBodyPrefab.lesionSafeArea` (`TreatmentBodyPrefab` currently exposes only `lesionRoot` and
  `lesionSpawnAnchors` for the Knife path — no safe-area collider).
- `LesionSpawnCandidate`, `TryBuildValidCandidate`, `IsCandidateInsideSafeArea`,
  `DoesCandidateOverlapAccepted` — no candidate/validation pass exists.
- Overlap prevention, safe-area corner checks, orientation retry, and rejection debug logs.
- Editor gizmo footprint preview on `LesionSpawnAnchor` and any `Validate Knife Spawn Anchors` tool.

## 2. Current System Analysis

### 2.1 Current Runtime Flow

The current flow in `KnifeMiniGame` is:

1. `AnatomyController` selects a treatment area and resolves the Knife body prefab.
2. `KnifeMiniGame.ApplyBodyPrefab()` reads `TreatmentBodyPrefab.GetLesionAnchorTransforms()` and rebuilds
   its `lesionSpawnAnchors` / `spawnAnchors` lists.
3. `PrepareLesions()` calls `SpawnLesions()` when both a lesion prefab source and an anchor source exist
   (otherwise it falls back to pre-placed lesions via `RefreshLesionList()`).
4. `SpawnLesions()` calls `GetShuffledAnchors()` (which also skips duplicate spawn points), then sets
   `count = min(Random(minLesions, maxLesions + 1), anchors.Count)`.
5. For each selected anchor, `TryGetPrefabForAnchor()` draws:
   - lesion type (`Tumor` / `Bulge`) from the anchor's allowed set (`TryGetRandomLesionType`);
   - cut orientation (`Horizontal` / `Vertical`) from the anchor's allowed set (`TryGetRandomOrientation`);
   - a matching lesion prefab.
6. The lesion is instantiated at `anchor.SpawnPoint.position` with `anchor.GetRotation(orientation)`
   (vertical orientation rotates the anchor's base rotation by +90°).
7. `LesionSpawnAnchor.ShowSpawnVisual(type, orientation)` enables the matching visual variant.

### 2.2 What Already Works

The existing system already supports these authoring rules (verified in `LesionSpawnAnchor`):

- Each spawn anchor can allow or block `Tumor` (`allowTumor`) and `Bulge` (`allowBulge`).
- Each spawn anchor can allow or block `Horizontal` (`allowHorizontal`) and `Vertical` (`allowVertical`).
- Each anchor can show different visual variants for lesion type and orientation.
- Duplicate use of the exact same spawn point is already avoided.
- The spawned count is already clamped to the number of usable anchors, so the game never forces more
  lesions than there are anchors (but it does not yet reduce the count for overlap / safe-area reasons).

### 2.3 Current Missing Checks

The current system does not validate the final occupied area of a spawned lesion. It does not check:

- whether the rotated lesion footprint stays inside the body shape;
- whether the lesion footprint overlaps another accepted lesion;
- whether an anchor has enough room for vertical orientation;
- whether the min/max lesion count is realistic for the current body part beyond the anchor-count clamp.

Because of this, an anchor can be valid by itself while the final vertical lesion is invalid after
rotation, and two anchors placed close together can still overlap.

## 3. Design Goals

The improved spawn system should:

- prevent lesion overlap;
- prevent lesions from extending outside the visible body area;
- preserve random spawn variety;
- support different body part shapes;
- keep designer control per spawn anchor;
- avoid forcing the game to spawn the maximum count when there is not enough safe room;
- provide editor visualization so spawn issues can be fixed before playtesting.

## 4. Proposed Design

> Status: **Not implemented — proposal.** Sections 4–11 describe work still to be done. Names below are
> reconciled to current code where a type already exists (e.g. the runtime wrapper is the existing
> `RuntimeLesionAnchor` nested in `KnifeMiniGame`, and lesion type/orientation enums are `LesionType` /
> `LesionCutOrientation`). `TreatmentBodyPrefab` and `Lesion` do **not** yet have the fields shown here.

### 4.1 Add Lesion Spawn Footprint

Each `Lesion` prefab should expose a spawn footprint.

Recommended fields:

```csharp
[Header("Spawn Footprint")]
[SerializeField] private Vector2 spawnFootprint = new Vector2(0.8f, 0.35f);
[SerializeField, Min(0f)] private float spawnPadding = 0.08f;
```

Purpose:

- `spawnFootprint.x` describes the lesion width.
- `spawnFootprint.y` describes the lesion height.
- `spawnPadding` adds spacing around the lesion so two lesions do not visually touch.

For vertical orientation, the system should rotate this footprint with the candidate orientation.

### 4.2 Add Body Safe Area

Each Knife body prefab should define where lesions are allowed to appear.

Recommended object:

```text
Knife_Arm_BodyPrefab
  BodySpriteRoot
  LesionRoot
  Spawn Points
  LesionSafeArea
```

`LesionSafeArea` can use:

- `PolygonCollider2D` for accurate arms/legs/body shapes;
- `BoxCollider2D` for simple rectangular areas;
- `CompositeCollider2D` later if the art needs complex multi-shape areas.

Recommended field on `TreatmentBodyPrefab`:

```csharp
[Header("Knife")]
[SerializeField] private Collider2D lesionSafeArea;
public Collider2D LesionSafeArea => lesionSafeArea;
```

The collider should be a trigger and should cover only the part of the body where a lesion is allowed to fully fit.

### 4.3 Runtime Candidate Validation

Before instantiating a lesion, `KnifeMiniGame` should build a candidate spawn result:

```text
Candidate
- anchor
- lesion prefab
- lesion type
- orientation
- world position
- world rotation
- oriented footprint bounds
```

Then validate it:

1. Is the candidate inside `LesionSafeArea`?
2. Does the candidate overlap any already accepted lesion candidate?
3. Is the candidate type allowed by this anchor?
4. Is the candidate orientation allowed by this anchor?

Only accepted candidates should be instantiated.

### 4.4 Overlap Prevention

The mini game should store accepted candidate bounds before spawning the actual objects.

Simple rule:

```csharp
if (candidateBounds.Overlaps(existingAcceptedBounds))
{
    rejectCandidate;
}
```

This is intentionally conservative. It is better to spawn fewer lesions than to spawn overlapping lesions.

### 4.5 Safe Area Validation

For each candidate, calculate the four footprint corners after rotation.

Then check whether all corners are inside the safe area:

```csharp
bool fits = safeArea == null || AllCornersInsideCollider(safeArea, candidateCorners);
```

If `safeArea` is missing, the system should fall back to the current behavior and log a debug warning when debug mode is enabled.

### 4.6 Orientation Retry

When a random orientation fails, the system should not immediately reject the whole anchor.

Recommended retry order:

1. Choose random type and random orientation from the anchor.
2. Validate candidate.
3. If invalid, try the other allowed orientation for the same type.
4. If still invalid, try another allowed type.
5. If all combinations fail, skip this anchor.

This keeps random behavior while giving the system a better chance to fill the body safely.

### 4.7 Spawn Count Behavior

The requested count should remain random between `minLesions` and `maxLesions`.

However, final spawned count can be lower if not enough valid candidates exist.

Recommended rule:

```text
requestedCount = Random.Range(minLesions, maxLesions + 1)
spawnedCount = min(requestedCount, validAcceptedCandidates.Count)
```

If the final count is below `minLesions`, the game should still continue if at least one lesion was spawned.

If zero lesions can be spawned, the system should:

- log a warning;
- fall back to pre-placed lesions if available;
- otherwise keep the mini game incomplete and clearly report the setup issue.

## 5. Editor Authoring Rules

### 5.1 Arm and Leg Prefabs

For narrow body parts, such as arm and leg:

- use more horizontal anchors than vertical anchors;
- disable `Allow Vertical` near the top and bottom edges;
- disable `Allow Vertical` near wrists, ankles, hands, and feet;
- allow vertical only in wide central areas;
- keep spawn anchors inside the safe area, not exactly on the border.

### 5.2 Torso Prefabs

For torso:

- vertical orientation can be allowed more often;
- safe area can be larger;
- min/max lesion count can be higher than arm/leg.

### 5.3 Head Prefabs

For head:

- use fewer lesion anchors;
- avoid vertical orientation unless the art has a clear vertical-safe zone;
- use small footprints or smaller lesion prefabs if needed.

## 6. Editor Preview Design

`LesionSpawnAnchor` should draw editor gizmos for spawn validation.

Recommended preview:

- green outline: horizontal footprint fits safe area;
- cyan outline: vertical footprint fits safe area;
- red outline: footprint does not fit safe area;
- yellow outline: footprint is close to another anchor footprint;
- small label: allowed lesion types and orientations.

This preview should work without entering Play Mode.

Optional future improvement:

- add a custom inspector button: `Validate Knife Spawn Anchors`.
- report invalid anchors in Console.
- select invalid anchor objects automatically.

## 7. Implementation Plan

### Step 1: Add Lesion Footprint Data

Modify `Lesion.cs`:

- add `spawnFootprint`;
- add `spawnPadding`;
- expose a method to calculate candidate footprint corners/bounds.

Suggested public methods:

```csharp
public Vector2 SpawnFootprint => spawnFootprint;
public float SpawnPadding => spawnPadding;
public Bounds GetSpawnBounds(Vector2 worldPosition, Quaternion worldRotation, LesionCutOrientation orientation);
public void GetSpawnCorners(Vector2 worldPosition, Quaternion worldRotation, LesionCutOrientation orientation, Vector2[] corners);
```

### Step 2: Add Knife Safe Area to Body Prefab

Modify `TreatmentBodyPrefab.cs`:

- add `Collider2D lesionSafeArea`;
- expose `LesionSafeArea`.

Prefab setup:

- add `LesionSafeArea` object to every Knife body prefab;
- assign collider reference in `TreatmentBodyPrefab`;
- ensure collider covers only valid lesion area.

### Step 3: Add Candidate Struct in KnifeMiniGame

Inside `KnifeMiniGame.cs`, add an internal runtime candidate type:

```csharp
private readonly struct LesionSpawnCandidate
{
    public readonly RuntimeLesionAnchor Anchor;
    public readonly Lesion Prefab;
    public readonly LesionType Type;
    public readonly LesionCutOrientation Orientation;
    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
    public readonly Bounds Bounds;
}
```

### Step 4: Refactor SpawnLesions

Change `SpawnLesions()` from direct instantiate to two phases:

1. Build accepted candidate list.
2. Instantiate accepted candidates.

Pseudo flow:

```text
Clear existing spawned lesions
Hide all spawn visuals
Shuffle anchors
requestedCount = random min/max
accepted = []

for each anchor:
    if accepted.Count >= requestedCount:
        break

    if TryBuildValidCandidate(anchor, accepted, out candidate):
        accepted.Add(candidate)

Instantiate accepted candidates
```

### Step 5: Candidate Validation

Add:

```csharp
private bool TryBuildValidCandidate(RuntimeLesionAnchor anchor, List<LesionSpawnCandidate> accepted, out LesionSpawnCandidate candidate)
private bool IsCandidateInsideSafeArea(LesionSpawnCandidate candidate)
private bool DoesCandidateOverlapAccepted(LesionSpawnCandidate candidate, List<LesionSpawnCandidate> accepted)
```

Validation order should be:

1. candidate prefab exists;
2. safe area contains all footprint corners;
3. candidate does not overlap accepted candidates.

### Step 6: Add Gizmos to LesionSpawnAnchor

Modify `LesionSpawnAnchor.cs`:

- draw horizontal and vertical footprint previews;
- optionally expose preview footprint size for anchors if no prefab is available in edit mode.

Recommended simple first version:

```csharp
[Header("Editor Preview")]
[SerializeField] private Vector2 previewFootprint = new Vector2(0.8f, 0.35f);
[SerializeField] private float previewPadding = 0.08f;
```

This avoids requiring the anchor to know all possible lesion prefabs.

### Step 7: Add Debug Logs

When `debugTreatmentFlow` is enabled, log:

- requested lesion count;
- accepted lesion count;
- skipped anchor name;
- rejection reason:
  - outside safe area;
  - overlaps another lesion;
  - no valid prefab;
  - no valid orientation.

This makes setup mistakes easy to diagnose.

## 8. Unity Setup Guide

### 8.1 For Each Knife Body Prefab

Open each Knife body prefab:

- `Knife_Arm_BodyPrefab`
- `Knife_Leg_BodyPrefab`
- `Knife_Torso_BodyPrefab`
- `Knife_Head_BodyPrefab`

Create:

```text
LesionSafeArea
```

Add:

- `PolygonCollider2D` or `BoxCollider2D`
- `Is Trigger = true`

Then assign it to:

```text
TreatmentBodyPrefab > Knife > Lesion Safe Area
```

### 8.2 For Each Lesion Prefab

Set:

- `Spawn Footprint`
- `Spawn Padding`

Recommended starting values:

| Prefab | Spawn Footprint | Padding |
|---|---:|---:|
| Tumor | `0.85, 0.38` | `0.08` |
| Bulge | `0.75, 0.34` | `0.08` |

Adjust after seeing the actual sprite size.

### 8.3 For Each Spawn Anchor

Set:

- `Allow Tumor`
- `Allow Bulge`
- `Allow Horizontal`
- `Allow Vertical`

Recommended arm/leg rule:

- outer/top/bottom anchors: horizontal only;
- middle wide anchors: horizontal and vertical;
- very narrow zones: reduce lesion types or disable the anchor.

## 9. Test Plan

### 9.1 Arm Test

Run Knife MiniGame on arm body:

- lesions should not overlap;
- vertical lesions should only appear in wide enough zones;
- no lesion should extend outside the arm visual.

### 9.2 Leg Test

Run Knife MiniGame on leg body:

- vertical orientation should be rare or disabled near narrow areas;
- lesions should remain inside the leg safe area.

### 9.3 Torso Test

Run Knife MiniGame on torso body:

- more vertical lesions are acceptable;
- count should still respect overlap spacing.

### 9.4 Stress Test

Set:

```text
Min Lesions = 5
Max Lesions = 7
```

On a narrow body part:

- system should spawn only valid lesions;
- if there is not enough room, it should spawn fewer lesions instead of overlapping.

## 10. Risks and Mitigations

### Risk: Too Few Lesions Spawn

If safe area and overlap rules are too strict, narrow body parts may spawn fewer lesions.

Mitigation:

- lower min/max lesion count for arm/leg;
- add more anchors;
- reduce lesion footprint;
- widen safe area only where visually valid.

### Risk: Designer Setup Takes Longer

Safe area and orientation rules require additional authoring.

Mitigation:

- use gizmo previews;
- provide validation logs;
- use prefab templates for each body part.

### Risk: Collider ContainsPoint Is Too Strict

Checking only corners may reject valid shapes or accept edge cases.

Mitigation:

- start with corner checks;
- later add center and edge midpoint checks;
- use `PolygonCollider2D` for better body fit.

## 11. Recommended Implementation Order

1. Add `spawnFootprint` and `spawnPadding` to `Lesion`.
2. Add `lesionSafeArea` to `TreatmentBodyPrefab`.
3. Add candidate validation in `KnifeMiniGame.SpawnLesions()`.
4. Add rejection debug logs.
5. Add simple gizmo footprint preview to `LesionSpawnAnchor`.
6. Setup safe areas in Knife body prefabs.
7. Tune anchors and orientation rules per body part.

## 12. Final Decision

The Knife MiniGame should move from simple anchor randomization to validated random spawning.

The design keeps the existing anchor-based workflow, but adds:

- lesion footprint awareness;
- body safe area checks;
- overlap prevention;
- per-anchor orientation rules;
- editor visualization.

This solves the current issue without replacing the existing Knife MiniGame architecture.

