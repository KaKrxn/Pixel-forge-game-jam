using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class KnifeMiniGameSetup
{
    private const string PlaceholderSpriteAssetPath = "Assets/Core/Data/Treatment/Tongs/TongsParasitePlaceholder.asset";

    [MenuItem("Tools/Pixel Forge/Treatment/Setup Knife MiniGame In Open Scene")]
    public static void SetupKnifeMiniGameInOpenScene()
    {
        KnifeMiniGame miniGame = FindSelectedOrSceneMiniGame();
        if (miniGame == null)
        {
            miniGame = CreateKnifeMiniGameRoot();
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Transform lesionRoot = FindOrCreateChild(miniGame.transform, "LesionRoot");
        Transform spawnRoot = FindOrCreateChild(miniGame.transform, "Spawn Anchor Manger");
        EnsureSampleLesions(lesionRoot);
        EnsureSpawnAnchors(spawnRoot);

        AssignMiniGame(miniGame, lesionRoot, spawnRoot);
        AssignAnatomyController(miniGame);
        AssignSharedOverlay(miniGame);

        EditorUtility.SetDirty(miniGame);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = miniGame.gameObject;
        Debug.Log("Knife mini game setup complete. Sample lesions and spawn anchors are ready to reposition.");
    }

    private static KnifeMiniGame FindSelectedOrSceneMiniGame()
    {
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            GameObject selected = Selection.gameObjects[i];
            if (selected == null)
            {
                continue;
            }

            KnifeMiniGame inParent = selected.GetComponentInParent<KnifeMiniGame>(true);
            if (inParent != null)
            {
                return inParent;
            }

            KnifeMiniGame inChildren = selected.GetComponentInChildren<KnifeMiniGame>(true);
            if (inChildren != null)
            {
                return inChildren;
            }
        }

        return FindSceneObject<KnifeMiniGame>();
    }

    private static KnifeMiniGame CreateKnifeMiniGameRoot()
    {
        GameObject root = new GameObject("KnifeMiniGameRoot");
        Undo.RegisterCreatedObjectUndo(root, "Create KnifeMiniGameRoot");

        Transform treatmentRoot = FindSceneTransform("Treatment RoomRoot");
        if (treatmentRoot != null)
        {
            Transform gameplayRoot = FindDeepChild(treatmentRoot, "03_Gameplay");
            root.transform.SetParent(gameplayRoot != null ? gameplayRoot : treatmentRoot, false);
        }

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return Undo.AddComponent<KnifeMiniGame>(root);
    }

    private static void AssignMiniGame(KnifeMiniGame miniGame, Transform lesionRoot, Transform spawnRoot)
    {
        SerializedObject serializedObject = new SerializedObject(miniGame);
        serializedObject.FindProperty("flow").objectReferenceValue = FindSceneObject<GameFlow>();
        serializedObject.FindProperty("root").objectReferenceValue = miniGame.gameObject;
        serializedObject.FindProperty("lesionRoot").objectReferenceValue = lesionRoot;
        serializedObject.FindProperty("inputCamera").objectReferenceValue = Camera.main;
        serializedObject.FindProperty("worldInputPlaneZ").floatValue = 0f;
        serializedObject.FindProperty("overlay").objectReferenceValue = FindSceneObject<MiniGameOverlay>();
        serializedObject.FindProperty("overlayToolId").stringValue = "Knife";
        serializedObject.FindProperty("spawnLesionsOnBegin").boolValue = false;
        serializedObject.FindProperty("autoCompleteWhenAllLesionsDone").boolValue = true;
        serializedObject.FindProperty("startHidden").boolValue = true;

        Lesion[] lesions = lesionRoot.GetComponentsInChildren<Lesion>(true);
        SerializedProperty lesionList = serializedObject.FindProperty("lesions");
        lesionList.ClearArray();
        for (int i = 0; i < lesions.Length; i++)
        {
            lesionList.InsertArrayElementAtIndex(i);
            lesionList.GetArrayElementAtIndex(i).objectReferenceValue = lesions[i];
        }

        Transform[] anchors = GetDirectChildren(spawnRoot);
        SerializedProperty spawnAnchors = serializedObject.FindProperty("spawnAnchors");
        spawnAnchors.ClearArray();
        for (int i = 0; i < anchors.Length; i++)
        {
            spawnAnchors.InsertArrayElementAtIndex(i);
            spawnAnchors.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignAnatomyController(KnifeMiniGame miniGame)
    {
        AnatomyController anatomyController = FindSceneObject<AnatomyController>();
        if (anatomyController == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(anatomyController);
        serializedObject.FindProperty("knifeMiniGame").objectReferenceValue = miniGame;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(anatomyController);
    }

    private static void AssignSharedOverlay(KnifeMiniGame miniGame)
    {
        MiniGameOverlay overlay = FindSceneObject<MiniGameOverlay>();
        if (overlay == null)
        {
            return;
        }

        SerializedObject miniGameObject = new SerializedObject(miniGame);
        miniGameObject.FindProperty("overlay").objectReferenceValue = overlay;
        miniGameObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject overlayObject = new SerializedObject(overlay);
        SerializedProperty toolButtons = overlayObject.FindProperty("toolButtons");
        EnsureToolButton(toolButtons, "Tongs", FindToggle(overlay.transform, "TongsToolButton"), FindButton(overlay.transform, "TongsToolButton"), FindIndicator(overlay.transform, "TongsToolButton"));
        EnsureToolButton(toolButtons, "Knife", FindToggle(overlay.transform, "KnifeToolButton"), FindButton(overlay.transform, "KnifeToolButton"), FindIndicator(overlay.transform, "KnifeToolButton"));
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

    private static void EnsureSampleLesions(Transform lesionRoot)
    {
        if (lesionRoot.GetComponentInChildren<Lesion>(true) != null)
        {
            return;
        }

        CreateSampleLesion(lesionRoot, "KnifeSampleLesion_Tumor", LesionType.Tumor, new Vector3(-0.9f, 0.15f, 0f), new Vector3(1.3f, 0.55f, 1f));
        CreateSampleLesion(lesionRoot, "KnifeSampleLesion_Bulge", LesionType.Bulge, new Vector3(0.9f, -0.2f, 0f), new Vector3(1.1f, 0.65f, 1f));
    }

    private static void CreateSampleLesion(Transform parent, string name, LesionType type, Vector3 localPosition, Vector3 localScale)
    {
        GameObject lesionObject = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Lesion));
        Undo.RegisterCreatedObjectUndo(lesionObject, $"Create {name}");
        lesionObject.transform.SetParent(parent, false);
        lesionObject.transform.localPosition = localPosition;
        lesionObject.transform.localRotation = Quaternion.identity;
        lesionObject.transform.localScale = localScale;

        SpriteRenderer renderer = lesionObject.GetComponent<SpriteRenderer>();
        renderer.sprite = LoadPlaceholderSprite();
        renderer.color = type == LesionType.Bulge
            ? new Color(0.95f, 0.35f, 0.38f, 0.95f)
            : new Color(0.8f, 0.16f, 0.2f, 0.95f);
        renderer.sortingOrder = 30;

        BoxCollider2D collider = lesionObject.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.9f, 0.45f);

        Lesion lesion = lesionObject.GetComponent<Lesion>();
        SerializedObject serializedObject = new SerializedObject(lesion);
        serializedObject.FindProperty("type").enumValueIndex = (int)type;
        serializedObject.FindProperty("targetRenderer").objectReferenceValue = renderer;
        serializedObject.FindProperty("pathHalfWidth").floatValue = 0.28f;
        serializedObject.FindProperty("startRadius").floatValue = 0.45f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureSpawnAnchors(Transform spawnRoot)
    {
        if (spawnRoot.childCount > 0)
        {
            return;
        }

        CreateAnchor(spawnRoot, "KnifeSpawnAnchor_01", new Vector3(-1.2f, 0.25f, 0f));
        CreateAnchor(spawnRoot, "KnifeSpawnAnchor_02", new Vector3(0f, -0.05f, 0f));
        CreateAnchor(spawnRoot, "KnifeSpawnAnchor_03", new Vector3(1.2f, 0.15f, 0f));
    }

    private static void CreateAnchor(Transform parent, string name, Vector3 localPosition)
    {
        GameObject anchor = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(anchor, $"Create {name}");
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = localPosition;
        anchor.transform.localRotation = Quaternion.identity;
        anchor.transform.localScale = Vector3.one;
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

    private static Button FindButton(Transform root, string buttonName)
    {
        Transform buttonTransform = FindDeepChild(root, buttonName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private static Toggle FindToggle(Transform root, string buttonName)
    {
        Transform buttonTransform = FindDeepChild(root, buttonName);
        if (buttonTransform == null)
        {
            return null;
        }

        Toggle toggle = buttonTransform.GetComponent<Toggle>();
        if (toggle != null)
        {
            return toggle;
        }

        return null;
    }

    private static GameObject FindIndicator(Transform root, string buttonName)
    {
        Transform buttonTransform = FindDeepChild(root, buttonName);
        Transform indicator = buttonTransform != null ? FindDeepChild(buttonTransform, "SelectedIndicator") : null;
        return indicator != null ? indicator.gameObject : null;
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

    private static Transform FindSceneTransform(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
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
