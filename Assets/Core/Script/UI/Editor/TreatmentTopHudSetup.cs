using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TreatmentTopHudSetup
{
    [MenuItem("Tools/Pixel Forge/Create Treatment Top HUD")]
    public static void CreateTreatmentTopHudShortcut()
    {
        CreateTreatmentTopHudInOpenScene();
    }

    [MenuItem("GameObject/UI/Treatment Top HUD", false, 10)]
    public static void CreateTreatmentTopHudFromGameObjectMenu()
    {
        CreateTreatmentTopHudInOpenScene();
    }

    [MenuItem("Tools/Pixel Forge/Treatment/Create Treatment Top HUD In Open Scene")]
    public static void CreateTreatmentTopHudInOpenScene()
    {
        Canvas canvas = FindMainCanvas();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        TreatmentTopHud existingHud = FindSceneObject<TreatmentTopHud>();
        if (existingHud != null)
        {
            existingHud.transform.SetParent(canvas.transform, false);
            ConfigureRootRect(existingHud.GetComponent<RectTransform>());
            EnsureCharacterVisualSlot(existingHud);
            BindTreatment(existingHud);
            existingHud.gameObject.SetActive(false);
            Selection.activeObject = existingHud.gameObject;
            Debug.Log($"TreatmentTopHud already exists. Reparented to main Canvas and repositioned: {existingHud.name}", existingHud);
            return;
        }

        GameObject root = CreateRect("TreatmentTopHUD", canvas.transform);
        ConfigureRootRect(root.GetComponent<RectTransform>());

        TreatmentTopHud hud = Undo.AddComponent<TreatmentTopHud>(root);
        Image rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0f);
        rootImage.raycastTarget = false;

        Image patientPanel = CreatePanel("PatientPanel", root.transform, new Color(0.06f, 0.06f, 0.07f, 0.88f));
        RectTransform patientRect = patientPanel.rectTransform;
        patientRect.anchorMin = new Vector2(0f, 0.5f);
        patientRect.anchorMax = new Vector2(0f, 0.5f);
        patientRect.pivot = new Vector2(0f, 0.5f);
        patientRect.anchoredPosition = Vector2.zero;
        patientRect.sizeDelta = new Vector2(224f, 104f);

        TMP_Text patientLabel = CreateText("PatientLabel", patientPanel.transform, "PATIENT", 8, FontStyles.UpperCase);
        RectTransform patientLabelRect = patientLabel.rectTransform;
        patientLabelRect.anchorMin = new Vector2(0f, 1f);
        patientLabelRect.anchorMax = new Vector2(0f, 1f);
        patientLabelRect.pivot = new Vector2(0f, 1f);
        patientLabelRect.anchoredPosition = new Vector2(14f, -10f);
        patientLabelRect.sizeDelta = new Vector2(184f, 16f);

        TMP_Text patientName = CreateText("PatientName", patientPanel.transform, "Name", 16, FontStyles.Normal);
        RectTransform patientNameRect = patientName.rectTransform;
        patientNameRect.anchorMin = new Vector2(0f, 1f);
        patientNameRect.anchorMax = new Vector2(1f, 1f);
        patientNameRect.pivot = new Vector2(0f, 1f);
        patientNameRect.anchoredPosition = new Vector2(14f, -26f);
        patientNameRect.sizeDelta = new Vector2(-28f, 40.7698f);

        Slider sanitySlider = CreateSlider("SanityBar", patientPanel.transform, out Image sanityFill);
        RectTransform sanityRect = sanitySlider.GetComponent<RectTransform>();
        sanityRect.anchorMin = new Vector2(0f, 0f);
        sanityRect.anchorMax = new Vector2(1f, 0f);
        sanityRect.pivot = new Vector2(0f, 0f);
        sanityRect.anchoredPosition = new Vector2(14f, 12f);
        sanityRect.sizeDelta = new Vector2(-28f, 14f);

        TMP_Text sanityValue = CreateText("SanityValue", patientPanel.transform, "0 / 100", 10, FontStyles.Bold);
        RectTransform sanityValueRect = sanityValue.rectTransform;
        sanityValueRect.anchorMin = new Vector2(0f, 0f);
        sanityValueRect.anchorMax = new Vector2(1f, 0f);
        sanityValueRect.pivot = new Vector2(0f, 0f);
        sanityValueRect.anchoredPosition = new Vector2(50f, 11f);
        sanityValueRect.sizeDelta = new Vector2(-58f, 16f);
        sanityValue.alignment = TextAlignmentOptions.Center;

        Image characterVisualPanel = CreatePanel("CharacterVisualPanel", root.transform, new Color(0.03f, 0.03f, 0.035f, 0.82f));
        RectTransform visualPanelRect = characterVisualPanel.rectTransform;
        visualPanelRect.anchorMin = new Vector2(0f, 1f);
        visualPanelRect.anchorMax = new Vector2(0f, 1f);
        visualPanelRect.pivot = new Vector2(0f, 1f);
        visualPanelRect.anchoredPosition = new Vector2(0f, -102f);
        visualPanelRect.sizeDelta = new Vector2(224f, 136f);

        Image characterVisual = CreatePanel("CharacterVisualImage", characterVisualPanel.transform, new Color(1f, 1f, 1f, 0f));
        RectTransform visualImageRect = characterVisual.rectTransform;
        visualImageRect.anchorMin = Vector2.zero;
        visualImageRect.anchorMax = Vector2.one;
        visualImageRect.pivot = new Vector2(0.5f, 0.5f);
        visualImageRect.offsetMin = new Vector2(8f, 8f);
        visualImageRect.offsetMax = new Vector2(-8f, -8f);
        characterVisual.preserveAspect = true;
        characterVisual.enabled = false;
        characterVisualPanel.gameObject.SetActive(false);

        Image dialogPanel = CreatePanel("TreatmentDialogPanel", root.transform, new Color(0.06f, 0.06f, 0.07f, 0.9f));
        RectTransform dialogRect = dialogPanel.rectTransform;
        dialogRect.anchorMin = new Vector2(0f, 0.5f);
        dialogRect.anchorMax = new Vector2(1f, 0.5f);
        dialogRect.pivot = new Vector2(0f, 0.5f);
        dialogRect.anchoredPosition = new Vector2(236f, 0f);
        dialogRect.sizeDelta = new Vector2(-654.093f, 104f);

        TMP_Text speaker = CreateText("SpeakerText", dialogPanel.transform, "PATIENT", 9, FontStyles.UpperCase);
        RectTransform speakerRect = speaker.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.anchoredPosition = new Vector2(14f, -8f);
        speakerRect.sizeDelta = new Vector2(-28f, 16f);

        TMP_Text line = CreateText("LineText", dialogPanel.transform, "Please be careful... it really stings.", 13, FontStyles.Italic);
        RectTransform lineRect = line.rectTransform;
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = new Vector2(14f, 1.5f);
        lineRect.sizeDelta = new Vector2(-28f, -3f);

        BindHud(hud, root, characterVisualPanel.gameObject, characterVisual, patientName, sanitySlider, sanityValue, sanityFill, speaker, line);
        BindTreatment(hud);
        root.SetActive(false);

        Selection.activeObject = root;
        EditorUtility.SetDirty(root);
        Debug.Log("Created TreatmentTopHUD. Assign an art portrait/fallback sprite later if the auto sprite is not good enough.", root);
    }

    private static void EnsureCharacterVisualSlot(TreatmentTopHud hud)
    {
        if (hud == null)
        {
            return;
        }

        Transform root = hud.transform;
        Transform panelTransform = root.Find("CharacterVisualPanel");
        Image panel = panelTransform != null ? panelTransform.GetComponent<Image>() : null;
        if (panel == null)
        {
            panel = CreatePanel("CharacterVisualPanel", root, new Color(0.03f, 0.03f, 0.035f, 0.82f));
        }

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -102f);
        panelRect.sizeDelta = new Vector2(224f, 136f);

        Transform imageTransform = panel.transform.Find("CharacterVisualImage");
        Image visualImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
        if (visualImage == null)
        {
            visualImage = CreatePanel("CharacterVisualImage", panel.transform, new Color(1f, 1f, 1f, 0f));
        }

        RectTransform imageRect = visualImage.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.offsetMin = new Vector2(8f, 8f);
        imageRect.offsetMax = new Vector2(-8f, -8f);
        visualImage.preserveAspect = true;
        visualImage.raycastTarget = false;
        visualImage.enabled = visualImage.sprite != null;
        panel.gameObject.SetActive(false);

        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("characterVisualRoot").objectReferenceValue = panel.gameObject;
        serializedHud.FindProperty("characterVisualImage").objectReferenceValue = visualImage;
        serializedHud.ApplyModifiedProperties();
        EditorUtility.SetDirty(hud);
    }

    private static void ConfigureRootRect(RectTransform rootRect)
    {
        if (rootRect == null)
        {
            return;
        }

        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(140f, -12f);
        rootRect.sizeDelta = new Vector2(-304f, 248f);
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Image CreatePanel(string name, Transform parent, Color color)
    {
        GameObject gameObject = CreateRect(name, parent);
        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, FontStyles style)
    {
        GameObject gameObject = CreateRect(name, parent);
        TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = new Color(0.9f, 0.88f, 0.82f, 1f);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.alignment = TextAlignmentOptions.Left;
        return label;
    }

    private static Slider CreateSlider(string name, Transform parent, out Image fillImage)
    {
        GameObject sliderObject = CreateRect(name, parent);
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        Image background = CreatePanel("Background", sliderObject.transform, new Color(0.18f, 0.16f, 0.17f, 1f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillArea = CreateRect("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        fillImage = CreatePanel("Fill", fillArea.transform, new Color(0.65f, 0.12f, 0.16f, 1f));
        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        return slider;
    }

    private static void BindHud(
        TreatmentTopHud hud,
        GameObject root,
        GameObject characterVisualRoot,
        Image characterVisualImage,
        TMP_Text patientName,
        Slider sanitySlider,
        TMP_Text sanityValue,
        Image sanityFill,
        TMP_Text speaker,
        TMP_Text line)
    {
        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("root").objectReferenceValue = root;
        serializedHud.FindProperty("characterVisualRoot").objectReferenceValue = characterVisualRoot;
        serializedHud.FindProperty("characterVisualImage").objectReferenceValue = characterVisualImage;
        serializedHud.FindProperty("patientNameText").objectReferenceValue = patientName;
        serializedHud.FindProperty("sanitySlider").objectReferenceValue = sanitySlider;
        serializedHud.FindProperty("sanityValueText").objectReferenceValue = sanityValue;
        serializedHud.FindProperty("sanityFillImage").objectReferenceValue = sanityFill;
        serializedHud.FindProperty("speakerText").objectReferenceValue = speaker;
        serializedHud.FindProperty("lineText").objectReferenceValue = line;
        serializedHud.ApplyModifiedProperties();
    }

    private static void BindTreatment(TreatmentTopHud hud)
    {
        Treatment treatment = FindSceneObject<Treatment>();
        if (treatment == null)
        {
            return;
        }

        SerializedObject serializedTreatment = new SerializedObject(treatment);
        SerializedProperty topHudProperty = serializedTreatment.FindProperty("topHud");
        if (topHudProperty != null)
        {
            topHudProperty.objectReferenceValue = hud;
            serializedTreatment.ApplyModifiedProperties();
            EditorUtility.SetDirty(treatment);
        }
    }

    private static Canvas FindMainCanvas()
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Canvas fallback = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.gameObject == null)
            {
                continue;
            }

            if (EditorUtility.IsPersistent(canvas.gameObject) || !canvas.gameObject.scene.IsValid())
            {
                continue;
            }

            string canvasName = canvas.name;
            bool isUtilityCanvas =
                canvasName.Contains("Mini") ||
                canvasName.Contains("Overlay") ||
                canvasName.Contains("Pause") ||
                canvasName.Contains("Loading") ||
                canvasName.Contains("Dialog");

            if (canvasName == "Canvas")
            {
                return canvas;
            }

            if (!isUtilityCanvas && fallback == null)
            {
                fallback = canvas;
            }
        }

        return fallback;
    }

    private static T FindSceneObject<T>() where T : Object
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in objects)
        {
            if (obj == null)
            {
                continue;
            }

            GameObject gameObject = null;
            if (obj is Component component)
            {
                gameObject = component.gameObject;
            }
            else if (obj is GameObject directGameObject)
            {
                gameObject = directGameObject;
            }

            if (gameObject == null)
            {
                continue;
            }

            if (EditorUtility.IsPersistent(gameObject))
            {
                continue;
            }

            if (!gameObject.scene.IsValid())
            {
                continue;
            }

            return obj;
        }

        return null;
    }
}
