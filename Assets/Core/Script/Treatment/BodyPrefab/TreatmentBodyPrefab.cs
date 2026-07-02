using System.Collections.Generic;
using UnityEngine;

public sealed class TreatmentBodyPrefab : MonoBehaviour
{
    [SerializeField] private TreatmentMiniGameType miniGameType = TreatmentMiniGameType.None;
    [SerializeField] private BodyArea area = BodyArea.Arm;
    [SerializeField] private Transform gameplayRoot;
    [SerializeField] private Transform bodySpriteRoot;

    [Header("Tongs")]
    [SerializeField] private Transform parasiteRoot;
    [SerializeField] private List<Transform> parasiteSpawnAnchors = new List<Transform>();

    [Header("Knife")]
    [SerializeField] private Transform lesionRoot;
    [SerializeField] private List<Transform> lesionSpawnAnchors = new List<Transform>();

    [Header("Needle")]
    [SerializeField] private Transform pustuleRoot;
    [SerializeField] private List<Transform> pustuleSpawnAnchors = new List<Transform>();

    public TreatmentMiniGameType MiniGameType => miniGameType;
    public BodyArea Area => area;
    public Transform GameplayRoot => gameplayRoot != null ? gameplayRoot : transform;
    public Transform BodySpriteRoot => bodySpriteRoot;
    public Transform ParasiteRoot => parasiteRoot;
    public IReadOnlyList<Transform> ParasiteSpawnAnchors => parasiteSpawnAnchors;
    public Transform LesionRoot => lesionRoot;
    public IReadOnlyList<Transform> LesionSpawnAnchors => lesionSpawnAnchors;
    public Transform PustuleRoot => pustuleRoot;
    public IReadOnlyList<Transform> PustuleSpawnAnchors => pustuleSpawnAnchors;

    public bool Matches(TreatmentMiniGameType requestedMiniGameType, BodyArea requestedArea)
    {
        return miniGameType == requestedMiniGameType && area == requestedArea;
    }

    public List<Transform> GetParasiteAnchorTransforms()
    {
        return CopyValidTransforms(parasiteSpawnAnchors);
    }

    public List<Transform> GetLesionAnchorTransforms()
    {
        return CopyValidTransforms(lesionSpawnAnchors);
    }

    public List<Transform> GetPustuleAnchorTransforms()
    {
        return CopyValidTransforms(pustuleSpawnAnchors);
    }

    private void Reset()
    {
        gameplayRoot = transform;
    }

    private void OnValidate()
    {
        if (gameplayRoot == null)
        {
            gameplayRoot = transform;
        }
    }

    private static List<Transform> CopyValidTransforms(IReadOnlyList<Transform> source)
    {
        List<Transform> result = new List<Transform>();
        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null && !result.Contains(source[i]))
            {
                result.Add(source[i]);
            }
        }

        return result;
    }
}
