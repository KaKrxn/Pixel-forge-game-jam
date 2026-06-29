# Dialog Speaker Name, Typewriter Effect, and Text Blip SFX Report

## 1. Objective

Improve the current dialog system so each dialog line can clearly show the speaker name and reveal text with a typewriter effect supported by short text blip sound effects.

Target result:

- Dialog lines still come from `DialogData` ScriptableObjects.
- Each line chooses whether the speaker is the Player or the Customer.
- The dialog UI displays the actual speaker name instead of only the enum value.
- Dialog body text appears gradually with a typewriter effect.
- A short blip sound can play while characters are revealed.
- Customer blip sound can be overridden per `DialogData` asset, so each customer can have a different voice texture.
- The Next button is hidden while the current line is still typing.
- The Next button appears only after the full line is visible / the sentence is finished.
- Clicking the Next button after it appears advances to the next line.

This report only plans the dialog upgrade. It does not implement the code yet.

## 2. Current Project Analysis

Current dialog files:

```text
Assets/Core/Script/Dialog/Dialog.cs
Assets/Core/Script/Dialog/DialogData.cs
Assets/Core/Script/Dialog/Bubble.cs
```

Current `DialogData` state:

- `DialogData` is a ScriptableObject.
- It stores a list of `DialogLine`.
- Each `DialogLine` currently has:
  - `DialogSpeaker speaker`
  - `string text`
- `DialogSpeaker` currently supports:
  - `Player`
  - `Customer`

Current `Dialog` runtime behavior:

- `Dialog.Open(customer)` starts at line index `0`.
- `Dialog.Advance()` moves to the next line.
- `ShowCurrentLine()` immediately assigns:
  - `speakerText.text = line.Speaker.ToString()`
  - `bodyText.text = line.Text`
- The dialog already uses TextMeshPro through `TMP_Text`.
- The dialog has a fallback UI creation path if UI references are missing.

Current limitations:

- The speaker display is generic.
  - It shows `Player` or `Customer`, not actual names.
- There is no per-dialog speaker display name data.
- There is no typewriter reveal.
- There is no text blip sound.
- The Next button always advances immediately.
- There is no rule that hides the Next button until the current line finishes typing.

## 3. Recommended Design

### 3.1 Speaker Name Model

The dialog line should continue using `DialogSpeaker` to identify who is speaking, but the visible name should be resolved separately.

Recommended first-pass fields in `DialogData`:

```text
string playerDisplayName = "Player"
string customerDisplayName = "Customer"
AudioClip customerBlipClip
List<AudioClip> customerBlipClips
List<DialogLine> lines
```

Reason:

- This keeps each line simple.
- Designers only choose `Player` or `Customer` per line.
- The same dialog asset can show nicer names without manually typing a speaker name for every line.

Example:

```text
playerDisplayName: "Apprentice"
customerDisplayName: "Mira"

Line 1:
speaker: Customer
text: "I felt something move under my wrist after touching the root."

Line 2:
speaker: Player
text: "Show me where the burning started."
```

Runtime output:

```text
Mira
I felt something move under my wrist after touching the root.

Apprentice
Show me where the burning started.
```

### 3.2 Future Data Upgrade

Later, when the project has multiple customers, speaker names should move into `CustomerData`.

Future design:

```text
CustomerData
  customerName
  dialogData
  treatmentCase
```

Then `Dialog.Open(CustomerAgent customer)` can read the name from the current customer instead of keeping every name in `DialogData`.

Recommended now:

- Add `playerDisplayName` and `customerDisplayName` to `DialogData`.
- Add optional `customerBlipClip` to `DialogData` for single customer-specific dialog blips.
- Add optional `customerBlipClips` to `DialogData` for 2-5 short customer blip variations.
- Keep a future path open for `CustomerData`.

## 4. Typewriter Effect Design

### 4.1 Runtime Behavior

When a dialog line is shown:

