# Scene Baseline - Current Layout and Sprite References

This baseline records the current saved `Assets/Scenes/SampleScene.unity` layout that should be treated as the user's authored setup.

Future implementation work should preserve these existing object positions, parent hierarchy, sprite references, sorting layers, and sorting orders unless the user explicitly asks to change them. New systems should add new objects, components, or child objects instead of rebuilding or repositioning the existing scene layout.

## Baseline Rule

- Do not overwrite the user's current room composition.
- Do not reset existing transform positions or sprite assignments.
- Do not rerun setup logic in a way that replaces manually arranged objects.
- Add new objects as children or separate helpers when more functionality is needed.
- If a required change affects an existing object below, confirm the reason before changing it.

## Room Roots

| Object | Parent | Active | Local Position | Local Scale |
|---|---|---:|---|---|
| `Main RoomRoot` | Scene Root | 1 | `{x: 0, y: 0, z: 0}` | `{x: 1, y: 1, z: 1}` |
| `Treatment RoomRoot` | Scene Root | 0 | `{x: 0, y: 0, z: 0}` | `{x: 1, y: 1, z: 1}` |
| `Main Camera` | Scene Root | 1 | `{x: 0, y: 0, z: -10}` | `{x: 1, y: 1, z: 1}` |
| `DialogCanvas` | Scene Root | 1 | `{x: 0, y: 0, z: 0}` | `{x: 1, y: 1, z: 1}` |
| `GameFlowManager` | Scene Root | 1 | `{x: 0, y: 0, z: 0}` | `{x: 1, y: 1, z: 1}` |

## Main Room Hierarchy

```text
Main RoomRoot
|-- 00_Background
|   `-- BG_Far_Placeholder
|-- 01_Midground
|   `-- BG_Mid_Placeholder
|-- 02_MainArea
|   `-- Main_Room_Placeholder
|-- 03_Gameplay
|   |-- Customer
|   `-- Door
|       |-- Door Open
|       `-- Door Close
|-- 04_Foreground
|   |-- Foreground_Placeholder
|   |-- Counter
|   `-- Candle
`-- 05_VFX
```

## Treatment Room Hierarchy

```text
Treatment RoomRoot
|-- 00_Background
|   `-- BG_Far_Placeholder
|-- 01_Midground
|   `-- BG_Mid_Placeholder
|-- 02_MainArea
|   `-- Main_Room_Placeholder
|-- 03_Gameplay
|-- 04_Foreground
|   `-- Foreground_Placeholder
`-- 05_VFX
```

## Important Gameplay Objects

| Object | Parent | Active | Local Position | Local Scale | Sorting | Sprite |
|---|---|---:|---|---|---|---|
| `Customer` | `03_Gameplay` | 1 | `{x: -5.9300003, y: -2, z: 0}` | `{x: 2.2, y: 2.2, z: 1}` | layer id `-913042597`, index `3`, order `-10` | `{fileID: 1446532583643695378, guid: ed66159c32b3404478de7c6c25e106b8, type: 3}` |
| `Door` | `03_Gameplay` | 1 | `{x: 4.51, y: -2, z: 0}` | `{x: 1, y: 1, z: 1}` | none | none |
| `Door Open` | `Door` | 0 | `{x: -12.0354, y: 1.9457, z: 0}` | `{x: 3.9078, y: 3.7673402, z: 1}` | layer id `-2108703231`, index `4`, order `0` | `{fileID: -873048637846740066, guid: 446aa78f295cf084892a9751d15dc1f1, type: 3}` |
| `Door Close` | `Door` | 1 | `{x: -14.18, y: 1.966, z: 0}` | `{x: 5.6763945, y: 4.5566444, z: 1}` | layer id `-2108703231`, index `4`, order `0` | `{fileID: -8146606374147342073, guid: 2265da6b7d4771f47beaa15d6ee86cd3, type: 3}` |
| `Counter` | `04_Foreground` | 1 | `{x: -3.66, y: -4.53, z: 0}` | `{x: 6, y: 6, z: 1}` | layer id `23284193`, index `7`, order `0` | `{fileID: -7908872469103156650, guid: b3b5d40f6d8eb1041a6e024724ebd37a, type: 3}` |
| `Candle` | `04_Foreground` | 1 | `{x: 4.8, y: -4.37, z: 0}` | `{x: 1.2, y: 1.2, z: 1}` | layer id `23284193`, index `7`, order `1` | `{fileID: 1768679433190182986, guid: bd4dcda62867aa2489b2af984eeaa278, type: 3}` |

## Placeholder Layer Objects

| Object | Parent | Active | Local Position | Local Scale | Sorting | Sprite |
|---|---|---:|---|---|---|---|
| `BG_Far_Placeholder` | `00_Background` | 1 | `{x: 0, y: 0.55, z: 0}` | `{x: 8, y: 8, z: 1}` | layer id `101301`, index `1`, order `0` | `{fileID: -8599495245105734691, guid: dffe161826b99fe43ac8b63721ef53bc, type: 3}` |
| `BG_Mid_Placeholder` | `01_Midground` | 1 | `{x: 0, y: 0.15, z: 0}` | `{x: 7, y: 7, z: 1}` | layer id `101302`, index `2`, order `0` | `{fileID: -8599495245105734691, guid: dffe161826b99fe43ac8b63721ef53bc, type: 3}` |
| `Main_Room_Placeholder` | `02_MainArea` | 1 | `{x: 0, y: -0.25, z: 0}` | `{x: 6.2, y: 6.2, z: 1}` | layer id `101303`, index `5`, order `0` | `{fileID: -8599495245105734691, guid: dffe161826b99fe43ac8b63721ef53bc, type: 3}` |
| `Foreground_Placeholder` | `04_Foreground` | 1 | `{x: 0, y: -2.85, z: 0}` | `{x: 6.5, y: 6.5, z: 1}` | layer id `101304`, index `8`, order `0` | `{fileID: -8599495245105734691, guid: dffe161826b99fe43ac8b63721ef53bc, type: 3}` |

## Waypoints

| Object | Parent | Active | Local Position | Local Scale |
|---|---|---:|---|---|
| `CounterPoint` | `Point Manager` | 1 | `{x: 10.9, y: -0.83, z: 0}` | `{x: 1, y: 1, z: 1}` |
| `OutsideDoorPoint` | `Door Point` | 1 | `{x: 0.8500004, y: -0.75, z: 0}` | `{x: 1, y: 1, z: 1}` |
| `InsideDoorPoint` | `Door Point` | 1 | `{x: 2.2, y: -0.75, z: 0}` | `{x: 1, y: 1, z: 1}` |
| `CustomerSpawnPoint` | `Customer point` | 1 | `{x: 0.63, y: -1.02, z: 0}` | `{x: 1, y: 1, z: 1}` |
| `CustomerExitPoint` | `Customer point` | 1 | `{x: -0.03999996, y: -1.02, z: 0}` | `{x: 1, y: 1, z: 1}` |

## Future Work Note

When adding Sanity, Candle logic, Treatment placeholder improvements, or future minigame systems, keep these objects stable and add new helper objects/components around them. For example:

- Add `CandleFlame` or `CandleGlow` under `05_VFX` instead of replacing `Candle`.
- Add treatment UI under `DialogCanvas` instead of moving room art.
- Add room state controllers on `GameFlowManager` instead of rebuilding `Main RoomRoot`.
- Add new treatment room art under `Treatment RoomRoot` without changing `Main RoomRoot` object positions.
