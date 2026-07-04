using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button creditButton;
    [SerializeField] private Button backButton;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string creditSceneName = "EndCedit";
    [SerializeField] private bool useLoadingSceneForPlay = true;

    [Header("Timing")]
    [SerializeField] private bool showMainOnStart = true;
    [SerializeField] private float actionDelay = 0.08f;

    private Coroutine pendingActionRoutine;

    private void OnEnable()
    {
        AddButtonListener(playButton, Play);
        AddButtonListener(settingButton, ShowSettings);
        AddButtonListener(quitButton, Quit);
        AddButtonListener(creditButton, OpenCredits);
        AddButtonListener(backButton, ShowMain);
    }

    private void Start()
    {
        if (showMainOnStart)
        {
            ShowMain();
        }
    }

    private void OnDisable()
    {
        RemoveButtonListener(playButton, Play);
        RemoveButtonListener(settingButton, ShowSettings);
        RemoveButtonListener(quitButton, Quit);
        RemoveButtonListener(creditButton, OpenCredits);
        RemoveButtonListener(backButton, ShowMain);
    }

    public void ShowMain()
    {
        SetPanelState(mainPanel, true);
        SetPanelState(settingsPanel, false);
    }

    public void ShowSettings()
    {
        SetPanelState(mainPanel, false);
        SetPanelState(settingsPanel, true);
    }

    public void Play()
    {
        LoadGameScene();
    }

    public void OpenCredits()
    {
        LoadScene(creditSceneName);
    }

    public void Quit()
    {
        StartDelayedAction(QuitApplication);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("MainMenuController cannot load a scene because the scene name is empty.");
            return;
        }

        StartDelayedAction(() => SceneManager.LoadScene(sceneName));
    }

    private void LoadGameScene()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogWarning("MainMenuController cannot load the game because the game scene name is empty.");
            return;
        }

        if (!useLoadingSceneForPlay || string.IsNullOrWhiteSpace(loadingSceneName))
        {
            LoadScene(gameSceneName);
            return;
        }

        SceneLoadRequest.SetTarget(gameSceneName);
        LoadScene(loadingSceneName);
    }

    private void StartDelayedAction(System.Action action)
    {
        if (pendingActionRoutine != null)
        {
            StopCoroutine(pendingActionRoutine);
        }

        pendingActionRoutine = StartCoroutine(RunDelayedAction(action));
    }

    private IEnumerator RunDelayedAction(System.Action action)
    {
        if (actionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(actionDelay);
        }

        pendingActionRoutine = null;
        action?.Invoke();
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        Debug.Log("Quit requested from Main Menu. Application.Quit is ignored in the Unity Editor.");
#else
        Application.Quit();
#endif
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
