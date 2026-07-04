# Patient Aggression / Obstacle System Design Report

**Project:** Cure Me, Please  
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`  
**Engine:** Unity 6000.3.17f1, 2D Pixel Art  
**Report date:** July 4, 2026  
**Module:** Treatment Obstacle / Patient Behavior  
**Document status:** Technical design and implementation plan  

---

## 1. Purpose

The Patient Aggression / Obstacle System adds a treatment-room obstacle where the patient struggles, twitches, or briefly loses control during a mini game.

The goal is not to make the patient attack the player directly. The goal is to make treatment harder by moving the body and all active treatment targets for a short burst.

During an aggression burst:

- The active treatment body moves or shakes.
- Parasites, lesions, pustules, masks, wound visuals, and spawn anchors move with the body.
- The player must time tool usage more carefully.
- Existing mini game mistake logic can raise Sanity if the player slips.
- Future medicine can suppress the burst for a short time.

---

## 2. Current Project Context

### 2.1 Existing Systems To Reuse

| System | Current role |
|---|---|
| `TreatmentBodyPrefab` | Owns the active treatment body, body sprite root, gameplay root, and mini game anchor roots. |
| `TreatmentBodyPrefabSpawner` | Instantiates the body prefab for a mini game + body area pair. |
| `AnatomyController` | Routes selected body areas to Tongs, Knife, or Needle and applies spawned body prefabs. |
| `TongsMiniGame` | Uses `ApplyBodyPrefab(TreatmentBodyPrefab body)` and has parasite roots / anchors. |
| `KnifeMiniGame` | Uses `ApplyBodyPrefab(TreatmentBodyPrefab body)` and has lesion roots / anchors. |
| `NeedleMiniGame` | Uses `ApplyBodyPrefab(TreatmentBodyPrefab body)` and has pustule roots / anchors. |
| `Sanity` | Tracks Stable, Warning, Critical, and Transformed states. |
| `TreatmentCaseData` | Stores the active case and required treatment areas. |
| `GameFlow` | Owns the active customer and treatment stress hooks. |

### 2.2 Best Integration Point

The current body prefab structure makes this system practical:

```text
TreatmentBodyPrefab
|-- GameplayRoot
|-- BodySpriteRoot
|-- ParasiteRoot / LesionRoot / PustuleRoot
|-- Spawn Points
`-- OptionalVFXRoot
```

If the system moves the body prefab root or a dedicated motion root, everything under it moves together:

- Body sprite.
- Parasites.
- Lesions.
- Pustules.
- Spawn anchors.
- Masks.
- Local VFX.

This is better than moving each parasite, lesion, pustule, or anchor separately.

---

## 3. Design Goals

1. Add a visible patient struggle obstacle during treatment mini games.
2. Keep the first version small and safe.
3. Move one root transform so all active treatment objects stay aligned.
4. Make aggression more likely when Sanity is high.
5. Pause or stop aggression when leaving the active mini game.
6. Reset body position after each burst.
7. Leave room for future medicine suppression.
8. Avoid changing existing authored scene positions or sprite assignments.

---

## 4. Non-Goals For The First Version

The first version should not implement:

- Medicine / sedative gameplay.
- Final patient animation.
- Final SFX.
- Patient attack behavior.
- Complex procedural animation.
- Per-species animation rigs.
- Result scoring.
- Full dialog reaction integration.

Those can be added after the body-motion obstacle is stable.

---

## 5. Core Design

The system should use three layers:

```text
PatientAggressionProfile
-> PatientAggressionController
-> BodyJitterController
```

### 5.1 PatientAggressionProfile

`PatientAggressionProfile` is a ScriptableObject that stores aggression tuning.

Recommended path:

```text
Assets/Core/Script/Treatment/Obstacle/PatientAggressionProfile.cs
```

Recommended asset path:

```text
Assets/Core/Data/Treatment/Obstacle/
```

Recommended fields:

```csharp
[SerializeField] private bool aggressionEnabled = true;
[SerializeField] private float baseChancePerSecond = 0.08f;
[SerializeField] private float stableChanceMultiplier = 0f;
[SerializeField] private float warningChanceMultiplier = 1f;
[SerializeField] private float criticalChanceMultiplier = 2f;
[SerializeField] private float burstDurationMin = 0.5f;
[SerializeField] private float burstDurationMax = 1.25f;
[SerializeField] private float cooldownMin = 3f;
[SerializeField] private float cooldownMax = 7f;
[SerializeField] private float shakeAmplitude = 0.08f;
[SerializeField] private float shakeFrequency = 18f;
[SerializeField] private float lurchAmplitude = 0.12f;
[SerializeField] private float lurchChancePerBurst = 0.35f;
```

