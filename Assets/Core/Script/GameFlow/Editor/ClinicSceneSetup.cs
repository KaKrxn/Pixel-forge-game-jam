using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ClinicSceneSetup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string DialogDataPath = "Assets/Core/Data/Dialog/TestCustomerDialog.asset";
    private const string RequestPath = "Assets/Core/Scene/ClinicSceneSetup.run";

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

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject gameFlowObject = FindOrCreate("GameFlow");
        GameObject customerObject = FindOrCreate("Customer");
        GameObject spawnPoint = FindOrCreate("CustomerSpawnPoint");
        GameObject outsideDoorPoint = FindOrCreate("OutsideDoorPoint");
        GameObject insideDoorPoint = FindOrCreate("InsideDoorPoint");
        GameObject counterPoint = FindOrCreate("CounterPoint");
        GameObject exitPoint = FindOrCreate("CustomerExitPoint");
        GameObject doorObject = FindOrCreate("Door");

        spawnPoint.transform.position = new Vector3(-5.93f, -2f, 0f);
        outsideDoorPoint.transform.position = new Vector3(-1.2f, -2f, 0f);
        insideDoorPoint.transform.position = new Vector3(0.15f, -2f, 0f);
        counterPoint.transform.position = new Vector3(5.37f, -2f, 0f);
        exitPoint.transform.position = new Vector3(-6.6f, -2f, 0f);
        doorObject.transform.position = outsideDoorPoint.transform.position;
        customerObject.transform.position = spawnPoint.transform.position;

        GameFlow gameFlow = GetOrAdd<GameFlow>(gameFlowObject);
        Dialog dialog = GetOrAdd<Dialog>(gameFlowObject);
        CustomerAgent customer = GetOrAdd<CustomerAgent>(customerObject);
        CustomerLayer customerLayer = GetOrAdd<CustomerLayer>(customerObject);
        Door door = GetOrAdd<Door>(doorObject);

        SpriteRenderer customerRenderer = GetOrAdd<SpriteRenderer>(customerObject);
        customerRenderer.sprite = FindFirstCoreSprite();
        customerRenderer.color = new Color(0.82f, 0.92f, 1f, 1f);
        customerRenderer.sortingLayerName = "Wall";
        customerRenderer.sortingOrder = -10;

        AssignObject(customerLayer, "targetRenderer", customerRenderer);
        AssignString(customerLayer, "outsideSortingLayer", "Wall");
        AssignInt(customerLayer, "outsideSortingOrder", -10);
        AssignString(customerLayer, "insideSortingLayer", "Customer");
        AssignInt(customerLayer, "insideSortingOrder", 0);

        GameObject bubbleObject = SetupBubble(customerObject);
        Bubble bubble = bubbleObject.GetComponent<Bubble>();

        GameObject canvasObject = FindOrCreateCanvas();
        DialogUi dialogUi = SetupDialogUi(canvasObject);

        DialogData dialogData = LoadOrCreateDialogData();

        AssignObject(gameFlow, "firstCustomer", customer);
        AssignObject(gameFlow, "dialog", dialog);
        AssignBool(gameFlow, "startOnPlay", true);

        AssignObject(customer, "spawnPoint", spawnPoint.transform);
        AssignObject(customer, "outsideDoorPoint", outsideDoorPoint.transform);
        AssignObject(customer, "insideDoorPoint", insideDoorPoint.transform);
        AssignObject(customer, "counterPoint", counterPoint.transform);
        AssignObject(customer, "exitPoint", exitPoint.transform);
        AssignObject(customer, "bubble", bubble);
        AssignObject(customer, "customerLayer", customerLayer);
        AssignObject(customer, "door", door);
        AssignFloat(customer, "moveSpeed", 2f);
        AssignFloat(customer, "stopDistance", 0.05f);

        AssignObject(dialog, "flow", gameFlow);
        AssignObject(dialog, "dialogData", dialogData);
        AssignObject(dialog, "root", dialogUi.Root);
        AssignObject(dialog, "speakerText", dialogUi.SpeakerText);
        AssignObject(dialog, "bodyText", dialogUi.BodyText);
        AssignObject(dialog, "nextButton", dialogUi.NextButton);

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Clinic scene setup complete.");
    }

    private static GameObject SetupBubble(GameObject customerObject)
    {
        GameObject bubbleObject = FindChild(customerObject.transform, "Bubble") ?? new GameObject("Bubble", typeof(RectTransform));
        bubbleObject.transform.SetParent(customerObject.transform, false);
        bubbleObject.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        bubbleObject.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = GetOrAdd<Canvas>(bubbleObject);
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 20;

        GetOrAdd<GraphicRaycaster>(bubbleObject);
        Bubble bubble = GetOrAdd<Bubble>(bubbleObject);

        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.sizeDelta = new Vector2(140f, 90f);

        GameObject buttonObject = FindChild(bubbleObject.transform, "BubbleButton") ?? new GameObject("BubbleButton", typeof(RectTransform));
        buttonObject.transform.SetParent(bubbleObject.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        Stretch(buttonRect);

        Image image = GetOrAdd<Image>(buttonObject);
        image.color = new Color(1f, 0.95f, 0.45f, 0.95f);

        Button button = GetOrAdd<Button>(buttonObject);
        button.targetGraphic = image;

        GameObject labelObject = FindChild(buttonObject.transform, "Label") ?? new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        Stretch(labelRect);

        Text label = GetOrAdd<Text>(labelObject);
        label.text = "...";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 42;
        label.color = Color.black;
        label.font = GetDefaultFont();

        AssignObject(bubble, "root", bubbleObject);
        AssignObject(bubble, "button", button);
        bubble.Hide();

        return bubbleObject;
    }

    private static DialogUi SetupDialogUi(GameObject canvasObject)
    {
        GameObject root = FindChild(canvasObject.transform, "DialogRoot") ?? new GameObject("DialogRoot", typeof(RectTransform));
        root.transform.SetParent(canvasObject.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.08f, 0.04f);
        rootRect.anchorMax = new Vector2(0.92f, 0.33f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image panel = GetOrAdd<Image>(root);
        panel.color = new Color(0.05f, 0.045f, 0.04f, 0.9f);

        GameObject speaker = FindChild(root.transform, "SpeakerText") ?? new GameObject("SpeakerText", typeof(RectTransform));
        speaker.transform.SetParent(root.transform, false);
        RectTransform speakerRect = speaker.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0.04f, 0.68f);
        speakerRect.anchorMax = new Vector2(0.56f, 0.92f);
        speakerRect.offsetMin = Vector2.zero;
        speakerRect.offsetMax = Vector2.zero;

        Text speakerText = GetOrAdd<Text>(speaker);
        speakerText.font = GetDefaultFont();
        speakerText.fontSize = 32;
        speakerText.color = new Color(1f, 0.86f, 0.42f, 1f);
        speakerText.alignment = TextAnchor.MiddleLeft;

        GameObject body = FindChild(root.transform, "BodyText") ?? new GameObject("BodyText", typeof(RectTransform));
        body.transform.SetParent(root.transform, false);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.04f, 0.17f);
        bodyRect.anchorMax = new Vector2(0.78f, 0.68f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        Text bodyText = GetOrAdd<Text>(body);
        bodyText.font = GetDefaultFont();
        bodyText.fontSize = 26;
        bodyText.color = Color.white;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        GameObject next = FindChild(root.transform, "NextButton") ?? new GameObject("NextButton", typeof(RectTransform));
        next.transform.SetParent(root.transform, false);
        RectTransform nextRect = next.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.8f, 0.15f);
        nextRect.anchorMax = new Vector2(0.96f, 0.42f);
        nextRect.offsetMin = Vector2.zero;
        nextRect.offsetMax = Vector2.zero;

        Image nextImage = GetOrAdd<Image>(next);
        nextImage.color = new Color(0.95f, 0.72f, 0.26f, 1f);

        Button nextButton = GetOrAdd<Button>(next);
        nextButton.targetGraphic = nextImage;

        GameObject nextLabel = FindChild(next.transform, "Text") ?? new GameObject("Text", typeof(RectTransform));
        nextLabel.transform.SetParent(next.transform, false);
        RectTransform nextLabelRect = nextLabel.GetComponent<RectTransform>();
        Stretch(nextLabelRect);

        Text nextText = GetOrAdd<Text>(nextLabel);
        nextText.font = GetDefaultFont();
        nextText.fontSize = 24;
        nextText.color = Color.black;
        nextText.alignment = TextAnchor.MiddleCenter;
        nextText.text = "Next";

        root.SetActive(false);

        return new DialogUi(root, speakerText, bodyText, nextButton);
    }

    private static GameObject FindOrCreateCanvas()
    {
        Canvas existingCanvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasObject = existingCanvas != null ? existingCanvas.gameObject : FindOrCreate("Canvas");

        Canvas canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GetOrAdd<GraphicRaycaster>(canvasObject);
        return canvasObject;
    }

    private static DialogData LoadOrCreateDialogData()
    {
        EnsureFolder("Assets/Core/Data");
        EnsureFolder("Assets/Core/Data/Dialog");

        string[] existingDialogGuids = AssetDatabase.FindAssets("t:DialogData", new[] { "Assets/Core/Data/Dialog" });
        if (existingDialogGuids.Length > 0)
        {
            string existingPath = AssetDatabase.GUIDToAssetPath(existingDialogGuids[0]);
            DialogData existing = AssetDatabase.LoadAssetAtPath<DialogData>(existingPath);
            if (existing != null)
            {
                return existing;
            }
        }

        DialogData data = AssetDatabase.LoadAssetAtPath<DialogData>(DialogDataPath);
        if (data != null)
        {
            return data;
        }

        data = ScriptableObject.CreateInstance<DialogData>();
        AssetDatabase.CreateAsset(data, DialogDataPath);

        SerializedObject serializedData = new SerializedObject(data);
        SerializedProperty lines = serializedData.FindProperty("lines");
        lines.arraySize = 3;
        SetDialogLine(lines.GetArrayElementAtIndex(0), DialogSpeaker.Customer, "I found something strange near the old market.");
        SetDialogLine(lines.GetArrayElementAtIndex(1), DialogSpeaker.Player, "Tell me exactly where it started hurting.");
        SetDialogLine(lines.GetArrayElementAtIndex(2), DialogSpeaker.Customer, "My wrist burns whenever I stand near the candle.");
        serializedData.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(data);
        return data;
    }

    private static void SetDialogLine(SerializedProperty line, DialogSpeaker speaker, string text)
    {
        line.FindPropertyRelative("speaker").enumValueIndex = (int)speaker;
        line.FindPropertyRelative("text").stringValue = text;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Sprite FindFirstCoreSprite()
    {
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Core/Texture" });
        if (spriteGuids.Length == 0)
        {
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(spriteGuids[0]);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static GameObject FindOrCreate(string name)
    {
        GameObject found = GameObject.Find(name);
        return found != null ? found : new GameObject(name);
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

    private static void AssignObject(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AssignBool(Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AssignFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AssignInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AssignString(Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }

    private readonly struct DialogUi
    {
        public DialogUi(GameObject root, Text speakerText, Text bodyText, Button nextButton)
        {
            Root = root;
            SpeakerText = speakerText;
            BodyText = bodyText;
            NextButton = nextButton;
        }

        public GameObject Root { get; }
        public Text SpeakerText { get; }
        public Text BodyText { get; }
        public Button NextButton { get; }
    }
}
