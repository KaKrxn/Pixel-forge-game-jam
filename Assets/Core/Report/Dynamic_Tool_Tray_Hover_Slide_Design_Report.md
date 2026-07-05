# Dynamic Tool Tray — Hover Slide Design Report

**Project:** Cure Me, Please
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`
**Engine:** Unity 6000.3.17f1, 2D Pixel Art
**Input:** Input System Package (New)
**Report date:** July 5, 2026
**Module:** Treatment UI / Tool Tray
**Document status:** Technical design (no code yet)

---

## 1. Purpose

The tool tray (the metal tray of tools at the bottom — tongs, knife, needle, bottle) is currently a
static UI element. This design makes it **hover-reactive**:

- The tray **rests low** at the bottom (peeking) while the player works on the patient.
- When the cursor moves **near the tray**, it **slides up** so the player can pick a tool.
- When the cursor moves **away** (past a set distance), the tray **slides back down**.

This keeps the tools out of the way during treatment but one flick of the mouse away when needed.

---

## 2. Current Project Context

| System | Current role | Relevance |
|---|---|---|
| `MiniGameOverlay` | Owns the shared tool buttons (the tray contents), on a Screen Space - Overlay canvas. Shown while a mini game is active. | The tray is its tool-button row; the slide moves that row's `RectTransform`. |
| Mouse (`Mouse.current`) | Already used by all mini games for world input. | Drives the hover distance check (screen space). |
| Tool selection | Clicking a tool toggle selects it (`MiniGameOverlay` tool buttons). | Must still work while the tray is up. |

The tray is a **UI RectTransform** on a Screen Space - Overlay canvas, so its position is controlled by
`anchoredPosition`, and the cursor position is a screen-space point — the hover test is a simple 2D
distance/containment check in screen (or canvas) space.

---

## 3. Confirmed Design Decisions

| Topic | Decision |
|---|---|
| Rest state | **Peek** (tray partially visible at the very bottom), not fully hidden — so the player knows it is there. |
| Reveal trigger | Cursor **near the tray region** (a reveal zone = the tray's shown rect expanded by a padding). |
| Hide trigger | Cursor moves **beyond a larger distance** than the reveal distance (**hysteresis**) so it does not flicker at the edge. |
| Motion | **Smooth slide** (lerp / SmoothDamp) between a shown and a hidden `anchoredPosition`. |
| Stays up while used | While the cursor is over the tray (picking a tool), it is inside the reveal zone → stays up automatically. |
| Time | **Unscaled time**, so it still animates if gameplay time is ever paused. |
| Active window | Only slides while the tray is active (a mini game is running / overlay content shown). |
| Scope | Pure UI feel; no gameplay rule changes. Tool selection logic is untouched. |

---

## 4. Non-Goals For The First Version

- Fancy easing curves / bounce (a simple smooth slide is enough first).
- Auto-hide on a timer.
- Per-tool tooltips or hover pop of individual tools.
- Controller/keyboard reveal (mouse hover only for now).

---

## 5. Component Design — `HoverSlideTray`

```text
Assets/Core/Script/Treatment/Feedback/HoverSlideTray.cs
```

Attached to the tray root (the `RectTransform` that holds the tool buttons, e.g. under the overlay).

```csharp
[SerializeField] private RectTransform trayRoot;        // the panel that slides
[SerializeField] private RectTransform revealZone;      // area that triggers reveal (defaults to trayRoot)
[SerializeField] private Canvas canvas;                 // for screen-point tests

[Header("Positions (anchoredPosition)")]
[SerializeField] private Vector2 shownPosition;         // fully up
[SerializeField] private Vector2 hiddenPosition;        // peeking down
                                                        // (or: hidden = shown - slideDistance)

[Header("Hover")]
[SerializeField] private float revealDistance = 90f;    // px from the zone to slide up
[SerializeField] private float hideDistance   = 200f;   // px to slide down (must be > revealDistance)

[Header("Motion")]
[SerializeField] private float slideSpeed = 14f;        // smoothing toward target
[SerializeField] private bool useUnscaledTime = true;
[SerializeField] private bool snapToPixelGrid = true;   // pixel-art crispness
```

Public / behavior:

```csharp
public void SetActive(bool active);   // enable slide (mini game running) vs force hidden
// Update(): read cursor -> distance to revealZone -> hysteresis -> pick target -> smooth move
```

---

## 6. Reveal Logic (with hysteresis)

Each frame while active:

```text
cursor = Mouse.current.position
d = distance from cursor to the revealZone rect (0 if inside the rect)

if d <= revealDistance      -> wantShown = true
else if d >= hideDistance   -> wantShown = false
else                        -> keep previous wantShown   // dead-band = no flicker

