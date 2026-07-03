# Codex Local Workflow Skill

**Project:** Cure Me, Please
**Purpose:** Local working rules for Codex-assisted implementation, progress tracking, and feature handoff in this repository.
**Last updated:** July 4, 2026

---

## 1. Primary Workflow

Use the project reports as the source of truth before making implementation decisions.

Priority order:

1. Read the current code and scene/prefab state.
2. Check the related report under `Assets/Core/Report`.
3. Make the smallest implementation change that moves the active feature forward.
4. Ask the user to do Unity-only validation when visual placement, Inspector state, or play-mode behavior must be confirmed.
5. Update the relevant report after a feature checkpoint changes.
6. Commit only after the user confirms the Unity playtest or visual result.

---

## 2. Current Feature Priority

Current phase:

```text
Mixed Treatment Flow stabilization
```

Priority queue:

1. Stabilize `Case_Test_MixedTreatment` across Knife, Tongs, and Needle.
2. Confirm `Knife + Torso` fallback to Anatomy after all lesions are complete.
3. Confirm `Tongs + Arm` fallback to Anatomy after all parasites are extracted.
4. Integrate and test `Needle + Leg` or the next available Needle body art required by the active case.
5. Expand remaining body-prefab matrix only when a case or art asset needs it.

---

## 3. Treatment Body Prefab Rules

Each treatment body prefab should own the gameplay surface for its mini game.

For Tongs:

- Body prefab may host or provide roots/anchors for `TongsMiniGame`.
- Parasite spawn anchors must stay aligned with wound/reveal visuals.
- `TongsMiniGame` should auto-complete and return to Anatomy when every parasite is extracted unless a test explicitly needs the manual complete button.
- Do not change Tongs spawn, reveal, or parasite visual values after a confirmed visual pass unless testing specifically targets Tongs.

For Knife:

- Knife body prefabs should host their own `KnifeMiniGame` when replacing old scene-authored roots.
- `AnatomyController` should spawn the body prefab, resolve `KnifeMiniGame` from the spawned body, and inject runtime scene references.
- `KnifeMiniGameRoot` should not remain in `Game_K` once `Knife_Torsoo_BodyPrefab` owns the Knife flow.
- Lesion prefabs and anchors should be configured on the body prefab so the spawned body can run without scene-only roots.
- Knife uses the `Knife` tool for cutting. Opened `Bulge` lesions must use the `Tongs` tool for pull/extraction, not bare-hand pull.

For Needle:

- Follow the Knife pattern when converting Needle from scene-authored root to body-prefab-owned gameplay.
- Keep existing serialized fallback until the first Needle body prefab passes Unity playtest.

For body prefab visuals:

- Treat GameObject `Layer: Default` / `0` as unrelated to 2D render order.
- Body sprites should render on Sprite Renderer sorting layer `Main`, order `20`.
- Prefer `TreatmentBodyPrefab` level sorting enforcement for reusable body visual layer bugs.
- Re-check both prefab asset values and any scene prefab overrides before tuning render order manually.

---

## 4. Scene Editing Rules

- Avoid broad scene edits unless the user is actively working in that scene.
- Do not revert unrelated user changes, especially in `CustomerShowScene.unity` or other scenes that may be open in Unity.
- When deleting a prefab instance from a Unity scene YAML, also remove its transform fileID from the parent `m_Children` list.
- Prefer prefab-level fixes for reusable visual/layer/sorting problems.
- Treat `Game_K` as the current integration test scene unless the user names another scene.

---

## 5. Report Update Rules

Update these reports when the corresponding checkpoint changes:

- `Project_Current_Status_Overview_Report.md`: broad project status, active flow, current test setup.
- `Treatment_Body_Prefab_Catalog_Technical_Design_Report.md`: body prefab matrix, catalog routing, phase checkpoint.
- `Knife_Lesion_Spawn_Randomization_Technical_Design_Report.md`: Knife spawn/anchor/body-prefab-specific status.
- `Needle_Pustule_Spawn_Visual_State_Technical_Design_Report.md`: Needle body-prefab and visual state progress.

Do not over-document temporary values that are still being tuned visually. Record only checkpoints and known rules.

---

## 6. Current Checkpoint

As of July 4, 2026:

- `Game_K` uses `Case_Test_MixedTreatment` for mixed treatment flow testing.
- `KnifeMiniGameRoot` has been removed from `Game_K`.
- `Knife_Torsoo_BodyPrefab` owns its `KnifeMiniGame`, lesion spawn options, and lesion anchors.
- `Knife + Torso` can return to Anatomy and mark Torso treated after completion.
- `KnifeMiniGame` uses `Knife` to cut and `Tongs` to pull opened `Bulge` lesions.
- `TongsMiniGame` now auto-raises completion when all parasites are extracted so Arm can return to Anatomy without pressing the overlay complete button.
- `TreatmentBodyPrefab` enforces body sprite sorting on `BodySpriteRoot` as `Main / 20`; this protects `Tongs_Arm_BodyPrefab` and `Knife_Torsoo_BodyPrefab` from runtime or scene override sorting regressions.
- Next validation target: complete the mixed case through Torso Knife, Arm Tongs, and the remaining required Needle area, then confirm `Complete Test` appears only after every required area is treated.
