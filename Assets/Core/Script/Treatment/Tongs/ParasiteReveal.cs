using UnityEngine;

public enum ParasiteRevealMode
{
    Smooth,
    Stepped
}

public sealed class ParasiteReveal : MonoBehaviour
{
    [SerializeField] private Transform revealRoot;
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Collider2D headCollider;
    [SerializeField] private Vector2 localRevealAxis = Vector2.up;
    [SerializeField] private float hiddenOffset = -0.45f;
    [SerializeField] private float exposedOffset;
    [SerializeField] private ParasiteRevealMode revealMode = ParasiteRevealMode.Smooth;
    [SerializeField, Min(1)] private int steppedSegments = 4;
    [SerializeField] private bool snapToPixelGrid = true;
    [SerializeField, Min(1f)] private float pixelsPerUnit = 16f;
    [SerializeField] private bool driveMaskWithPointer = true;
    [SerializeField, Min(0.01f)] private float dynamicMaskWidth = 0.85f;
    [SerializeField, Min(0.01f)] private float dynamicMaskMinLength = 0.08f;
    [SerializeField, Min(0f)] private float dynamicMaskExtraLength = 0.35f;
    [SerializeField] private bool fadeBodyWhenNoMask = true;
    [SerializeField] private SpriteMaskInteraction bodyMaskInteractionWhenBound = SpriteMaskInteraction.VisibleInsideMask;
    [SerializeField] private SpriteMaskInteraction bodyMaskInteractionWithoutMask = SpriteMaskInteraction.None;
    [SerializeField] private SpriteMaskInteraction headMaskInteraction = SpriteMaskInteraction.None;

    private SpriteMask woundMask;
    private Vector3 baseLocalPosition;
    private Color baseBodyColor = Color.white;
    private Vector3 maskOriginWorld;
    private Sprite runtimeMaskSprite;
    private Transform runtimeMaskParent;
    private int bodySortingOrder;
    private bool hasBaseLocalPosition;
    private bool hasBaseBodyColor;
    private bool hasMaskOrigin;
    private bool ownsRuntimeMask;

    public SpriteMask WoundMask => woundMask;
    public Collider2D HeadCollider => headCollider;

    private void Awake()
    {
        ResolveReferences();
        CaptureBasePose();
        CaptureBaseBodyColor();
        ApplyMaskSettings();
        SetProgress(0f);
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyMaskSettings();
    }

    private void OnDestroy()
    {
        ReleaseRuntimeMask();
    }

    public void Bind(SpriteMask mask)
    {
        Bind(mask, null, null, bodyRenderer != null ? bodyRenderer.sortingOrder : 0);
    }

    public void Bind(SpriteMask mask, Transform maskAnchor, Sprite fallbackMaskSprite, int sortingOrder)
    {
        ResolveReferences();
        CaptureBaseBodyColor();

        bodySortingOrder = sortingOrder;
        runtimeMaskParent = maskAnchor != null ? maskAnchor.parent : transform.parent;
        runtimeMaskSprite = fallbackMaskSprite;
        hasMaskOrigin = maskAnchor != null;
        if (hasMaskOrigin)
        {
            maskOriginWorld = maskAnchor.position;
        }

        if (mask != null)
        {
            ReleaseRuntimeMask();
            woundMask = mask;
            ownsRuntimeMask = false;
        }
        else
        {
            ReleaseRuntimeMask();
            woundMask = GetOrCreateRuntimeMask();
        }

        ConfigureSortingRange();
        ApplyMaskSettings();
        UpdateDynamicMask(maskOriginWorld);
    }

    public void ResetReveal()
    {
        SetProgress(0f);
    }

    public void SetProgress(float progress)
    {
        ResolveReferences();

        if (revealRoot == null)
        {
            return;
        }

        if (!hasBaseLocalPosition)
        {
            CaptureBasePose();
        }

        float evaluatedProgress = EvaluateProgress(Mathf.Clamp01(progress));
        float offset = Mathf.Lerp(hiddenOffset, exposedOffset, evaluatedProgress);
        Vector2 axis = localRevealAxis.sqrMagnitude > 0f ? localRevealAxis.normalized : Vector2.up;
        Vector3 localPosition = baseLocalPosition + new Vector3(axis.x, axis.y, 0f) * offset;
        revealRoot.localPosition = snapToPixelGrid ? SnapLocalPosition(localPosition) : localPosition;
        ApplyBodyRevealAlpha(evaluatedProgress);
    }