1. The speaker name appears immediately.
2. The body text starts empty.
3. The body text reveals one character at a time.
4. The Next button is hidden while the line is still typing.
5. When the full line is visible:
   - The typewriter stops.
   - The Next button appears.
6. If the player clicks Next after the button appears:
   - The dialog advances to the next line.
7. If there is no next line:
   - The dialog closes.
   - `GameFlow.CompleteDialog()` runs.

This makes the dialog pacing intentional. The player cannot accidentally skip a line before the sentence has finished appearing.

### 4.2 Typewriter Timing

Recommended serialized fields in `Dialog.cs`:

```text
float charactersPerSecond = 45f
bool useUnscaledTime = true
```

Recommended behavior:

- `charactersPerSecond` controls reveal speed.
- `useUnscaledTime` allows dialog to work even if gameplay time is paused later.

Default value:

```text
charactersPerSecond = 45
```

This should feel fast enough for repeated play while still visibly animating text.

### 4.3 Rich Text Handling

TextMeshPro text may eventually use tags such as:

```text
<color=#ffcc66>burning</color>
<i>whispering</i>
```

Simple first-pass typewriter:

- Assign the full text to `bodyText.text`.
- Reveal using `bodyText.maxVisibleCharacters`.

Why this is better than substring:

- TMP rich text tags remain intact.
- The script does not accidentally reveal partial markup tags.
- It avoids broken formatting during the reveal.

Recommended implementation:

```text
bodyText.text = fullLineText
bodyText.maxVisibleCharacters = 0
bodyText.ForceMeshUpdate()
visibleCharacterCount = bodyText.textInfo.characterCount
```

Then increase `maxVisibleCharacters` over time.

## 5. Text Blip SFX Design

### 5.1 Audio Behavior

The text blip sound should be short, subtle, and not play on every single character if that becomes noisy.

Recommended behavior:

- Play blip only when a visible non-space character appears.
- Skip spaces and line breaks.
- Limit blip frequency with a small interval.
- Stop any remaining one-shot blip tail when the current line completes.

Recommended serialized fields in `Dialog.cs`:

```text
AudioSource dialogAudioSource
AudioClip playerBlipClip
AudioClip customerBlipClip
float blipVolume = 0.45f
int blipEveryVisibleCharacters = 2
float minimumBlipInterval = 0.025f
bool stopBlipsWhenLineCompletes = true
```

### 5.2 Speaker-Based Blips

Use different clips for Player and Customer if available. Customer lines should also support a per-dialog override because different customers may need different voice textures.

- `playerBlipClip`
- `DialogData.customerBlipClips`
- `DialogData.customerBlipClip`
- `Dialog.customerBlipClip`

Clip priority:

```text
Player line:
1. Dialog.playerBlipClip

Customer line:
1. DialogData.customerBlipClips variation list
2. DialogData.customerBlipClip single fallback
3. Dialog.customerBlipClip fallback
```

If only one clip exists:

- Assign the same clip to `playerBlipClip` and `customerBlipClip`.
- Leave `DialogData.customerBlipClip` empty unless that customer needs a unique clip.
- Leave `DialogData.customerBlipClips` empty unless that customer has multiple short variations.

Recommended first implementation:

- Keep a general `customerBlipClip` fallback on `Dialog`.
- Let designers assign a unique `customerBlipClip` or 2-5 clips in `customerBlipClips` on each `DialogData` asset.
- Randomize one customer variation when a Customer line starts, then reuse that same clip for every blip in that line.
- Avoid using the same Customer line clip twice in a row when more than one valid variation exists.

### 5.3 Audio Mixer Routing

Dialog blip is a gameplay/UI sound, not music.

Recommended routing:

- Dialog blip `AudioSource.outputAudioMixerGroup` should be assigned to the `Game` mixer group.
- Volume should be controlled by `GameVolume`.

The project already has an AudioMixer direction:

```text
MusicVolume -> Music group
GameVolume  -> Game group
```

Dialog blip should use the `Game` group.

## 6. Recommended Script Changes

### 6.1 DialogData.cs

