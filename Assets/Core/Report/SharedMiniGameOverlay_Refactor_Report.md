# Shared Mini Game Overlay — Refactor Report

**Project:** Cure Me, Please
**Module:** Treatment — shared HUD for all treatment mini games
**Engine:** Unity 6000.3.17f1 (Unity 6.3 LTS), 2D
**Input:** Input System Package (New) — `UnityEngine.InputSystem`
**Document status:** Implemented (code done, scene wiring pending)
**Last updated:** July 1, 2026

---

## 1. Objective

Before this change, every treatment mini game owned its **own** overlay Canvas with its own
`progressSlider`, `painSlider`, and `completeButton`. Adding a new mini game meant building a new
Canvas each time.

Goal: **one shared overlay Canvas** reused by every mini game. It already carries the progress
slider, pain slider, and complete button, and it shows every tool button. Adding a new mini game
should only require the new tool's logic plus one tool-group entry — no new Canvas, sliders, or
complete button.

---

## 2. Before vs After

| Aspect | Before | After |
|---|---|---|
| Progress + Pain sliders | One pair **per** mini game | One shared pair on `MiniGameOverlay` |
| Complete button | One **per** mini game, each wired to its own `CompleteMiniGame` | One shared button; forwards to the **active** mini game |
| Tool buttons | Each mini game's tool on its own Canvas | All tools on the shared Canvas, grouped per mini game |
| Canvas count | One per mini game | One shared Canvas |
| Adding a mini game | Build a new Canvas + sliders + button | Add tool logic + one tool-group entry, reuse overlay |

---

## 3. New Component: `MiniGameOverlay`

`Assets/Core/Script/Treatment/MiniGameOverlay.cs`

The shared HUD component. It owns the shared UI and exposes a small API the mini games drive.

Serialized fields:

```text
content            // root object shown while a mini game is active (must NOT be the overlay's own GameObject)
progressSlider     // shared progress meter
painSlider         // shared pain meter
completeButton     // shared complete button
toolGroups[]       // { id, root } — one per mini game (e.g. "Tongs", "Knife")
showOnlyActiveToolGroup  // when true, only the active mini game's tool group is shown
```

Public API:

```csharp
void Activate(string toolId, Action completeHandler); // mini game started
void Deactivate(Action completeHandler);              // mini game stopped/paused
void SetMeters(float progress, float pain);           // drive the two sliders
void SetCompleteVisible(bool visible);                // show/hide the shared complete button
```

Key behaviors:

- **Shared complete button:** the active mini game passes its `CompleteMiniGame` into `Activate`.
  Clicking the button forwards to that handler. Nothing else needs to wire the button.
- **Ownership guard on `Deactivate`:** the overlay only clears itself if the caller still owns it
  (`activeCompleteHandler == completeHandler`). This prevents a late `Deactivate` from wiping a mini
  game that has already taken over.
- **`content` is separate from the overlay's GameObject:** `SetContentVisible` refuses to disable the
  object the component lives on, so `Awake`/`OnDestroy` wiring never breaks when the content is hidden.
- **Tool groups:** with `showOnlyActiveToolGroup = true`, entering Tongs shows only the Tongs tool
  button, entering Knife shows only the Knife tool button.

---

## 4. Mini Game Changes

Both mini games dropped their three UI fields (`progressSlider`/`pullProgressSlider`, `painSlider`,
`completeButton`) and now reference a single overlay.

### 4.1 `TongsMiniGame`

- Removed `pullProgressSlider`, `painSlider`, `completeButton`.
- Added `[SerializeField] MiniGameOverlay overlay;` and `[SerializeField] string overlayToolId = "Tongs";`
- `Awake` / `OnDestroy` no longer wire a complete button (the overlay does).
- `Begin` / `Resume` → `overlay.Activate(overlayToolId, CompleteMiniGame)`.
- `Stop` / `Pause` → `overlay.Deactivate(CompleteMiniGame)`.
- `RefreshMeters` → `overlay.SetMeters(progress, pain)`.
- `SetCompleteButtonVisible` → `overlay.SetCompleteVisible(visible)`.

### 4.2 `KnifeMiniGame`

