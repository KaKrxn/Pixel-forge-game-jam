using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class ClinicAdditiveStatusSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RequestPath = "Assets/Core/Scene/ClinicAdditiveStatusSetup.run";

    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            File.Delete(RequestPath);
            if (File.Exists(RequestPath + ".meta"))
            {
                File.Delete(RequestPath + ".meta");
            }

            AssetDatabase.Refresh();
            Run();
        };
    }

    [MenuItem("Pixel Forge/Setup/Run Additive Status Setup")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject gameFlowObject = FindSceneObject("GameFlowManager");
        GameObject candleObject = FindSceneObject("Candle");
        GameObject customerObject = FindSceneObject("Customer");
        GameObject canvasObject = FindSceneObject("DialogCanvas") ?? FindSceneObject("Canvas");

        if (gameFlowObject == null || candleObject == null || customerObject == null || canvasObject == null)
        {
            Debug.LogWarning("Additive status setup skipped because GameFlowManager, Candle, Customer, or Canvas was not found.");
            return;
        }

        GameFlow gameFlow = GetOrAdd<GameFlow>(gameFlowObject);
        Candle candle = GetOrAdd<Candle>(candleObject);
        Sanity sanity = GetOrAdd<Sanity>(customerObject);
        BasicStatusHud hud = SetupHud(canvasObject, candle, sanity);
        ConvertExistingCoreText(canvasObject, gameFlowObject, customerObject);

        AssignObject(gameFlow, "candle", candle);
        AssignObject(sanity, "candle", candle);
        AssignFloat(candle, "maxLight", 100f);
        AssignFloat(candle, "currentLight", 100f);
        AssignFloat(candle, "drainRate", 2f);
        AssignFloat(candle, "refillRate", 8f);
        AssignFloat(candle, "clickRefillAmount", 3f);
        AssignInt(candle, "clickRefillCapPerSecond", 8);
        AssignFloat(candle, "lowLightThreshold", 60f);
        AssignFloat(candle, "flickeringThreshold", 25f);
        AssignBool(candle, "drainOnPlay", true);

        AssignFloat(sanity, "maxSanity", 100f);
        AssignFloat(sanity, "currentSanity", 0f);
        AssignFloat(sanity, "candleProtectedIncreaseRate", 0.35f);
        AssignFloat(sanity, "candleOutIncreaseRate", 5f);
        AssignBool(sanity, "useCandleTierMultipliers", true);
        AssignFloat(sanity, "lowLightSanityMultiplier", 1.5f);
        AssignFloat(sanity, "flickeringSanityMultiplier", 2.5f);
        AssignFloat(sanity, "extinguishedSanityMultiplier", 4f);
        AssignFloat(sanity, "treatmentStressIncreaseRate", 1.5f);
        AssignFloat(sanity, "warningThreshold", 50f);
        AssignFloat(sanity, "criticalThreshold", 80f);
        AssignBool(sanity, "monitoring", false);

        AssignObject(hud, "candle", candle);
        AssignObject(hud, "sanity", sanity);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Additive Candle/Sanity setup complete. Existing scene layout and sprites were preserved.");
    }

    private static BasicStatusHud SetupHud(GameObject canvasObject, Candle candle, Sanity sanity)
    {
        GameObject root = FindChild(canvasObject.transform, "BasicStatusHud") ?? new GameObject("BasicStatusHud", typeof(RectTransform));
        root.transform.SetParent(canvasObject.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.02f, 0.82f);
        rootRect.anchorMax = new Vector2(0.34f, 0.98f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        BasicStatusHud hud = GetOrAdd<BasicStatusHud>(root);

        Slider candleSlider = SetupSlider(root.transform, "CandleMeter", new Vector2(0f, 0.54f), new Vector2(1f, 0.92f), new Color(1f, 0.78f, 0.25f, 1f));
        Slider sanitySlider = SetupSlider(root.transform, "SanityMeter", new Vector2(0f, 0.06f), new Vector2(1f, 0.44f), new Color(0.82f, 0.2f, 0.38f, 1f));
        TMP_Text candleLabel = SetupLabel(root.transform, "CandleLabel", "Candle: Bright", new Vector2(0f, 0.78f), new Vector2(1f, 1f));
        TMP_Text sanityLabel = SetupLabel(root.transform, "SanityLabel", "Sanity: Stable", new Vector2(0f, 0.3f), new Vector2(1f, 0.52f));

        AssignObject(hud, "candle", candle);
        AssignObject(hud, "sanity", sanity);
        AssignObject(hud, "candleSlider", candleSlider);
        AssignObject(hud, "sanitySlider", sanitySlider);
        AssignObject(hud, "candleLabel", candleLabel);
        AssignObject(hud, "sanityLabel", sanityLabel);

        return hud;
    }

    private static Slider SetupSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color fillColor)
    {
        GameObject sliderObject = FindChild(parent, name) ?? new GameObject(name, typeof(RectTransform));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = anchorMin;
        sliderRect.anchorMax = anchorMax;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        Slider slider = GetOrAdd<Slider>(sliderObject);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.transition = Selectable.Transition.None;

        GameObject background = FindChild(sliderObject.transform, "Background") ?? new GameObject("Background", typeof(RectTransform));
        background.transform.SetParent(sliderObject.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        Stretch(backgroundRect);
        Image backgroundImage = GetOrAdd<Image>(background);
        backgroundImage.color = new Color(0.05f, 0.045f, 0.04f, 0.86f);

        GameObject fillArea = FindChild(sliderObject.transform, "Fill Area") ?? new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fill = FindChild(fillArea.transform, "Fill") ?? new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);
        Image fillImage = GetOrAdd<Image>(fill);
        fillImage.color = fillColor;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private static TMP_Text SetupLabel(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject labelObject = FindChild(parent, name) ?? new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = anchorMin;
        labelRect.anchorMax = anchorMax;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text label = GetOrAddTmpText(labelObject);
        label.text = text;
        label.fontSize = 18;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }

    private static void ConvertExistingCoreText(GameObject canvasObject, GameObject gameFlowObject, GameObject customerObject)
    {
        TMP_Text dialogSpeaker = ConvertExistingText(canvasObject.transform, "DialogRoot/SpeakerText", "Customer", 32, new Color(1f, 0.86f, 0.42f, 1f), TextAlignmentOptions.MidlineLeft);
        TMP_Text dialogBody = ConvertExistingText(canvasObject.transform, "DialogRoot/BodyText", string.Empty, 26, Color.white, TextAlignmentOptions.TopLeft);
        ConvertExistingText(canvasObject.transform, "DialogRoot/NextButton/Text", "Next", 24, Color.black, TextAlignmentOptions.Center);
        ConvertExistingText(customerObject.transform, "Bubble/BubbleButton/Label", "...", 42, Color.black, TextAlignmentOptions.Center);

        TMP_Text treatmentTitle = ConvertExistingText(canvasObject.transform, "TreatmentRoot/TitleText", "Treatment Room", 36, new Color(1f, 0.82f, 0.36f, 1f), TextAlignmentOptions.Center);
        TMP_Text treatmentBody = ConvertExistingText(canvasObject.transform, "TreatmentRoot/BodyText", "Placeholder treatment state.", 26, Color.white, TextAlignmentOptions.Top);
        ConvertExistingText(canvasObject.transform, "TreatmentRoot/ReturnCounterButton/Text", "Return Counter", 22, Color.black, TextAlignmentOptions.Center);
        ConvertExistingText(canvasObject.transform, "TreatmentRoot/CompleteTreatmentButton/Text", "Complete Test", 22, Color.black, TextAlignmentOptions.Center);
        ConvertExistingText(canvasObject.transform, "TreatmentRoot/ResumeTreatmentButton/Text", "Resume", 22, Color.black, TextAlignmentOptions.Center);

        Dialog dialog = gameFlowObject.GetComponent<Dialog>();
        if (dialog != null)
        {
            AssignObject(dialog, "speakerText", dialogSpeaker);
            AssignObject(dialog, "bodyText", dialogBody);
        }

        Treatment treatment = gameFlowObject.GetComponent<Treatment>();
        if (treatment != null)
        {
            AssignObject(treatment, "titleText", treatmentTitle);
            AssignObject(treatment, "bodyText", treatmentBody);
        }
    }

    private static TMP_Text ConvertExistingText(Transform root, string path, string fallbackText, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        Transform textTransform = root.Find(path);
        if (textTransform == null)
        {
            return null;
        }

        TMP_Text text = GetOrAddTmpText(textTransform.gameObject);
        if (string.IsNullOrEmpty(text.text))
        {
            text.text = fallbackText;
        }

        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static GameObject FindSceneObject(string name)
    {
        GameObject activeFound = GameObject.Find(name);
        if (activeFound != null)
        {
            return activeFound;
        }

        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject sceneObject in objects)
        {
            if (sceneObject.name == name && sceneObject.scene.IsValid())
            {
                return sceneObject;
            }
        }

        return null;
    }

    private static GameObject FindChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static TMP_Text GetOrAddTmpText(GameObject gameObject)
    {
        Text legacyText = gameObject.GetComponent<Text>();
        string preservedText = legacyText != null ? legacyText.text : null;
        if (legacyText != null)
        {
            Object.DestroyImmediate(legacyText);
        }

        TextMeshProUGUI tmpText = gameObject.GetComponent<TextMeshProUGUI>();
        if (tmpText == null)
        {
            tmpText = gameObject.AddComponent<TextMeshProUGUI>();
        }

        if (!string.IsNullOrEmpty(preservedText) && string.IsNullOrEmpty(tmpText.text))
        {
            tmpText.text = preservedText;
        }

        return tmpText;
    }

    private static void AssignObject(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }

    private static void AssignBool(Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }

    private static void AssignFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }

    private static void AssignInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
}
