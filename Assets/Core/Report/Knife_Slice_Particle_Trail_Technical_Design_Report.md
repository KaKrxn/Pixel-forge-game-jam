# Knife Slice Particle Trail — Technical Design Report

## 1. Overview
Add a particle effect that plays while the player slices a lesion with the knife in
`KnifeMiniGame`. Unlike the needle's one-shot pierce burst, slicing is a **continuous drag**
action, so the effect is a **trail that follows the blade tip** for the whole duration of the
slice (e.g. blood spray / debris streaming off the cut). The effect is limited to the **Slice**
action (knife tool) and never plays for the **Pull** action (tongs tool).

## 2. Goal
- Emit a particle trail that tracks the knife tip (pointer world position) while a slice is active.
- Start when a slice begins, follow the blade every frame, and stop cleanly on release.
- Work whether the assigned reference is a **prefab asset** or a scene object.
- Never leak instances or leave particles alive after the mini game stops/pauses.

## 3. Current System Analysis
`KnifeMiniGame` already exposes the exact hooks needed:

| Hook | Role | Use for the trail |
| --- | --- | --- |
| `BeginActionAt(pointer)` | Chooses `Slice` (knife) vs `Pull` (tongs) and plays the start SFX. | Start the trail only when the mode resolves to `Slice`. |
| `Update()` → `TickLesions(worldPointer, dt)` | Runs every frame while `isRunning`; drives `Lesion.TickSlice(...)`. | Move the trail to the current knife tip each frame. |
| `EndAction()` | Single exit point, called on pointer release **and** from `Stop()` / `Pause()`. | Stop the trail here so every exit path is covered. |

Slicing is confirmed continuous: `TickLesions` calls `lesion.TickSlice(worldPointer, deltaTime, activeSanity)`
each frame for the active lesion, so the pointer world position is already available per frame.

### Lesson carried over from the Needle particle work
The needle particle initially failed because the assigned reference was a **prefab asset**, which is
never `activeInHierarchy`, so `ParticleSystem.Play()` silently rendered nothing. The fix — and the
approach reused here — is to **`Instantiate`** the particle at runtime (detached at scene root, always
active) instead of playing the referenced object directly.

## 4. Design Decision — Option B (Follow Trail)
Two options were considered:
- **A. One-shot burst at slice start** — simplest, mirrors the needle pierce.
- **B. Trail following the blade for the whole slice** — chosen, because slicing is a dragging motion
  and an effect that streams along the cut reads as far more connected to the action.

## 5. Architecture

### 5.1 Serialized fields (`[Header("Slice Particle")]`)
```csharp
[SerializeField] private ParticleSystem sliceParticle;      // prefab emitter that follows the blade
[SerializeField] private bool moveParticleToCutPoint = true; // follow the knife tip each frame
```

### 5.2 Runtime state
```csharp
private ParticleSystem activeSliceParticle; // the live instantiated emitter while a slice is active
```

### 5.3 Control flow
```
BeginActionAt(pointer)                         Update() every frame            EndAction()
  target = FindLesionAt(pointer)                 TickLesions(worldPointer)        StopSliceTrail()
  mode = Slice | Pull                            if mode == Slice:
  PlaySfx(...)                                      UpdateSliceTrail(worldPointer)
  if mode == Slice:
    BeginSliceTrail(pointer)
```

### 5.4 Helper methods
- **`BeginSliceTrail(worldPoint)`** — guards on `sliceParticle == null`; calls `StopSliceTrail()` first so
  two trails never overlap; `Instantiate`s a detached copy at `(x, y, worldInputPlaneZ)`, activates it, and
  `Play(true)`. Stores it in `activeSliceParticle`.
- **`UpdateSliceTrail(worldPoint)`** — if a trail is live and `moveParticleToCutPoint` is on, moves it to the
  current knife tip on the gameplay plane (`worldInputPlaneZ`).
- **`StopSliceTrail()`** — `Stop(true, ParticleSystemStopBehavior.StopEmitting)` so already-spawned particles
  finish naturally, then `Destroy` the instance after `main.startLifetime.constantMax`, and clears the reference.

### 5.5 Why Instantiate + detach
`Instantiate(sliceParticle, ...)` with no parent puts the emitter at scene root, so it is always
`activeInHierarchy` and is unaffected by `ReplaceRuntimeRoot` / `HideSceneRootChildren` deactivating the
mini game / body hierarchy on body swaps.

## 6. Lifecycle & Cleanup
- Slice release → `EndAction()` → `StopSliceTrail()`.
- `Stop()` / `Pause()` both call `EndAction()`, so the trail is stopped when the mini game ends or pauses.
- `OnDestroy()` also calls `StopSliceTrail()` so a mid-slice teardown does not leak the emitter.
- Stopped emitters self-destroy after their particles' max lifetime; no manual pooling required.

## 7. Edge Cases
- **Prefab asset reference** — handled by `Instantiate` (the whole reason for this approach).
- **Rapid press/release** — `BeginSliceTrail` calls `StopSliceTrail` first; only one emitter is ever live.
- **Pull action (tongs)** — trail is gated on `KnifeActionMode.Slice`, so tongs never spawn it.
- **Unassigned field** — logged and skipped; the mini game behaves exactly as before.
- **Body swap mid-run** — emitter lives at scene root, unaffected by hierarchy deactivation.

## 8. Inspector Setup
1. Create a `ParticleSystem` **prefab** for the slice effect (blood spray / debris).
   - `Play On Awake = Off`.
   - `Looping = On` (it is stopped explicitly on release), OR long enough duration to cover a slice.
   - Emission: **Rate over Distance** (or Rate over Time) so it streams while the blade drags.
   - Renderer: assign a **Material**, and set **Sorting Layer / Order above the body sprite** so it is not occluded.
2. Assign the prefab to `Knife MiniGame → Slice Particle`.
3. Leave `Move Particle To Cut Point = On` for the trail to follow the blade.

## 9. Testing
- Equip the **Knife** tool and drag across a lesion → a trail follows the blade tip and streams for the
  whole slice.
- Release → the trail stops emitting and the remaining particles fade, then the instance is destroyed.
- Switch to the **Tongs** tool and pull → no slice trail appears.
- Leave the field empty → no errors, no effect (log note only).

## 10. Files Touched
- `Assets/Core/Script/Treatment/Knife/KnifeMiniGame.cs` — fields, `BeginActionAt`, `Update`, `EndAction`,
  `OnDestroy`, and the `BeginSliceTrail` / `UpdateSliceTrail` / `StopSliceTrail` helpers.
