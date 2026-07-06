using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LesionSpawnAnchor : MonoBehaviour
{
    [Serializable]
    private sealed class LesionSpawnVisualVariant
    {
        [SerializeField] private LesionType lesionType = LesionType.Tumor;
        [SerializeField] private LesionCutOrientation orientation = LesionCutOrientation.Horizontal;
        [SerializeField] private GameObject visualRoot;

        public bool Matches(LesionType requestedType, LesionCutOrientation requestedOrientation)
        {
            return lesionType == requestedType && orientation == requestedOrientation;
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
    [Header("Allowed Lesions")]
    [SerializeField] private bool allowTumor = true;
    [SerializeField] private bool allowBulge = true;
    [Header("Allowed Cut Directions")]
    [SerializeField] private bool allowHorizontal = true;
    [SerializeField] private bool allowVertical = true;
    [Header("Visual")]
    [SerializeField] private GameObject defaultVisualRoot;
    [SerializeField] private List<LesionSpawnVisualVariant> visualVariants = new List<LesionSpawnVisualVariant>();
    [Header("Editor Preview")]
    [Tooltip("Footprint drawn as a gizmo to preview lesion coverage per allowed orientation (green=horizontal, cyan=vertical).")]
    [SerializeField] private Vector2 previewFootprint = new Vector2(0.85f, 0.38f);
    [SerializeField, Min(0f)] private float previewPadding = 0.08f;

    public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;
    public bool HasAllowedLesionType => allowTumor || allowBulge;
    public bool HasAllowedOrientation => allowHorizontal || allowVertical;
    public bool IsUsable => SpawnPoint != null && HasAllowedLesionType && HasAllowedOrientation;

    public bool TryGetRandomLesionType(out LesionType lesionType)
    {
        int count = (allowTumor ? 1 : 0) + (allowBulge ? 1 : 0);
        if (count <= 0)
        {
            lesionType = LesionType.Tumor;
            return false;
        }

        int choice = UnityEngine.Random.Range(0, count);
        if (allowTumor && choice-- == 0)
        {
            lesionType = LesionType.Tumor;
            return true;
        }

        lesionType = LesionType.Bulge;
        return true;
    }

    public bool TryGetRandomOrientation(out LesionCutOrientation orientation)
    {
        int count = (allowHorizontal ? 1 : 0) + (allowVertical ? 1 : 0);
        if (count <= 0)
        {
            orientation = LesionCutOrientation.Horizontal;
            return false;
        }

        int choice = UnityEngine.Random.Range(0, count);
        if (allowHorizontal && choice-- == 0)
        {
            orientation = LesionCutOrientation.Horizontal;
            return true;
        }

        orientation = LesionCutOrientation.Vertical;
        return true;
    }

    public Quaternion GetSpawnRotation(LesionCutOrientation orientation)
    {
        Quaternion baseRotation = SpawnPoint != null ? SpawnPoint.rotation : transform.rotation;
        return orientation == LesionCutOrientation.Vertical
            ? baseRotation * Quaternion.Euler(0f, 0f, 90f)
            : baseRotation;
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

    public void ShowSpawnVisual(LesionType lesionType, LesionCutOrientation orientation)
    {
        HideSpawnVisuals();

        if (visualVariants != null)
        {
            for (int i = 0; i < visualVariants.Count; i++)
            {
                LesionSpawnVisualVariant variant = visualVariants[i];
                if (variant != null && variant.Matches(lesionType, orientation))
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = SpawnPoint != null ? SpawnPoint.position : transform.position;
        if (allowHorizontal)
        {
            DrawFootprintGizmo(center, GetSpawnRotation(LesionCutOrientation.Horizontal), Color.green);
        }

        if (allowVertical)
        {
            DrawFootprintGizmo(center, GetSpawnRotation(LesionCutOrientation.Vertical), Color.cyan);
        }
    }

    private void DrawFootprintGizmo(Vector3 center, Quaternion rotation, Color color)
    {
        Vector2 half = previewFootprint * 0.5f + Vector2.one * previewPadding;
        Vector3 right = rotation * Vector3.right * half.x;
        Vector3 up = rotation * Vector3.up * half.y;
        Vector3 topRight = center + right + up;
        Vector3 bottomRight = center + right - up;
        Vector3 bottomLeft = center - right - up;
        Vector3 topLeft = center - right + up;

        Gizmos.color = color;
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
    }
#endif
}