- Removed `progressSlider`, `painSlider`, `completeButton`.
- Added `overlay` + `overlayToolId = "Knife"`. Kept `knifeButton` (it is a tool, not a meter).
- Same `Begin`/`Resume`/`Stop`/`Pause`/`RefreshMeters` rerouting as Tongs.
- `SetCompleteButtonVisible` keeps its `&& !autoCompleteWhenAllLesionsDone` guard, routed through the overlay.

Both mini games keep the same public contract used by `AnatomyController`
(`Begin`/`Stop`/`Pause`/`Resume` + `event Action MiniGameCompleted`), so nothing upstream changed.

---

## 5. Editor Setup Change

`Assets/Core/Script/Treatment/Tongs/Editor/TongsMiniGameSetup.cs`

The "Create Tongs World MiniGame Slice" menu now builds the shared overlay instead of assigning
sliders directly to the mini game:

- Creates the overlay Canvas (`TreatmentMiniGameOverlay`), adds a `MiniGameOverlay` component, and a
  full-stretch `OverlayContent` child that holds the sliders, complete button, and tool button.
- New helpers `CreateOverlayContent(...)` and `AssignOverlay(...)` wire the overlay's `content`,
  `progressSlider`, `painSlider`, `completeButton`, and one tool group `{ id: "Tongs", root: toolButton }`.
- `AssignMiniGame(...)` now assigns `overlay` + `overlayToolId` instead of the three removed slider/button
  properties.

This keeps the setup menu working (no `FindProperty` on removed fields → no NullReferenceException).

---

## 6. Verification

- `dotnet build "Pixel-forge-game-jam.slnx"` → **Build succeeded, 0 warnings, 0 errors.**
- No remaining references to the removed fields (`pullProgressSlider`, `progressSlider`, `painSlider`,
  or the mini games' `completeButton`) anywhere except each type's own definition.
- `Treatment.completeButton` is unrelated (the outer treatment placeholder button) and was left untouched.

> Build note: `MiniGameOverlay.cs` was temporarily added to `Assembly-CSharp.csproj` to verify the
> `dotnet` build. Unity regenerates the `.csproj` and creates the `.meta` file automatically on the next
> editor import — no manual action needed.

---

## 7. Pending Scene Wiring (do in Unity)

The code is done; the scene must be re-wired once:

1. Place the `MiniGameOverlay` Canvas **outside** any mini game root (e.g. under the Treatment room
   root), because each mini game's `root` is toggled on/off — a shared overlay parented under it would
   be hidden when that mini game stops.
2. On that Canvas assign: `content` (the child that wraps the sliders/tools), `progressSlider`,
   `painSlider`, `completeButton`.
3. Set `toolGroups`: `{ id: "Tongs", root: <Tongs tool button> }`, `{ id: "Knife", root: <Knife tool button> }`.
4. Point both `TongsMiniGame.overlay` and `KnifeMiniGame.overlay` at the **same** `MiniGameOverlay`.
5. Move the Knife tool button onto this Canvas (inside the Knife tool group).

---

## 8. Adding a New Mini Game Later

1. Give the new mini game a `MiniGameOverlay overlay` reference and an `overlayToolId`.
2. Call `overlay.Activate(overlayToolId, CompleteMiniGame)` on begin/resume,
   `overlay.Deactivate(CompleteMiniGame)` on stop/pause, and `overlay.SetMeters(...)` for the meters.
3. Add the new tool button under the shared Canvas and register a tool group `{ id, root }`.
4. Wire the area(s) to it in `AnatomyController` (new mini game field + `HandleXCompleted`), matching the
   existing Tongs/Knife pattern.

No new Canvas, sliders, or complete button are required.

---

## 9. Files Touched

| File | Change |
|---|---|
| `Assets/Core/Script/Treatment/MiniGameOverlay.cs` | **New** shared HUD component. |
| `Assets/Core/Script/Treatment/Tongs/TongsMiniGame.cs` | Use shared `overlay` instead of own sliders/button. |
| `Assets/Core/Script/Treatment/Knife/KnifeMiniGame.cs` | Use shared `overlay` instead of own sliders/button. |
| `Assets/Core/Script/Treatment/Tongs/Editor/TongsMiniGameSetup.cs` | Build/assign the shared overlay in the setup menu. |
