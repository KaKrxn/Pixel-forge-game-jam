# Treatment Feedback — Cursor Pain, Red Cursor & Camera Shake — Design Report

**Project:** Cure Me, Please
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`
**Engine:** Unity 6000.3.17f1, 2D Pixel Art
**Input:** Input System Package (New)
**Report date:** July 5, 2026
**Module:** Treatment Feedback (pain readout + game feel)
**Document status:** Technical design (no code yet)

---

## 1. Purpose

Three feedback features for the treatment mini games, all reading the **pain** value the mini games
already produce:

1. **Pain bar follows the cursor** — a compact pain meter floats near the mouse instead of sitting in a
   fixed corner, so the player reads pain without looking away from what they are doing.
2. **Cursor turns red with pain** — the mouse cursor tints gradually toward red as the pain bar fills;
   the more pain, the redder.
3. **Camera shake on mistakes** — when the player slips or hurts the customer (pain spike, straying off
   the path/edge/pustule, wrong action), the camera gives an impact jolt.

---

## 2. Current Project Context

| System | Current role | Relevance |
|---|---|---|
| `MiniGameOverlay.SetMeters(progress, pain)` | Single funnel for the shared progress + pain sliders. All mini games call it every frame. | **The one place pain is known.** Broadcast pain from here. |
| Mini games (`TongsMiniGame`, `KnifeMiniGame`, `NeedleMiniGame`) | Compute local pain (0–1) and the "hurt" moments. `Parasite`/`Lesion`/`Pustule` raise spike + edge/stray events → `Sanity.AddSanity`. | Source of both the **pain value** and the **hurt events**. |
| `Sanity` | Danger meter. Mini games add sanity on pain-full spike + edge/stray. | Hurt correlates with Sanity gains. |
| `CameraSway` (on Main Camera) | Final camera position writer: `target.position = basePosition + sway`, in `LateUpdate`, pixel-snapped. Has `Recenter()`. | Camera shake must layer on top **without a second position writer fighting it**. |
| `Mouse.current` (Input System) | Mouse position/read is already used by all mini games. | Drives the custom cursor. |
| OS hardware cursor | Currently shown as-is (no custom cursor). | Hide it and draw a tintable software cursor. |

---

## 3. Confirmed Design Decisions

| Topic | Decision |
|---|---|
| Pain source of truth | The value passed to `MiniGameOverlay.SetMeters(..., pain)`. Broadcast it (event/property); do not recompute. |
| Cursor model | **Hide the OS cursor and draw a custom software cursor** (UI Image following the mouse). Only a software cursor can be tinted smoothly. |
| Pain bar location | A **compact floating bar/ring near the cursor**, following the mouse. The overlay's fixed pain slider can stay as a dev/debug readout (like the candle bar) or be removed later. |
| Cursor tint | `Color.Lerp(normalColor, painColor, pain)` — continuous, more pain = redder. |
| Camera shake integration | A `CameraShake` provides a **decaying additive offset**; `CameraSway` (the single final writer) adds it. No second transform fights for the camera position. |
| Hurt triggers | Pain-full spike (strong), stray/edge/off-target contact (light), wrong tool (light). Intensity scales with severity. |
| Wiring | A small shared **`TreatmentFeedback`** hub carries `Pain` + a `Hurt(intensity)` event. Cursor and camera subscribe; mini games push. Keeps mini games decoupled from cursor/camera. |
| Scope | Treatment mini games only. Active while a mini game overlay is active; OS cursor restored otherwise. |

---

## 4. Shared Piece: `TreatmentFeedback` Hub

A single lightweight event channel so cursor and camera do not each hook every mini game.

```text
Assets/Core/Script/Treatment/Feedback/TreatmentFeedback.cs
```

```csharp
public sealed class TreatmentFeedback : MonoBehaviour   // scene singleton (or a ScriptableObject channel)
{
    public float Pain { get; private set; }             // 0..1, latest pain
    public event System.Action<float> PainChanged;      // cursor listens
    public event System.Action<float> Hurt;             // camera (and future SFX/flash) listen, intensity 0..1

