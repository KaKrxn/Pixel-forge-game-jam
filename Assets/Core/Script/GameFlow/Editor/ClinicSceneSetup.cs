using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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

        GameObject gameFlowObject = FindOrCreateAny("GameFlowManager", "GameFlow");
        GameObject customerObject = FindOrCreate("Customer");
        GameObject spawnPoint = FindOrCreate("CustomerSpawnPoint");
        GameObject outsideDoorPoint = FindOrCreate("OutsideDoorPoint");
        GameObject insideDoorPoint = FindOrCreate("InsideDoorPoint");
        GameObject counterPoint = FindOrCreate("CounterPoint");
        GameObject exitPoint = FindOrCreate("CustomerExitPoint");
        GameObject doorObject = FindOrCreate("Door");
        RoomLayers mainRoom = SetupRoomHierarchy("Main RoomRoot");
        RoomLayers treatmentRoom = SetupRoomHierarchy("Treatment RoomRoot");

        spawnPoint.transform.position = new Vector3(-5.93f, -2f, 0f);
        outsideDoorPoint.transform.position = new Vector3(-1.2f, -2f, 0f);
        insideDoorPoint.transform.position = new Vector3(0.15f, -2f, 0f);
        counterPoint.transform.position = new Vector3(5.37f, -2f, 0f);
        exitPoint.transform.position = new Vector3(-6.6f, -2f, 0f);
        doorObject.transform.position = outsideDoorPoint.transform.position;
        customerObject.transform.position = spawnPoint.transform.position;
        customerObject.transform.SetParent(mainRoom.Gameplay.transform, true);
        doorObject.transform.SetParent(mainRoom.Gameplay.transform, true);

        GameFlow gameFlow = GetOrAdd<GameFlow>(gameFlowObject);
        Dialog dialog = GetOrAdd<Dialog>(gameFlowObject);
        RoomTransition roomTransition = GetOrAdd<RoomTransition>(gameFlowObject);
        Treatment treatment = GetOrAdd<Treatment>(gameFlowObject);
        CustomerAgent customer = GetOrAdd<CustomerAgent>(customerObject);
        CustomerLayer customerLayer = GetOrAdd<CustomerLayer>(customerObject);
        Door door = GetOrAdd<Door>(doorObject);

        SpriteRenderer customerRenderer = GetOrAdd<SpriteRenderer>(customerObject);
        customerRenderer.sprite = FindFirstCoreSprite();
        customerRenderer.color = new Color(0.82f, 0.92f, 1f, 1f);
        customerRenderer.sortingLayerName = "Wall";
        customerRenderer.sortingOrder = -10;

        AssignObject(customerLayer, "targetRenderer", customerRenderer);
        AssignString(customerLayer, "outsideSortingLayer", "BG_Far");
        AssignInt(customerLayer, "outsideSortingOrder", -10);
        AssignString(customerLayer, "insideSortingLayer", "Customer");
        AssignInt(customerLayer, "insideSortingOrder", 0);

        GameObject bubbleObject = SetupBubble(customerObject);
        Bubble bubble = bubbleObject.GetComponent<Bubble>();

        GameObject canvasObject = FindOrCreateCanvas();
        DialogUi dialogUi = SetupDialogUi(canvasObject);
        TreatmentUi treatmentUi = SetupTreatmentUi(canvasObject);
        CanvasGroup fadeOverlay = SetupFadeOverlay(canvasObject);

        DialogData dialogData = LoadOrCreateDialogData();

        AssignObject(gameFlow, "firstCustomer", customer);
        AssignObject(gameFlow, "dialog", dialog);
        AssignObject(gameFlow, "roomTransition", roomTransition);
        AssignObject(gameFlow, "treatment", treatment);
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
        Camera camera = SetupCamera();
        SetupParallaxPlaceholders(mainRoom);
        SetupParallaxPlaceholders(treatmentRoom);
        SetupRoomTransition(roomTransition, mainRoom, treatmentRoom, camera, fadeOverlay);
        SetupTreatment(treatment, gameFlow, roomTransition, treatmentUi);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Clinic scene setup complete.");
    }

    [MenuItem("Pixel Forge/Setup/Run Clinic Scene Setup")]
    public static void RunFromMenu()
    {
        Run();
    }

    private static RoomLayers SetupRoomHierarchy(string rootName)
    {
        GameObject root = FindOrCreate(rootName);
        root.transform.position = Vector3.zero;

        GameObject background = FindOrCreateChild(root.transform, "00_Background");
        GameObject midground = FindOrCreateChild(root.transform, "01_Midground");
        GameObject mainArea = FindOrCreateChild(root.transform, "02_MainArea");
        GameObject gameplay = FindOrCreateChild(root.transform, "03_Gameplay");
        GameObject foreground = FindOrCreateChild(root.transform, "04_Foreground");
        GameObject vfx = FindOrCreateChild(root.transform, "05_VFX");

        return new RoomLayers(root, background, midground, mainArea, gameplay, foreground, vfx);
    }

    private static Camera SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = FindOrCreate("Main Camera");
            camera = GetOrAdd<Camera>(cameraObject);
            cameraObject.tag = "MainCamera";
        }

        camera.transform.SetParent(null, true);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.03f, 0.04f, 1f);

        MouseParallax mouseParallax = GetOrAdd<MouseParallax>(camera.gameObject);
        AssignObject(mouseParallax, "target", camera.transform);
        AssignVector2(mouseParallax, "maxOffset", new Vector2(0.5f, 0.28f));
        AssignFloat(mouseParallax, "smoothTime", 0.08f);
        AssignBool(mouseParallax, "snapToPixelGrid", true);
        AssignFloat(mouseParallax, "pixelsPerUnit", 16f);

        CameraSway cameraSway = GetOrAdd<CameraSway>(camera.gameObject);
        cameraSway.enabled = false;
        AssignObject(cameraSway, "target", camera.transform);
        AssignVector2(cameraSway, "amplitude", new Vector2(0.125f, 0.0625f));
        AssignVector2(cameraSway, "frequency", new Vector2(0.28f, 0.21f));
        AssignBool(cameraSway, "snapToPixelGrid", true);
        AssignFloat(cameraSway, "pixelsPerUnit", 16f);

        AddOrConfigurePixelPerfectCamera(camera.gameObject);
        return camera;
    }

    private static void SetupRoomTransition(RoomTransition roomTransition, RoomLayers mainRoom, RoomLayers treatmentRoom, Camera camera, CanvasGroup fadeOverlay)
    {
        AssignObject(roomTransition, "counterRoomRoot", mainRoom.Root);
        AssignObject(roomTransition, "treatmentRoomRoot", treatmentRoom.Root);
        AssignObject(roomTransition, "targetCamera", camera);
        AssignObject(roomTransition, "fadeCanvasGroup", fadeOverlay);
        AssignVector3(roomTransition, "counterCameraPosition", new Vector3(0f, 0f, -10f));
        AssignVector3(roomTransition, "treatmentCameraPosition", new Vector3(0f, 0f, -10f));
        AssignFloat(roomTransition, "closeEyeDuration", 0.18f);
        AssignFloat(roomTransition, "closedEyeHoldDuration", 0.08f);
        AssignFloat(roomTransition, "openEyeDuration", 0.22f);
        AssignBool(roomTransition, "useUnscaledTime", true);

        mainRoom.Root.SetActive(true);
        treatmentRoom.Root.SetActive(false);
        EditorUtility.SetDirty(mainRoom.Root);
        EditorUtility.SetDirty(treatmentRoom.Root);
    }

    private static void SetupTreatment(Treatment treatment, GameFlow gameFlow, RoomTransition roomTransition, TreatmentUi treatmentUi)
    {
        AssignObject(treatment, "flow", gameFlow);
        AssignObject(treatment, "roomTransition", roomTransition);
        AssignObject(treatment, "root", treatmentUi.Root);
        AssignObject(treatment, "titleText", treatmentUi.TitleText);
        AssignObject(treatment, "bodyText", treatmentUi.BodyText);
        AssignObject(treatment, "completeButton", treatmentUi.CompleteButton);
        AssignObject(treatment, "returnButton", treatmentUi.ReturnButton);
        AssignObject(treatment, "resumeButton", treatmentUi.ResumeButton);
        treatmentUi.Root.SetActive(false);
    }

    private static void AddOrConfigurePixelPerfectCamera(GameObject cameraObject)
    {
        System.Type pixelPerfectType = System.AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("UnityEngine.Rendering.Universal.PixelPerfectCamera"))
            .FirstOrDefault(type => type != null);

        if (pixelPerfectType == null)
        {
            Debug.LogWarning("URP Pixel Perfect Camera type was not found. Add it manually if the package is available.");
            return;
        }

        Component pixelPerfect = cameraObject.GetComponent(pixelPerfectType) ?? cameraObject.AddComponent(pixelPerfectType);
        SerializedObject serializedPixelPerfect = new SerializedObject(pixelPerfect);
        SetSerializedInt(serializedPixelPerfect, "m_AssetsPPU", 16);
        SetSerializedInt(serializedPixelPerfect, "m_RefResolutionX", 320);
        SetSerializedInt(serializedPixelPerfect, "m_RefResolutionY", 180);
        SetSerializedEnum(serializedPixelPerfect, "m_GridSnapping", 1);
        SetSerializedEnum(serializedPixelPerfect, "m_CropFrame", 0);
        SetSerializedEnum(serializedPixelPerfect, "m_FilterMode", 1);
        serializedPixelPerfect.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pixelPerfect);
    }

    private static void SetupParallaxPlaceholders(RoomLayers roomLayers)
    {
        Sprite sprite = FindFirstCoreSprite();
        CreateLayerSprite(roomLayers.Background.transform, "BG_Far_Placeholder", sprite, "BG_Far", 0, new Vector3(0f, 0.55f, 8f), new Color(0.18f, 0.2f, 0.28f, 1f), new Vector2(0.03f, 0.02f));
        CreateLayerSprite(roomLayers.Midground.transform, "BG_Mid_Placeholder", sprite, "BG_Mid", 0, new Vector3(0f, 0.15f, 7f), new Color(0.28f, 0.24f, 0.24f, 1f), new Vector2(0.08f, 0.04f));
        CreateLayerSprite(roomLayers.MainArea.transform, "Main_Room_Placeholder", sprite, "Main", 0, new Vector3(0f, -0.25f, 6.2f), new Color(0.34f, 0.29f, 0.25f, 1f), new Vector2(0.12f, 0.06f));
        CreateLayerSprite(roomLayers.Foreground.transform, "Foreground_Placeholder", sprite, "Foreground", 0, new Vector3(0f, -2.85f, 6.5f), new Color(0.1f, 0.08f, 0.09f, 0.9f), new Vector2(0.22f, 0.12f));
    }

    private static void CreateLayerSprite(Transform parent, string name, Sprite sprite, string sortingLayer, int sortingOrder, Vector3 positionAndScale, Color color, Vector2 parallaxStrength)
    {
        GameObject layerObject = FindOrCreateChild(parent, name);
        layerObject.transform.localPosition = new Vector3(positionAndScale.x, positionAndScale.y, 0f);
        layerObject.transform.localScale = new Vector3(positionAndScale.z, positionAndScale.z, 1f);

        SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(layerObject);
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = sortingOrder;

        ParallaxLayer parallaxLayer = GetOrAdd<ParallaxLayer>(parent.gameObject);
        AssignObject(parallaxLayer, "cameraTransform", Camera.main != null ? Camera.main.transform : null);
        AssignVector2(parallaxLayer, "parallaxStrength", parallaxStrength);
        AssignBool(parallaxLayer, "snapToPixelGrid", true);
        AssignFloat(parallaxLayer, "pixelsPerUnit", 16f);
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

        TMP_Text label = GetOrAddTmpText(labelObject);
        label.text = "...";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 42;
        label.color = Color.black;

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

        TMP_Text speakerText = GetOrAddTmpText(speaker);
        speakerText.fontSize = 32;
        speakerText.color = new Color(1f, 0.86f, 0.42f, 1f);
        speakerText.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject body = FindChild(root.transform, "BodyText") ?? new GameObject("BodyText", typeof(RectTransform));
        body.transform.SetParent(root.transform, false);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.04f, 0.17f);
        bodyRect.anchorMax = new Vector2(0.78f, 0.68f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        TMP_Text bodyText = GetOrAddTmpText(body);
        bodyText.fontSize = 26;
        bodyText.color = Color.white;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Overflow;

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

        TMP_Text nextText = GetOrAddTmpText(nextLabel);
        nextText.fontSize = 24;
        nextText.color = Color.black;
        nextText.alignment = TextAlignmentOptions.Center;
        nextText.text = "Next";

        root.SetActive(false);

        return new DialogUi(root, speakerText, bodyText, nextButton);
    }

    private static TreatmentUi SetupTreatmentUi(GameObject canvasObject)
    {
        GameObject root = FindChild(canvasObject.transform, "TreatmentRoot") ?? new GameObject("TreatmentRoot", typeof(RectTransform));
        root.transform.SetParent(canvasObject.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.18f, 0.16f);
        rootRect.anchorMax = new Vector2(0.82f, 0.78f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image panel = GetOrAdd<Image>(root);
        panel.color = new Color(0.045f, 0.038f, 0.035f, 0.92f);

        TMP_Text titleText = SetupText(root.transform, "TitleText", new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.94f), 36, new Color(1f, 0.82f, 0.36f, 1f), TextAlignmentOptions.Center);
        titleText.text = "Treatment Room";

        TMP_Text bodyText = SetupText(root.transform, "BodyText", new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.74f), 26, Color.white, TextAlignmentOptions.Top);
        bodyText.text = "Placeholder treatment state.";

        Button returnButton = SetupButton(root.transform, "ReturnCounterButton", "Return Counter", new Vector2(0.08f, 0.1f), new Vector2(0.34f, 0.26f));
        Button completeButton = SetupButton(root.transform, "CompleteTreatmentButton", "Complete Test", new Vector2(0.37f, 0.1f), new Vector2(0.63f, 0.26f));
        Button resumeButton = SetupButton(root.transform, "ResumeTreatmentButton", "Resume", new Vector2(0.66f, 0.1f), new Vector2(0.92f, 0.26f));

        root.SetActive(false);
        return new TreatmentUi(root, titleText, bodyText, completeButton, returnButton, resumeButton);
    }

    private static CanvasGroup SetupFadeOverlay(GameObject canvasObject)
    {
        GameObject overlay = FindChild(canvasObject.transform, "BlinkFadeOverlay") ?? new GameObject("BlinkFadeOverlay", typeof(RectTransform));
        overlay.transform.SetParent(canvasObject.transform, false);
        overlay.transform.SetAsLastSibling();

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect);

        Image image = GetOrAdd<Image>(overlay);
        image.color = Color.black;
        image.raycastTarget = true;

        CanvasGroup canvasGroup = GetOrAdd<CanvasGroup>(overlay);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        overlay.SetActive(false);

        return canvasGroup;
    }

    private static TMP_Text SetupText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = FindChild(parent, name) ?? new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = GetOrAddTmpText(textObject);
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static Button SetupButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = FindChild(parent, name) ?? new GameObject(name, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image buttonImage = GetOrAdd<Image>(buttonObject);
        buttonImage.color = new Color(0.9f, 0.62f, 0.22f, 1f);

        Button button = GetOrAdd<Button>(buttonObject);
        button.targetGraphic = buttonImage;

        GameObject labelObject = FindChild(buttonObject.transform, "Text") ?? new GameObject("Text", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        Stretch(labelRect);

        TMP_Text buttonText = GetOrAddTmpText(labelObject);
        buttonText.fontSize = 22;
        buttonText.color = Color.black;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.text = label;

        return button;
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

    private static GameObject FindOrCreate(string name)
    {
        GameObject found = FindSceneObject(name);
        return found != null ? found : new GameObject(name);
    }

    private static GameObject FindOrCreateAny(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject found = FindSceneObject(name);
            if (found != null)
            {
                return found;
            }
        }

        return new GameObject(names[0]);
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

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        GameObject child = FindChild(parent, name);
        if (child != null)
        {
            return child;
        }

        child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
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

    private static void AssignVector2(Object target, string propertyName, Vector2 value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).vector2Value = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AssignVector3(Object target, string propertyName, Vector3 value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).vector3Value = value;
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

    private static void SetSerializedInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetSerializedEnum(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.enumValueIndex = value;
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
        public DialogUi(GameObject root, TMP_Text speakerText, TMP_Text bodyText, Button nextButton)
        {
            Root = root;
            SpeakerText = speakerText;
            BodyText = bodyText;
            NextButton = nextButton;
        }

        public GameObject Root { get; }
        public TMP_Text SpeakerText { get; }
        public TMP_Text BodyText { get; }
        public Button NextButton { get; }
    }

    private readonly struct TreatmentUi
    {
        public TreatmentUi(GameObject root, TMP_Text titleText, TMP_Text bodyText, Button completeButton, Button returnButton, Button resumeButton)
        {
            Root = root;
            TitleText = titleText;
            BodyText = bodyText;
            CompleteButton = completeButton;
            ReturnButton = returnButton;
            ResumeButton = resumeButton;
        }

        public GameObject Root { get; }
        public TMP_Text TitleText { get; }
        public TMP_Text BodyText { get; }
        public Button CompleteButton { get; }
        public Button ReturnButton { get; }
        public Button ResumeButton { get; }
    }

    private readonly struct RoomLayers
    {
        public RoomLayers(GameObject root, GameObject background, GameObject midground, GameObject mainArea, GameObject gameplay, GameObject foreground, GameObject vfx)
        {
            Root = root;
            Background = background;
            Midground = midground;
            MainArea = mainArea;
            Gameplay = gameplay;
            Foreground = foreground;
            Vfx = vfx;
        }

        public GameObject Root { get; }
        public GameObject Background { get; }
        public GameObject Midground { get; }
        public GameObject MainArea { get; }
        public GameObject Gameplay { get; }
        public GameObject Foreground { get; }
        public GameObject Vfx { get; }
    }
}