Add display name fields:

```text
[SerializeField] private string playerDisplayName = "Player";
[SerializeField] private string customerDisplayName = "Customer";
[SerializeField] private AudioClip customerBlipClip;
[SerializeField] private List<AudioClip> customerBlipClips;

public string PlayerDisplayName => playerDisplayName;
public string CustomerDisplayName => customerDisplayName;
public AudioClip CustomerBlipClip => customerBlipClip;
public IReadOnlyList<AudioClip> CustomerBlipClips => customerBlipClips;
```

Add helper methods:

```text
public string GetDisplayName(DialogSpeaker speaker)
public AudioClip GetBlipClip(DialogSpeaker speaker, AudioClip playerFallback, AudioClip customerFallback, AudioClip previousCustomerClip = null)
```

Recommended behavior:

- If speaker is `Player`, return `playerDisplayName`.
- If speaker is `Customer`, return `customerDisplayName`.
- If a display name is empty, fall back to `speaker.ToString()`.
- If Customer variation clips exist in `DialogData`, choose one randomly at the start of each Customer line before using the single clip or `Dialog` fallback clip.
- Reuse the selected Customer clip for every blip in that line.
- If more than one valid Customer variation exists, avoid choosing the same line clip twice in a row.

### 6.2 Dialog.cs

Add typewriter fields:

```text
[Header("Typewriter")]
[SerializeField] private float charactersPerSecond = 45f;
[SerializeField] private bool useUnscaledTime = true;

[Header("Text Blip SFX")]
[SerializeField] private AudioSource dialogAudioSource;
[SerializeField] private AudioClip playerBlipClip;
[SerializeField] private AudioClip customerBlipClip;
[SerializeField, Range(0f, 1f)] private float blipVolume = 0.45f;
[SerializeField] private int blipEveryVisibleCharacters = 2;
[SerializeField] private float minimumBlipInterval = 0.025f;
[SerializeField] private bool stopBlipsWhenLineCompletes = true;
```

Add runtime fields:

```text
Coroutine typewriterRoutine;
string currentLineText;
DialogSpeaker currentSpeaker;
bool isTyping;
```

Change `Advance()` behavior:

```text
if (isTyping)
{
    return;
}

AdvanceToNextLine();
```

The Next button should normally be inactive while `isTyping` is true, so this guard mostly protects against keyboard submit, double-click timing, or direct event calls.

Change `ShowCurrentLine()` behavior:

- Resolve speaker display name.
- Hide the Next button.
- Start typewriter coroutine.
- Do not assign the whole line as instantly visible.

When the typewriter finishes:

- Set `isTyping` to false.
- Show the Next button.

### 6.3 Optional Split

If `Dialog.cs` becomes too large, create:

```text
Assets/Core/Script/Dialog/DialogTypewriter.cs
```

Recommended first pass:

- Keep it inside `Dialog.cs`.

Reason:

- The current system is still small.
- The typewriter effect directly depends on dialog line advancement.
- A split can happen later if the dialog system grows.

## 7. Detailed Implementation Plan

### Phase 1: Update Dialog Data

Goal:

Let each dialog asset define actual visible names for Player and Customer.

Steps:

1. Open `DialogData.cs`.
2. Add:
   - `playerDisplayName`
   - `customerDisplayName`
   - `customerBlipClip`
   - `customerBlipClips`
3. Add read-only properties.
4. Add `GetDisplayName(DialogSpeaker speaker)`.
5. Add `GetBlipClip(DialogSpeaker speaker, AudioClip playerFallback, AudioClip customerFallback, AudioClip previousCustomerClip = null)`.
6. Keep the existing `speaker` enum on each line.

Validation:

- Existing dialog assets still compile.
- Existing line speaker choices remain intact.
- Inspector shows the new name and audio fields.
- Existing single Customer blip assignment still works as fallback.

### Phase 2: Update Dialog Name Display

Goal:

Show the actual display name instead of `Player` or `Customer`.

Steps:

