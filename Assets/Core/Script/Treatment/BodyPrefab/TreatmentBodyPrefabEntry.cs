using System;
using UnityEngine;

[Serializable]
public sealed class TreatmentBodyPrefabEntry
{
    [SerializeField] private TreatmentMiniGameType miniGameType = TreatmentMiniGameType.None;
    [SerializeField] private BodyArea area = BodyArea.Arm;
    [SerializeField] private TreatmentBodyPrefab prefab;

    public TreatmentMiniGameType MiniGameType => miniGameType;
    public BodyArea Area => area;
    public TreatmentBodyPrefab Prefab => prefab;
    public bool IsValid => miniGameType != TreatmentMiniGameType.None && prefab != null;

    public bool Matches(TreatmentMiniGameType requestedMiniGameType, BodyArea requestedArea)
    {
        return miniGameType == requestedMiniGameType && area == requestedArea;
    }
}
