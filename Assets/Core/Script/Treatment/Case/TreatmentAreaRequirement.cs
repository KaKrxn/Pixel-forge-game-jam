using System;
using UnityEngine;

[Serializable]
public sealed class TreatmentAreaRequirement
{
    [SerializeField] private BodyArea area;
    [SerializeField] private TreatmentMiniGameType miniGameType = TreatmentMiniGameType.None;
    [SerializeField, TextArea(2, 4)] private string designerNote;

    public BodyArea Area => area;
    public TreatmentMiniGameType MiniGameType => miniGameType;
    public string DesignerNote => designerNote ?? string.Empty;
    public bool HasMiniGame => miniGameType != TreatmentMiniGameType.None;
}
