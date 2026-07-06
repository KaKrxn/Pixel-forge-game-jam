using System;
using UnityEngine;

public sealed class Parasite : MonoBehaviour
{
    [SerializeField] private ParasiteType type;
    [SerializeField] private int requiredDirection = 1;
    [SerializeField] private bool hideWhenExtracted = true;
    [SerializeField] private bool edgeContactEnabled;
    [SerializeField] private bool jitterEnabled;
    [SerializeField] private float channelCenterOffset;
    [SerializeField, Min(0.01f)] private float channelDepth = 2f;
    [SerializeField] private float channelTopOffset;
    [SerializeField] private Transform spawnAnchor;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private ParasiteReveal parasiteReveal;
    [SerializeField] private bool moveVisualWithPull;
    [SerializeField, Min(0f)] private float maxVisualFollowDistance = 0.65f;
    [SerializeField, Range(0f, 1f)] private float visualFollowStrength = 0.85f;
    [SerializeField, Min(0f)] private float maxVisualTiltDegrees = 28f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color edgeContactColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color extractedColor = new Color(1f, 1f, 1f, 0.25f);

    private float pullProgress;
    private float painLevel;
    private bool isHeld;
    private bool isExtracted;
    private bool edgeContact;
    private Vector2 previousPointerPosition;
    private Vector2 holdStartPointerPosition;
    private float jitterSeed;
    private float currentTipX;
    private float currentJitterOffset;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private bool hasVisualBasePosition;

    public ParasiteType Type => type;
    public float PullProgress => pullProgress;
    public float PainLevel => painLevel;
    public bool IsHeld => isHeld;
    public bool IsExtracted => isExtracted;
    public bool EdgeContact => edgeContact;
    public int RequiredDirection => requiredDirection < 0 ? -1 : 1;
    public Vector3 SpawnAnchorWorldPosition => GetSpawnAnchorWorldPosition();
    public bool HasSpawnAnchor => spawnAnchor != null;

    public event Action<Parasite> Extracted;
    public event Action<float> PullProgressChanged;
    public event Action<float> PainChanged;
    public event Action<float> SanitySpikeRequested;
    public event Action<float> ContinuousSanityRequested;

    private void Awake()
    {
        ResolveRenderer();
        CacheVisualBasePosition();
        UpdateVisualState();
    }

    private void OnValidate()
    {
        ResolveRenderer();
    }

    public void Configure(ParasiteType parasiteType, int direction = 1)
    {
        type = parasiteType;
        requiredDirection = direction < 0 ? -1 : 1;
        ResetRuntimeState();
    }

    public void BeginHold(Vector2 pointerPosition)
    {
        if (type == null || isExtracted)
        {
            return;
        }

        isHeld = true;
        previousPointerPosition = pointerPosition;
        holdStartPointerPosition = pointerPosition;
        currentTipX = pointerPosition.x;
        jitterSeed = UnityEngine.Random.value * 1000f;
        UpdatePullVisual(pointerPosition);
        UpdateRevealMask(pointerPosition);
    }

    public void EndHold()
    {
        isHeld = false;
        edgeContact = false;
        ResetVisualPosition();
        UpdateRevealProgress();
        UpdateVisualState();
    }

    public void TickPull(Vector2 pointerPosition, float deltaTime)
    {
        if (type == null || isExtracted || deltaTime <= 0f)
        {
            return;
        }

        if (isHeld)
        {
            TickHeld(pointerPosition, deltaTime);
        }
        else
        {
            DrainPain(deltaTime);
            previousPointerPosition = pointerPosition;
        }
    }

    public void ResetRuntimeState()
    {
        pullProgress = 0f;
        painLevel = 0f;
        isHeld = false;
        isExtracted = false;
        edgeContact = false;
        previousPointerPosition = Vector2.zero;
        holdStartPointerPosition = Vector2.zero;
        currentTipX = 0f;
        jitterSeed = 0f;
        gameObject.SetActive(true);
        ResetVisualPosition();
        UpdateRevealProgress();
        UpdateVisualState();
        PullProgressChanged?.Invoke(pullProgress);
        PainChanged?.Invoke(painLevel);
    }

    public void RequestContinuousSanity(float amount)
    {
        if (amount > 0f)
        {
            ContinuousSanityRequested?.Invoke(amount);
        }
    }

    public void AlignSpawnAnchorToWorld(Vector3 anchorWorldPosition)
    {
        Vector3 currentAnchor = GetSpawnAnchorWorldPosition();
        transform.position += anchorWorldPosition - currentAnchor;
        RebaseVisualPosition();
    }

    public void AlignTopToWorld(Vector3 topWorldPosition)
    {
        AlignSpawnAnchorToWorld(topWorldPosition);
    }

    public void RebaseVisualPosition()
    {
        CacheVisualBasePosition();
    }

