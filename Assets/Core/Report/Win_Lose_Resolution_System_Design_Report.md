# Win / Lose Resolution System Design Report

**Project:** Cure Me, Please  
**Workspace:** `Z:\.Project Unity\Pixel-forge-game-jam`  
**Engine:** Unity 6000.3.17f1, 2D Pixel Art  
**Report date:** July 4, 2026  
**Module:** Game Flow / Result Resolution  
**Document status:** Technical design and implementation plan  

---

## 1. Purpose

The Win / Lose Resolution System turns the current clinic runtime state into a clear game result.

The project already has the core trigger points:

- `ClinicFlowState.AllCustomersComplete`
- `ClinicFlowState.GameOver`
- `Sanity.TransformationReached`
- `GameFlow.StateChanged`

However, the project does not yet have a dedicated result layer that:

- Shows a win panel when all queued customers are cured.
- Shows a lose panel when the active customer transforms.
- Explains why the player lost.
- Locks gameplay after the run ends.
- Offers Restart and Main Menu actions.

This report designs that missing layer without rewriting the existing `GameFlow`.

---

## 2. Current Project Context

### 2.1 Existing Systems To Reuse

| System | Current role |
|---|---|
| `GameFlow` | Owns the clinic state machine and broadcasts `StateChanged`. |
| `Sanity` | Tracks patient sanity and invokes `TransformationReached` when max sanity is reached. |
| `CustomerQueueRuntime` | Advances customers and reaches completion when the queue is empty. |
| `Treatment` | Completes a customer case and calls `GameFlow.CompleteTreatment(customer)`. |
| `RoomTransition` | Moves between Counter Room and Treatment Room. |
| `MainMenuController` | Already shows how scene loading / quit actions are handled in menu code. |

### 2.2 Existing Win Trigger

The queue path already reaches:

```text
CustomerLeft
-> customerQueue.Advance()
-> no next customer
-> GameFlow.SetState(AllCustomersComplete)
```

This should become the win trigger.

### 2.3 Existing Lose Trigger

The sanity path already reaches:

```text
Sanity reaches max
-> Sanity.TransformationReached
-> GameFlow.TriggerTransformation()
-> GameFlow.SetState(GameOver)
```

This should become the lose trigger.

---

## 3. Design Goals

1. Keep `GameFlow` as the source of gameplay state.
2. Keep UI result display outside `GameFlow`.
3. Use `AllCustomersComplete` as the first win condition.
4. Use `GameOver` as the first lose condition.
5. Start with one concrete fail reason: `SanityMaxed`.
6. Make the system easy to extend later with mistake tracking, fail tips, score, and multiple endings.
7. Avoid disturbing current scene layout, existing sprites, and manually authored room objects.

---

## 4. Non-Goals For The First Version

The first version should not implement these yet:

- Final transformation animation.
- Multiple endings.
- Full scoring.
- Full mistake tracking.
- Save data.
- Detailed fail analytics.
- Patient-specific result art.
- Final UI art.

Those systems can be added after the basic result loop is stable.

---

## 5. Result Model

### 5.1 Game Result Type

Add:

```csharp
public enum GameResultType
{
    None,
    Win,
    Lose
}
```

Recommended path:

```text
Assets/Core/Script/GameFlow/GameResultType.cs
```

### 5.2 Fail Reason

Add:

```csharp
public enum FailReason
{
    None,
    SanityMaxed,
    CandleExtinguishedTooLong,
    TooManyTreatmentMistakes,
    WrongToolUsed,
    PatientIgnored
}
```

Recommended path:

```text
Assets/Core/Script/GameFlow/FailReason.cs
```

First implementation only needs to use:

```text
SanityMaxed
```

The other enum values are reserved for future systems.

---

## 6. Core Runtime Design

### 6.1 GameResolutionController

Add:

```text
Assets/Core/Script/GameFlow/GameResolutionController.cs
```

Responsibilities:

- Reference `GameFlow`.
- Reference `GameResultPanel`.
- Subscribe to `GameFlow.StateChanged`.
- Show win result when state becomes `AllCustomersComplete`.
- Show lose result when state becomes `GameOver`.
- Prevent duplicate result display.
- Optionally pause gameplay time after the result appears.

Basic behavior:

```text
OnEnable
-> subscribe to flow.StateChanged

OnDisable
-> unsubscribe

HandleStateChanged(AllCustomersComplete)
-> ResolveWin()

HandleStateChanged(GameOver)
-> ResolveLose(SanityMaxed)
```

