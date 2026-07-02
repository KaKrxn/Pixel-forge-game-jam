# Parasite Mask / Head Reveal Technical Design Report

**Project:** Cure Me, Please  
**Module:** Treatment Mini Game - Tongs / Parasite Visibility and Pull Reveal  
**Engine:** Unity 6000.3.17f1, 2D, Pixel Art, World-space GameObjects  
**Document status:** Implemented baseline with tuning notes  
**Last updated:** July 2, 2026

---

## 1. Purpose

This report defines how parasites appear embedded under the patient's skin in the Tongs mini game.

The target presentation is:

1. The parasite starts mostly hidden inside the body.
2. Only the head or exposed tip is visible and clickable.
3. While the player pulls with the Tongs, the parasite body follows the cursor and becomes visible from the wound point.
4. The reveal mask begins at the spawn anchor, so only the part pulled past that boundary is shown.
5. Nearby parasites do not get revealed by the wrong mask.

This system builds on the existing `Parasite`, `TongsMiniGame`, and `ParasiteType` extraction logic. It does not replace pain, Sanity, tool selection, extraction completion, or spawn rules.

---

## 2. Current System Context

Relevant scripts:

```text
Assets/Core/Script/Treatment/Tongs/Parasite.cs
Assets/Core/Script/Treatment/Tongs/TongsMiniGame.cs
Assets/Core/Script/Treatment/Tongs/ParasiteReveal.cs
Assets/Core/Script/Treatment/Tongs/ParasiteSpawnAnchor.cs
Assets/Core/Script/Treatment/Tongs/ParasiteType.cs
```

Relevant prefabs:

```text
Assets/Core/Prefab/Parasites/SmallParasite_World.prefab
Assets/Core/Prefab/Parasites/LongParasite_World.prefab
Assets/Core/Prefab/Parasites/BigParasite_World.prefab
```

The current parasite flow already supports:

- Runtime spawning from configured spawn anchors.
- Multiple parasite prefab/type pairs.
- Non-repeating spawn anchors per spawn batch.
- Pull progress from 0 to 1.
- Visual follow and tilt while dragging.
- Pain, edge contact, and Sanity events.
- Completion once all parasites are extracted.

---

## 3. Implemented Prefab Structure

Each parasite prefab is split into a visible head and a hidden body section.

```text
Parasite_World
|-- SpawnAnchor
|-- Body
|   `-- SpriteRenderer
|-- Head
|   |-- SpriteRenderer
|   `-- Collider2D
|-- Parasite
`-- ParasiteReveal
```

Rules:

- `Head` stays visible and clickable.
- `Body` uses `SpriteMaskInteraction.VisibleInsideMask`.
- `Body` collider is disabled or avoided for selection.
- `Head` collider is the main click target.
- `ParasiteReveal` controls body reveal, mask binding, pixel snapping, and dynamic mask growth.

---

## 4. Spawn Anchor Strategy

The Tongs mini game still accepts `Transform` anchors, but anchors can now be upgraded with `ParasiteSpawnAnchor`.

```text
SpawnAnchor
|-- ParasiteSpawnAnchor component
|-- optional SpriteMask woundMask
|-- optional visual wound sprite
`-- optional spawnPoint
```

`ParasiteSpawnAnchor` provides:

- `WoundMask`: authored SpriteMask if the anchor already has one.
- `SpawnPosition`: exact position used to align the parasite spawn anchor.
- `RequiredDirection`: direction rule for directional parasites.

Fallback behavior:

- If there is no `ParasiteSpawnAnchor`, the Transform position is still used.
- If there is no authored `SpriteMask`, `ParasiteReveal` creates a runtime mask.
- If the anchor has a child SpriteRenderer, that sprite can be reused as the fallback mask sprite.
- If no fallback mask sprite exists, the head sprite is used as the last fallback.

---

## 5. Active Pull Reveal Mask

The updated reveal behavior is cursor-driven.

At spawn:

```text
Mask origin = selected Spawn Anchor position
Mask size = minimum reveal length
Body = clipped by the mask
Head = visible outside the mask
```

While pulling:

```text
Player clicks the parasite head.
Parasite.BeginHold stores the pointer position.
Parasite.TickPull updates PullProgress.
ParasiteReveal.UpdateDynamicMask(pointerWorldPosition) expands the mask from the Spawn Anchor toward the cursor.
ParasiteReveal.SetProgress(PullProgress) moves the parasite body/root outward.
```

The current baseline reveal direction is upward because the current Tongs extraction rule is upward pull. The mask length is calculated from the cursor distance above the spawn anchor.

Current adjustable fields on `ParasiteReveal`:

```text
driveMaskWithPointer
dynamicMaskWidth
dynamicMaskMinLength
dynamicMaskExtraLength
hiddenOffset
exposedOffset
snapToPixelGrid
pixelsPerUnit
```

Design intent:

- `dynamicMaskWidth` controls how wide the reveal window is.
- `dynamicMaskMinLength` prevents the mask from disappearing completely.
- `dynamicMaskExtraLength` gives a small buffer so the revealed body does not flicker near the anchor.
- `hiddenOffset` and `exposedOffset` control how much the parasite body starts inside the skin.

---

## 6. Mask Overlap Rules

The player asked what happens if a growing mask overlaps a nearby parasite.

The implemented solution is:

```text
Each spawned parasite receives its own body sorting order.
Each parasite head receives body sorting order + 1.
Each runtime/authored mask uses a custom sorting range.
The custom sorting range targets only that parasite body's sorting order.
```

Result:

- A mask may visually overlap another parasite's position.
- It should not reveal another parasite body because the other body uses a different sorting order.
- The mask only reveals the body renderer whose sorting order is inside the mask's custom range.

Important setup rule:

```text
Do not reuse the same sorting order for multiple active parasite bodies.
```

`TongsMiniGame` now assigns sorting orders using:

```text
parasiteSortingOrderStart
parasiteSortingOrderStep
```

This gives each spawned parasite a separated sorting slot.

---

## 7. Runtime Flow

### 7.1 Spawn

```text
TongsMiniGame selects unique spawn anchors.
TongsMiniGame selects parasite type/prefab from spawn options.
Parasite is instantiated under parasiteRoot.
Parasite.Configure applies type and direction.
TongsMiniGame aligns the parasite's SpawnAnchor to the selected spawn position.
Parasite.BindSpawnAnchor passes mask/anchor data to ParasiteReveal.
ParasiteReveal sets sorting order and creates/binds a mask.
ParasiteReveal.ResetReveal hides the body to its start state.
```

### 7.2 Pull

```text
Player equips Tongs.
Player clicks the visible parasite head.
Parasite.BeginHold starts dragging.
Parasite.TickPull reads pointer delta and updates PullProgress.
Parasite.UpdatePullVisual makes the parasite lean/follow the cursor.
ParasiteReveal.UpdateDynamicMask expands the reveal mask from the spawn anchor.
ParasiteReveal.SetProgress moves the hidden body outward.
Pain and Sanity events continue through the existing Parasite system.
```

### 7.3 Extraction

```text
PullProgress reaches 1.
Parasite completes extraction.
Extracted event fires.
TongsMiniGame refreshes overlay meters and completion state.
When all parasites are extracted, the complete button becomes available.
```

---

## 8. Unity Setup Guide

### Parasite Prefab

1. Open each parasite prefab.
2. Keep `Parasite` on the prefab root.
3. Add or keep `ParasiteReveal` on the prefab root.
4. Assign:
   - `Reveal Root`: the transform that should move from hidden to exposed.
   - `Head Renderer`: the visible/clickable head.
   - `Body Renderer`: the hidden body.
   - `Head Collider`: the collider used for clicking.
5. Set body renderer:
   - `Mask Interaction = Visible Inside Mask`.
6. Set head renderer:
   - `Mask Interaction = None`.
7. Tune:
   - `Hidden Offset`
   - `Exposed Offset`
   - `Dynamic Mask Width`
   - `Dynamic Mask Extra Length`

### Spawn Anchors

1. Create one Transform per possible parasite spawn point.
2. Place the Transform at the skin boundary where the parasite should start emerging.
3. Optionally add `ParasiteSpawnAnchor`.
4. Optionally assign an authored `SpriteMask` for that wound.
5. If there is no authored `SpriteMask`, the runtime mask will be created automatically.

### TongsMiniGame

