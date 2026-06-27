using UnityEngine;
using UnityEngine.UI;

public sealed class Dialog : MonoBehaviour
{
    [SerializeField] private GameFlow flow;
    [SerializeField] private DialogData dialogData;
    [SerializeField] private GameObject root;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Button nextButton;

    private int lineIndex;

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

        if (dialogData == null || dialogData.Count == 0)
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
        lineIndex++;

        if (dialogData == null || lineIndex >= dialogData.Count)
        {
            Complete();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (dialogData == null || !dialogData.TryGetLine(lineIndex, out DialogLine line))
        {
            Complete();
            return;
        }

        if (speakerText != null)
        {
            speakerText.text = line.Speaker.ToString();
        }

        if (bodyText != null)
        {
            bodyText.text = line.Text;
        }
    }

    private void Complete()
    {
        Hide();

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

        speakerText = CreateText(rootObject.transform, "SpeakerText", new Vector2(0.04f, 0.68f), new Vector2(0.56f, 0.92f), 32, new Color(1f, 0.86f, 0.42f, 1f), TextAnchor.MiddleLeft);
        bodyText = CreateText(rootObject.transform, "BodyText", new Vector2(0.04f, 0.17f), new Vector2(0.78f, 0.68f), 26, Color.white, TextAnchor.UpperLeft);

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

        Text nextText = CreateText(buttonObject.transform, "Text", Vector2.zero, Vector2.one, 24, Color.black, TextAnchor.MiddleCenter);
        nextText.text = "Next";
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }
}
