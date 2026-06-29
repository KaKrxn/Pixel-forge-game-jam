# Main Menu UI Implementation Report

## 1. Objective

Design and implement a dedicated main menu flow for `Cure Me, Please` while pausing further work on the current Game Scene systems.

The menu must follow the provided mockups:

- Main menu screen with title, subtitle, primary menu buttons, bottom flavor text, and a credit button.
- Settings screen with the same title/subtitle identity, two volume sliders, and a back button.
- Visual tone: dark, quiet, clinic-horror fantasy, minimal UI, warm cream text, muted red primary actions.

This report focuses on analysis, planning, and detailed implementation procedure. It does not continue the current Game Scene treatment/candle/sanity work.

## 2. Current Project Analysis

Project context found in the workspace:

- Existing report folder:
  - `Assets/Core/Report`
- Existing scene folder:
  - `Assets/Core/Scene`
- Existing scenes:
  - `Assets/Core/Scene/MainMenu.unity`
  - `Assets/Core/Scene/GameScene.unity`
  - `Assets/Core/Scene/EndCedit.unity`
- Current Build Settings:
  - Only `Assets/Scenes/SampleScene.unity` is currently listed.
- TextMeshPro is already available in the project.
- The project is already using UI and TMP for the current gameplay UI work.

Important implication:

The Main Menu scene already exists, but Build Settings have not yet been aligned with the new scene structure. The main menu implementation should use the existing `MainMenu.unity` scene and should later add the correct scene order to Build Settings.

Recommended scene order:

1. `Assets/Core/Scene/MainMenu.unity`
2. `Assets/Core/Scene/GameScene.unity`
3. `Assets/Core/Scene/EndCedit.unity`

Note:

`EndCedit.unity` appears to be named with a typo. The implementation can still use the existing path for now to avoid breaking scene references. A later cleanup task can rename it to `EndCredit.unity` if desired.

## 3. UI Design Breakdown From Mockups

### 3.1 Shared Visual Identity

Both mockups share the same base composition:

- Full-screen dark background.
- Large game title at the upper-left area.
- Subtitle directly below the title.
- Main interactive content placed on the left side.
- Bottom-left flavor text.
- Minimal decoration.
- No heavy frames or noisy visual elements.

Recommended palette:

- Background: very dark wine / near-black red.
- Main title: warm cream.
- Subtitle: muted warm grey.
- Primary red: muted dark crimson.
- Primary red highlight: slightly brighter crimson.
- Button border: low-opacity warm red/grey.
- Disabled or secondary surface: transparent dark panel.

Suggested color values:

```text
Background: #1A0E11
Title:      #F6E7D8
Subtitle:   #BFAEA6
Primary:    #8D1F31
Primary 2:  #A62A3E
Panel:      #21181A
Border:     #6B454B
Footer:     #8F7972
```

### 3.2 Main Menu Screen

Required elements:

- Title:
  - Text: `Cure Me, Please`
  - Large, bold, left aligned.
- Subtitle:
  - Text: `A parasite treatment clinic in a dying fantasy world`
  - Smaller, muted, left aligned.
- Main buttons:
  - `Play`
  - `Setting`
  - `Quit`
- Credit button:
  - Text: `Credit`
  - Positioned at bottom-right.
- Flavor text:
  - Text: `The candle is low. The next patient is waiting.`
  - Positioned near bottom-left.

Main button behavior:

- `Play`
  - Loads the gameplay scene.
  - Target scene: `GameScene`.
- `Setting`
  - Opens the settings panel/screen inside the Main Menu scene.
- `Quit`
  - Quits the application in build.
  - In Unity Editor, logs a quit message instead of closing the editor.
- `Credit`
  - Either loads the credit scene or opens a credit panel.
  - Recommended first implementation: load existing `EndCedit.unity` scene because it already exists.

### 3.3 Settings Screen

Required elements:

- Same title:
  - `Cure Me, Please`
- Same subtitle:
  - `A parasite treatment clinic in a dying fantasy world`
- Section title:
  - `Setting`
- Slider label:
  - `Music Volume`
- Slider label:
  - `Game Volume`
- Back button:
  - Text: `Back`
- Same bottom flavor text:
  - `The candle is low. The next patient is waiting.`

Settings behavior:

- `Music Volume`
  - Controls background music volume.
  - Should be saved using `PlayerPrefs`.
- `Game Volume`
  - Controls SFX/gameplay audio volume.
  - Should be saved using `PlayerPrefs`.
- `Back`
  - Returns to the main menu panel without changing scenes.

## 4. Recommended Technical Design

### 4.1 Scene Approach

Use one dedicated `MainMenu.unity` scene with two panels:

```text
MainMenu Scene
  MainMenuCanvas
    Root
      SharedHeader
      MainPanel
      SettingsPanel
      FooterText
```

Reason:

- The setting screen is visually a panel state, not a separate scene.
- Switching panels is faster and cleaner than scene loading.
- Both screens share title, subtitle, and footer text.

### 4.2 Canvas Setup

Recommended Canvas components:

- `Canvas`
  - Render Mode: `Screen Space - Overlay`
- `CanvasScaler`
  - UI Scale Mode: `Scale With Screen Size`
  - Reference Resolution: `1920 x 1080`
  - Match: `0.5`
- `GraphicRaycaster`

Recommended EventSystem:

- `EventSystem`
- `StandaloneInputModule` or Input System UI module depending on project input setup.

For this project, a simple `StandaloneInputModule` is enough for the first menu pass unless the project has already standardized UI input through the new Input System.

### 4.3 UI Technology

Use:

- TextMeshProUGUI for all text.
- Unity UI Button for buttons.
- Unity UI Slider for volume sliders.
- Image components for button backgrounds, slider fills, slider handles, and the background panel.

Avoid:

- Legacy `UnityEngine.UI.Text`.
- Hard-coded screen pixel positions without anchors.
- Scene-wide changes to Game Scene objects.

### 4.4 Script Categories

Recommended folder:

```text
Assets/Core/Script/MainMenu/
```

Recommended scripts:

```text
MainMenuController.cs
MenuAudioSettings.cs
MenuButtonSfx.cs
MenuButtonStyle.cs
```

Minimum first implementation:

```text
MainMenuController.cs
MenuAudioSettings.cs
MenuButtonSfx.cs
```

### 4.5 MainMenuController Responsibilities

`MainMenuController` should control screen state and button actions.

Responsibilities:

- Show the main panel.
- Show the settings panel.
- Load the game scene.
- Load the credit scene.
- Quit the game.
- Keep all scene names editable in the Inspector.

Recommended serialized fields:

```text
GameObject mainPanel
GameObject settingsPanel
Button playButton
Button settingButton
Button quitButton
Button creditButton
Button backButton
string gameSceneName = "GameScene"
string creditSceneName = "EndCedit"
```

Recommended public methods:

```text
ShowMain()
ShowSettings()
Play()
OpenCredits()
Quit()
```

### 4.6 MenuAudioSettings Responsibilities

`MenuAudioSettings` should manage slider values and save them.

Responsibilities:

- Load saved volume values from `PlayerPrefs`.
- Apply slider values on scene start.
- Update music volume when the music slider changes.
- Update game/SFX volume when the game volume slider changes.
- Save changed values.

Recommended `PlayerPrefs` keys:

```text
Settings.MusicVolume
Settings.GameVolume
```

Recommended serialized fields:

```text
Slider musicSlider
Slider gameSlider
AudioSource musicSource
AudioMixer audioMixer
```

First implementation options:

- Simple version:
  - Directly set `AudioSource.volume` for music.
  - Store game volume for later use.
- Better version:
  - Use `AudioMixer` exposed parameters:
    - `MusicVolume`
    - `GameVolume`

Recommended first pass:

Use the simple version if there is no project-wide audio mixer yet. Upgrade to an AudioMixer after the sound system becomes clearer.

### 4.7 MenuButtonSfx Responsibilities

`MenuButtonSfx` should play short UI sounds when the player points at or clicks menu buttons.

Responsibilities:

- Play a hover sound when the cursor enters a button.
- Play a click sound when the button is pressed.
- Use the current Game Volume setting from `PlayerPrefs`.
- Avoid interrupting the music source.
- Work on every main menu button and settings button.

Recommended serialized fields:

```text
AudioSource uiAudioSource
AudioClip hoverClip
AudioClip clickClip
float hoverVolume = 0.6
float clickVolume = 0.8
```