target = wantShown ? shownPosition : hiddenPosition
```

- **`revealZone`** is the tray's shown area (optionally expanded). Using the *shown* rect means the
  reveal band is measured against where the tray *will be*, so approaching from above triggers it.
- **Distance to a rect:** clamp the cursor to the rect's min/max and measure to the clamped point
  (0 when the cursor is inside). Simple and robust for a bottom-anchored bar.
- **Hysteresis** (`hideDistance > revealDistance`) is the key to a calm tray: it will not vibrate when
  the cursor sits right at the trigger edge.

> Alternative simpler trigger for a full-width bottom tray: reveal when `cursor.y < revealY`, hide when
> `cursor.y > hideY` (with `hideY > revealY`). The rect-distance version above also covers a
> centered/narrow tray.

---

## 7. Motion

```text
current = trayRoot.anchoredPosition
current = Vector2.Lerp(current, target, 1 - exp(-slideSpeed * dt))   // frame-rate independent smoothing
if snapToPixelGrid: snap current to the pixel grid
trayRoot.anchoredPosition = current
```

- `dt` = unscaled delta time.
- `SmoothDamp` is an equally good choice; either gives a soft slide.
- **Pixel snap:** snap the final anchored position so the pixel-art tray does not shimmer mid-slide.
  (Snap to whole UI pixels; if the canvas is scaled, snap in canvas units.)

---

## 8. Integration

- **Placement:** put `HoverSlideTray` on the tray panel `RectTransform` inside the overlay. The tool
  buttons are children, so sliding the panel moves the whole tray + tools together.
- **Active window:** the tray should only react while a mini game is running. Two easy options:
  1. Place the tray under the overlay's `content` (already toggled on/off by `MiniGameOverlay`), so the
     component only runs while the overlay is shown.
  2. Or call `SetActive(true/false)` from `MiniGameOverlay.Activate/Deactivate`; when inactive, force
     the tray to `hiddenPosition`.
- **Selection still works:** when the tray is up, the cursor is over it → inside the reveal zone → it
  stays up long enough to click a tool. No change to tool toggle logic.
- **Cursor feedback:** the custom cursor (from the pain/cursor feedback) still floats on top; keep the
  tray canvas below the cursor canvas so the cursor is always visible over the tray.

---

## 9. Implementation Plan (phased)

### Phase 1 — Slide Between Two Positions
- Add `HoverSlideTray` with `shownPosition` / `hiddenPosition` and smooth move toward a target.
- Test with the target hard-set (button/inspector) to tune the two positions + speed.
- Run `dotnet build "Pixel-forge-game-jam.slnx"`.

### Phase 2 — Hover Trigger + Hysteresis
- Compute cursor distance to `revealZone`; apply `revealDistance` / `hideDistance` dead-band.
- Verify no flicker at the edge; tray stays up while hovering the tools.

### Phase 3 — Pixel Snap + Active Window
- Snap the sliding position for crispness.
- Gate the slide to the mini game active window (under overlay content, or `SetActive`).

### Phase 4 — Polish (optional)
- Ease curve / slight overshoot, a soft slide SFX, a subtle glow when revealed.
- Optional: also reveal when a tool is *needed* (e.g., no tool selected) as a hint.

---

## 10. Risks & Notes

- **Flicker at the trigger edge:** solved by hysteresis (`hideDistance > revealDistance`). Do not use a
  single threshold.
- **Tray covering the target:** when up, the tray sits over the lower screen; make sure the patient
  body/mini game targets are above it, or keep the shown height modest.
- **Mouse null:** if `Mouse.current` is null, hold the tray hidden (or last state) — do not error.
- **Pixel shimmer:** snap the anchored position; unsnapped sub-pixel motion looks fuzzy in pixel art.
- **Canvas scaling:** measure the cursor distance in the same space you move the tray (screen vs canvas
  local). With a `CanvasScaler`, convert the mouse to canvas local, or compare in screen space and keep
  distances in screen pixels.
- **Reveal while dragging a tool action:** decide if the tray should stay down while actively
  slicing/pulling (cursor is up on the body, far from the tray → it hides naturally). This is usually the
  desired behavior and needs no special case.

---

## 11. Summary

Add a `HoverSlideTray` on the tool-tray `RectTransform` that smoothly slides between a **peeking**
`hiddenPosition` and an **up** `shownPosition`. Reveal when the cursor comes within `revealDistance` of
the tray region and hide when it passes `hideDistance` away — a **hysteresis dead-band** keeps it from
flickering. Motion is a frame-rate-independent smooth move on `anchoredPosition`, pixel-snapped for
crispness, on unscaled time, and only active while a mini game is running. Tool selection is unchanged:
hovering to pick a tool keeps the cursor inside the reveal zone, so the tray stays up while in use and
slides away the moment the player returns to the patient.
