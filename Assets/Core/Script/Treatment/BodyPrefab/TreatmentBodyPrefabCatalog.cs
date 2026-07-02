using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TreatmentBodyPrefabCatalog", menuName = "Pixel Forge/Treatment/Body Prefab Catalog")]
public sealed class TreatmentBodyPrefabCatalog : ScriptableObject
{
    [SerializeField] private List<TreatmentBodyPrefabEntry> entries = new List<TreatmentBodyPrefabEntry>();

    public IReadOnlyList<TreatmentBodyPrefabEntry> Entries => entries;

    public bool TryGetPrefab(TreatmentMiniGameType miniGameType, BodyArea area, out TreatmentBodyPrefab prefab)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                TreatmentBodyPrefabEntry entry = entries[i];
                if (entry != null && entry.IsValid && entry.Matches(miniGameType, area))
                {
                    prefab = entry.Prefab;
                    return true;
                }
            }
        }

        prefab = null;
        return false;
    }

    public bool HasEntry(TreatmentMiniGameType miniGameType, BodyArea area)
    {
        return TryGetPrefab(miniGameType, area, out _);
    }
}
