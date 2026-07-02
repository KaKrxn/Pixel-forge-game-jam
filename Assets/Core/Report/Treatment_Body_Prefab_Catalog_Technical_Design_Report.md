# Treatment Body Prefab Catalog Technical Design Report

**Project:** Cure Me, Please  
**Module:** Treatment System - Mini Game Body Prefab Routing  
**Engine:** Unity 6000.3.17f1, 2D, World-space Treatment Mini Games  
**Document status:** Active design specification and next implementation target  
**Last updated:** July 3, 2026

---

## 1. Purpose

This report defines a new system for selecting the correct body prefab when entering a treatment mini game.

The project currently routes treatment by:

```text
BodyArea + TreatmentMiniGameType
```

For example:

```text
Arm + Tongs
Torso + Knife
Leg + Needle
```

The new system should use that same pair to spawn the correct treatment body prefab. Each mini game needs its own body prefab per body area because spawn points, masks, guide lines, and hit areas are different for each treatment type.

---

## 2. Problem

Using one shared treatment body for every mini game is too limiting.

Different mini games need different authored data:

| Mini Game | Needs |
|---|---|
| Tongs | Parasite spawn anchors, wound masks, parasite reveal masks, parasite roots. |
| Knife | Lesion spawn anchors, cut guide lines, lesion roots, body hit layout. |
| Needle | Pustule spawn anchors, pierce/squeeze/drain layout, pustule roots. |

Even if the player selects the same body area, the prefab can be different depending on the required mini game.

Example:

```text
Arm + Tongs  -> body prefab with parasite masks
Arm + Needle -> body prefab with pustule anchors
Arm + Knife  -> body prefab with lesion/cut-path anchors
```

Therefore the body prefab must be selected from both:

```text
BodyArea
TreatmentMiniGameType
```

not from `BodyArea` alone.

---

## 3. Current Project Systems To Reuse

Current relevant scripts:

```text
Assets/Core/Script/Treatment/Anatomy/AnatomyController.cs
Assets/Core/Script/Treatment/Anatomy/BodyArea.cs
Assets/Core/Script/Treatment/Case/TreatmentMiniGameType.cs
Assets/Core/Script/Treatment/Case/TreatmentCaseData.cs
Assets/Core/Script/Treatment/Case/TreatmentAreaRequirement.cs
Assets/Core/Script/Treatment/Tongs/TongsMiniGame.cs
Assets/Core/Script/Treatment/Knife/KnifeMiniGame.cs
Assets/Core/Script/Treatment/Needle/NeedleMiniGame.cs
```

The current case data already stores:

```csharp
BodyArea area;
TreatmentMiniGameType miniGameType;
```

That means this system does **not** need a new case-routing concept. It only needs to resolve the visual/gameplay body prefab from the existing treatment requirement.

### 3.1 Current Implementation Snapshot

As of July 3, 2026, the project already has several systems that this catalog should reuse rather than replace.

Implemented or partially implemented:

| System | Current status | Notes |
|---|---|---|
| `TreatmentCaseData` / `TreatmentAreaRequirement` | Implemented | Stores `BodyArea + TreatmentMiniGameType` per required treatment area. |
| `TreatmentCaseRuntime` | Implemented | Tracks required areas and treated areas during the active customer case. |
| `AnatomyController` | Implemented | Resolves the selected body area into the required mini game and returns to Anatomy after mini game completion. |
| `TongsMiniGame` | Implemented baseline | Supports parasite prefab/type options, runtime spawn anchors, non-repeating spawn points, tool selection, and completion state. |
| `ParasiteSpawnAnchor` | Implemented baseline | Provides optional wound mask, spawn point, pull direction, and required direction data. |
| `ParasiteReveal` | Implemented baseline | Supports visible head, masked body, runtime reveal mask, and pull-driven reveal behavior. |
| `KnifeMiniGame` | Implemented baseline | Has its own root/spawn setup and is ready to receive body-specific lesion anchors later. |
| `NeedleMiniGame` | Implemented baseline | Has its own root/spawn setup and is ready to receive body-specific pustule anchors later. |

Not implemented yet:

| System | Needed for this report |
|---|---|
| `TreatmentBodyPrefab` root component | Exposes body prefab metadata, body area, mini game type, roots, and authored anchors. |
| `TreatmentBodyPrefabCatalog` ScriptableObject | Maps `TreatmentMiniGameType + BodyArea` to a prefab. |
| `TreatmentBodyPrefabSpawner` | Instantiates the correct body prefab when a treatment area starts. |
| Anatomy-to-body-prefab hook | Lets `AnatomyController` spawn and pass the body prefab before mini game begin. |
| Mini game body binding methods | Lets Tongs, Knife, and Needle consume roots/anchors from the spawned body prefab. |

The current Tongs scene setup should be treated as the live prototype for what a future `Tongs_Arm_BodyPrefab` needs to contain.

---

## 4. Core Design

### 4.1 Key Lookup

The core lookup key is:

```text
TreatmentMiniGameType + BodyArea
```

Example table:

| Mini Game | Head | Torso | Arm | Leg |
|---|---|---|---|---|
| Tongs | Tongs_Head_BodyPrefab | Tongs_Torso_BodyPrefab | Tongs_Arm_BodyPrefab | Tongs_Leg_BodyPrefab |
| Knife | Knife_Head_BodyPrefab | Knife_Torso_BodyPrefab | Knife_Arm_BodyPrefab | Knife_Leg_BodyPrefab |
| Needle | Needle_Head_BodyPrefab | Needle_Torso_BodyPrefab | Needle_Arm_BodyPrefab | Needle_Leg_BodyPrefab |

### 4.2 Runtime Flow

```text
Player selects a body area in Anatomy.
AnatomyController checks active TreatmentCaseData.
TreatmentCaseData returns the mini game required for that area.
BodyPrefabCatalog resolves BodyArea + TreatmentMiniGameType.
BodyPrefabSpawner instantiates that body prefab under the active mini game root.
Mini game reads its roots / anchors from the spawned body prefab.
Mini game begins.
```

### 4.3 Current Fallback Requirement

The first implementation must keep all existing scene-authored mini game roots working.

If no body prefab catalog is assigned, or if a catalog entry is missing, the mini game should continue using its current serialized scene references:

```text
TongsMiniGame.parasiteRoot + spawnAnchors
KnifeMiniGame existing lesion roots/anchors
NeedleMiniGame existing pustule roots/anchors
```

This is important because the current test scene already has working manually authored objects, and the project should migrate gradually.

---

## 5. Recommended Data Model

### 5.1 TreatmentBodyPrefabCatalog

Use a ScriptableObject as the project-level catalog.

Recommended path:

```text
Assets/Core/Data/Treatment/BodyPrefabs/TreatmentBodyPrefabCatalog.asset
```

Recommended script:

```text
Assets/Core/Script/Treatment/BodyPrefab/TreatmentBodyPrefabCatalog.cs
```

Sketch:

```csharp
[CreateAssetMenu(fileName = "TreatmentBodyPrefabCatalog", menuName = "Pixel Forge/Treatment/Body Prefab Catalog")]
public sealed class TreatmentBodyPrefabCatalog : ScriptableObject
{
    [SerializeField] private List<TreatmentBodyPrefabEntry> entries;

    public bool TryGetPrefab(TreatmentMiniGameType miniGameType, BodyArea area, out TreatmentBodyPrefab prefab)
    {
        // Search entries by miniGameType + area.
    }
}
```

### 5.2 TreatmentBodyPrefabEntry

```csharp
[Serializable]
public sealed class TreatmentBodyPrefabEntry
{
    [SerializeField] private TreatmentMiniGameType miniGameType;
    [SerializeField] private BodyArea area;
    [SerializeField] private TreatmentBodyPrefab prefab;
}
```

This flat list is recommended over nested fields because it is easier to expand later if the project adds:

- alternate poses,
- difficulty variants,
- customer species variants,
- left/right limb variants,
- special boss cases.

---

## 6. Body Prefab Component

Each body prefab should have a root component so mini games can read authored references in a consistent way.

Recommended script:

```text
Assets/Core/Script/Treatment/BodyPrefab/TreatmentBodyPrefab.cs
```

Sketch:

```csharp
public sealed class TreatmentBodyPrefab : MonoBehaviour
{
    [SerializeField] private TreatmentMiniGameType miniGameType;
    [SerializeField] private BodyArea area;
    [SerializeField] private Transform gameplayRoot;
    [SerializeField] private Transform spawnRoot;

    [Header("Tongs")]
    [SerializeField] private Transform parasiteRoot;
    [SerializeField] private List<ParasiteSpawnAnchor> parasiteSpawnAnchors;

    [Header("Knife")]
    [SerializeField] private Transform lesionRoot;
    [SerializeField] private List<Transform> lesionSpawnAnchors;

    [Header("Needle")]
    [SerializeField] private Transform pustuleRoot;
    [SerializeField] private List<Transform> pustuleSpawnAnchors;
}
```

The first implementation can keep the references simple. It does not need to be fully generic on day one.

### 6.1 Minimum First Version

The first version should expose only what is needed by the current mini games.

Minimum required fields:

```text
TreatmentMiniGameType MiniGameType
BodyArea Area
Transform GameplayRoot
Transform BodySpriteRoot
```

Tongs-specific fields:

```text
Transform ParasiteRoot
List<Transform> ParasiteSpawnAnchors
```

Knife-specific fields:

```text
Transform LesionRoot
List<Transform> LesionSpawnAnchors
```

Needle-specific fields:

```text
Transform PustuleRoot
List<Transform> PustuleSpawnAnchors
```

The lists can use `Transform` first because the current mini games already accept transform anchors. Specific anchor components can be added later where needed.

---

## 7. Prefab Hierarchy

### 7.1 Tongs Body Prefab

```text
Tongs_Arm_BodyPrefab
├─ TreatmentBodyPrefab
├─ BodySprite
├─ ParasiteRoot
├─ SpawnAnchors
│  ├─ ParasiteSpawnAnchor_01
│  │  ├─ WoundMask
│  │  └─ WoundSprite
│  ├─ ParasiteSpawnAnchor_02
│  │  ├─ WoundMask
│  │  └─ WoundSprite
│  └─ ParasiteSpawnAnchor_03
│     ├─ WoundMask
│     └─ WoundSprite
└─ OptionalVFXRoot
```

Tongs-specific contents:

- Parasite spawn points.
- Parasite masks / wound masks.
- Wound sprites.
- Optional parasite reveal references.

### 7.2 Knife Body Prefab

```text
Knife_Leg_BodyPrefab
├─ TreatmentBodyPrefab
├─ BodySprite
├─ LesionRoot
├─ SpawnAnchors
│  ├─ LesionSpawnAnchor_01
│  ├─ LesionSpawnAnchor_02
│  └─ LesionSpawnAnchor_03
├─ CutGuideRoot
└─ OptionalBloodVFXRoot
```

Knife-specific contents:

- Lesion spawn points.
- Cut guide line anchors.
- Optional default lesion placements.
- Blood / cut VFX roots.

### 7.3 Needle Body Prefab

```text
Needle_Arm_BodyPrefab
├─ TreatmentBodyPrefab
├─ BodySprite
├─ PustuleRoot
├─ SpawnAnchors
│  ├─ PustuleSpawnAnchor_01
│  ├─ PustuleSpawnAnchor_02
│  └─ PustuleSpawnAnchor_03
└─ OptionalFluidVFXRoot
```

Needle-specific contents:

- Pustule spawn points.
- Pierce/squeeze/drain layout anchors.
- Optional fluid VFX roots.

---

## 8. Body Prefab Spawner

Add a component that owns the active spawned body prefab.

Recommended script:

```text
Assets/Core/Script/Treatment/BodyPrefab/TreatmentBodyPrefabSpawner.cs
```

Responsibilities:

- Hold a `TreatmentBodyPrefabCatalog`.
- Hold a spawn parent transform, likely under `Treatment RoomRoot / 03_Gameplay`.
- Destroy or deactivate the previous body prefab when a new area starts.
- Instantiate the resolved body prefab.
- Return the spawned `TreatmentBodyPrefab` to the caller.

Sketch:

```csharp
public sealed class TreatmentBodyPrefabSpawner : MonoBehaviour
{
    [SerializeField] private TreatmentBodyPrefabCatalog catalog;
    [SerializeField] private Transform bodyParent;

    private TreatmentBodyPrefab activeBody;

    public bool TrySpawn(TreatmentMiniGameType miniGameType, BodyArea area, out TreatmentBodyPrefab body)
    {
        ClearActiveBody();

        if (catalog == null || !catalog.TryGetPrefab(miniGameType, area, out TreatmentBodyPrefab prefab))
        {
            body = null;
            return false;
        }

        activeBody = Instantiate(prefab, bodyParent);
        body = activeBody;
        return body != null;
    }
}
```

---

## 9. Integration With AnatomyController

Current `AnatomyController.EnterInfectedArea(area)` already resolves the mini game:

```text
GetMiniGameTypeForArea(area)
```

The new body-prefab system should run after that lookup and before starting the mini game.

Recommended flow:

```text
TreatmentMiniGameType miniGameType = GetMiniGameTypeForArea(area);
TreatmentBodyPrefab bodyPrefab = bodySpawner.Spawn(miniGameType, area);
Start selected mini game using the spawned body prefab data.
```

For the first implementation, each mini game can expose a method like:

```csharp
public void SetBodyPrefab(TreatmentBodyPrefab bodyPrefab);
```

Then:

```csharp
tongsMiniGame.SetBodyPrefab(bodyPrefab);
tongsMiniGame.Begin(activeCustomer);
```

Longer term, mini games could share an interface, but that is optional.

---

## 10. Mini Game Integration

### 10.1 TongsMiniGame

`TongsMiniGame` should receive:

- `parasiteRoot`
- parasite spawn anchors
- optional `ParasiteSpawnAnchor` data
- optional wound mask / reveal anchor data through child `ParasiteSpawnAnchor` components

Current `TongsMiniGame` already has:

```csharp
Transform parasiteRoot;
List<Transform> spawnAnchors;
```

The new body prefab can populate these at runtime.

Recommended method:

```csharp
public void SetBodyPrefab(TreatmentBodyPrefab body)
{
    parasiteRoot = body.ParasiteRoot;
    spawnAnchors = body.GetParasiteAnchorTransforms();
}
```

Current Tongs-specific behavior to preserve:

- Runtime parasite prefab selection through parasite spawn options.
- Non-repeating spawn anchors per run.
- `ParasiteSpawnAnchor` optional component lookup.
- `ParasiteReveal` mask binding.
- Existing fallback anchors if no body prefab is provided.

### 10.2 KnifeMiniGame

`KnifeMiniGame` should receive:

- `lesionRoot`
- lesion spawn anchors

Current `KnifeMiniGame` already has:

```csharp
Transform lesionRoot;
List<Transform> spawnAnchors;
```

The new body prefab can populate these at runtime.

Current Knife behavior to preserve:

- Knife-only tool gating.
- Lesion/bulge interaction flow.
- Existing scene-authored roots if no body prefab is available.

### 10.3 NeedleMiniGame

`NeedleMiniGame` should receive:

- `pustuleRoot`
- pustule spawn anchors

Current `NeedleMiniGame` already has:

```csharp
Transform pustuleRoot;
List<Transform> spawnAnchors;
```

The new body prefab can populate these at runtime.

Current Needle behavior to preserve:

- Needle-only tool gating.
- Pustule pierce/squeeze/drain flow.
- Existing scene-authored roots if no body prefab is available.

---

## 11. Relation To TreatmentCaseData

The body prefab should not be selected manually in every case unless a special override is needed.

Default:

```text
TreatmentCaseData requirement says: Arm + Needle
Catalog resolves: Needle_Arm_BodyPrefab
```

Optional future override:

```csharp
TreatmentAreaRequirement
├─ BodyArea area
├─ TreatmentMiniGameType miniGameType
├─ TreatmentBodyPrefab overrideBodyPrefab
└─ DesignerNote
```

Do **not** add the override in the first implementation unless a specific case needs it. Keep the first version catalog-driven.

---

## 12. Detailed Implementation Plan

### Phase 1 - Report And Data Design

Goal: Lock the data model before code changes.

Tasks:

1. Define the catalog.
2. Define the prefab root component.
3. Define how mini games receive body prefab data.
4. Decide folder structure.

Validation:

- The team can name every needed prefab using `MiniGameType_BodyArea_BodyPrefab`.
- The catalog can represent all current combinations.

Status:

```text
In progress / this report updated on July 3, 2026.
```

### Phase 2 - Core Runtime Components

Goal: Add the runtime system without changing mini game behavior yet.

Tasks:

1. Add `TreatmentBodyPrefab`.
2. Add `TreatmentBodyPrefabEntry`.
3. Add `TreatmentBodyPrefabCatalog`.
4. Add `TreatmentBodyPrefabSpawner`.
5. Create the asset folder:

```text
Assets/Core/Script/Treatment/BodyPrefab
Assets/Core/Data/Treatment/BodyPrefabs
Assets/Core/Prefab/TreatmentBodies
```

Validation:

- `dotnet build "Pixel-forge-game-jam.slnx"` passes.
- A catalog asset can be created in Unity.

Recommended next implementation step:

```text
Start here next.
```

This phase is low risk because it adds new scripts and data assets without changing current mini game flow.

### Phase 3 - AnatomyController Hook

Goal: Spawn the body prefab when the player enters an infected area.

Tasks:

1. Add a `TreatmentBodyPrefabSpawner` reference to `AnatomyController`.
2. In `EnterInfectedArea`, resolve `miniGameType`.
3. Spawn the body prefab using `miniGameType + area`.
4. Pass the spawned body prefab to the selected mini game.
5. Keep fallback behavior if the catalog or prefab is missing.

Validation:

- Existing scenes still work if no catalog is assigned.
- Missing body prefab logs a warning and does not hard crash.

Implementation note:

`AnatomyController.EnterInfectedArea(area)` currently resolves:

```text
TreatmentMiniGameType miniGameType = GetMiniGameTypeForArea(area);
```

The body prefab spawn should happen immediately after that lookup and before `StartMiniGame(miniGameType)`.

### Phase 4 - Mini Game Body Binding

Goal: Let each mini game consume body-specific roots and anchors.

Tasks:

1. Add `SetBodyPrefab(TreatmentBodyPrefab body)` to Tongs.
2. Add `SetBodyPrefab(TreatmentBodyPrefab body)` to Knife.
3. Add `SetBodyPrefab(TreatmentBodyPrefab body)` to Needle.
4. Each method updates the relevant root and anchor references.

Validation:

- Tongs can use anchors from `Tongs_Arm_BodyPrefab`.
- Knife can use anchors from `Knife_Leg_BodyPrefab`.
- Needle can use anchors from `Needle_Arm_BodyPrefab`.

Implementation note:

Each mini game binding method should be optional and fallback-safe:

```text
If body == null -> keep current serialized scene references.
If body lacks needed anchors -> log warning and keep current serialized scene references.
If body has valid roots/anchors -> replace runtime references for this mini game run.
```

### Phase 5 - Prefab Authoring

Goal: Create real prefab assets.

Tasks:

1. Create one body prefab first, recommended:

```text
Tongs_Arm_BodyPrefab
```

2. Add `TreatmentBodyPrefab` to the root.
3. Add body sprite.
4. Add Tongs parasite root.
5. Add spawn anchors.
6. Add wound masks for the parasite reveal design.
7. Assign it in the catalog.

Validation:

- Selecting Arm with Tongs spawns the Tongs arm body prefab.
- Parasites spawn from that prefab's anchors.

Current prototype source:

The existing scene-authored Tongs arm setup can be used as the reference for the first prefab:

```text
Treatment RoomRoot / 03_Gameplay / TongsMiniGameRoot
```

The prefab should preserve the authored body sprite placement, parasite root, spawn points, wound markers, and reveal mask behavior.

### Phase 6 - Expand To Other Mini Games

Goal: Fill the matrix.

Recommended order:

1. Tongs Arm.
2. Knife Torso or Knife Leg.
3. Needle Arm.
4. Remaining combinations as art becomes available.

---

## 13. Unity Setup Guide

### 13.1 Catalog Asset

Create:

```text
Assets/Core/Data/Treatment/BodyPrefabs/TreatmentBodyPrefabCatalog.asset
```

Then add entries like:

```text
Tongs + Arm  -> Tongs_Arm_BodyPrefab
Knife + Leg  -> Knife_Leg_BodyPrefab
Needle + Arm -> Needle_Arm_BodyPrefab
```

### 13.2 Body Prefab Asset

Create prefabs under:

```text
Assets/Core/Prefab/TreatmentBodies
```

Recommended names:

```text
Tongs_Head_BodyPrefab
Tongs_Torso_BodyPrefab
Tongs_Arm_BodyPrefab
Tongs_Leg_BodyPrefab
Knife_Head_BodyPrefab
Knife_Torso_BodyPrefab
Knife_Arm_BodyPrefab
Knife_Leg_BodyPrefab
Needle_Head_BodyPrefab
Needle_Torso_BodyPrefab
Needle_Arm_BodyPrefab
Needle_Leg_BodyPrefab
```

### 13.3 Scene Setup

Add one `TreatmentBodyPrefabSpawner` to the Treatment gameplay root or a manager object.

Assign:

- Catalog asset.
- Body parent transform.

Then assign the spawner to:

```text
AnatomyController
```

---

## 14. Fallback Rules

The system should be forgiving during development.

Recommended fallback behavior:

| Missing | Behavior |
---|---|
| No catalog assigned | Use existing mini game scene roots and anchors. |
| No entry for mini game + area | Log warning and use existing mini game setup. |
| Entry exists but prefab missing | Log warning and use existing mini game setup. |
| Spawned body missing needed root | Log warning; mini game uses existing serialized root if available. |

This prevents the new system from breaking current test scenes while it is being built.

Additional migration rule:

```text
Do not move or replace the current scene-authored treatment room layout while introducing this system.
```

Create body prefabs as new assets first, then connect them through the catalog.

---

## 15. Risks

### Risk: Prefab Matrix Becomes Large

There are currently:

```text
3 mini games x 4 body areas = 12 body prefabs
```

This is manageable, but art workload can grow. Build only the combinations that the current cases use first.

### Risk: Existing Scene Setup Breaks

If mini games immediately require body prefabs, old tests will fail.

Fix: keep serialized roots and anchors as fallback.

### Risk: Wrong Prefab For The Treatment

Cause: wrong catalog entry.

Fix: the spawned `TreatmentBodyPrefab` should store its own `miniGameType` and `area`, and optionally validate against the requested pair.

### Risk: Spawned Prefab Does Not Clean Up

Fix: `TreatmentBodyPrefabSpawner` should own the active instance and clear it when:

- returning to Anatomy level,
- stopping treatment,
- completing the case,
- selecting another body area.

### Risk: Mini Games Become Too Coupled To The Catalog

Fix: mini games should receive the spawned `TreatmentBodyPrefab` or a small binding object. They should not search the catalog themselves.

---

## 16. Acceptance Criteria

The first implementation is complete when:

- A catalog asset can map `TreatmentMiniGameType + BodyArea` to a body prefab.
- A body prefab root can expose mini-game-specific roots and spawn anchors.
- Anatomy selection can spawn a body prefab before starting a mini game.
- Tongs can read parasite roots/anchors from a spawned Tongs body prefab.
- Missing catalog/prefab data does not break existing scene tests.
- The system works with existing `TreatmentCaseData` requirements.
- `dotnet build "Pixel-forge-game-jam.slnx"` passes.

Development checkpoint:

The first code checkpoint should stop after Phase 2 if needed:

```text
Catalog + body prefab component + spawner compile successfully.
No current scene behavior changes yet.
```

The first playable checkpoint should stop after Phase 4:

```text
Anatomy can spawn a body prefab.
Tongs can consume spawned body anchors.
Existing scene fallback still works.
```

---

## 17. Summary

The Treatment Body Prefab Catalog system should resolve the body view from the same data that already drives treatment routing: `BodyArea + TreatmentMiniGameType`. Each mini game owns its own body prefab per body area because Tongs, Knife, and Needle require different spawn anchors, masks, guide lines, and interaction layouts. The recommended implementation is a ScriptableObject catalog, a `TreatmentBodyPrefab` root component, and a `TreatmentBodyPrefabSpawner` controlled by `AnatomyController`. The system should be introduced with safe fallbacks so existing test scenes continue working while the new prefab pipeline is authored.
