using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NeedleMiniGameSetup
{
    private const string PlaceholderSpriteAssetPath = "Assets/Core/Data/Treatment/Tongs/TongsParasitePlaceholder.asset";

    [MenuItem("Tools/Pixel Forge/Treatment/Setup Needle MiniGame In Open Scene")]
    public static void SetupNeedleMiniGameInOpenScene()
    {
        NeedleMiniGame miniGame = FindSelectedOrSceneMiniGame();
        if (miniGame == null)
        {
            miniGame = CreateNeedleMiniGameRoot();
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Transform pustuleRoot = FindOrCreateChild(miniGame.transform, "PustuleRoot");
        Transform spawnRoot = FindOrCreateChild(miniGame.transform, "Spawn Anchor Manager");
        EnsureSamplePustules(pustuleRoot);
        EnsureSpawnAnchors(spawnRoot);
        EnsureSpawnAnchorComponents(spawnRoot);

        AssignMiniGame(miniGame, pustuleRoot, spawnRoot);
        AssignAnatomyController(miniGame);
        AssignSharedOverlay(miniGame);

        EditorUtility.SetDirty(miniGame);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = miniGame.gameObject;
        Debug.Log("Needle mini game setup complete. Sample pustules and spawn anchors are ready to reposition.");
    }

    private static NeedleMiniGame FindSelectedOrSceneMiniGame()
    {
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            GameObject selected = Selection.gameObjects[i];
            if (selected == null)
            {
                continue;
            }

            NeedleMiniGame inParent = selected.GetComponentInParent<NeedleMiniGame>(true);
            if (inParent != null)
            {
                return inParent;
            }

            NeedleMiniGame inChildren = selected.GetComponentInChildren<NeedleMiniGame>(true);
            if (inChildren != null)
            {
                return inChildren;
            }
        }

        return FindSceneObject<NeedleMiniGame>();
    }

    private static NeedleMiniGame CreateNeedleMiniGameRoot()
    {
        GameObject root = new GameObject("NeedleMiniGameRoot");
        Undo.RegisterCreatedObjectUndo(root, "Create NeedleMiniGameRoot");

        Transform treatmentRoot = FindSceneTransform("Treatment RoomRoot");
        if (treatmentRoot != null)
        {
            Transform gameplayRoot = FindDeepChild(treatmentRoot, "03_Gameplay");
            root.transform.SetParent(gameplayRoot != null ? gameplayRoot : treatmentRoot, false);
        }

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return Undo.AddComponent<NeedleMiniGame>(root);
    }

    private static void AssignMiniGame(NeedleMiniGame miniGame, Transform pustuleRoot, Transform spawnRoot)
    {
        SerializedObject serializedObject = new SerializedObject(miniGame);
        serializedObject.FindProperty("flow").objectReferenceValue = FindSceneObject<GameFlow>();
        serializedObject.FindProperty("root").objectReferenceValue = miniGame.gameObject;
        serializedObject.FindProperty("pustuleRoot").objectReferenceValue = pustuleRoot;
        serializedObject.FindProperty("inputCamera").objectReferenceValue = Camera.main;
        serializedObject.FindProperty("worldInputPlaneZ").floatValue = 0f;
        serializedObject.FindProperty("overlay").objectReferenceValue = FindSceneObject<MiniGameOverlay>();
        serializedObject.FindProperty("overlayToolId").stringValue = "Needle";
        serializedObject.FindProperty("spawnPustulesOnBegin").boolValue = false;
        serializedObject.FindProperty("autoCompleteWhenAllPustulesDone").boolValue = true;
        serializedObject.FindProperty("startHidden").boolValue = true;

        Pustule[] pustules = pustuleRoot.GetComponentsInChildren<Pustule>(true);
        SerializedProperty pustuleList = serializedObject.FindProperty("pustules");
        pustuleList.ClearArray();
        for (int i = 0; i < pustules.Length; i++)
        {
            pustuleList.InsertArrayElementAtIndex(i);
            pustuleList.GetArrayElementAtIndex(i).objectReferenceValue = pustules[i];
        }

        Transform[] anchors = GetDirectChildren(spawnRoot);
        PustuleSpawnAnchor[] pustuleAnchors = GetDirectChildComponents<PustuleSpawnAnchor>(spawnRoot);
        SerializedProperty pustuleSpawnAnchors = serializedObject.FindProperty("pustuleSpawnAnchors");
        pustuleSpawnAnchors.ClearArray();
        for (int i = 0; i < pustuleAnchors.Length; i++)
        {
            pustuleSpawnAnchors.InsertArrayElementAtIndex(i);
            pustuleSpawnAnchors.GetArrayElementAtIndex(i).objectReferenceValue = pustuleAnchors[i];
        }

        SerializedProperty spawnAnchors = serializedObject.FindProperty("spawnAnchors");
        spawnAnchors.ClearArray();
        for (int i = 0; i < anchors.Length; i++)
        {
            spawnAnchors.InsertArrayElementAtIndex(i);
            spawnAnchors.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignAnatomyController(NeedleMiniGame miniGame)
    {
        AnatomyController anatomyController = FindSceneObject<AnatomyController>();
        if (anatomyController == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(anatomyController);
        serializedObject.FindProperty("needleMiniGame").objectReferenceValue = miniGame;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(anatomyController);
    }

    private static void AssignSharedOverlay(NeedleMiniGame miniGame)
    {
        MiniGameOverlay overlay = FindSceneObject<MiniGameOverlay>();
        if (overlay == null)
        {
            return;
        }

        Toggle toggle = FindToggle(overlay.transform, "NeedleToolButton");
        Button button = FindButton(overlay.transform, "NeedleToolButton");
        GameObject indicator = FindIndicator(overlay.transform, "NeedleToolButton");
        if (toggle == null && button == null)
        {
            GameObject toolButton = CreateOverlayToolButton(overlay, "NeedleToolButton", "Needle");
            toggle = toolButton.GetComponent<Toggle>();
            button = toolButton.GetComponent<Button>();
            indicator = FindIndicator(toolButton.transform, "NeedleToolButton");
        }

        SerializedObject miniGameObject = new SerializedObject(miniGame);
        miniGameObject.FindProperty("overlay").objectReferenceValue = overlay;
        miniGameObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject overlayObject = new SerializedObject(overlay);
        SerializedProperty toolButtons = overlayObject.FindProperty("toolButtons");
        EnsureToolButton(toolButtons, "Needle", toggle, button, indicator);
        overlayObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(overlay);
    }

    private static void EnsureToolButton(SerializedProperty toolButtons, string id, Toggle toggle, Button button, GameObject indicator)
    {
        SerializedProperty entry = FindToolButtonEntry(toolButtons, id);
        if (entry == null)
        {
            int index = toolButtons.arraySize;
            toolButtons.InsertArrayElementAtIndex(index);
            entry = toolButtons.GetArrayElementAtIndex(index);
        }

        entry.FindPropertyRelative("id").stringValue = id;
        entry.FindPropertyRelative("toggle").objectReferenceValue = toggle;
        entry.FindPropertyRelative("button").objectReferenceValue = button;
        entry.FindPropertyRelative("selectedIndicator").objectReferenceValue = indicator;
    }

    private static SerializedProperty FindToolButtonEntry(SerializedProperty toolButtons, string id)
    {
        for (int i = 0; i < toolButtons.arraySize; i++)
        {
            SerializedProperty entry = toolButtons.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("id").stringValue == id)
            {
                return entry;
            }
        }

        return null;
    }

    private static void EnsureSamplePustules(Transform pustuleRoot)
    {
        if (pustuleRoot.GetComponentInChildren<Pustule>(true) != null)
        {
            return;
        }

        CreateSamplePustule(pustuleRoot, "NeedleSamplePustule_Small", PustuleType.Small, new Vector3(-0.75f, 0.18f, 0f), new Vector3(0.45f, 0.45f, 1f));
        CreateSamplePustule(pustuleRoot, "NeedleSamplePustule_Big", PustuleType.Big, new Vector3(0.75f, -0.12f, 0f), new Vector3(0.7f, 0.7f, 1f));
    }

    private static void CreateSamplePustule(Transform parent, string name, PustuleType type, Vector3 localPosition, Vector3 localScale)
    {
        GameObject pustuleObject = new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Pustule));
        Undo.RegisterCreatedObjectUndo(pustuleObject, $"Create {name}");
        pustuleObject.transform.SetParent(parent, false);
        pustuleObject.transform.localPosition = localPosition;
        pustuleObject.transform.localRotation = Quaternion.identity;
        pustuleObject.transform.localScale = localScale;

        SpriteRenderer renderer = pustuleObject.GetComponent<SpriteRenderer>();
        renderer.sprite = LoadPlaceholderSprite();
        renderer.color = type == PustuleType.Big
            ? new Color(0.95f, 0.56f, 0.18f, 0.95f)
            : new Color(1f, 0.75f, 0.32f, 0.95f);
        renderer.sortingOrder = 32;

        CircleCollider2D collider = pustuleObject.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        Pustule pustule = pustuleObject.GetComponent<Pustule>();
        SerializedObject serializedObject = new SerializedObject(pustule);
        serializedObject.FindProperty("type").enumValueIndex = (int)type;
        serializedObject.FindProperty("targetRenderer").objectReferenceValue = renderer;
        serializedObject.FindProperty("pustuleRadius").floatValue = type == PustuleType.Big ? 0.4f : 0.32f;
        serializedObject.FindProperty("pierceTime").floatValue = type == PustuleType.Big ? 0.5f : 0.4f;
        serializedObject.FindProperty("drainSpeed").floatValue = type == PustuleType.Big ? 0.4f : 0.6f;
        serializedObject.FindProperty("painPerSecond").floatValue = type == PustuleType.Big ? 0.28f : 0.22f;
        serializedObject.FindProperty("jitterStrength").floatValue = type == PustuleType.Big ? 0.15f : 0f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureSpawnAnchors(Transform spawnRoot)
    {
        if (spawnRoot.childCount > 0)
        {
            return;
        }

        CreateAnchor(spawnRoot, "NeedleSpawnAnchor_01", new Vector3(-1.4f, 0.32f, 0f));
        CreateAnchor(spawnRoot, "NeedleSpawnAnchor_02", new Vector3(-0.65f, -0.18f, 0f));
        CreateAnchor(spawnRoot, "NeedleSpawnAnchor_03", new Vector3(0.1f, 0.22f, 0f));
        CreateAnchor(spawnRoot, "NeedleSpawnAnchor_04", new Vector3(0.8f, -0.25f, 0f));
        CreateAnchor(spawnRoot, "NeedleSpawnAnchor_05", new Vector3(1.45f, 0.16f, 0f));
    }

    private static void CreateAnchor(Transform parent, string name, Vector3 localPosition)
    {
        GameObject anchor = new GameObject(name, typeof(PustuleSpawnAnchor));
        Undo.RegisterCreatedObjectUndo(anchor, $"Create {name}");
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = localPosition;
        anchor.transform.localRotation = Quaternion.identity;
        anchor.transform.localScale = Vector3.one;
    }

    private static void EnsureSpawnAnchorComponents(Transform spawnRoot)
    {
        Transform[] anchors = GetDirectChildren(spawnRoot);
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null && anchors[i].GetComponent<PustuleSpawnAnchor>() == null)
            {
                Undo.AddComponent<PustuleSpawnAnchor>(anchors[i].gameObject);
            }
        }
    }

    private static GameObject CreateOverlayToolButton(MiniGameOverlay overlay, string objectName, string label)
    {
        Transform parent = ResolveOverlayContent(overlay);
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Toggle));
        Undo.RegisterCreatedObjectUndo(buttonObject, $"Create {objectName}");
        buttonObject.transform.SetParent(parent != null ? parent : overlay.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(96f, 36f);
        rect.anchoredPosition = new Vector2(216f, 24f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.73f, 0.18f, 0.2f, 0.9f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Toggle toggle = buttonObject.GetComponent<Toggle>();
        toggle.targetGraphic = image;
        toggle.isOn = false;

        GameObject indicatorObject = new GameObject("SelectedIndicator", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(indicatorObject, "Create SelectedIndicator");
        indicatorObject.transform.SetParent(buttonObject.transform, false);

        RectTransform indicatorRect = indicatorObject.GetComponent<RectTransform>();
        indicatorRect.anchorMin = Vector2.zero;
        indicatorRect.anchorMax = Vector2.one;
        indicatorRect.offsetMin = Vector2.zero;
        indicatorRect.offsetMax = Vector2.zero;

        Image indicatorImage = indicatorObject.GetComponent<Image>();
        indicatorImage.color = new Color(1f, 0.8f, 0.2f, 0.35f);
        indicatorObject.SetActive(false);
        toggle.graphic = indicatorImage;

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(labelObject, "Create NeedleToolButton Text");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMPro.TextMeshProUGUI text = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = Color.white;

        return buttonObject;
    }

    private static Transform ResolveOverlayContent(MiniGameOverlay overlay)
    {
        SerializedObject serializedObject = new SerializedObject(overlay);
        SerializedProperty content = serializedObject.FindProperty("content");
        return content != null && content.objectReferenceValue is GameObject contentObject
            ? contentObject.transform
            : overlay.transform;
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
        child.transform.SetParent(parent, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child.transform;
    }

    private static Transform[] GetDirectChildren(Transform parent)
    {
        Transform[] children = new Transform[parent.childCount];
        for (int i = 0; i < parent.childCount; i++)
        {
            children[i] = parent.GetChild(i);
        }

        return children;
    }

    private static T[] GetDirectChildComponents<T>(Transform parent) where T : Component
    {
        System.Collections.Generic.List<T> components = new System.Collections.Generic.List<T>();
        for (int i = 0; i < parent.childCount; i++)
        {
            T component = parent.GetChild(i).GetComponent<T>();
            if (component != null)
            {
                components.Add(component);
            }
        }

        return components.ToArray();
    }

    private static Button FindButton(Transform root, string buttonName)
    {
        Transform buttonTransform = FindDeepChild(root, buttonName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private static Toggle FindToggle(Transform root, string buttonName)
    {
        Transform buttonTransform = FindDeepChild(root, buttonName);
        return buttonTransform != null ? buttonTransform.GetComponent<Toggle>() : null;
    }

    private static GameObject FindIndicator(Transform root, string buttonName)
    {
        Transform buttonTransform = FindDeepChild(root, buttonName);
        Transform indicator = buttonTransform != null ? FindDeepChild(buttonTransform, "SelectedIndicator") : null;
        return indicator != null ? indicator.gameObject : null;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChild(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static T FindSceneObject<T>() where T : Object
    {
        T[] objects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return objects.Length > 0 ? objects[0] : null;
    }

    private static Sprite LoadPlaceholderSprite()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PlaceholderSpriteAssetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }
}