### 6.2 GameResultPanel

Add:

```text
Assets/Core/Script/UI/GameResultPanel.cs
```

Responsibilities:

- Own the result UI root.
- Set title text.
- Set description text.
- Set tip text.
- Bind Restart button.
- Bind Main Menu button.
- Hide itself on startup.

Recommended serialized fields:

```csharp
[SerializeField] private GameObject root;
[SerializeField] private TMP_Text titleText;
[SerializeField] private TMP_Text descriptionText;
[SerializeField] private TMP_Text tipText;
[SerializeField] private Button restartButton;
[SerializeField] private Button mainMenuButton;
[SerializeField] private string mainMenuSceneName = "MainMenuScene";
```

### 6.3 Optional Result Data Object

The first version can hard-code placeholder copy in `GameResultPanel`.

If the result copy grows, add:

```text
Assets/Core/Script/GameFlow/GameResultMessageSet.cs
```

or a ScriptableObject:

```text
Assets/Core/Data/GameFlow/GameResultMessages.asset
```

This is not required for the first pass.

---

## 7. Result Flow

### 7.1 Win Flow

```text
Final customer cured
-> Customer exits shop
-> GameFlow.CustomerLeft(customer)
-> queue has no next customer
-> GameFlow state becomes AllCustomersComplete
-> GameResolutionController receives state
-> GameResultPanel.ShowWin()
```

Suggested win copy:

```text
Title: Cure Complete
Description: Every patient survived the night.
Tip: The candle still burns.
```

### 7.2 Lose Flow

```text
Active customer sanity reaches max
-> Sanity.TransformationReached
-> GameFlow.TriggerTransformation()
-> GameFlow state becomes GameOver
-> GameResolutionController receives state
-> GameResultPanel.ShowLose(SanityMaxed)
```

Suggested lose copy:

```text
Title: Treatment Failed
Description: The parasite took over the patient.
Tip: Keep Sanity low by treating carefully and maintaining the candle.
```

---

## 8. Scene UI Design

Create this UI under the main gameplay Canvas:

```text
GameResultPanel
├── Background
├── TitleText
├── DescriptionText
├── TipText
├── RestartButton
│   └── Text
└── MainMenuButton
    └── Text
```

Initial setup:

- `GameResultPanel` root inactive on Play.
- Full-screen dark background.
- Text uses TMP.
- Buttons use standard Unity UI Button.
- UI should render over Dialog, Anatomy, and MiniGameOverlay.

Recommended first-pass layout:

- Centered panel.
- Title at top.
- Description under title.
- Tip under description.
- Restart and Main Menu buttons at the bottom.

---

## 9. Input And Gameplay Locking

The first version can simply show a blocking result panel and set:

```csharp
Time.timeScale = 0f;
```

However, because some existing systems may use unscaled time later, this should be treated as a temporary lock.

Recommended first-pass behavior:

- Result panel blocks raycasts.
- `GameResolutionController` optionally pauses time.
- Restart restores `Time.timeScale = 1f`.
- Main Menu restores `Time.timeScale = 1f`.

Future version:

- Add a dedicated `GameplayInputLock`.
- Let Dialog, Treatment, and MiniGame systems query the lock.

---

## 10. Implementation Plan

### Step 1: Add Result Enums

Create:

```text
Assets/Core/Script/GameFlow/GameResultType.cs
Assets/Core/Script/GameFlow/FailReason.cs
```

Purpose:

- Give the result system stable names for win/lose and fail causes.
- Avoid string comparisons.

### Step 2: Add GameResultPanel

Create:

```text
Assets/Core/Script/UI/GameResultPanel.cs
```

Required public methods:

```csharp
public void Hide();
public void ShowWin();
public void ShowLose(FailReason reason);
```

Button behavior:

- Restart reloads the active scene.
- Main Menu loads the configured menu scene.
- Both restore `Time.timeScale`.

### Step 3: Add GameResolutionController

Create:

```text
Assets/Core/Script/GameFlow/GameResolutionController.cs
```

Required behavior:

- Auto-find `GameFlow` if not assigned.
- Auto-find `GameResultPanel` if not assigned.
- Subscribe to state changes.
- Ignore repeated result calls after resolved.

### Step 4: Connect To Existing GameFlow States

Mapping:

| `ClinicFlowState` | Result |
|---|---|
| `AllCustomersComplete` | Win |
| `GameOver` | Lose |

First lose reason:

```text
GameOver -> SanityMaxed
```

