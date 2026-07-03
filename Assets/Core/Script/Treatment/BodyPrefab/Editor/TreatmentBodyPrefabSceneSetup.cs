using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TreatmentBodyPrefabSceneSetup
{
    private const string CatalogPath = "Assets/Core/Data/Treatment/BodyPrefabs/TreatmentBodyPrefabCatalog.asset";

    [MenuItem("Tools/Pixel Forge/Treatment/Setup Dynamic Body Prefab Spawner In Open Scene")]
    public static void SetupDynamicBodyPrefabSpawnerInOpenScene()
    {
        AnatomyController anatomy = FindSelectedOrSceneAnatomy();
        if (anatomy == null)
        {
            Debug.LogWarning("Body prefab spawner setup skipped because no AnatomyController was selected or found in the open scene.");
            return;
        }

        TreatmentBodyPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<TreatmentBodyPrefabCatalog>(CatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning($"Body prefab spawner setup skipped because the catalog asset was not found at {CatalogPath}.");
            return;
        }

        TreatmentBodyPrefabSpawner spawner = anatomy.GetComponent<TreatmentBodyPrefabSpawner>();
        if (spawner == null)
        {
            spawner = Undo.AddComponent<TreatmentBodyPrefabSpawner>(anatomy.gameObject);
        }

        Transform bodyParent = ResolveBodyParent(anatomy);
        AssignSpawner(spawner, catalog, bodyParent);
        int disabledBodies = DisableSceneAuthoredBodyPrefabs(spawner.gameObject.scene);

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Selection.activeGameObject = anatomy.gameObject;

        string parentName = bodyParent != null ? bodyParent.name : anatomy.name;
        Debug.Log($"Configured {nameof(TreatmentBodyPrefabSpawner)} on {anatomy.name}. Body parent: {parentName}. Disabled static body prefabs: {disabledBodies}.");
    }

    private static void AssignSpawner(TreatmentBodyPrefabSpawner spawner, TreatmentBodyPrefabCatalog catalog, Transform bodyParent)
    {
        SerializedObject serializedObject = new SerializedObject(spawner);
        serializedObject.FindProperty("catalog").objectReferenceValue = catalog;
        serializedObject.FindProperty("bodyParent").objectReferenceValue = bodyParent;
        serializedObject.FindProperty("logWarnings").boolValue = true;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AnatomyController FindSelectedOrSceneAnatomy()
    {
        for (int i = 0; i < Selection.gameObjects.Length; i++)
        {
            GameObject selected = Selection.gameObjects[i];
            if (selected == null)
            {
                continue;
            }

            AnatomyController inParent = selected.GetComponentInParent<AnatomyController>(true);
            if (inParent != null)
            {
                return inParent;
            }

            AnatomyController inChildren = selected.GetComponentInChildren<AnatomyController>(true);
            if (inChildren != null)
            {
                return inChildren;
            }
        }

        AnatomyController[] anatomies = Object.FindObjectsByType<AnatomyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return anatomies.Length > 0 ? anatomies[0] : null;
    }

    private static Transform ResolveBodyParent(AnatomyController anatomy)
    {
        Transform tongsRootParent = ResolveTongsRootParent(anatomy.gameObject.scene);
        if (tongsRootParent != null)
        {
            return tongsRootParent;
        }

        TreatmentBodyPrefab[] bodies = Object.FindObjectsByType<TreatmentBodyPrefab>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bodies.Length; i++)
        {
            if (IsSceneObjectInSameScene(bodies[i], anatomy.gameObject.scene) && bodies[i].transform.parent != null)
            {
                return bodies[i].transform.parent;
            }
        }

        Transform gameplayRoot = FindSceneTransformByName(anatomy.gameObject.scene, "03_Gameplay");
        if (gameplayRoot != null)
        {
            return gameplayRoot;
        }

        return anatomy.transform.parent != null ? anatomy.transform.parent : anatomy.transform;
    }

    private static Transform ResolveTongsRootParent(Scene scene)
    {
        TongsMiniGame[] tongsMiniGames = Object.FindObjectsByType<TongsMiniGame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tongsMiniGames.Length; i++)
        {
            TongsMiniGame tongsMiniGame = tongsMiniGames[i];
            if (tongsMiniGame == null || tongsMiniGame.gameObject.scene != scene)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(tongsMiniGame);
            GameObject miniGameRoot = serializedObject.FindProperty("root").objectReferenceValue as GameObject;
            if (miniGameRoot != null && miniGameRoot.scene == scene && miniGameRoot.transform.parent != null)
            {
                return miniGameRoot.transform.parent;
            }
        }

        return null;
    }

    private static int DisableSceneAuthoredBodyPrefabs(Scene scene)
    {
        int disabled = 0;
        TreatmentBodyPrefab[] bodies = Object.FindObjectsByType<TreatmentBodyPrefab>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bodies.Length; i++)
        {
            TreatmentBodyPrefab body = bodies[i];
            if (!IsSceneObjectInSameScene(body, scene) || !body.gameObject.activeSelf)
            {
                continue;
            }

            if (HostsMiniGame(body))
            {
                continue;
            }

            Undo.RecordObject(body.gameObject, "Disable static treatment body prefab");
            body.gameObject.SetActive(false);
            EditorUtility.SetDirty(body.gameObject);
            disabled++;
        }

        return disabled;
    }

    private static Transform FindSceneTransformByName(Scene scene, string transformName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && current.name == transformName && current.gameObject.scene == scene)
            {
                return current;
            }
        }

        return null;
    }

    private static bool HostsMiniGame(TreatmentBodyPrefab body)
    {
        return body != null
            && (body.GetComponentInChildren<TongsMiniGame>(true) != null
                || body.GetComponentInChildren<KnifeMiniGame>(true) != null
                || body.GetComponentInChildren<NeedleMiniGame>(true) != null);
    }

    private static bool IsSceneObjectInSameScene(Component component, Scene scene)
    {
        return component != null
            && component.gameObject.scene == scene
            && !PrefabUtility.IsPartOfPrefabAsset(component.gameObject);
    }
}