    public void BindSpawnAnchor(ParasiteSpawnAnchor anchor)
    {
        BindSpawnAnchor(anchor, anchor != null ? anchor.transform : null, null, 0);
    }

    public void BindSpawnAnchor(ParasiteSpawnAnchor anchor, Transform anchorTransform, Sprite fallbackMaskSprite, int bodySortingOrder)
    {
        ResolveRenderer();

        if (parasiteReveal != null)
        {
            parasiteReveal.Bind(anchor != null ? anchor.WoundMask : null, anchorTransform, fallbackMaskSprite, bodySortingOrder);
            parasiteReveal.ResetReveal();
        }
    }

    private void TickHeld(Vector2 pointerPosition, float deltaTime)
    {
        Vector2 pointerDelta = pointerPosition - previousPointerPosition;
        previousPointerPosition = pointerPosition;

        ApplyPullProgress(pointerDelta);
        ApplyPain(deltaTime);
        ApplyEdgeAndJitter(pointerPosition, deltaTime);
        UpdatePullVisual(pointerPosition);
        UpdateRevealMask(pointerPosition);
    }

    private void ApplyPullProgress(Vector2 pointerDelta)
    {
        float correctDragAmount = GetCorrectDragAmount(pointerDelta);
        if (correctDragAmount <= 0f)
        {
            return;
        }

        float previousProgress = pullProgress;
        pullProgress = Mathf.Clamp01(pullProgress + correctDragAmount * type.PullSpeed / type.RequiredDistance);

        if (!Mathf.Approximately(previousProgress, pullProgress))
        {
            UpdateRevealProgress();
            PullProgressChanged?.Invoke(pullProgress);
        }

        if (pullProgress >= 1f)
        {
            CompleteExtraction();
        }
    }

    private float GetCorrectDragAmount(Vector2 pointerDelta)
    {
        float upwardDrag = Mathf.Max(0f, pointerDelta.y);
        if (!type.NeedsDirection)
        {
            return upwardDrag;
        }

        float directionalDrag = Mathf.Max(0f, pointerDelta.x * RequiredDirection);
        return Mathf.Min(upwardDrag, directionalDrag);
    }

    private void ApplyPain(float deltaTime)
    {
        if (type.PainPerSecond <= 0f)
        {
            return;
        }

        SetPain(painLevel + type.PainPerSecond * deltaTime);

        if (painLevel < 1f)
        {
            return;
        }

        SanitySpikeRequested?.Invoke(type.PainSpikeAmount);
        SetPain(0f);
    }

    private void ApplyEdgeAndJitter(Vector2 pointerPosition, float deltaTime)
    {
        edgeContact = false;

        // Compute the jitter offset whenever the type defines a strength, so the tool visibly sways
        // (applied to the visual in UpdatePullVisual) even if edge-contact penalties are off.
        // Jitter grows with pain: more pain -> more sway.
        currentJitterOffset = 0f;
        if (type != null && type.JitterStrength > 0f)
        {
            float frequency = Mathf.Max(0.01f, type.JitterFrequency);
            float noise = Mathf.PerlinNoise((Time.time + jitterSeed) * frequency, jitterSeed);
            float scaledJitterStrength = type.JitterStrength * Mathf.Lerp(1f, 2f, painLevel);
            currentJitterOffset = (noise - 0.5f) * 2f * scaledJitterStrength;
        }

        if (!edgeContactEnabled || type == null)
        {
            UpdateVisualState();
            return;
        }

        float channelCenterX = holdStartPointerPosition.x + channelCenterOffset;
        if (type.NeedsDirection)
        {
            channelCenterX += RequiredDirection * type.ChannelHalfWidth * 0.35f;
        }

        float edgeJitter = jitterEnabled ? currentJitterOffset : 0f;
        currentTipX = pointerPosition.x + edgeJitter;
        float offset = Mathf.Abs(currentTipX - channelCenterX);
        edgeContact = offset > type.ChannelHalfWidth;

        if (edgeContact)
        {
            RequestContinuousSanity(type.EdgePenaltyPerSecond * deltaTime);
        }

        UpdateVisualState();
    }

    private void DrainPain(float deltaTime)
    {
        if (painLevel <= 0f || type.PainDrainRate <= 0f)
        {
            return;
        }

        SetPain(painLevel - type.PainDrainRate * deltaTime);
    }

    private void SetPain(float value)
    {
        float previousPain = painLevel;
        painLevel = Mathf.Clamp01(value);

        if (!Mathf.Approximately(previousPain, painLevel))
        {
            PainChanged?.Invoke(painLevel);
        }
    }

    private void CompleteExtraction()
    {
        if (isExtracted)
        {
            return;
        }

        isExtracted = true;
        isHeld = false;
        edgeContact = false;
        UpdateRevealProgress();
        UpdateVisualState();
        UpdatePullVisual(holdStartPointerPosition + Vector2.up * type.RequiredDistance);

        if (hideWhenExtracted)
        {
            gameObject.SetActive(false);
        }

        Extracted?.Invoke(this);
    }