This is slightly broad because future `GameOver` causes may not all be sanity. Later, `GameFlow` should expose a `LastFailReason`.

### Step 5: Create Scene UI

Add the result panel under the active gameplay Canvas.

Assign:

- Root object.
- Title TMP text.
- Description TMP text.
- Tip TMP text.
- Restart button.
- Main Menu button.

### Step 6: Test

Win test:

1. Create or use a queue with one quick customer.
2. Complete treatment.
3. Let the customer exit.
4. Confirm `AllCustomersComplete`.
5. Confirm win panel appears.

Lose test:

1. Start a customer.
2. Force Sanity to max, or temporarily increase Sanity rate for testing.
3. Confirm `GameOver`.
4. Confirm lose panel appears.

Restart test:

1. Trigger win or lose.
2. Click Restart.
3. Confirm active scene reloads.
4. Confirm time scale is restored.

Main menu test:

1. Trigger win or lose.
2. Click Main Menu.
3. Confirm configured menu scene loads.
4. Confirm time scale is restored.

---

## 11. Future Extensions

### 11.1 Last Fail Reason On GameFlow

Later, add:

```csharp
public FailReason LastFailReason { get; private set; }
```

Then change `TriggerTransformation()` to:

```csharp
LastFailReason = FailReason.SanityMaxed;
SetState(ClinicFlowState.GameOver);
```

### 11.2 Mistake Tracking

Add:

```text
MistakeTracker.cs
MistakeType.cs
```

This should collect:

- Knife path mistakes.
- Tongs edge hits.
- Needle stray movement.
- Wrong tool usage.
- Candle-related pressure.

### 11.3 Fail Tip Resolver

Add:

```text
FailTipData.cs
FailTipResolver.cs
```

This should map fail causes to player guidance.

### 11.4 Result Summary

Add:

```text
CaseResultController.cs
RunResultData.cs
```

Possible summary values:

- Customers cured.
- Customers failed.
- Final candle level.
- Final sanity level.
- Treatment mistakes.
- Run duration.

---

## 12. Risks

| Risk | Mitigation |
|---|---|
| Result UI appears behind other UI | Put it under the highest gameplay Canvas or give it highest sorting order. |
| Time scale remains paused after restart/menu | Always restore `Time.timeScale = 1f` before scene load. |
| `GameOver` later has many causes | Add `GameFlow.LastFailReason` before adding new fail causes. |
| Result triggers multiple times | `GameResolutionController` should keep a `resolved` bool. |
| Current single-customer fallback ends at `Complete`, not `AllCustomersComplete` | Result win should only use queue completion for now, or optionally support `Complete` in debug mode. |

---

## 13. Recommended First Implementation Scope

Implement now:

- `GameResultType`
- `FailReason`
- `GameResultPanel`
- `GameResolutionController`
- Win from `AllCustomersComplete`
- Lose from `GameOver`
- Restart button
- Main Menu button

Do later:

- Mistake tracker.
- Fail tip ScriptableObject.
- Multiple endings.
- Final transformation scene.
- Detailed result summary.

---

## 14. Setup Checklist

1. Add `GameResolutionController` to a scene object, ideally `GameFlowManager`.
2. Assign `GameFlow`.
3. Create `GameResultPanel` UI under the gameplay Canvas.
4. Add `GameResultPanel` component.
5. Assign all TMP text and button references.
6. Assign the panel to `GameResolutionController`.
7. Set the main menu scene name.
8. Test win through `AllCustomersComplete`.
9. Test lose through `GameOver`.

---

## 15. Definition Of Done

The system is complete for the first pass when:

- `AllCustomersComplete` shows the win panel.
- `GameOver` shows the lose panel.
- Lose result displays `SanityMaxed` copy.
- Restart reloads the current scene.
- Main Menu loads the configured menu scene.
- Time scale is restored after leaving the result screen.
- Result UI blocks gameplay input.
- No existing GameFlow, Treatment, Candle, Sanity, or mini game flow is broken.

---

## 16. Summary

The current project already has the gameplay trigger points for win and lose, but it does not yet have a result presentation layer. The recommended design is to keep `GameFlow` as the state source, add a `GameResolutionController` that watches `GameFlow.StateChanged`, and add a `GameResultPanel` that displays the final player-facing result. The first pass should stay small: `AllCustomersComplete` means win, `GameOver` means lose, and `SanityMaxed` is the first fail reason. This creates a complete end-of-run loop while leaving room for future mistake tracking, fail tips, scoring, and multiple endings.