    public void SetPain(float pain);                     // called with the same value as SetMeters(..., pain)
    public void ReportHurt(float intensity);            // called on a mistake
}
```

- **Pain** is pushed by mini games (or by `MiniGameOverlay.SetMeters` forwarding to the hub).
- **Hurt** is pushed by mini games at mistake moments.
- Consumers (cursor, camera, later SFX/screen-flash) only depend on this hub, not on each mini game.

> Alternative with even less new code: add `event Action<float> PainChanged` directly to `MiniGameOverlay`
> and a `Hurt` event on the hub. Either works; the hub keeps future consumers clean.

---

## 5. Feature A — Cursor Pain Feedback (follow + red + floating bar)

### 5.1 Component

```text
Assets/Core/Script/Treatment/Feedback/CursorPainFeedback.cs
```

A UI element on a **top-most overlay canvas** (above meters/dialog) that:

- Hides the OS cursor while active (`Cursor.visible = false`) and restores it on disable.
- Each frame, moves its `RectTransform` to `Mouse.current.position` (screen-space overlay = position is
  the screen point directly; for a scaled canvas, convert via the canvas).
- Tints the cursor image `Color.Lerp(normalColor, painColor, Pain)`.
- Drives a small **floating pain bar/ring** (an `Image` fill or tiny slider) offset near the cursor by `Pain`.

```csharp
[SerializeField] private RectTransform cursorRoot;    // follows the mouse
[SerializeField] private Image cursorImage;           // tinted by pain
[SerializeField] private Image painFill;              // radial/linear fill = pain (near the cursor)
[SerializeField] private Color normalColor = Color.white;
[SerializeField] private Color painColor = new Color(0.9f, 0.1f, 0.1f, 1f);
[SerializeField] private Vector2 painBarOffset = new Vector2(24f, -24f);
[SerializeField] private float colorLerpSpeed = 12f;  // smooth toward target tint
```

### 5.2 Behavior

- **Active window:** show the custom cursor + pain bar only while a treatment mini game is active
  (e.g., overlay content visible). Otherwise restore the OS cursor and hide the element.
- **Smoothing:** lerp the tint toward the pain-target so it eases red↔normal instead of snapping.
- **Pain bar follow:** the bar sits at `cursorPos + painBarOffset`, so it is always readable next to
  the tool without covering the target.

### 5.3 Notes

- **Software cursor lag:** update the cursor position in `Update` (or `LateUpdate`) reading
  `Mouse.current.position` so it tracks tightly. Keep the sprite tiny and pixel-crisp (point filter).
- **UI raycast:** the cursor image must have `raycastTarget = false` so it never blocks clicks.
- **Fallback:** if `Mouse.current` is null (no mouse), leave the OS cursor on.

---

## 6. Feature B — Camera Shake On Hurt

### 6.1 Component

```text
Assets/Core/Script/Camera/CameraShake.cs
```

Produces a **decaying offset**, not a direct position write:

```csharp
[SerializeField] private float maxTranslation = 0.18f;  // world units at full intensity
[SerializeField] private float maxRotation = 1.5f;      // optional degrees
[SerializeField] private float decayPerSecond = 3.5f;
[SerializeField] private float frequency = 26f;         // Perlin shake speed

public Vector3 CurrentOffset { get; private set; }      // read by CameraSway
public float CurrentRotation { get; private set; }
public void Shake(float intensity);                     // 0..1, adds trauma (clamped)
```

- `Shake(intensity)` adds "trauma" (clamped 0..1); trauma decays each frame.
- Offset = Perlin-noise direction * `maxTranslation` * trauma² (squared feels punchier).
- Exposes `CurrentOffset` (and optional rotation) for the position writer.

### 6.2 Integration With `CameraSway` (no fighting)

`CameraSway` is already the single writer of the camera position. Extend its final computation to add the
shake offset (one optional reference), so there is still exactly one writer:

```text
nextPosition = basePosition + sway + cameraShake.CurrentOffset;   // (pixel-snap after adding)
```

- Keeps `Recenter()` working (base unchanged by shake).
- Respects pixel snapping (snap the summed result). For a punchier non-snapped shake, snapping can be
  skipped for the shake term only — tune in play.
- If a "CameraRig parent + Camera child" split is preferred later, the shake can move the child instead;
  for now, folding the offset into the existing writer is the least invasive.

### 6.3 Triggers (what counts as "hurt")

Subscribe `CameraShake.Shake` to `TreatmentFeedback.Hurt`. Mini games call `ReportHurt(intensity)` at:

| Mistake | Source (existing) | Suggested intensity |
|---|---|---|
| Pain bar fills → Sanity spike | `Parasite.SanitySpikeRequested` / lesion / pustule pain-full | **1.0** (strong jolt) |
| Stray off cut path (Knife) / edge (Tongs) / off pustule (Needle) | continuous stray/edge events | **0.25–0.4** (light rumble, optionally rate-limited) |
| Wrong tool / wrong action | mini game input guards | **0.3** |

> Continuous straying should be **rate-limited** (e.g., a light shake at most a few times per second)
> so it rumbles rather than vibrates every frame.

---

## 7. Data Flow

```
Mini game (Tongs/Knife/Needle)
  -> every frame: overlay.SetMeters(progress, pain)  AND  feedback.SetPain(pain)
       -> feedback.PainChanged -> CursorPainFeedback: move to cursor, tint red by pain, fill floating bar
  -> on mistake: feedback.ReportHurt(intensity)
       -> feedback.Hurt -> CameraShake.Shake(intensity) -> CameraSway adds CurrentOffset