First-pass defaults should be subtle because the mini games already contain accuracy pressure.

### 5.2 PatientAggressionController

`PatientAggressionController` decides when aggression starts and stops.

Recommended path:

```text
Assets/Core/Script/Treatment/Obstacle/PatientAggressionController.cs
```

Responsibilities:

- Hold the active `Sanity`.
- Hold the active `PatientAggressionProfile`.
- Hold a `BodyJitterController`.
- Tick cooldown and trigger chance.
- Start aggression bursts.
- Stop bursts.
- Pause / resume when leaving and returning to treatment.
- Expose events for future SFX and dialog.

Recommended events:

```csharp
public event Action AggressionStarted;
public event Action AggressionEnded;
```

Recommended public methods:

```csharp
public void Begin(Sanity sanity, Transform bodyMotionRoot, PatientAggressionProfile profile);
public void Stop();
public void Pause();
public void Resume();
public void Suppress(float duration);
```

`Suppress(float duration)` is included for future medicine, but it does not need a user-facing medicine system yet.

### 5.3 BodyJitterController

`BodyJitterController` moves the active transform.

Recommended path:

```text
Assets/Core/Script/Treatment/Obstacle/BodyJitterController.cs
```

Responsibilities:

- Store the original local position.
- Apply a temporary motion offset.
- Optionally add a one-time lurch offset.
- Reset the target after the burst.
- Support pause and stop safely.

Recommended public methods:

```csharp
public void Bind(Transform target);
public void StartBurst(PatientAggressionProfile profile);
public void Tick(float deltaTime);
public void StopAndReset();
```

---

## 6. Data Ownership

There are two possible places to store the aggression profile.

### Option A: Store On CustomerDefinition

Use this if aggression is a personality trait.

Example:

```text
Goblin customer -> aggressive profile
Calm villager -> calm profile
Wolf traveler -> shaky profile
```

Pros:

- Personality belongs to the customer.
- Random customer queue can choose behavior naturally.

Cons:

- Case difficulty is less centralized.

### Option B: Store On TreatmentCaseData

Use this if aggression is a symptom / case difficulty value.

Pros:

- `TreatmentCaseData` already owns treatment requirements.
- Difficulty can live beside required treatments.
- The current project already treats the case as the source of treatment truth.

Cons:

- Patient personality and treatment difficulty are mixed.

### Recommendation

Start with `TreatmentCaseData`.

Add a field later:

```csharp
[SerializeField] private PatientAggressionProfile aggressionProfile;
public PatientAggressionProfile AggressionProfile => aggressionProfile;
```

Reason:

- The current treatment pipeline already resolves the active case from `CustomerCaseProvider`.
- The first version needs case-driven difficulty more than character personality.
- A future `PatientBehaviorProfile` can combine personality, voice, visual variant, and aggression.

---

## 7. Trigger Rules

Aggression should be tied to Sanity so it feels fair and readable.

First-pass rule:

| Sanity state | Aggression chance |
|---|---|
| Stable | No trigger or very low trigger. |
| Warning | Normal profile chance. |
| Critical | Higher profile chance. |
| Transformed | No aggression, because the game is already lost. |

Recommended calculation:

```text
chanceThisFrame = baseChancePerSecond * stateMultiplier * deltaTime
```

State multipliers:

```text
Stable = 0
Warning = 1
Critical = 2
Transformed = 0
```

This makes aggression a pressure response rather than random noise.

---

## 8. Motion Design

### 8.1 Burst Motion

The first version should use a burst:

```text
Duration: 0.5 to 1.25 seconds
Motion: small jitter with optional lurch
Cooldown: 3 to 7 seconds
```

Example movement:

```text
x = sine(time * frequency) * amplitude
y = sine(time * frequency * 0.7) * amplitude * 0.5
```

Optional lurch:

```text
lurchOffset = randomDirection * lurchAmplitude
```

The lurch should ease back to zero before the burst ends.

### 8.2 What Transform Should Move

Preferred target:

```text
TreatmentBodyPrefab.GameplayRoot
```

Fallback target:

```text
TreatmentBodyPrefab.transform
```

If a future prefab needs the background to remain still but only the patient arm / torso to move, add a dedicated child:

```text
BodyMotionRoot
|-- BodySpriteRoot
|-- ParasiteRoot / LesionRoot / PustuleRoot
|-- Spawn Points
`-- OptionalVFXRoot
```

Then bind aggression to `BodyMotionRoot`.

---

## 9. Sanity Interaction

The first version should not increase Sanity directly during aggression.

Reason:

- The obstacle already makes the player more likely to slip.
- Existing mini game mistake logic can raise Sanity.
- Direct Sanity gain from aggression may feel unfair.

Future optional field:

```csharp
[SerializeField] private float sanityPenaltyPerSecondWhileAggressive;
```

Default should be `0`.

---

## 10. Mini Game Integration

Each mini game already has `ApplyBodyPrefab(TreatmentBodyPrefab body)`.

When a body prefab is applied:

```text
mini game receives body prefab
-> resolve body motion root
-> aggression controller begins using that root
```

Recommended integration paths:

### Option A: Central Integration In AnatomyController

Pros:

- One place knows which body prefab was spawned.
- One place knows when mini games start and complete.

Cons:

- `AnatomyController` gets more responsibilities.

### Option B: Integration In Each Mini Game

Pros:

- Each mini game controls its own runtime root.
- Aggression pauses/stops with the mini game.

Cons:

- Repeated code across Tongs, Knife, and Needle.

### Recommendation

Use a small shared controller and call it from each mini game's `ApplyBodyPrefab`.

Reason:

- Tongs, Knife, and Needle already have body-specific setup in `ApplyBodyPrefab`.
- It keeps motion tied to the active mini game root.
- It avoids making `AnatomyController` too large.

---

## 11. Pause / Resume / Stop Rules

### Start

Start aggression after:

```text
Mini game begins
-> body prefab is applied
-> controller has Sanity and body root
```

### Pause

Pause aggression when:

- Player returns to the counter.
- Room transition begins.
- Mini game is paused.
- Result screen appears.

Pause should:

- Stop current burst.
- Reset body root.
- Keep cooldown data if needed.

### Resume

Resume aggression when:

- Player returns to treatment.
- The same mini game continues.

### Stop

Stop aggression when:

- Mini game completes.
- Treatment case completes.
- Customer exits.
- GameOver occurs.

Stop should:

- Reset body root.
- Clear references.
- Unsubscribe events.

---

## 12. Medicine Hook

The first version does not implement medicine.

Still, `PatientAggressionController` should expose:

```csharp
public void Suppress(float duration)
```

Future sedative behavior:

```text
Medicine used
-> aggressionController.Suppress(8 seconds)
-> current burst ends
-> body resets
-> new bursts cannot start until suppress timer ends
```

This gives the future Medicine System a clean integration point.

---

## 13. Event Hooks For Future Systems

Aggression should expose events even if no other system uses them in the first pass.

Possible consumers:

- Treatment chat reactions.
- Patient voice SFX.
- Sanity audio intensity.
- Camera shake.
- UI warning text.

Recommended events:

```csharp
AggressionStarted
AggressionEnded
AggressionSuppressed
```

---

## 14. Implementation Plan

### Step 1: Add Obstacle Folder

Create:

```text
Assets/Core/Script/Treatment/Obstacle/
```

### Step 2: Add PatientAggressionProfile

Create:

```text
PatientAggressionProfile.cs
```

Use `CreateAssetMenu`:

```csharp
[CreateAssetMenu(fileName = "PatientAggressionProfile", menuName = "Pixel Forge/Treatment/Patient Aggression Profile")]
```

### Step 3: Add BodyJitterController

Create:

```text
BodyJitterController.cs
```

Implement:

- Bind target.
- Store original local position.
- Start burst.
- Tick movement.
- Stop and reset.

### Step 4: Add PatientAggressionController

Create:

```text
PatientAggressionController.cs
```

Implement:

- Sanity state chance multiplier.
- Cooldown.
- Burst duration.
- Suppression timer.
- Pause / resume / stop.

### Step 5: Add Profile Reference To TreatmentCaseData

Add optional field:

```csharp
[SerializeField] private PatientAggressionProfile aggressionProfile;
public PatientAggressionProfile AggressionProfile => aggressionProfile;
```

If the field is null, aggression is disabled for that case.

### Step 6: Resolve Active Profile

Each mini game can resolve:

```text
active customer
-> CustomerCaseProvider
-> TreatmentCaseData
-> AggressionProfile
```

### Step 7: Integrate With Body Prefab Application

When `ApplyBodyPrefab(TreatmentBodyPrefab body)` runs:

```text
resolve profile
resolve sanity
resolve body.GameplayRoot
begin aggression
```

### Step 8: Stop On Mini Game End

When mini game stops or completes:

```text
aggressionController.Stop()
```

### Step 9: Pause On Counter Return

When treatment returns to the counter:

```text
aggressionController.Pause()
```

If central pause wiring is not ready, first pass can stop aggression on mini game pause and restart it on resume.

### Step 10: Test

Test cases:

1. No profile assigned: no aggression.
2. Profile assigned, Sanity stable: little or no aggression.
3. Profile assigned, Sanity warning: burst can happen.
4. Profile assigned, Sanity critical: burst happens more often.
5. Body resets after burst.
6. Parasites / lesions / pustules move with the body.
7. Returning to counter resets the body.
8. Completing the mini game stops aggression.

---

## 15. Setup Checklist

1. Create a `PatientAggressionProfile` asset.
2. Assign conservative values:
   - `baseChancePerSecond = 0.08`
   - `warningChanceMultiplier = 1`
   - `criticalChanceMultiplier = 2`
   - `burstDurationMin = 0.5`
   - `burstDurationMax = 1.25`
   - `cooldownMin = 3`
   - `cooldownMax = 7`
   - `shakeAmplitude = 0.08`
3. Assign the profile to a test `TreatmentCaseData`.
4. Enter a mini game through Anatomy.
5. Force or wait for Sanity Warning / Critical.
6. Confirm body movement.
7. Confirm all targets move with the body.
8. Confirm body resets after the burst.

---

## 16. Definition Of Done

The first pass is complete when:

- A treatment case can enable or disable aggression through a profile.
- Aggression triggers only during active treatment mini games.
- Aggression becomes more likely at higher Sanity states.
- The active body root jitters or lurches for short bursts.
- Body, parasites, lesions, pustules, anchors, and masks remain aligned during movement.
- Body position resets after each burst.
- Aggression stops when the mini game completes.
- Aggression pauses or stops when returning to the counter.
- No existing Tongs, Knife, Needle, Anatomy, or body prefab flow is broken.

---

## 17. Future Extensions

### 17.1 Medicine Suppression

Connect sedative medicine to:

```text
PatientAggressionController.Suppress(duration)
```

### 17.2 Reaction Dialog

Use `AggressionStarted` to show a small treatment chat line:

```text
"Hold still. I am almost done."
"Something in them is fighting back."
```

### 17.3 Patient Behavior Profile

Later, combine aggression with:

- Voice profile.
- Starting sanity.
- Confused speech.
- Character visual variant.
- Species-specific body behavior.

### 17.4 Animation Based Motion

Replace procedural jitter with authored animation clips if final art needs more controlled movement.

---

## 18. Risks

| Risk | Mitigation |
|---|---|
| Targets desync from the body | Move a shared root, not individual targets. |
| Jitter feels unfair | Keep first-pass amplitude low and only trigger at Warning / Critical. |
| Body does not reset | Always store and restore original local position. |
| Mini game input becomes too hard | Use cooldowns and short burst durations. |
| Return-to-counter leaves the body offset | Stop and reset on pause / counter return. |
| Future medicine has no hook | Add `Suppress(duration)` from the first version. |

---

## 19. Recommended First Implementation Scope

Implement now:

- `PatientAggressionProfile`
- `BodyJitterController`
- `PatientAggressionController`
- Optional `TreatmentCaseData.AggressionProfile`
- Basic integration with active mini game body prefab

Do later:

- Medicine.
- Reaction dialog.
- SFX.
- Patient-specific animations.
- Final balancing.

---

## 20. Summary

The Patient Aggression / Obstacle System should be built as a lightweight body-root motion system. The current project already has the correct structure for this because active treatment content is grouped under `TreatmentBodyPrefab`. The safest first version is to move the active body gameplay root during short bursts, triggered more often as Sanity rises. This makes treatment harder while keeping parasites, lesions, pustules, masks, and anchors aligned. The system should start as case-driven tuning through `PatientAggressionProfile`, with a future hook for medicine suppression and treatment reaction dialog.