Recommended implementation:

- Add `MenuButtonSfx` to each Button object.
- Implement `IPointerEnterHandler` for hover audio.
- Implement `IPointerClickHandler` for click audio.
- Play sounds through `AudioSource.PlayOneShot`.
- Read `Settings.GameVolume` from PlayerPrefs before playing.

Recommended target buttons:

- `PlayButton`
- `SettingButton`
- `QuitButton`
- `CreditButton`
- `BackButton`

Optional later upgrade:

- Replace per-button components with a shared `MenuSfxRouter` if the menu grows.
- Add separate sound types for disabled buttons, slider movement, or panel open/close.

## 5. Detailed Implementation Plan

### Phase 1: Prepare Main Menu Scene

Goal:

Create a stable Main Menu scene structure that matches the mockup layout.

Steps:

1. Open `Assets/Core/Scene/MainMenu.unity`.
2. Create a Canvas named `MainMenuCanvas`.
3. Add or confirm:
   - `Canvas`
   - `CanvasScaler`
   - `GraphicRaycaster`
4. Set Canvas Scaler:
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1920 x 1080`
   - Match: `0.5`
5. Add EventSystem if missing.
6. Create full-screen background image:
   - Name: `Background`
   - Anchor: stretch full screen.
   - Color: dark wine background.

Validation:

- Enter Play Mode.
- The background fills the screen.
- No console errors appear.

### Phase 2: Build Shared Header and Footer

Goal:

Create the title/subtitle/footer that appear on both menu states.

Hierarchy:

```text
MainMenuCanvas
  Root
    Header
      TitleText
      SubtitleText
    FooterText
```

Title setup:

- Text: `Cure Me, Please`
- TMP font size: large.
- Alignment: left.
- Color: warm cream.
- Anchor: upper-left region.

Subtitle setup:

- Text: `A parasite treatment clinic in a dying fantasy world`
- TMP font size: medium.
- Alignment: left.
- Color: muted warm grey.
- Position: directly below title.

Footer setup:

- Text: `The candle is low. The next patient is waiting.`
- TMP font size: small.
- Alignment: left.
- Color: muted footer color.
- Position: lower-left.

Validation:

- The title reads clearly at 1920x1080.
- The layout still looks acceptable at 16:9 lower resolutions.

### Phase 3: Build Main Panel

Goal:

Create the visible menu state from the first mockup.

Hierarchy:

```text
Root
  MainPanel
    PlayButton
    SettingButton
    QuitButton
    CreditButton
```

Button layout:

- Main buttons stacked vertically on the left.
- `Play` is highlighted red by default.
- `Setting` and `Quit` use darker secondary styling.
- `Credit` sits at bottom-right.

Button sizes:

- Main buttons should have the same width and height.
- Credit button is smaller and placed separately.

Button styling:

- Rounded look can be approximated with a sliced sprite later.
- First pass can use rectangular `Image` components with border-like color treatment.
- Use TMP text inside each button.

Validation:

- Buttons can be selected and clicked.
- Text is readable.
- The layout matches the mockup composition.

### Phase 4: Build Settings Panel

Goal:

Create the visible settings state from the second mockup.

Hierarchy:

```text
Root
  SettingsPanel
    SettingTitleText
    MusicVolumeLabel
    MusicVolumeSlider
    GameVolumeLabel
    GameVolumeSlider
    BackButton
