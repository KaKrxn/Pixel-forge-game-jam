using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class TreatmentBodyPrefabSpawner : MonoBehaviour
{
    [SerializeField] private TreatmentBodyPrefabCatalog catalog;
    [SerializeField] private Transform bodyParent;
    [SerializeField] private bool logWarnings = true;
    [SerializeField] private bool debugTreatmentFlow = true;

    private TreatmentBodyPrefab activeBody;

    public TreatmentBodyPrefab ActiveBody => activeBody;

    public bool TrySpawn(TreatmentMiniGameType miniGameType, BodyArea area, out TreatmentBodyPrefab body)
    {
        LogDebug($"TrySpawn request miniGameType={miniGameType} area={area} catalog={DescribeObject(catalog)} bodyParent={DescribeObject(bodyParent)} activeBodyBefore={DescribeBody(activeBody)}");
        ClearActiveBody();

        if (catalog == null)
        {
            LogWarning("No TreatmentBodyPrefabCatalog assigned. Existing mini game scene references will be used.");
            body = null;
            return false;
        }

        if (!catalog.TryGetPrefab(miniGameType, area, out TreatmentBodyPrefab prefab) || prefab == null)
        {
            LogWarning($"No body prefab entry found for {miniGameType} + {area}. Existing mini game scene references will be used.");
            LogDebug($"TrySpawn failed: catalog has no prefab for {miniGameType}/{area}");
            body = null;
            return false;
        }

        LogDebug($"TrySpawn catalog prefab={DescribeBody(prefab)} prefabAssetName={prefab.name}");
        CleanupInactiveBodyPrefabs();
        Transform parent = bodyParent != null ? bodyParent : transform;
        activeBody = Instantiate(prefab, parent);
        activeBody.name = $"{miniGameType}_{area}_Body";
        activeBody.ApplyBodySpriteSorting();
        CleanupInactiveBodyPrefabs(activeBody);
        body = activeBody;
        LogDebug($"TrySpawn spawned activeBody={DescribeBody(activeBody)} parent={DescribeObject(parent)}");

        if (!activeBody.Matches(miniGameType, area))
        {
            LogWarning($"Spawned body '{activeBody.name}' is cataloged for {miniGameType} + {area}, but its component says {activeBody.MiniGameType} + {activeBody.Area}.");
        }

        return body != null;
    }

    public void ClearActiveBody()
    {
        LogDebug($"ClearActiveBody activeBody={DescribeBody(activeBody)}");
        if (activeBody == null)
        {
            CleanupInactiveBodyPrefabs();
            return;
        }

        DestroyBody(activeBody);

        activeBody = null;
        CleanupInactiveBodyPrefabs();
    }

    private void CleanupInactiveBodyPrefabs(TreatmentBodyPrefab visibleBody = null)
    {
        Transform parent = bodyParent != null ? bodyParent : transform;
        TreatmentBodyPrefab[] bodies = parent.GetComponentsInChildren<TreatmentBodyPrefab>(true);
        LogDebug($"CleanupInactiveBodyPrefabs parent={DescribeObject(parent)} visibleBody={DescribeBody(visibleBody)} found={bodies.Length}");
        for (int i = 0; i < bodies.Length; i++)
        {
            TreatmentBodyPrefab body = bodies[i];
            if (body == null || body == visibleBody)
            {
                continue;
            }

            if (IsRuntimeBodyInstance(body))
            {
                LogDebug($"CleanupInactiveBodyPrefabs destroying stale runtime body {DescribeBody(body)}");
                DestroyBody(body);
                continue;
            }

            body.gameObject.SetActive(false);
            LogDebug($"CleanupInactiveBodyPrefabs disabled scene body {DescribeBody(body)}");
        }
    }

    private static bool IsRuntimeBodyInstance(TreatmentBodyPrefab body)
    {
        if (body == null)
        {
            return false;
        }

        return body.name == $"{body.MiniGameType}_{body.Area}_Body" || body.name.EndsWith("(Clone)");
    }

    private static void DestroyBody(TreatmentBodyPrefab body)
    {
        if (body == null)
        {
            return;
        }

        GameObject target = body.gameObject;
#if UNITY_EDITOR
        ClearEditorSelectionIfInside(target);
#endif
        target.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private void LogWarning(string message)
    {
        if (logWarnings)
        {
            Debug.LogWarning($"[TreatmentBodyPrefabSpawner] {message}", this);
        }
    }

#if UNITY_EDITOR
    private static void ClearEditorSelectionIfInside(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            GameObject selectedObject = selectedObjects[i];
            if (selectedObject == null)
            {
                continue;
            }

            if (selectedObject == target || selectedObject.transform.IsChildOf(target.transform))
            {
                Selection.activeObject = null;
                return;
            }
        }
    }
#endif

    private void LogDebug(string message)
    {
        if (debugTreatmentFlow)
        {
            Debug.Log($"[TreatmentFlow][BodyPrefabSpawner] {message}", this);
        }
    }

    private static string DescribeObject(Object target)
    {
        if (target == null)
        {
            return "null";
        }

        return $"{target.name} ({target.GetType().Name})";
    }

    private static string DescribeBody(TreatmentBodyPrefab body)
    {
        if (body == null)
        {
            return "null";
        }

        return $"{body.name} ({body.MiniGameType}/{body.Area}, active={body.gameObject.activeSelf})";
    }
}
