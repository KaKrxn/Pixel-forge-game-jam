using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Lesion : MonoBehaviour
{
    [SerializeField] private LesionType type = LesionType.Tumor;
    [SerializeField] private List<Vector2> cutPath = new List<Vector2>
    {
        new Vector2(-0.6f, -0.25f),
        new Vector2(-0.25f, 0.18f),
        new Vector2(0.25f, 0.18f),
        new Vector2(0.6f, -0.25f)
    };
    [SerializeField] private bool pathIsLocal = true;
    [SerializeField, Min(0.01f)] private float pathHalfWidth = 0.3f;
    [SerializeField, Min(0f)] private float startRadius = 0.45f;
    [SerializeField, Min(0f)] private float rollbackPerSecond = 0.35f;
    [SerializeField, Min(0f)] private float strayPenaltyPerSecond = 8f;
    [SerializeField, Min(0f)] private float painPerSecond = 0.26f;
    [SerializeField, Min(0f)] private float painDrainRate = 0.4f;
    [SerializeField, Min(0f)] private float painSpikeAmount = 25f;
    [SerializeField, Min(0f)] private float jitterStrength;
    [SerializeField, Min(0f)] private float jitterFrequency = 2f;
    [SerializeField, Min(0f)] private float pullPerSecond = 0.5f;
    [SerializeField, Min(0f)] private float pullPainPerSecond = 0.22f;
    [SerializeField] private bool hideWhenCompleted = true;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color woundOpenColor = new Color(1f, 0.55f, 0.45f, 1f);
    [SerializeField] private Color completedColor = new Color(1f, 1f, 1f, 0.25f);
    [Header("State Visuals")]
    [SerializeField] private GameObject closedVisualRoot;
    [SerializeField] private GameObject openedVisualRoot;
    [Header("Cut Guide")]
    [SerializeField] private CutGuideLine guideLine;
    [Header("Pull Visual")]
    [SerializeField] private Transform pullVisualRoot;
    [SerializeField] private bool followPointerWhilePulling = true;
    [SerializeField, Min(0f)] private float pullVisualMaxDistance = 0.45f;
    [SerializeField, Min(0f)] private float pullVisualFollowSpeed = 16f;

    private Collider2D hitCollider;
    private readonly List<Vector2> worldPath = new List<Vector2>();
    private float cutProgress;
    private float pullProgress;
    private float painLevel;
    private int nextWaypointIndex = 1;
    private bool sliceStarted;
    private bool woundOpen;
    private bool completed;
    private float jitterSeed;
    private Vector2 currentKnifeTip;
    private bool isOutsidePath;
    private Vector3 initialPullVisualLocalPosition;
    private bool hasInitialPullVisualPosition;

    public LesionType Type => type;
    public float CutProgress => cutProgress;
    public float PullProgress => pullProgress;
    public float OverallProgress => GetOverallProgress();
    public float PainLevel => painLevel;
    public bool IsCompleted => completed;
    public bool IsWoundOpen => woundOpen;
    public bool CanSlice => !completed && (!woundOpen || type == LesionType.Tumor);
    public bool CanPull => !completed && type == LesionType.Bulge && woundOpen;

    public event Action<Lesion> Completed;
    public event Action<float> PainChanged;
    public event Action<float> ProgressChanged;

    private void Awake()
    {
        ResolveReferences();
        RefreshWorldPath();
        ResetRuntimeState();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void ResetRuntimeState()
    {
        cutProgress = 0f;
        pullProgress = 0f;
        painLevel = 0f;
        nextWaypointIndex = 1;
        sliceStarted = false;
        woundOpen = false;
        completed = false;
        jitterSeed = UnityEngine.Random.value * 1000f;
        isOutsidePath = false;
        currentKnifeTip = transform.position;
        ResetPullVisualPosition();
        gameObject.SetActive(true);
        ApplyVisualState();
        RefreshGuideLine();
        PainChanged?.Invoke(painLevel);
        ProgressChanged?.Invoke(GetOverallProgress());
    }

    public bool ContainsPoint(Vector2 worldPoint)
    {
        ResolveReferences();
        return hitCollider != null && hitCollider.OverlapPoint(worldPoint);
    }

    public bool CanBeginSliceAt(Vector2 worldPoint)
    {
        if (!CanSlice)
        {
            return false;
        }

        RefreshWorldPath();
        if (worldPath.Count < 2)
        {
            return false;
        }

        float startDistance = Vector2.Distance(worldPoint, worldPath[0]);
        if (startDistance <= Mathf.Max(startRadius, pathHalfWidth))
        {
            return true;
        }

        return GetDistanceToPath(worldPoint) <= pathHalfWidth;
    }

    public void SetKnifeTipPreview(Vector2 worldPoint)
    {
        currentKnifeTip = worldPoint;
    }

    public void TickSlice(Vector2 pointerWorldPosition, float deltaTime, Sanity sanity)
    {
        if (!CanSlice || deltaTime <= 0f)
        {
            DrainPain(deltaTime);
            return;
        }

        RefreshWorldPath();
        if (worldPath.Count < 2)
        {
            return;
        }

        Vector2 knifeTip = ApplySliceJitter(pointerWorldPosition);
        currentKnifeTip = knifeTip;

        if (!sliceStarted)
        {
            float distanceToStart = Vector2.Distance(knifeTip, worldPath[0]);
            if (distanceToStart > Mathf.Max(startRadius, pathHalfWidth))
            {
                DrainPain(deltaTime);
                return;
            }

            sliceStarted = true;
        }

        float distanceToPath = GetDistanceToPath(knifeTip);
        isOutsidePath = distanceToPath > pathHalfWidth;
        ApplyPain(painPerSecond, deltaTime, sanity);

        if (isOutsidePath)
        {
            sanity?.AddSanity(strayPenaltyPerSecond * deltaTime);
            RollBackCut(deltaTime);
            return;
        }

        AdvanceOrderedCut(knifeTip);
    }

    public void TickPull(Vector2 pointerWorldPosition, float deltaTime, Sanity sanity)
    {
        if (!CanPull || deltaTime <= 0f)
        {
            DrainPain(deltaTime);
            return;
        }

        currentKnifeTip = pointerWorldPosition;
        isOutsidePath = false;
        UpdatePullVisual(pointerWorldPosition, deltaTime);
        ApplyPain(pullPainPerSecond, deltaTime, sanity);
        pullProgress = Mathf.Clamp01(pullProgress + pullPerSecond * deltaTime);
        ProgressChanged?.Invoke(GetOverallProgress());

        if (pullProgress >= 1f)
        {
            CompleteLesion();
        }
    }

    public void TickIdle(float deltaTime)
    {
        DrainPain(deltaTime);
    }

    private Vector2 ApplySliceJitter(Vector2 pointerWorldPosition)
    {
        if (jitterStrength <= 0f)
        {
            return pointerWorldPosition;
        }

        float frequency = Mathf.Max(0.01f, jitterFrequency);
        float noiseX = Mathf.PerlinNoise((Time.time + jitterSeed) * frequency, jitterSeed);
        float noiseY = Mathf.PerlinNoise(jitterSeed, (Time.time + jitterSeed) * frequency);
        Vector2 jitter = new Vector2(noiseX - 0.5f, noiseY - 0.5f) * (2f * jitterStrength * Mathf.Lerp(1f, 2f, painLevel));
        return pointerWorldPosition + jitter;
    }

    private void AdvanceOrderedCut(Vector2 knifeTip)
    {
        if (nextWaypointIndex >= worldPath.Count)
        {
            CompleteCut();
            return;
        }

        float waypointRadius = Mathf.Max(pathHalfWidth, 0.05f);
        while (nextWaypointIndex < worldPath.Count && Vector2.Distance(knifeTip, worldPath[nextWaypointIndex]) <= waypointRadius)
        {
            nextWaypointIndex++;
        }

        float previousProgress = cutProgress;
        cutProgress = Mathf.Clamp01((nextWaypointIndex - 1f) / Mathf.Max(1f, worldPath.Count - 1f));
        if (!Mathf.Approximately(previousProgress, cutProgress))
        {
            RefreshGuideLine();
            ProgressChanged?.Invoke(GetOverallProgress());
        }

        if (nextWaypointIndex >= worldPath.Count)
        {
            CompleteCut();
        }
    }

    private void CompleteCut()
    {
        cutProgress = 1f;
        RefreshGuideLine();
        ProgressChanged?.Invoke(GetOverallProgress());

        if (type == LesionType.Bulge && !woundOpen)
        {
            woundOpen = true;
            sliceStarted = false;
            ApplyVisualState();
            RefreshGuideLine();
            return;
        }

        CompleteLesion();
    }

    private void RollBackCut(float deltaTime)
    {
        if (rollbackPerSecond <= 0f)
        {
            return;
        }

        float previousProgress = cutProgress;
        cutProgress = Mathf.Clamp01(cutProgress - rollbackPerSecond * deltaTime);
        nextWaypointIndex = Mathf.Clamp(Mathf.FloorToInt(cutProgress * Mathf.Max(1, worldPath.Count - 1)) + 1, 1, worldPath.Count);

        if (!Mathf.Approximately(previousProgress, cutProgress))
        {
            RefreshGuideLine();
            ProgressChanged?.Invoke(GetOverallProgress());
        }
    }

    private void ApplyPain(float rate, float deltaTime, Sanity sanity)
    {
        if (rate <= 0f)
        {
            return;
        }

        SetPain(painLevel + rate * deltaTime);
        if (painLevel >= 1f)
        {
            sanity?.AddSanity(painSpikeAmount);
            SetPain(0f);
        }
    }

    private void DrainPain(float deltaTime)
    {
        if (deltaTime <= 0f || painLevel <= 0f || painDrainRate <= 0f)
        {
            return;
        }

        SetPain(painLevel - painDrainRate * deltaTime);
    }

    private void SetPain(float value)
    {
        float previous = painLevel;
        painLevel = Mathf.Clamp01(value);
        if (!Mathf.Approximately(previous, painLevel))
        {
            PainChanged?.Invoke(painLevel);
        }
    }

    private void CompleteLesion()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        cutProgress = 1f;
        pullProgress = type == LesionType.Bulge ? 1f : pullProgress;
        painLevel = 0f;
        ApplyVisualState();
        RefreshGuideLine();
        ProgressChanged?.Invoke(GetOverallProgress());
        PainChanged?.Invoke(painLevel);

        if (hideWhenCompleted)
        {
            gameObject.SetActive(false);
        }

        Completed?.Invoke(this);
    }

    private float GetOverallProgress()
    {
        if (type == LesionType.Tumor)
        {
            return cutProgress;
        }

        return woundOpen ? 0.5f + pullProgress * 0.5f : cutProgress * 0.5f;
    }

    private float GetDistanceToPath(Vector2 point)
    {
        float closest = float.MaxValue;
        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            closest = Mathf.Min(closest, DistancePointToSegment(point, worldPath[i], worldPath[i + 1]));
        }

        return closest;
    }

    public static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float t = Vector2.Dot(point - start, segment) / Mathf.Max(segment.sqrMagnitude, 0.000001f);
        t = Mathf.Clamp01(t);
        return Vector2.Distance(point, start + segment * t);
    }

    private void RefreshWorldPath()
    {
        worldPath.Clear();
        for (int i = 0; i < cutPath.Count; i++)
        {
            worldPath.Add(pathIsLocal ? (Vector2)transform.TransformPoint(cutPath[i]) : cutPath[i]);
        }
    }

    private void ResolveReferences()
    {
        if (hitCollider == null)
        {
            hitCollider = GetComponent<Collider2D>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (pullVisualRoot == null && targetRenderer != null)
        {
            pullVisualRoot = targetRenderer.transform;
        }

        if (guideLine == null)
        {
            guideLine = GetComponentInChildren<CutGuideLine>(true);
        }

        if (pullVisualRoot != null && !hasInitialPullVisualPosition)
        {
            initialPullVisualLocalPosition = pullVisualRoot.localPosition;
            hasInitialPullVisualPosition = true;
        }
    }

    private void UpdatePullVisual(Vector2 pointerWorldPosition, float deltaTime)
    {
        if (!followPointerWhilePulling || pullVisualRoot == null || pullVisualMaxDistance <= 0f)
        {
            return;
        }

        EnsurePullVisualInitialPosition();

        Transform visualParent = pullVisualRoot.parent;
        Vector3 originWorld = visualParent != null
            ? visualParent.TransformPoint(initialPullVisualLocalPosition)
            : initialPullVisualLocalPosition;

        Vector2 offset = pointerWorldPosition - (Vector2)originWorld;
        if (offset.magnitude > pullVisualMaxDistance)
        {
            offset = offset.normalized * pullVisualMaxDistance;
        }

        Vector3 targetWorld = originWorld + (Vector3)offset;
        Vector3 targetLocal = visualParent != null ? visualParent.InverseTransformPoint(targetWorld) : targetWorld;
        float t = pullVisualFollowSpeed <= 0f ? 1f : 1f - Mathf.Exp(-pullVisualFollowSpeed * deltaTime);
        pullVisualRoot.localPosition = Vector3.Lerp(pullVisualRoot.localPosition, targetLocal, t);
    }

    private void ResetPullVisualPosition()
    {
        if (pullVisualRoot == null)
        {
            return;
        }

        EnsurePullVisualInitialPosition();
        pullVisualRoot.localPosition = initialPullVisualLocalPosition;
    }

    private void EnsurePullVisualInitialPosition()
    {
        if (hasInitialPullVisualPosition || pullVisualRoot == null)
        {
            return;
        }

        initialPullVisualLocalPosition = pullVisualRoot.localPosition;
        hasInitialPullVisualPosition = true;
    }

    private void ApplyVisualState()
    {
        ApplyStateVisualRoots();

        if (targetRenderer == null)
        {
            return;
        }

        if (completed)
        {
            targetRenderer.color = completedColor;
        }
        else if (woundOpen)
        {
            targetRenderer.color = woundOpenColor;
        }
        else
        {
            targetRenderer.color = normalColor;
        }
    }

    private void ApplyStateVisualRoots()
    {
        if (closedVisualRoot == null && openedVisualRoot == null)
        {
            return;
        }

        bool showOpened = !completed && type == LesionType.Bulge && woundOpen;
        bool showClosed = !completed && !showOpened;

        if (closedVisualRoot != null)
        {
            closedVisualRoot.SetActive(showClosed);
        }

        if (openedVisualRoot != null)
        {
            openedVisualRoot.SetActive(showOpened);
        }
    }

    private void RefreshGuideLine()
    {
        if (guideLine == null)
        {
            return;
        }

        RefreshWorldPath();

        if (completed || (type == LesionType.Bulge && woundOpen))
        {
            guideLine.Hide();
            return;
        }

        guideLine.ShowRemaining(worldPath, cutProgress);
    }

    private void OnDrawGizmos()
    {
        RefreshWorldPath();
        if (worldPath.Count < 2)
        {
            return;
        }

        Gizmos.color = isOutsidePath ? Color.red : Color.cyan;
        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Gizmos.DrawLine(worldPath[i], worldPath[i + 1]);
        }

        Gizmos.color = Color.green;
        for (int i = 0; i < worldPath.Count; i++)
        {
            Gizmos.DrawWireSphere(worldPath[i], pathHalfWidth);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentKnifeTip, 0.08f);

        if (nextWaypointIndex >= 0 && nextWaypointIndex < worldPath.Count)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(worldPath[nextWaypointIndex], 0.12f);
        }
    }
}