1. Open `Dialog.cs`.
2. In `ShowCurrentLine()`, replace:
   - `line.Speaker.ToString()`
3. Use:
   - `dialogData.GetDisplayName(line.Speaker)`
4. If no display name is set, fall back to enum text.

Validation:

- Customer line shows the configured customer name.
- Player line shows the configured player name.

### Phase 3: Add Typewriter Reveal

Goal:

Reveal body text over time.

Steps:

1. Add typewriter serialized fields.
2. Add typewriter runtime state.
3. Add `StartTypewriter(DialogLine line)`.
4. Add `RunTypewriter(string text)`.
5. Use `TMP_Text.maxVisibleCharacters`.
6. Stop the previous coroutine before starting a new line.
7. Reset `maxVisibleCharacters` when hiding or completing.

Validation:

- Text begins empty.
- Text reveals over time.
- Rich text does not break if TMP tags are used.

### Phase 4: Add Next Button Reveal Behavior

Goal:

Prevent the player from advancing until the current sentence has finished typing.

Steps:

1. Change `Advance()`.
2. If typing:
   - Ignore the input.
   - Do not advance line index.
   - Do not force-complete the line.
3. If not typing:
   - Advance to next line as before.
4. Hide `nextButton.gameObject` when starting a new line.
5. Show `nextButton.gameObject` only when the typewriter coroutine finishes.

Validation:

- Next button is not visible while text is typing.
- Player cannot click Next before the sentence finishes.
- Next button appears after the line is fully visible.
- Clicking Next after it appears advances to the next line.
- Last line closes dialog correctly.

### Phase 5: Add Text Blip SFX

Goal:

Play subtle sounds as text appears.

Steps:

1. Add `AudioSource` field.
2. Add player/customer blip clip fields.
3. Add blip settings.
4. During typewriter reveal:
   - Check newly visible characters.
   - Skip spaces and line breaks.
   - Respect `blipEveryVisibleCharacters`.
   - Respect `minimumBlipInterval`.
5. Play via `dialogAudioSource.PlayOneShot`.
6. Stop remaining blip playback when the line finishes if `stopBlipsWhenLineCompletes` is enabled.

Validation:

- Customer lines use customer blip.
- Customer lines use `DialogData.customerBlipClips` first when assigned.
- Customer variation clips are randomized once per Customer line and avoid immediate line-to-line repeats when possible.
- Customer lines fall back to `DialogData.customerBlipClip`, then `Dialog.customerBlipClip`.
- Player lines use player blip.
- If no clip is assigned, no error appears.
- Blips do not play too fast or too loudly.
- Blips do not keep playing after the current line is finished.

### Phase 6: Unity Setup

Goal:

Connect scene references and audio assets.

Steps:

1. Select the GameObject with `Dialog`.
2. Assign:
   - `speakerText`
   - `bodyText`
   - `nextButton`
   - `dialogData`
3. Add or select a Dialog AudioSource.
4. Set AudioSource:
   - `Play On Awake`: off
   - `Output`: `Game` mixer group
5. Assign:
   - `playerBlipClip`
   - `customerBlipClip`
6. Open each `DialogData` asset and assign `customerBlipClips` with 2-5 short clips when that customer should have variation, for example:
   - `Customer_A_Blips_01`
   - `Customer_A_Blips_02`
   - `Customer_A_Blips_03`
7. Use `customerBlipClip` only as a single fallback clip.
8. Tune:
   - `charactersPerSecond`
   - `blipVolume`
   - `blipEveryVisibleCharacters`
   - `minimumBlipInterval`
   - `stopBlipsWhenLineCompletes`

Validation:

- Dialog opens normally.
- Names display correctly.
- Text types in.
- Text blip plays through Game volume.
- Next button appears only after the current sentence is finished.

## 8. Suggested Defaults

Recommended default values:

```text
Player Display Name: "Player"
Customer Display Name: "Customer"
Characters Per Second: 45
Use Unscaled Time: true
Blip Volume: 0.45
Blip Every Visible Characters: 2
Minimum Blip Interval: 0.025
Stop Blips When Line Completes: true
```

