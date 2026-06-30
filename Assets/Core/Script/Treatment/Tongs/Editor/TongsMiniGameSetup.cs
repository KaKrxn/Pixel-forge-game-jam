using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TongsMiniGameSetup
{
    private const string SmallParasiteTypeAssetPath = "Assets/Core/Data/Treatment/Tongs/SmallParasiteType.asset";
    private const string LongParasiteTypeAssetPath = "Assets/Core/Data/Treatment/Tongs/LongParasiteType.asset";
    private const string BigParasiteTypeAssetPath = "Assets/Core/Data/Treatment/Tongs/BigParasiteType.asset";
    private const string SmallParasitePrefabPath = "Assets/Core/Prefab/Parasites/SmallParasite_World.prefab";
    private const string LongParasitePrefabPath = "Assets/Core/Prefab/Parasites/LongParasite_World.prefab";
    private const string BigParasitePrefabPath = "Assets/Core/Prefab/Parasites/BigParasite_World.prefab";
    private const string PlaceholderSpriteAssetPath = "Assets/Core/Data/Treatment/Tongs/TongsParasitePlaceholder.asset";

    [MenuItem("Tools/Pixel Forge/Treatment/Create Tongs World MiniGame Slice")]
    public static void CreateTongsWorldMiniGameSlice()
    {
        GameObject parent = Selection.activeGameObject;
        if (parent == null)
        {
            Debug.LogWarning("Select the Treatment Room root or a world-space Treatment parent before creating the Tongs mini game slice.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        ParasiteType[] parasiteTypes = GetAllParasiteTypes();
        ParasiteType smallType = parasiteTypes.Length > 0 ? parasiteTypes[0] : null;
        Sprite placeholderSprite = GetOrCreatePlaceholderSprite();

        GameObject root = CreateWorldObject("TongsMiniGameRoot", parent.transform, new Vector3(0f, -0.25f, 0f));
        TongsMiniGame miniGame = Undo.AddComponent<TongsMiniGame>(root);

        GameObject parasiteRoot = CreateWorldObject("ParasiteRoot", root.transform, Vector3.zero);
        GameObject parasiteObject = CreateWorldObject("SmallParasite_World", parasiteRoot.transform, new Vector3(0f, -0.35f, 0f));

        SpriteRenderer parasiteRenderer = Undo.AddComponent<SpriteRenderer>(parasiteObject);
        parasiteRenderer.sprite = placeholderSprite;
        parasiteRenderer.color = new Color(0.56f, 0.12f, 0.18f, 1f);
        parasiteRenderer.sortingOrder = 25;

        BoxCollider2D parasiteCollider = Undo.AddComponent<BoxCollider2D>(parasiteObject);
        parasiteCollider.size = new Vector2(0.24f, 0.72f);
        parasiteCollider.offset = new Vector2(0f, 0.32f);

        GameObject parasiteSpawnAnchor = CreateWorldObject("SpawnAnchor", parasiteObject.transform, new Vector3(0f, 0.68f, 0f));
        Parasite parasite = Undo.AddComponent<Parasite>(parasiteObject);

        GameObject overlayCanvas = CreateOverlayCanvas("TongsOverlayCanvas", root.transform);
        GameObject progressSliderObject = CreateSlider("PullProgressSlider", overlayCanvas.transform, new Vector2(0.5f, 0.14f), new Vector2(420f, 22f));
        Slider progressSlider = progressSliderObject.GetComponent<Slider>();

        GameObject painSliderObject = CreateSlider("PainSlider", overlayCanvas.transform, new Vector2(0.5f, 0.09f), new Vector2(420f, 22f));
        Slider painSlider = painSliderObject.GetComponent<Slider>();

        GameObject completeButtonObject = CreateButton("CompleteTreatmentButton_Tongs", overlayCanvas.transform, "Complete", new Vector2(0.82f, 0.09f), new Vector2(150f, 52f));
        Button completeButton = completeButtonObject.GetComponent<Button>();

        GameObject toolButtonObject = CreateTongsToolButton(overlayCanvas.transform, miniGame);

        AssignMiniGame(miniGame, root, parasiteRoot.transform, progressSlider, painSlider, completeButton, parasiteTypes);
        AssignParasite(parasite, smallType, parasiteRenderer, parasiteSpawnAnchor.transform);
        TryAssignTreatment(parent, miniGame);

        root.SetActive(false);
        completeButtonObject.SetActive(false);
        SetSelectedIndicatorVisible(toolButtonObject, false);

        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = root;

        Debug.Log("Created a world-space Tongs mini game slice under the selected Treatment parent.");
    }

    [MenuItem("Tools/Pixel Forge/Treatment/Add Tongs Equip Button To Selected MiniGame")]
    public static void AddTongsEquipButtonToSelectedMiniGame()
    {
        TongsMiniGame miniGame = FindSelectedMiniGame();
        if (miniGame == null)
        {
            Debug.LogWarning("Select an existing TongsMiniGameRoot, a child of it, or a parent that contains it before adding the Tongs equip button.");
            return;
        }

        Transform overlayCanvas = FindOrCreateOverlayCanvas(miniGame.transform);
        GameObject toolButtonObject = FindExistingTongsToolButton(overlayCanvas);
        if (toolButtonObject == null)
        {
            toolButtonObject = CreateTongsToolButton(overlayCanvas, miniGame);
        }
        else
        {
            BindExistingTongsToolButton(toolButtonObject, miniGame);
        }

        SetSelectedIndicatorVisible(toolButtonObject, false);

        SerializedObject serializedObject = new SerializedObject(miniGame);
        serializedObject.FindProperty("requireTongsEquipped").boolValue = true;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(miniGame);
        Selection.activeGameObject = toolButtonObject;
        Debug.Log("Added a Tongs equip button and enabled Require Tongs Equipped on the selected mini game.");
    }

    [MenuItem("Tools/Pixel Forge/Treatment/Enable Tongs Pull Visual Feedback")]
    public static void EnableTongsPullVisualFeedback()
    {
        TongsMiniGame miniGame = FindSelectedMiniGame();
        Parasite[] parasites = miniGame != null
            ? miniGame.GetComponentsInChildren<Parasite>(true)
            : FindSelectedParasites();

        if (parasites.Length == 0)
        {
            Debug.LogWarning("Select a TongsMiniGameRoot, a parent containing parasites, or a Parasite object before enabling pull visual feedback.");
            return;
        }

        for (int i = 0; i < parasites.Length; i++)
        {
            Parasite parasite = parasites[i];
            if (parasite == null)
            {
                continue;
            }

            SpriteRenderer renderer = parasite.GetComponentInChildren<SpriteRenderer>(true);
            SerializedObject serializedObject = new SerializedObject(parasite);
            if (renderer != null)
            {
                serializedObject.FindProperty("targetRenderer").objectReferenceValue = renderer;
                serializedObject.FindProperty("visualRoot").objectReferenceValue = renderer.transform;
            }

            serializedObject.FindProperty("moveVisualWithPull").boolValue = true;
            serializedObject.FindProperty("maxVisualFollowDistance").floatValue = 0.65f;
            serializedObject.FindProperty("visualFollowStrength").floatValue = 0.85f;
            serializedObject.FindProperty("maxVisualTiltDegrees").floatValue = 28f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(parasite);
        }

        Debug.Log($"Enabled pull visual feedback on {parasites.Length} parasite object(s).");
    }

    [MenuItem("Tools/Pixel Forge/Treatment/Create Parasite Spawn Anchors")]
    public static void CreateParasiteSpawnAnchors()
    {
        TongsMiniGame miniGame = FindSelectedMiniGame();
        Parasite[] parasites = miniGame != null
            ? miniGame.GetComponentsInChildren<Parasite>(true)
            : FindSelectedParasites();

        if (parasites.Length == 0)
        {
            Debug.LogWarning("Select a TongsMiniGameRoot, a parent containing parasites, or a Parasite object before creating parasite spawn anchors.");
            return;
        }

        for (int i = 0; i < parasites.Length; i++)
        {
            Parasite parasite = parasites[i];
            if (parasite == null)
            {
                continue;
            }

            Transform spawnAnchor = FindOrCreateParasiteSpawnAnchor(parasite);
            SerializedObject serializedObject = new SerializedObject(parasite);
            serializedObject.FindProperty("spawnAnchor").objectReferenceValue = spawnAnchor;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(parasite);
        }

        Debug.Log($"Created or assigned spawn anchors on {parasites.Length} parasite object(s).");
    }

    [MenuItem("Tools/Pixel Forge/Treatment/Assign All Tongs Spawn Options")]
    public static void AssignAllTongsParasiteTypes()
    {
        TongsMiniGame miniGame = FindSelectedMiniGame();
        if (miniGame == null)
        {
            Debug.LogWarning("Select a TongsMiniGameRoot or a parent that contains it before assigning parasite types.");
            return;
        }

        SerializedObject serializedObject = new SerializedObject(miniGame);
        AssignParasiteSpawnOptionList(serializedObject, GetDefaultSpawnOptions());
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(miniGame);
        Debug.Log("Assigned Small, Long, and Big parasite spawn options to the selected Tongs mini game.");
    }

    private static TongsMiniGame FindSelectedMiniGame()
    {
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            GameObject selected = Selection.gameObjects[i];
            if (selected == null)
            {
                continue;
            }

            TongsMiniGame miniGame = selected.GetComponentInParent<TongsMiniGame>();
            if (miniGame != null)
            {
                return miniGame;
            }

            miniGame = selected.GetComponentInChildren<TongsMiniGame>(true);
            if (miniGame != null)
            {
                return miniGame;
            }
        }

        return null;
    }

    private static Parasite[] FindSelectedParasites()
    {
        System.Collections.Generic.List<Parasite> parasites = new System.Collections.Generic.List<Parasite>();
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            GameObject selected = Selection.gameObjects[i];
            if (selected == null)
            {
                continue;
            }

            Parasite direct = selected.GetComponent<Parasite>();
            if (direct != null && !parasites.Contains(direct))
            {
                parasites.Add(direct);
            }

            Parasite[] children = selected.GetComponentsInChildren<Parasite>(true);
            for (int childIndex = 0; childIndex < children.Length; childIndex++)
            {
                if (children[childIndex] != null && !parasites.Contains(children[childIndex]))
                {
                    parasites.Add(children[childIndex]);
                }
            }
        }

        return parasites.ToArray();
    }

    private static ParasiteType GetOrCreateSmallParasiteType()
    {
        ParasiteType existing = AssetDatabase.LoadAssetAtPath<ParasiteType>(SmallParasiteTypeAssetPath);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(SmallParasiteTypeAssetPath));
        ParasiteType created = ScriptableObject.CreateInstance<ParasiteType>();
        AssetDatabase.CreateAsset(created, SmallParasiteTypeAssetPath);
        AssetDatabase.SaveAssets();
        return created;
    }

    private static ParasiteType[] GetAllParasiteTypes()
    {
        System.Collections.Generic.List<ParasiteType> types = new System.Collections.Generic.List<ParasiteType>();
        AddIfNotNull(types, GetOrCreateSmallParasiteType());
        AddIfNotNull(types, AssetDatabase.LoadAssetAtPath<ParasiteType>(LongParasiteTypeAssetPath));
        AddIfNotNull(types, AssetDatabase.LoadAssetAtPath<ParasiteType>(BigParasiteTypeAssetPath));
        return types.ToArray();
    }

    private static ParasiteSpawnOptionData[] GetDefaultSpawnOptions()
    {
        return new[]
        {
            new ParasiteSpawnOptionData(
                AssetDatabase.LoadAssetAtPath<ParasiteType>(SmallParasiteTypeAssetPath),
                AssetDatabase.LoadAssetAtPath<Parasite>(SmallParasitePrefabPath)),
            new ParasiteSpawnOptionData(
                AssetDatabase.LoadAssetAtPath<ParasiteType>(LongParasiteTypeAssetPath),
                AssetDatabase.LoadAssetAtPath<Parasite>(LongParasitePrefabPath)),
            new ParasiteSpawnOptionData(
                AssetDatabase.LoadAssetAtPath<ParasiteType>(BigParasiteTypeAssetPath),
                AssetDatabase.LoadAssetAtPath<Parasite>(BigParasitePrefabPath))
        };
    }

    private static void AddIfNotNull(System.Collections.Generic.List<ParasiteType> types, ParasiteType type)
    {
        if (type != null && !types.Contains(type))
        {
            types.Add(type);
        }
    }

    private static Sprite GetOrCreatePlaceholderSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpriteAssetPath);
        if (existing != null)
        {
            return existing;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(PlaceholderSpriteAssetPath));

        const int width = 8;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "TongsParasitePlaceholderTexture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color body = new Color(0.56f, 0.12f, 0.18f, 1f);
        Color highlight = new Color(0.85f, 0.32f, 0.38f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inside = x >= 2 && x <= 5;
                bool roundedTip = y > height - 5 && x >= 3 && x <= 4;
                Color color = inside || roundedTip ? body : clear;
                if (color.a > 0f && x == 3 && y > 4)
                {
                    color = highlight;
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.08f), 16f);
        sprite.name = "TongsParasitePlaceholderSprite";

        AssetDatabase.CreateAsset(texture, PlaceholderSpriteAssetPath);
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssets();
        return sprite;
    }

    private static GameObject CreateWorldObject(string name, Transform parent, Vector3 localPosition)
    {
        GameObject gameObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;
        return gameObject;
    }

    private static GameObject CreateOverlayCanvas(string name, Transform parent)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, $"Create {name}");
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObject;
    }

    private static GameObject CreateSlider(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        DefaultControls.Resources resources = new DefaultControls.Resources();
        GameObject sliderObject = DefaultControls.CreateSlider(resources);
        Undo.RegisterCreatedObjectUndo(sliderObject, $"Create {name}");
        sliderObject.name = name;
        sliderObject.transform.SetParent(parent, false);

        RectTransform rectTransform = sliderObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        return sliderObject;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size)
    {
        DefaultControls.Resources resources = new DefaultControls.Resources();
        GameObject buttonObject = DefaultControls.CreateButton(resources);
        Undo.RegisterCreatedObjectUndo(buttonObject, $"Create {name}");
        buttonObject.name = name;
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;

        Text text = buttonObject.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = label;
        }

        return buttonObject;
    }

    private static GameObject CreateTongsToolButton(Transform parent, TongsMiniGame miniGame)
    {
        GameObject toolButtonObject = CreateButton("TongsToolButton", parent, "Tongs", new Vector2(0.16f, 0.09f), new Vector2(150f, 52f));
        Button toolButton = toolButtonObject.GetComponent<Button>();

        GameObject selectedIndicator = CreateUiObject("SelectedIndicator", toolButtonObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(18f, 18f));
        Image selectedImage = Undo.AddComponent<Image>(selectedIndicator);
        selectedImage.color = new Color(1f, 0.78f, 0.2f, 1f);

        TongsTool tool = Undo.AddComponent<TongsTool>(toolButtonObject);
        AssignTool(tool, miniGame, toolButton, selectedIndicator);
        return toolButtonObject;
    }

    private static GameObject FindExistingTongsToolButton(Transform overlayCanvas)
    {
        Transform existing = overlayCanvas.Find("TongsToolButton");
        if (existing != null)
        {
            return existing.gameObject;
        }

        TongsTool existingTool = overlayCanvas.GetComponentInChildren<TongsTool>(true);
        return existingTool != null ? existingTool.gameObject : null;
    }

    private static void BindExistingTongsToolButton(GameObject toolButtonObject, TongsMiniGame miniGame)
    {
        Button button = toolButtonObject.GetComponent<Button>();
        if (button == null)
        {
            button = Undo.AddComponent<Button>(toolButtonObject);
        }

        Transform selectedIndicatorTransform = toolButtonObject.transform.Find("SelectedIndicator");
        GameObject selectedIndicator = selectedIndicatorTransform != null ? selectedIndicatorTransform.gameObject : null;
        if (selectedIndicator == null)
        {
            selectedIndicator = CreateUiObject("SelectedIndicator", toolButtonObject.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(18f, 18f));
            Image selectedImage = Undo.AddComponent<Image>(selectedIndicator);
            selectedImage.color = new Color(1f, 0.78f, 0.2f, 1f);
        }

        TongsTool tool = toolButtonObject.GetComponent<TongsTool>();
        if (tool == null)
        {
            tool = Undo.AddComponent<TongsTool>(toolButtonObject);
        }

        AssignTool(tool, miniGame, button, selectedIndicator);
    }

    private static GameObject CreateUiObject(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
        gameObject.transform.SetParent(parent, false);

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = pivot;
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;
        return gameObject;
    }

    private static Transform FindOrCreateOverlayCanvas(Transform root)
    {
        Transform existing = root.Find("TongsOverlayCanvas");
        if (existing != null)
        {
            return existing;
        }

        return CreateOverlayCanvas("TongsOverlayCanvas", root).transform;
    }

    private static void SetSelectedIndicatorVisible(GameObject toolButtonObject, bool visible)
    {
        Transform indicator = toolButtonObject != null ? toolButtonObject.transform.Find("SelectedIndicator") : null;
        if (indicator != null)
        {
            indicator.gameObject.SetActive(visible);
        }
    }

    private static void AssignMiniGame(TongsMiniGame miniGame, GameObject root, Transform parasiteRoot, Slider progressSlider, Slider painSlider, Button completeButton, ParasiteType[] parasiteTypes)
    {
        SerializedObject serializedObject = new SerializedObject(miniGame);
        serializedObject.FindProperty("root").objectReferenceValue = root;
        serializedObject.FindProperty("parasiteRoot").objectReferenceValue = parasiteRoot;
        serializedObject.FindProperty("inputCamera").objectReferenceValue = Camera.main;
        serializedObject.FindProperty("worldInputPlaneZ").floatValue = 0f;
        serializedObject.FindProperty("alignParasiteAnchorToSpawnPoint").boolValue = true;
        serializedObject.FindProperty("pullProgressSlider").objectReferenceValue = progressSlider;
        serializedObject.FindProperty("painSlider").objectReferenceValue = painSlider;
        serializedObject.FindProperty("completeButton").objectReferenceValue = completeButton;
        serializedObject.FindProperty("autoFindParasitesInChildren").boolValue = true;
        serializedObject.FindProperty("startHidden").boolValue = true;
        serializedObject.FindProperty("requireTongsEquipped").boolValue = true;
        serializedObject.FindProperty("completeTreatmentOnButton").boolValue = false;
        AssignParasiteSpawnOptionList(serializedObject, GetDefaultSpawnOptions());
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignParasiteSpawnOptionList(SerializedObject serializedObject, ParasiteSpawnOptionData[] spawnOptions)
    {
        SerializedProperty optionList = serializedObject.FindProperty("parasiteSpawnOptions");
        optionList.ClearArray();

        for (int i = 0; i < spawnOptions.Length; i++)
        {
            if (!spawnOptions[i].IsValid)
            {
                continue;
            }

            int index = optionList.arraySize;
            optionList.InsertArrayElementAtIndex(index);
            SerializedProperty option = optionList.GetArrayElementAtIndex(index);
            option.FindPropertyRelative("parasiteType").objectReferenceValue = spawnOptions[i].ParasiteType;
            option.FindPropertyRelative("parasitePrefab").objectReferenceValue = spawnOptions[i].ParasitePrefab;
        }
    }

    private static void AssignTool(TongsTool tool, TongsMiniGame miniGame, Button button, GameObject selectedIndicator)
    {
        SerializedObject serializedObject = new SerializedObject(tool);
        serializedObject.FindProperty("miniGame").objectReferenceValue = miniGame;
        serializedObject.FindProperty("button").objectReferenceValue = button;
        serializedObject.FindProperty("selectedIndicator").objectReferenceValue = selectedIndicator;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignParasite(Parasite parasite, ParasiteType parasiteType, SpriteRenderer targetRenderer, Transform spawnAnchor)
    {
        SerializedObject serializedObject = new SerializedObject(parasite);
        serializedObject.FindProperty("type").objectReferenceValue = parasiteType;
        serializedObject.FindProperty("spawnAnchor").objectReferenceValue = spawnAnchor;
        serializedObject.FindProperty("targetRenderer").objectReferenceValue = targetRenderer;
        serializedObject.FindProperty("edgeContactEnabled").boolValue = false;
        serializedObject.FindProperty("jitterEnabled").boolValue = false;
        serializedObject.FindProperty("channelDepth").floatValue = 2f;
        serializedObject.FindProperty("channelTopOffset").floatValue = 0f;
        serializedObject.FindProperty("visualRoot").objectReferenceValue = targetRenderer.transform;
        serializedObject.FindProperty("moveVisualWithPull").boolValue = true;
        serializedObject.FindProperty("maxVisualFollowDistance").floatValue = 0.65f;
        serializedObject.FindProperty("visualFollowStrength").floatValue = 0.85f;
        serializedObject.FindProperty("maxVisualTiltDegrees").floatValue = 28f;
        serializedObject.FindProperty("normalColor").colorValue = targetRenderer != null ? targetRenderer.color : Color.white;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform FindOrCreateParasiteSpawnAnchor(Parasite parasite)
    {
        Transform existing = parasite.transform.Find("SpawnAnchor");
        if (existing != null)
        {
            return existing;
        }

        Vector3 localPosition = GetDefaultSpawnAnchorLocalPosition(parasite);
        GameObject spawnAnchor = CreateWorldObject("SpawnAnchor", parasite.transform, localPosition);
        return spawnAnchor.transform;
    }

    private static Vector3 GetDefaultSpawnAnchorLocalPosition(Parasite parasite)
    {
        BoxCollider2D boxCollider = parasite.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            return new Vector3(boxCollider.offset.x, boxCollider.offset.y + boxCollider.size.y * 0.5f, 0f);
        }

        SpriteRenderer renderer = parasite.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null)
        {
            Vector3 worldTop = new Vector3(renderer.bounds.center.x, renderer.bounds.max.y, parasite.transform.position.z);
            return parasite.transform.InverseTransformPoint(worldTop);
        }

        return Vector3.zero;
    }

    private static void TryAssignTreatment(GameObject parent, TongsMiniGame miniGame)
    {
        Treatment treatment = parent.GetComponentInParent<Treatment>();
        if (treatment == null)
        {
            Debug.Log("Created Tongs world mini game slice. No Treatment component was found in the selected object's parents, so assign TongsMiniGame manually.");
            return;
        }

        SerializedObject serializedObject = new SerializedObject(treatment);
        serializedObject.FindProperty("tongsMiniGame").objectReferenceValue = miniGame;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(treatment);
        Debug.Log("Created Tongs world mini game slice and assigned it to the parent Treatment component.");
    }

    private readonly struct ParasiteSpawnOptionData
    {
        public readonly ParasiteType ParasiteType;
        public readonly Parasite ParasitePrefab;

        public ParasiteSpawnOptionData(ParasiteType parasiteType, Parasite parasitePrefab)
        {
            ParasiteType = parasiteType;
            ParasitePrefab = parasitePrefab;
        }

        public bool IsValid => ParasiteType != null && ParasitePrefab != null;
    }
}
