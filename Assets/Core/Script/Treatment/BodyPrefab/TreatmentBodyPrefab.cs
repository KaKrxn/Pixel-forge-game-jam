using System.Collections.Generic;
using UnityEngine;

public sealed class TreatmentBodyPrefab : MonoBehaviour
{
    [SerializeField] private TreatmentMiniGameType miniGameType = TreatmentMiniGameType.None;
    [SerializeField] private BodyArea area = BodyArea.Arm;
    [SerializeField] private Transform gameplayRoot;
    [SerializeField] private Transform bodySpriteRoot;
    [SerializeField] private bool enforceBodySpriteSorting = true;
    [SerializeField] private string bodySortingLayerName = "Main";
    [SerializeField] private int bodySortingOrder = 20;

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

    private void Awake()
    {
        ApplyBodySpriteSorting();
    }

    public bool Matches(TreatmentMiniGameType requestedMiniGameType, BodyArea requestedArea)
    {
        return miniGameType == requestedMiniGameType && area == requestedArea;
    }

    public void ApplyBodySpriteSorting()
    {
        if (!enforceBodySpriteSorting || bodySpriteRoot == null)
        {
            return;
        }

        SpriteRenderer[] renderers = bodySpriteRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].sortingLayerName = bodySortingLayerName;
            renderers[i].sortingOrder = bodySortingOrder;
        }
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

        ApplyBodySpriteSorting();
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
