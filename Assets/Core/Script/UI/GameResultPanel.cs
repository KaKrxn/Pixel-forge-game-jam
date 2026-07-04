using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GameResultPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Copy")]
    [SerializeField] private string winTitle = "Cure Complete";
    [SerializeField] private string winDescription = "Every patient survived the night.";
    [SerializeField] private string winTip = "The candle still burns.";
    [SerializeField] private string loseTitle = "Treatment Failed";
    [SerializeField] private string sanityLoseDescription = "The parasite took over the patient.";
    [SerializeField] private string sanityLoseTip = "Keep Sanity low by treating carefully and maintaining the candle.";

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartScene);
            restartButton.onClick.AddListener(RestartScene);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(LoadMainMenu);
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        }

        Hide();
    }

    public void Hide()
    {
        ResolveRoot();

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public void ShowWin()
    {
        Show(GameResultType.Win, FailReason.None);
    }

    public void ShowLose(FailReason reason)
    {
        Show(GameResultType.Lose, reason);
    }

    public void Show(GameResultType resultType, FailReason failReason)
    {
        ResolveRoot();

        if (titleText != null)
        {
            titleText.text = resultType == GameResultType.Win ? winTitle : loseTitle;
        }

        if (descriptionText != null)
        {
            descriptionText.text = resultType == GameResultType.Win
                ? winDescription
                : GetLoseDescription(failReason);
        }

        if (tipText != null)
        {
            tipText.text = resultType == GameResultType.Win
                ? winTip
                : GetLoseTip(failReason);
        }

        if (root != null)
        {
            root.SetActive(true);
        }
    }

    private string GetLoseDescription(FailReason reason)
    {
        switch (reason)
        {
            case FailReason.SanityMaxed:
            default:
                return sanityLoseDescription;
        }
    }

    private string GetLoseTip(FailReason reason)
    {
        switch (reason)
        {
            case FailReason.SanityMaxed:
            default:
                return sanityLoseTip;
        }
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void LoadMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogWarning("GameResultPanel cannot load main menu because no scene name is assigned.", this);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ResolveRoot()
    {
        if (root == null)
        {
            root = gameObject;
        }
    }
}