```

Settings panel defaults:

- `SettingsPanel` should start inactive.
- `MainPanel` should start active.

Slider visual style:

- Background track: pale grey.
- Fill area: muted red.
- Handle: darker red.
- Label text: warm cream.

Recommended default values:

- Music Volume: `0.75`
- Game Volume: `0.65`

Validation:

- Clicking `Setting` hides MainPanel and shows SettingsPanel.
- Clicking `Back` hides SettingsPanel and shows MainPanel.
- Slider handles can move.

### Phase 5: Add MainMenuController

Goal:

Wire all buttons into scene behavior.

Script path:

```text
Assets/Core/Script/MainMenu/MainMenuController.cs
```

Implementation behavior:

- `Play`
  - Calls `SceneManager.LoadScene(gameSceneName)`.
- `Setting`
  - Calls `ShowSettings()`.
- `Back`
  - Calls `ShowMain()`.
- `Credit`
  - Calls `SceneManager.LoadScene(creditSceneName)`.
- `Quit`
  - In build: `Application.Quit()`.
  - In Editor: log a message.

Inspector setup:

- Assign `MainPanel`.
- Assign `SettingsPanel`.
- Assign all buttons.
- Set `gameSceneName` to `GameScene`.
- Set `creditSceneName` to `EndCedit`.

Validation:

- `Setting` and `Back` panel switching works.
- `Play` attempts to load the game scene.
- `Credit` attempts to load the credit scene.
- `Quit` does not throw errors in Editor.

### Phase 6: Add MenuAudioSettings

Goal:

Make settings sliders functional and persistent.

Script path:

```text
Assets/Core/Script/MainMenu/MenuAudioSettings.cs
```

Implementation behavior:

- On start:
  - Load saved values from PlayerPrefs.
  - Apply values to sliders.
  - Apply values to audio.
- On slider changed:
  - Update value.
  - Save to PlayerPrefs.

Simple first-pass behavior:

- Music slider sets `musicSource.volume`.
- Game slider saves `Settings.GameVolume` for future SFX usage.

Optional later upgrade:

- Add `AudioMixer`.
- Expose `MusicVolume` and `GameVolume`.
- Convert linear slider value to decibel value.

Validation:

- Slider values persist after Play Mode restart.
- Music volume changes if a music source is assigned.
- No error occurs if no music source exists yet.

### Phase 7: Add Button Hover and Click SFX

Goal:

Make the menu feel responsive when the player moves the cursor over a button or presses it.

Script path:

```text
Assets/Core/Script/MainMenu/MenuButtonSfx.cs
```

Implementation behavior:

- On pointer hover:
  - Play `hoverClip`.
- On pointer click:
  - Play `clickClip`.
- Use one shared `AudioSource` for UI sounds.
- Multiply the clip volume by saved `Settings.GameVolume`.

Unity setup:

1. Create an object named `MenuAudio`.
2. Add an `AudioSource`.
3. Disable `Play On Awake`.
4. Assign the `AudioSource` to each `MenuButtonSfx`.
5. Assign a short hover sound clip.
6. Assign a short click sound clip.
7. Add `MenuButtonSfx` to:
   - `PlayButton`
   - `SettingButton`
   - `QuitButton`
   - `CreditButton`
   - `BackButton`

Validation:

- Moving the cursor over a button plays the hover sound once per entry.
- Clicking a button plays the click sound.
- Changing Game Volume changes UI sound loudness.
- UI sounds do not restart or interrupt background music.

### Phase 8: Build Settings Alignment

Goal:

Make scene loading work in a player build.

Steps:

1. Open Build Settings.
2. Remove or disable old `Assets/Scenes/SampleScene.unity` if it is no longer used.
3. Add scenes in this order:
   - `Assets/Core/Scene/MainMenu.unity`
   - `Assets/Core/Scene/GameScene.unity`
   - `Assets/Core/Scene/EndCedit.unity`
4. Confirm MainMenu is index `0`.

Validation:

- Press Play from MainMenu scene.
- Click Play.
- The game scene loads.
- Return to MainMenu can be added later if needed.

## 6. Suggested Scene Hierarchy

```text
MainMenu
  Main Camera
  EventSystem
  MainMenuCanvas
    Background
    Root
      Header
        TitleText
        SubtitleText
      MainPanel
        PlayButton
          Label
        SettingButton
          Label
        QuitButton
          Label
        CreditButton
          Label
      SettingsPanel
        SettingTitleText
        MusicVolumeLabel
        MusicVolumeSlider
          Background
          Fill Area
          Handle Slide Area
        GameVolumeLabel
        GameVolumeSlider
          Background
          Fill Area
          Handle Slide Area
        BackButton
          Label
      FooterText
  MainMenuController
  MenuAudioSettings
  MenuAudio
```

## 7. Recommended File Structure

```text
Assets/Core/Script/
  MainMenu/
    MainMenuController.cs
    MenuAudioSettings.cs
    MenuButtonSfx.cs

Assets/Core/Scene/
  MainMenu.unity
  GameScene.unity
  EndCedit.unity

Assets/Core/Report/
  MainMenuUIImplementation_Report.md