If the blip is too noisy:

```text
Blip Every Visible Characters: 3
Minimum Blip Interval: 0.04
```

If the text feels too slow:

```text
Characters Per Second: 60
```

## 9. Testing Checklist

### Data Test

- `DialogData` shows player and customer display name fields.
- `DialogData` shows an optional customer blip clip field and customer blip variation list.
- Existing dialog lines are still visible in the Inspector.
- Speaker enum selection still works.

### Name Display Test

- Player lines show the player display name.
- Customer lines show the customer display name.
- Empty name fields fall back safely.

### Typewriter Test

- A line begins hidden.
- Text reveals gradually.
- Next button is hidden while typing.
- Next button appears after the full line is visible.
- Clicking Next after typing advances.
- Last line closes the dialog.

### SFX Test

- Blips play while visible characters appear.
- Blips do not play for every space or line break.
- Player and Customer can use different blips.
- Different `DialogData` assets can use different Customer blip clips.
- Customer variation clips are randomized per line without immediate line-to-line repeats when more than one valid clip exists.
- Blips stop when the current line completes.
- Missing clips do not throw errors.
- Dialog blips follow Game Volume through the AudioMixer.

### Regression Test

- Bubble still opens dialog.
- Dialog still completes into treatment flow.
- The fallback runtime UI still works if scene references are missing.
- No console errors appear.

## 10. Risks and Mitigation

### Risk: Typewriter breaks rich text

Mitigation:

- Use `TMP_Text.maxVisibleCharacters` instead of substring.

### Risk: Blip audio becomes annoying

Mitigation:

- Skip whitespace.
- Use `blipEveryVisibleCharacters`.
- Use `minimumBlipInterval`.
- Keep volume low.

### Risk: Dialog pacing feels too slow

Mitigation:

- Tune `charactersPerSecond` so text appears quickly enough.
- Keep the sentence length reasonable.
- Consider adding a later accessibility option to reveal text instantly if needed.

### Risk: Customer names will later move to CustomerData

Mitigation:

- Keep `DialogData` display names as the first-pass solution.
- Later, `Dialog.Open(CustomerAgent customer)` can override customer display name from `CustomerData`.

## 11. Recommended Work Order

1. Add display names to `DialogData`.
2. Update `Dialog` speaker display logic.
3. Add typewriter coroutine using `maxVisibleCharacters`.
4. Hide Next while typing and reveal it when the current line is complete.
5. Add text blip SFX.
6. Assign AudioSource and clips in Unity.
7. Test the existing bubble to dialog to treatment flow.

## 12. Acceptance Criteria

The upgrade is complete when:

- DialogData can define visible names for Player and Customer.
- Dialog UI shows the correct visible speaker name.
- Dialog body text appears with a typewriter effect.
- Next button is hidden while the current line is typing.
- Next button appears only after the full line is visible.
- Next button advances only after the sentence is finished.
- Text blip sound plays during the typewriter reveal.
- Text blip sound can differ between Player and Customer.
- Text blip sound can differ between Customer `DialogData` assets.
- Customer `DialogData` assets can define 2-5 short blip variations such as `Customer_A_Blips_01`, `Customer_A_Blips_02`, and `Customer_A_Blips_03`.
- Dialog still completes into the existing treatment flow.
- No scene layout or sprite placement is reset.

## 13. Summary

The current dialog system already has a good foundation: ScriptableObject data, Player/Customer speaker selection, TMP text, and a working Next button. The next improvement should add visible speaker names, typewriter text reveal, and subtle text blip SFX.

The safest implementation is to extend `DialogData` with display names, an optional single customer blip clip, and an optional customer blip variation list, then extend `Dialog.cs` with a typewriter coroutine that uses TextMeshPro `maxVisibleCharacters`. This keeps existing dialog assets compatible while adding a more polished and readable presentation layer.
