using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class Dialog : MonoBehaviour
{
    [SerializeField] private GameFlow flow;
    [SerializeField] private DialogData dialogData;
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button nextButton;

    [Header("Typewriter")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Text Blip SFX")]
    [SerializeField] private AudioSource dialogAudioSource;
    [SerializeField] private AudioClip playerBlipClip;
    [SerializeField] private AudioClip customerBlipClip;
    [SerializeField, Range(0f, 1f)] private float blipVolume = 0.45f;
    [SerializeField, Min(1)] private int blipEveryVisibleCharacters = 2;
    [SerializeField, Min(0f)] private float minimumBlipInterval = 0.025f;
    [SerializeField] private bool stopBlipsWhenLineCompletes = true;

    private int lineIndex;
    private Coroutine typewriterRoutine;
    private DialogSpeaker currentSpeaker;
    private bool isTyping;
    private int visibleCharactersSinceLastBlip;
    private float lastBlipTime;
    private AudioClip lastCustomerBlipClip;
    private AudioClip currentLineBlipClip;
    private DialogData activeDialogData;

    private void Awake()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(Advance);
        }

        Hide();
    }

    public void Open(CustomerAgent customer)
    {
        lineIndex = 0;
        lastCustomerBlipClip = null;
        currentLineBlipClip = null;
        activeDialogData = ResolveDialogData(customer);
        StopTypewriter(resetTextVisibility: true);

        if (activeDialogData == null || activeDialogData.Count == 0)
        {
            Complete();
            return;
        }

        EnsureRuntimeUi();

        if (root != null)
        {
            root.SetActive(true);
        }

        ShowCurrentLine();
    }

    public void Advance()
    {
        if (isTyping)
        {
            return;
        }

        lineIndex++;

        if (activeDialogData == null || lineIndex >= activeDialogData.Count)
        {
            Complete();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (activeDialogData == null || !activeDialogData.TryGetLine(lineIndex, out DialogLine line))
        {
            Complete();
            return;
        }

        if (speakerText != null)
        {
            speakerText.text = activeDialogData.GetDisplayName(line.Speaker);
        }

        currentSpeaker = line.Speaker;
        SelectLineBlipClip();
        StartTypewriter(line.Text);
    }

    private void Complete()
    {
        StopTypewriter(resetTextVisibility: true);
        Hide();
        activeDialogData = null;

        if (flow != null)
        {
            flow.CompleteDialog();
        }
    }

    private void EnsureRuntimeUi()
    {
        if (root != null && speakerText != null && bodyText != null && nextButton != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject rootObject = new GameObject("DialogRoot", typeof(RectTransform), typeof(Image));
        rootObject.transform.SetParent(canvas.transform, false);
        root = rootObject;

        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.08f, 0.04f);
        rootRect.anchorMax = new Vector2(0.92f, 0.33f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image panel = rootObject.GetComponent<Image>();
        panel.color = new Color(0.05f, 0.045f, 0.04f, 0.9f);

        speakerText = CreateText(rootObject.transform, "SpeakerText", new Vector2(0.04f, 0.68f), new Vector2(0.56f, 0.92f), 32, new Color(1f, 0.86f, 0.42f, 1f), TextAlignmentOptions.MidlineLeft);
        bodyText = CreateText(rootObject.transform, "BodyText", new Vector2(0.04f, 0.17f), new Vector2(0.78f, 0.68f), 26, Color.white, TextAlignmentOptions.TopLeft);

        GameObject buttonObject = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(rootObject.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.8f, 0.15f);
        buttonRect.anchorMax = new Vector2(0.96f, 0.42f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.95f, 0.72f, 0.26f, 1f);

        nextButton = buttonObject.GetComponent<Button>();
        nextButton.targetGraphic = buttonImage;
        nextButton.onClick.AddListener(Advance);

        TMP_Text nextText = CreateText(buttonObject.transform, "Text", Vector2.zero, Vector2.one, 24, Color.black, TextAlignmentOptions.Center);
        nextText.text = "Next";
    }

    private void StartTypewriter(string text)
    {
        StopTypewriter(resetTextVisibility: true);
        SetNextButtonVisible(false);

        if (bodyText == null)
        {
            FinishTypewriter();
            return;
        }

        bodyText.text = text ?? string.Empty;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        int characterCount = bodyText.textInfo.characterCount;
        if (characterCount == 0 || charactersPerSecond <= 0f)
        {
            bodyText.maxVisibleCharacters = characterCount;
            FinishTypewriter();
            return;
        }

        isTyping = true;
        visibleCharactersSinceLastBlip = 0;
        lastBlipTime = -minimumBlipInterval;
        typewriterRoutine = StartCoroutine(RunTypewriter(characterCount));
    }

    private IEnumerator RunTypewriter(int characterCount)
    {
        int visibleCharacters = 0;
        float characterInterval = 1f / charactersPerSecond;
        float revealAccumulator = 0f;

        while (visibleCharacters < characterCount)
        {
            revealAccumulator += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            int charactersToReveal = Mathf.FloorToInt(revealAccumulator / characterInterval);

            if (charactersToReveal <= 0)
            {
                yield return null;
                continue;
            }

            revealAccumulator -= charactersToReveal * characterInterval;

            for (int i = 0; i < charactersToReveal && visibleCharacters < characterCount; i++)
            {
                visibleCharacters++;
                bodyText.maxVisibleCharacters = visibleCharacters;
                TryPlayBlipForCharacter(visibleCharacters - 1);
            }

            yield return null;
        }

        FinishTypewriter();
    }

    private void TryPlayBlipForCharacter(int characterIndex)
    {
        if (bodyText == null || dialogAudioSource == null)
        {
            return;
        }

        TMP_TextInfo textInfo = bodyText.textInfo;
        if (characterIndex < 0 || characterIndex >= textInfo.characterCount)
        {
            return;
        }

        char character = textInfo.characterInfo[characterIndex].character;
        if (char.IsWhiteSpace(character))
        {
            return;
        }

        visibleCharactersSinceLastBlip++;
        if (visibleCharactersSinceLastBlip < blipEveryVisibleCharacters)
        {
            return;
        }

        float currentTime = useUnscaledTime ? Time.unscaledTime : Time.time;
        if (currentTime - lastBlipTime < minimumBlipInterval)
        {
            return;
        }

        if (currentLineBlipClip == null)
        {
            return;
        }

        visibleCharactersSinceLastBlip = 0;
        lastBlipTime = currentTime;
        dialogAudioSource.PlayOneShot(currentLineBlipClip, blipVolume);
    }

    private void SelectLineBlipClip()
    {
        currentLineBlipClip = activeDialogData != null
            ? activeDialogData.GetBlipClip(currentSpeaker, playerBlipClip, customerBlipClip, lastCustomerBlipClip)
            : (currentSpeaker == DialogSpeaker.Player ? playerBlipClip : customerBlipClip);

        if (currentSpeaker == DialogSpeaker.Customer && currentLineBlipClip != null)
        {
            lastCustomerBlipClip = currentLineBlipClip;
        }
    }

    private DialogData ResolveDialogData(CustomerAgent customer)
    {
        CustomerCaseProvider caseProvider = customer != null ? customer.GetComponent<CustomerCaseProvider>() : null;
        if (caseProvider != null && caseProvider.CaseData != null && caseProvider.CaseData.DialogData != null)
        {
            return caseProvider.CaseData.DialogData;
        }

        return dialogData;
    }

    private void FinishTypewriter()
    {
        isTyping = false;
        typewriterRoutine = null;
        StopDialogBlips();
        SetNextButtonVisible(true);
    }

    private void StopTypewriter(bool resetTextVisibility)
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }

        isTyping = false;
        StopDialogBlips();

        if (resetTextVisibility && bodyText != null)
        {
            bodyText.maxVisibleCharacters = int.MaxValue;
        }
    }

    private void StopDialogBlips()
    {
        if (stopBlipsWhenLineCompletes && dialogAudioSource != null)
        {
            dialogAudioSource.Stop();
        }
    }

    private void SetNextButtonVisible(bool visible)
    {
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(visible);
        }
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;

        return text;
    }

    private void Hide()
    {
        StopTypewriter(resetTextVisibility: true);
        SetNextButtonVisible(false);

        if (root != null)
        {
            root.SetActive(false);
        }
    }
}
