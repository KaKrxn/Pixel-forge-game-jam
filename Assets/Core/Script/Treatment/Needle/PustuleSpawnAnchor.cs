using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PustuleSpawnAnchor : MonoBehaviour
{
    [Serializable]
    private sealed class PustuleSpawnVisualVariant
    {
        [SerializeField] private PustuleType pustuleType = PustuleType.Small;
        [SerializeField] private GameObject visualRoot;

        public bool Matches(PustuleType requestedType)
        {
            return pustuleType == requestedType;
        }

        public void SetVisible(bool visible)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(visible);
            }
        }
    }

    [SerializeField] private Transform spawnPoint;
    [Header("Allowed Pustules")]
    [SerializeField] private bool allowSmall = true;
    [SerializeField] private bool allowBig = true;
    [Header("Optional Overrides")]
    [SerializeField] private bool overrideRadius;
    [SerializeField, Min(0.01f)] private float radiusOverride = 0.4f;
    [Header("Visual")]
    [SerializeField] private GameObject defaultVisualRoot;
    [SerializeField] private List<PustuleSpawnVisualVariant> visualVariants = new List<PustuleSpawnVisualVariant>();

    public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;
    public bool HasAllowedPustuleType => allowSmall || allowBig;
    public bool IsUsable => SpawnPoint != null && HasAllowedPustuleType;

    public bool TryGetRandomPustuleType(out PustuleType pustuleType)
    {
        int count = (allowSmall ? 1 : 0) + (allowBig ? 1 : 0);
        if (count <= 0)
        {
            pustuleType = PustuleType.Small;
            return false;
        }

        int choice = UnityEngine.Random.Range(0, count);
        if (allowSmall && choice-- == 0)
        {
            pustuleType = PustuleType.Small;
            return true;
        }

        pustuleType = PustuleType.Big;
        return true;
    }

    public bool TryGetRadiusOverride(out float radius)
    {
        radius = radiusOverride;
        return overrideRadius && radiusOverride > 0f;
    }

    public void HideSpawnVisuals()
    {
        if (defaultVisualRoot != null)
        {
            defaultVisualRoot.SetActive(false);
        }

        if (visualVariants == null)
        {
            return;
        }

        for (int i = 0; i < visualVariants.Count; i++)
        {
            visualVariants[i]?.SetVisible(false);
        }
    }

    public void ShowSpawnVisual(PustuleType pustuleType)
    {
        HideSpawnVisuals();

        if (visualVariants != null)
        {
            for (int i = 0; i < visualVariants.Count; i++)
            {
                PustuleSpawnVisualVariant variant = visualVariants[i];
                if (variant != null && variant.Matches(pustuleType))
                {
                    variant.SetVisible(true);
                    return;
                }
            }
        }

        if (defaultVisualRoot != null)
        {
            defaultVisualRoot.SetActive(true);
        }
    }
}