    private void UpdateVisualState()
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (isExtracted)
        {
            targetRenderer.color = extractedColor;
        }
        else if (edgeContact)
        {
            targetRenderer.color = edgeContactColor;
        }
        else
        {
            targetRenderer.color = normalColor;
        }
    }

    private void ResolveRenderer()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (parasiteReveal == null)
        {
            parasiteReveal = GetComponentInChildren<ParasiteReveal>(true);
        }
    }

    private void UpdateRevealProgress()
    {
        if (parasiteReveal != null)
        {
            parasiteReveal.SetProgress(pullProgress);
        }
    }

    private void UpdateRevealMask(Vector2 pointerPosition)
    {
        if (parasiteReveal != null)
        {
            parasiteReveal.UpdateDynamicMask(pointerPosition);
        }
    }

    private void CacheVisualBasePosition()
    {
        Transform visual = visualRoot != null ? visualRoot : null;
        if (visual == null)
        {
            hasVisualBasePosition = false;
            return;
        }

        visualBaseLocalPosition = visual.localPosition;
        visualBaseLocalRotation = visual.localRotation;
        hasVisualBasePosition = true;
    }

    private void ResetVisualPosition()
    {
        if (!hasVisualBasePosition)
        {
            CacheVisualBasePosition();
        }

        Transform visual = visualRoot != null ? visualRoot : null;
        if (visual != null && hasVisualBasePosition)
        {
            visual.localPosition = visualBaseLocalPosition;
            visual.localRotation = visualBaseLocalRotation;
        }
    }

    private void UpdatePullVisual(Vector2 pointerPosition)
    {
        if (!moveVisualWithPull || type == null)
        {
            return;
        }

        if (!hasVisualBasePosition)
        {
            CacheVisualBasePosition();
        }

        Transform visual = visualRoot != null ? visualRoot : null;
        if (visual == null || !hasVisualBasePosition)
        {
            return;
        }

        Vector2 worldOffset = pointerPosition - holdStartPointerPosition;

        if (!type.NeedsDirection)
        {
            worldOffset.x = Mathf.Clamp(worldOffset.x, -type.ChannelHalfWidth, type.ChannelHalfWidth);
        }

        float maxDistance = maxVisualFollowDistance > 0f ? maxVisualFollowDistance : type.RequiredDistance;
        Vector2 followOffset = new Vector2(worldOffset.x, 0f);
        Vector2 clampedOffset = Vector2.ClampMagnitude(followOffset * visualFollowStrength, maxDistance);
        clampedOffset.x += currentJitterOffset; // visible tool sway (Perlin jitter, grows with pain)
        Vector3 localOffset = transform.InverseTransformVector(new Vector3(clampedOffset.x, clampedOffset.y, 0f));
        // Only apply the lateral (X) follow + tilt. Preserve the vertical position so the upward
        // "emerge from the wound" driven by ParasiteReveal.SetProgress is not overwritten each frame.
        Vector3 targetLocalPosition = visualBaseLocalPosition + localOffset;
        targetLocalPosition.y = visual.localPosition.y;
        visual.localPosition = targetLocalPosition;

        float tilt = maxDistance > 0f ? Mathf.Clamp(clampedOffset.x / maxDistance, -1f, 1f) * -maxVisualTiltDegrees : 0f;
        visual.localRotation = visualBaseLocalRotation * Quaternion.Euler(0f, 0f, tilt);
    }

    private Vector3 GetTopWorldPosition()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Bounds bounds = collider.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, transform.position.z);
        }

        ResolveRenderer();
        if (targetRenderer != null)
        {
            Bounds bounds = targetRenderer.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, transform.position.z);
        }

        return transform.position;
    }

    private Vector3 GetSpawnAnchorWorldPosition()
    {
        if (spawnAnchor != null)
        {
            return spawnAnchor.position;
        }

        return GetTopWorldPosition();
    }

    private void OnDrawGizmosSelected()
    {
        if (type == null)
        {
            return;
        }

        float centerX = Application.isPlaying ? holdStartPointerPosition.x + channelCenterOffset : transform.position.x + channelCenterOffset;
        float halfWidth = type.ChannelHalfWidth;
        float topY = transform.position.y + channelTopOffset;
        float bottomY = topY - channelDepth;

        Gizmos.color = edgeContact ? Color.red : Color.green;
        Gizmos.DrawLine(new Vector3(centerX - halfWidth, bottomY, 0f), new Vector3(centerX - halfWidth, topY, 0f));
        Gizmos.DrawLine(new Vector3(centerX + halfWidth, bottomY, 0f), new Vector3(centerX + halfWidth, topY, 0f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(centerX, bottomY, 0f), new Vector3(centerX, topY, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetSpawnAnchorWorldPosition(), 0.06f);
    }
}