    public void UpdateDynamicMask(Vector2 pointerWorldPosition)
    {
        if (!driveMaskWithPointer || woundMask == null || !hasMaskOrigin)
        {
            return;
        }

        Vector2 origin = maskOriginWorld;
        Vector2 drag = pointerWorldPosition - origin;
        float length = Mathf.Max(dynamicMaskMinLength, Vector2.Dot(drag, Vector2.up) + dynamicMaskExtraLength);
        Vector2 center = origin + Vector2.up * (length * 0.5f);
        Transform maskTransform = woundMask.transform;
        maskTransform.position = new Vector3(center.x, center.y, maskTransform.position.z);
        maskTransform.rotation = Quaternion.identity;

        Vector2 spriteSize = woundMask.sprite != null ? woundMask.sprite.bounds.size : Vector2.one;
        float width = Mathf.Max(0.01f, spriteSize.x);
        float height = Mathf.Max(0.01f, spriteSize.y);
        maskTransform.localScale = new Vector3(dynamicMaskWidth / width, length / height, 1f);
    }

    public void CaptureBasePose()
    {
        ResolveReferences();

        if (revealRoot == null)
        {
            hasBaseLocalPosition = false;
            return;
        }

        baseLocalPosition = revealRoot.localPosition;
        hasBaseLocalPosition = true;
    }

    private float EvaluateProgress(float progress)
    {
        if (revealMode != ParasiteRevealMode.Stepped)
        {
            return progress;
        }

        int segments = Mathf.Max(1, steppedSegments);
        return Mathf.Round(progress * segments) / segments;
    }

    private Vector3 SnapLocalPosition(Vector3 localPosition)
    {
        float unit = 1f / Mathf.Max(1f, pixelsPerUnit);
        localPosition.x = Mathf.Round(localPosition.x / unit) * unit;
        localPosition.y = Mathf.Round(localPosition.y / unit) * unit;
        return localPosition;
    }

    private void ResolveReferences()
    {
        if (revealRoot == null)
        {
            revealRoot = transform;
        }

        if (headRenderer == null)
        {
            Transform head = transform.Find("Head");
            if (head != null)
            {
                headRenderer = head.GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (bodyRenderer == null)
        {
            Transform body = transform.Find("Body");
            if (body != null)
            {
                bodyRenderer = body.GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (headCollider == null && headRenderer != null)
        {
            headCollider = headRenderer.GetComponentInChildren<Collider2D>(true);
        }
    }

    private void CaptureBaseBodyColor()
    {
        if (bodyRenderer == null || hasBaseBodyColor)
        {
            return;
        }

        baseBodyColor = bodyRenderer.color;
        hasBaseBodyColor = true;
    }

    private void ApplyBodyRevealAlpha(float progress)
    {
        if (bodyRenderer == null || !fadeBodyWhenNoMask || woundMask != null)
        {
            return;
        }

        CaptureBaseBodyColor();
        Color color = baseBodyColor;
        color.a = baseBodyColor.a * Mathf.Clamp01(progress);
        bodyRenderer.color = color;
    }

    private SpriteMask GetOrCreateRuntimeMask()
    {
        Sprite maskSprite = runtimeMaskSprite;
        if (maskSprite == null && headRenderer != null)
        {
            maskSprite = headRenderer.sprite;
        }

        if (maskSprite == null)
        {
            return null;
        }

        GameObject maskObject = new GameObject($"{name}_RuntimeRevealMask");
        Transform parent = runtimeMaskParent != null ? runtimeMaskParent : transform.parent;
        if (parent != null)
        {
            maskObject.transform.SetParent(parent, worldPositionStays: true);
        }

        maskObject.transform.position = hasMaskOrigin ? maskOriginWorld : transform.position;
        SpriteMask spriteMask = maskObject.AddComponent<SpriteMask>();
        spriteMask.sprite = maskSprite;
        ownsRuntimeMask = true;
        return spriteMask;
    }

    private void ReleaseRuntimeMask()
    {
        if (!ownsRuntimeMask || woundMask == null)
        {
            ownsRuntimeMask = false;
            return;
        }

        Destroy(woundMask.gameObject);
        woundMask = null;
        ownsRuntimeMask = false;
    }

    private void ConfigureSortingRange()
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.sortingOrder = bodySortingOrder;
        }

        if (headRenderer != null)
        {
            headRenderer.sortingOrder = bodySortingOrder + 1;
        }

        if (woundMask == null || bodyRenderer == null)
        {
            return;
        }

        woundMask.isCustomRangeActive = true;
        woundMask.frontSortingLayerID = bodyRenderer.sortingLayerID;
        woundMask.backSortingLayerID = bodyRenderer.sortingLayerID;
        woundMask.frontSortingOrder = bodySortingOrder + 1;
        woundMask.backSortingOrder = bodySortingOrder - 1;
    }

    private void ApplyMaskSettings()
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.maskInteraction = woundMask != null ? bodyMaskInteractionWhenBound : bodyMaskInteractionWithoutMask;
            if (woundMask != null)
            {
                CaptureBaseBodyColor();
                bodyRenderer.color = baseBodyColor;
            }
        }

        if (headRenderer != null)
        {
            headRenderer.maskInteraction = headMaskInteraction;
        }

        if (headCollider != null)
        {
            headCollider.enabled = true;
        }
    }
}
