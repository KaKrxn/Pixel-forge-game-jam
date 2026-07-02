using UnityEngine;

public sealed class TreatmentBodyPrefabSpawner : MonoBehaviour
{
    [SerializeField] private TreatmentBodyPrefabCatalog catalog;
    [SerializeField] private Transform bodyParent;
    [SerializeField] private bool logWarnings = true;

    private TreatmentBodyPrefab activeBody;

    public TreatmentBodyPrefab ActiveBody => activeBody;

    public bool TrySpawn(TreatmentMiniGameType miniGameType, BodyArea area, out TreatmentBodyPrefab body)
    {
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
            body = null;
            return false;
        }

        Transform parent = bodyParent != null ? bodyParent : transform;
        activeBody = Instantiate(prefab, parent);
        activeBody.name = $"{miniGameType}_{area}_Body";
        body = activeBody;

        if (!activeBody.Matches(miniGameType, area))
        {
            LogWarning($"Spawned body '{activeBody.name}' is cataloged for {miniGameType} + {area}, but its component says {activeBody.MiniGameType} + {activeBody.Area}.");
        }

        return body != null;
    }

    public void ClearActiveBody()
    {
        if (activeBody == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(activeBody.gameObject);
        }
        else
        {
            DestroyImmediate(activeBody.gameObject);
        }

        activeBody = null;
    }

    private void LogWarning(string message)
    {
        if (logWarnings)
        {
            Debug.LogWarning($"[TreatmentBodyPrefabSpawner] {message}", this);
        }
    }
}
