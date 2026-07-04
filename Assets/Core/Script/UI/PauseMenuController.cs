using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button backButton;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Rules")]
    [SerializeField] private bool canPause = true;
    [SerializeField] private bool pauseOnEscape = true;
    [SerializeField] private bool closeSettingsToMenuOnEscape = true;

    public bool IsPaused { get; private set; }
    public bool CanPause => canPause;

    private void Awake()
    {
        ResolveRoot();
        HideImmediate();
    }

    private void OnEnable()
    {
        AddButtonListener(resumeButton, Resume);
        AddButtonListener(settingsButton, ShowSettings);
        AddButtonListener(mainMenuButton, LoadMainMenu);
        AddButtonListener(backButton, ShowMenu);
    }

    private void OnDisable()
    {
        RemoveButtonListener(resumeButton, Resume);
        RemoveButtonListener(settingsButton, ShowSettings);
        RemoveButtonListener(mainMenuButton, LoadMainMenu);
        RemoveButtonListener(backButton, ShowMenu);
    }

    private void Update()
    {
        if (!pauseOnEscape || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        HandleEscapePressed();
    }

    public void SetCanPause(bool value)
    {
        canPause = value;

        if (!canPause && IsPaused)
        {
            Resume();
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
            return;
        }

        Pause();
    }

    public void Pause()
    {
        if (!canPause || IsPaused)
        {
            return;
        }

        IsPaused = true;
        Time.timeScale = 0f;
        ShowMenu();

        if (root != null)
        {
            root.SetActive(true);
        }
    }

    public void Resume()
    {
        if (!IsPaused)
        {
            HideImmediate();
            return;
        }

        IsPaused = false;
        Time.timeScale = 1f;
        HideImmediate();
    }

    public void ShowMenu()
    {
        SetPanelState(menuPanel, true);
        SetPanelState(settingsPanel, false);
    }

    public void ShowSettings()
    {
        if (!IsPaused)
        {
            Pause();
        }

        SetPanelState(menuPanel, false);
        SetPanelState(settingsPanel, true);
    }

    public void LoadMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("PauseMenuController cannot load Main Menu because no scene name is assigned.", this);
            return;
        }

        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HandleEscapePressed()
    {
        if (!IsPaused)
        {
            Pause();
            return;
        }

        if (closeSettingsToMenuOnEscape && settingsPanel != null && settingsPanel.activeSelf)
        {
            ShowMenu();
            return;
        }

        Resume();
    }

    private void HideImmediate()
    {
        ResolveRoot();
        SetPanelState(menuPanel, true);
        SetPanelState(settingsPanel, false);

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void ResolveRoot()
    {
        if (root == null)
        {
            root = gameObject;
        }
    }

    private static void SetPanelState(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }
}