1. Add a parent object or individual anchors to `Spawn Anchors`.
2. Add all valid parasite prefab/type pairs to `Parasite Spawn Options`.
3. Tune:
   - `Min Parasites`
   - `Max Parasites`
   - `Parasite Sorting Order Start`
   - `Parasite Sorting Order Step`
4. Keep `Parasite Sorting Order Step` at least 2 so body/head/mask ranges do not collide.

---

## 9. Tuning Notes

Recommended starting values:

```text
Dynamic Mask Width: 0.85
Dynamic Mask Min Length: 0.08
Dynamic Mask Extra Length: 0.35
Pixels Per Unit: 16
Reveal Mode: Smooth
Snap To Pixel Grid: Enabled
```

If too much parasite body appears at rest:

```text
Lower Dynamic Mask Min Length.
Increase Hidden Offset in the negative direction.
Move the Spawn Anchor closer to the skin exit point.
```

If the body is hard to see while dragging:

```text
Increase Dynamic Mask Width.
Increase Dynamic Mask Extra Length.
Check body/head sorting order.
Check that body renderer uses Visible Inside Mask.
```

If another parasite appears through the wrong mask:

```text
Increase Parasite Sorting Order Step.
Confirm every spawned body receives a unique sorting order.
Confirm each mask has custom range enabled at runtime.
```

---

## 10. Risks And Fixes

### Risk: Parasite Spawns At The Center

Cause: The spawner may be reading the parent object instead of child spawn anchors, or the prefab's `SpawnAnchor` is not aligned to the selected anchor.

Fix: `TongsMiniGame` expands only known spawn-anchor groups and aligns `Parasite.SpawnAnchor` to the chosen anchor position.

### Risk: Body Does Not Reveal While Pulling

Cause: The body has no mask, the mask is too small, or the mask is not growing with the pointer.

Fix: `ParasiteReveal.UpdateDynamicMask` runs while the parasite is held. Runtime masks are created automatically when no authored mask exists.

### Risk: Mask Reveals Nearby Parasites

Cause: Multiple parasite bodies share the same sorting order or the mask has a broad sorting range.

Fix: Each spawned parasite gets a unique body sorting order. Each mask uses a custom sorting range targeting only that body order.

### Risk: Runtime Masks Stay In The Scene After Test

Cause: Runtime masks are parented near anchors, not under the parasite root.

Fix: `ParasiteReveal` owns runtime masks it creates and destroys them when the parasite is destroyed or rebound.

### Risk: Pixel Art Looks Blurry

Cause: Sub-pixel movement while dragging or revealing.

Fix: Keep `Snap To Pixel Grid` enabled and tune `Pixels Per Unit` to match the project asset scale.

---

## 11. Acceptance Criteria

This implementation is ready for scene tuning when:

- Parasite prefabs are split into head/body.
- The head remains visible and clickable at rest.
- The body is hidden/clipped by a mask at rest.
- The parasite can be selected by clicking the head collider.
- Pulling with Tongs makes the body follow and tilt.
- The reveal mask expands from the selected Spawn Anchor toward the cursor.
- Nearby parasites are not revealed by the active parasite's mask.
- Runtime masks are cleaned up with spawned parasites.
- Existing pull, pain, Sanity, and completion behavior still work.

---

## 12. Future Extension

Future body-prefab-per-mini-game work should keep this pattern:

```text
BodyArea + MiniGameType -> Mini-game-specific body prefab
```

Examples:

```text
Arm + Tongs  -> Tongs Arm body prefab with parasite anchors/masks
Leg + Knife  -> Knife Leg body prefab with cut/bulge anchors
Arm + Needle -> Needle Arm body prefab with injection anchors
```

For future directional parasites, `ParasiteSpawnAnchor` can provide a pull direction Transform. `ParasiteReveal.UpdateDynamicMask` can then grow along that authored direction instead of always using world up.

---

## 13. Summary

The parasite reveal system now supports a visible clickable head, a hidden masked body, and a cursor-driven reveal mask that grows from the selected Spawn Anchor while the player pulls. Each spawned parasite receives its own sorting range so overlapping masks do not reveal neighboring parasite bodies. This keeps the visual readable for pixel art and leaves room for future body-prefab-per-mini-game integration.