```

The mini games already compute `pain` and already detect the mistake moments; the only new calls are
`feedback.SetPain(pain)` next to the existing `SetMeters`, and `feedback.ReportHurt(...)` at the mistake
points (which currently already call `Sanity.AddSanity`).

---

## 8. Implementation Plan (phased)

### Phase 1 — Feedback Hub + Pain Broadcast
- Add `TreatmentFeedback` (Pain + PainChanged + Hurt + ReportHurt).
- Push pain: each mini game calls `feedback.SetPain(pain)` next to `overlay.SetMeters(...)` (or have the
  overlay forward it). Run `dotnet build "Pixel-forge-game-jam.slnx"`.

### Phase 2 — Cursor Follow + Floating Pain Bar
- Add `CursorPainFeedback` on a top overlay canvas; hide OS cursor while active.
- Follow the mouse; drive the floating pain bar from `Pain`.

### Phase 3 — Cursor Red Tint
- Lerp `cursorImage.color` from normal → red by `Pain` (smoothed).

### Phase 4 — Camera Shake
- Add `CameraShake`; fold `CurrentOffset` into `CameraSway`'s final position.
- Subscribe `Shake` to `feedback.Hurt`.

### Phase 5 — Hurt Triggers + Tuning
- Call `feedback.ReportHurt(...)` at pain-full spike, stray/edge/off-target (rate-limited), wrong tool.
- Tune intensities, decay, tint curve, bar offset in Play Mode.

### Phase 6 — Polish (optional)
- Screen red-flash / vignette pulse on big spikes (same `Hurt` event).
- Hit SFX on `Hurt`; heartbeat that speeds up with `Pain`.
- Restore/skip pixel-snap on the shake term for feel.

---

## 9. Risks & Notes

- **Two writers on the camera:** do **not** add a second component that also sets the camera transform —
  route shake through `CameraSway`'s single write, or it will jitter/fight the sway and pixel snap.
- **Custom cursor over UI:** the cursor image must be top-most and `raycastTarget = false`; the OS cursor
  must be restored when leaving treatment (menus, dialog) so the player is never left cursorless.
- **Pixel-art crispness:** keep the cursor + pain bar sprites point-filtered; snapping the shake avoids
  sub-pixel shimmer but can feel stiff — expose a toggle and tune.
- **Continuous stray spam:** rate-limit `ReportHurt` for continuous mistakes so the camera rumbles, not
  seizures.
- **Pause/complete:** clear trauma and reset the tint when a mini game stops, so shake/red don't linger
  into the anatomy screen or counter.
- **Single active mini game:** only one mini game runs at a time, so one pain value + one hub is enough.

---

## 10. Summary

All three effects read the **pain value the mini games already produce**. A small `TreatmentFeedback` hub
broadcasts `Pain` (for the cursor) and a `Hurt(intensity)` event (for the camera). A **custom software
cursor** replaces the OS cursor: it follows the mouse, carries a **floating pain bar**, and **tints toward
red as pain rises**. A `CameraShake` adds a **decaying offset that `CameraSway` sums into its single
position write** (so nothing fights the camera), triggered on mistakes with intensity scaled to severity
(strong on a pain-full spike, light on straying). The only new calls inside the mini games are
`SetPain(pain)` beside the existing `SetMeters`, and `ReportHurt(...)` at the mistake points they already
detect — keeping the change small and the systems decoupled.
