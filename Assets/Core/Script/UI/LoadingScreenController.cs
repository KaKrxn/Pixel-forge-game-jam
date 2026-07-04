using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingScreenController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string fallbackSceneName = "GameScene";
    [SerializeField, Min(0f)] private float minimumLoadingTime = 0.75f;
    [SerializeField] private bool activateWhenReady = true;

    [Header("UI")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private Image progressRing;
    [SerializeField] private Slider progressSlider;

    [Header("Tips")]
    [SerializeField]
    private string[] tips =
    {
        "Tip: Stay quiet. Some things can hear you.",
        "Tip: The candle buys time, not safety.",
        "Tip: A calm patient is easier to save.",
        "Tip: Do not rush the blade.",
        "Tip: If the room goes dark, finish what is in your hand first."
    };

    private Coroutine loadingRoutine;

    private void Start()
    {
        StartLoading();
    }

    public void StartLoading()
    {
        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
        }

        string targetScene = SceneLoadRequest.ConsumeTarget(fallbackSceneName);
        loadingRoutine = StartCoroutine(LoadSceneRoutine(targetScene));
    }

    private IEnumerator LoadSceneRoutine(string targetScene)
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogWarning("LoadingScreenController cannot load a scene because no target or fallback scene is assigned.", this);
            SetProgress(1f);
            yield break;
        }

        Time.timeScale = 1f;
        ShowRandomTip();
        SetLoadingLabel();
        SetupProgressSlider();
        SetProgress(0f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        if (operation == null)
        {
            Debug.LogError($"LoadingScreenController could not start loading scene '{targetScene}'. Check Build Settings.", this);
            yield break;
        }

        operation.allowSceneActivation = false;
        float elapsed = 0f;

        while (operation.progress < 0.9f || elapsed < minimumLoadingTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            SetProgress(progress);
            yield return null;
        }

        SetProgress(1f);

        if (activateWhenReady)
        {
            operation.allowSceneActivation = true;
        }

        loadingRoutine = null;
    }

    private void SetProgress(float normalizedProgress)
    {
        float progress = Mathf.Clamp01(normalizedProgress);

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        if (progressRing != null)
        {
            progressRing.fillAmount = progress;
        }

        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(progress);
        }
    }

    private void SetupProgressSlider()
    {
        if (progressSlider == null)
        {
            return;
        }

        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
    }

    private void SetLoadingLabel()
    {
        if (loadingText != null && string.IsNullOrWhiteSpace(loadingText.text))
        {
            loadingText.text = "loading";
        }
    }

    private void ShowRandomTip()
    {
        if (tipText == null || tips == null || tips.Length == 0)
        {
            return;
        }

        int validCount = 0;
        for (int i = 0; i < tips.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tips[i]))
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return;
        }

        int selected = Random.Range(0, validCount);
        for (int i = 0; i < tips.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(tips[i]))
            {
                continue;
            }

            if (selected == 0)
            {
                tipText.text = tips[i];
                return;
            }

            selected--;
        }
    }
}