```

Optional future assets:

```text
Assets/Core/UI/
  Sprite/
    ButtonPanel_9Slice.png
    SliderHandle.png
  Font/
    MenuFont.asset
```

## 8. Implementation Risks

### 8.1 Scene Name Mismatch

Risk:

`Play` or `Credit` buttons fail if scene names are not in Build Settings.

Mitigation:

- Use serialized scene name fields.
- Add all target scenes to Build Settings.
- Test scene loading in Play Mode.

### 8.2 Legacy Text Usage

Risk:

Using legacy `UnityEngine.UI.Text` would conflict with the current direction of converting UI to TMP.

Mitigation:

- Use `TextMeshProUGUI` for every text element.

### 8.3 Resolution Scaling

Risk:

The mockup is wide 16:9. UI may drift on other resolutions.

Mitigation:

- Use anchors.
- Use `CanvasScaler`.
- Test at 1920x1080, 1600x900, and 1280x720.

### 8.4 Audio System Is Not Final

Risk:

Volume sliders may need to change later if a full AudioMixer system is added.

Mitigation:

- Keep `MenuAudioSettings` small.
- Save values through PlayerPrefs.
- Later systems can read the same keys.

## 9. Testing Checklist

### Visual Test

- Main menu matches the first mockup.
- Settings screen matches the second mockup.
- Title, subtitle, and footer remain consistent across both states.
- Button colors and spacing feel close to the design.
- Sliders match the red/grey visual direction.

### Interaction Test

- `Play` loads `GameScene`.
- `Setting` opens the settings panel.
- `Back` returns to the main panel.
- `Credit` loads `EndCedit`.
- `Quit` behaves safely in the Unity Editor.

### Button Sound Test

- Hovering over `Play` plays the hover sound.
- Hovering over `Setting`, `Quit`, `Credit`, and `Back` plays the hover sound.
- Clicking each button plays the click sound.
- Button sounds respect the saved Game Volume value.
- Button sounds do not interrupt menu music.

### Persistence Test

- Change Music Volume.
- Change Game Volume.
- Stop Play Mode.
- Start Play Mode again.
- Values remain saved.

### Build Settings Test

- MainMenu is scene index `0`.
- GameScene is included.
- EndCedit is included.
- No missing scene errors appear.

## 10. Recommended Work Order

1. Build the MainMenu scene UI layout.
2. Create `MainMenuController.cs`.
3. Wire buttons and panel switching.
4. Create `MenuAudioSettings.cs`.
5. Wire sliders and PlayerPrefs.
6. Create `MenuButtonSfx.cs`.
7. Add hover and click sounds to each menu button.
8. Add scenes to Build Settings.
9. Test Play, Setting, Back, Credit, Quit, hover sound, and click sound.
10. Polish colors, spacing, and hover/pressed states.

## 11. First Implementation Scope

The first implementation should include:

- Main menu screen.
- Settings panel.
- Functional Play button.
- Functional Setting and Back buttons.
- Functional Credit button.
- Safe Quit behavior.
- Music and Game volume sliders with saved values.
- Hover sound when the cursor enters a button.
- Click sound when a button is pressed.

The first implementation should not include:

- Animated menu transitions.
- Background art beyond the simple dark background.
- Full AudioMixer architecture unless the project already has one ready.
- Save slot selection.
- New Game / Continue separation.
- Game Scene changes.

## 12. Future Polish Ideas

After the basic Main Menu is working:

- Add subtle candle flicker light in the background.
- Add faint UI fade transitions.
- Add a small animated parasite/candle silhouette.
- Rename `EndCedit.unity` to `EndCredit.unity` with reference-safe Unity rename.
- Add a Credits panel instead of loading a separate credit scene if faster navigation is preferred.

## 13. Summary

The Main Menu should be implemented as a dedicated UI scene using the existing `Assets/Core/Scene/MainMenu.unity` file. The visual layout should follow the provided mockups closely: large title, minimal dark background, left-side menu controls, bottom flavor text, and a separate settings state with two sliders.

The cleanest implementation is a two-panel menu controlled by `MainMenuController`, with audio slider persistence handled by `MenuAudioSettings`. This keeps the menu independent from the paused Game Scene work and allows the project to move toward a proper start flow without disturbing the current gameplay scene.
