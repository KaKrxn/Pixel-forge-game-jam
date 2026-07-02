using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class CutGuideLine : MonoBehaviour
{
    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 40;

    [Header("Line Look")]
    [SerializeField, Min(0.001f)] private float lineWidth = 0.06f;
    [SerializeField, Min(0.01f)] private float dotsPerUnit = 4f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private Material lineMaterial;

    private LineRenderer line;

    private void Awake()
    {
        ResolveLine();
        ApplySettings();
    }

    private void OnValidate()
    {
        ResolveLine();
        ApplySettings();
    }

    public void ShowRemaining(IReadOnlyList<Vector2> path, float progress)
    {
        ResolveLine();
        ApplySettings();

        if (path == null || path.Count < 2 || progress >= 1f)
        {
            Hide();
            return;
        }

        float clampedProgress = Mathf.Clamp01(progress);
        float pathLength = GetPathLength(path);
        float cutDistance = pathLength * clampedProgress;
        float traversed = 0f;
        int firstSegmentIndex = 0;
        Vector2 startPoint = path[0];

        for (int i = 0; i < path.Count - 1; i++)
        {
            float segmentLength = Vector2.Distance(path[i], path[i + 1]);
            if (traversed + segmentLength >= cutDistance)
            {
                float segmentT = segmentLength <= 0f ? 0f : (cutDistance - traversed) / segmentLength;
                startPoint = Vector2.Lerp(path[i], path[i + 1], Mathf.Clamp01(segmentT));
                firstSegmentIndex = i + 1;
                break;
            }

            traversed += segmentLength;
        }

        int count = path.Count - firstSegmentIndex + 1;
        if (count < 2)
        {
            Hide();
            return;
        }

        line.positionCount = count;
        line.SetPosition(0, new Vector3(startPoint.x, startPoint.y, 0f));
        for (int i = 1; i < count; i++)
        {
            Vector2 point = path[firstSegmentIndex + i - 1];
            line.SetPosition(i, new Vector3(point.x, point.y, 0f));
        }

        ApplyTextureScale(GetRenderedLength());
    }

    public void ShowRemaining(IReadOnlyList<Vector2> path, int startIndex)
    {
        if (path == null || path.Count < 2)
        {
            Hide();
            return;
        }

        float progress = Mathf.Clamp01(startIndex / Mathf.Max(1f, path.Count - 1f));
        ShowRemaining(path, progress);
    }

    public void Hide()
    {
        ResolveLine();
        if (line != null)
        {
            line.positionCount = 0;
        }
    }

    private void ResolveLine()
    {
        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }
    }

    private void ApplySettings()
    {
        if (line == null)
        {
            return;
        }

        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = sortingOrder;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = lineColor;
        line.endColor = lineColor;
        line.textureMode = LineTextureMode.Tile;
        line.alignment = LineAlignment.View;
        line.useWorldSpace = true;

        if (lineMaterial != null)
        {
            line.sharedMaterial = lineMaterial;
        }
    }

    private void ApplyTextureScale(float length)
    {
        if (line == null || line.sharedMaterial == null)
        {
            return;
        }

        line.sharedMaterial.mainTextureScale = new Vector2(length * dotsPerUnit, 1f);
    }

    private float GetRenderedLength()
    {
        if (line == null || line.positionCount < 2)
        {
            return 0f;
        }

        float length = 0f;
        for (int i = 0; i < line.positionCount - 1; i++)
        {
            length += Vector3.Distance(line.GetPosition(i), line.GetPosition(i + 1));
        }

        return length;
    }

    private static float GetPathLength(IReadOnlyList<Vector2> path)
    {
        float length = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            length += Vector2.Distance(path[i], path[i + 1]);
        }

        return length;
    }
}
