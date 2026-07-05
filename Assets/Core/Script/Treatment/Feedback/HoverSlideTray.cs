using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Slides the tool tray up when the cursor comes near it and back down (to a peeking rest) when the cursor
/// moves away. A reveal/hide dead-band (hysteresis) keeps it from flickering at the edge. Put this on the
/// tray panel RectTransform (the tool buttons are its children, so the whole tray + tools slide together).
/// </summary>
public sealed class HoverSlideTray : MonoBehaviour
{
    [SerializeField] private RectTransform trayRoot;      // the panel that slides (defaults to this)
    [SerializeField] private RectTransform revealZone;    // area that triggers reveal (defaults to trayRoot)
    [SerializeField] private Canvas canvas;               // for screen-point tests

    [Header("Positions (anchoredPosition)")]
    [SerializeField] private Vector2 shownPosition;       // fully up
    [SerializeField] private Vector2 hiddenPosition;      // peeking down

    [Header("Hover")]
    [SerializeField, Min(0f)] private float revealDistance = 90f;  // px from the zone to slide up
    [SerializeField, Min(0f)] private float hideDistance = 200f;   // px to slide down (should be > revealDistance)

    [Header("Motion")]
    [SerializeField, Min(0f)] private float slideSpeed = 14f;      // smoothing toward target
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool snapToPixelGrid = true;

    private readonly Vector3[] corners = new Vector3[4];
    private bool active = true;
    private bool wantShown;

    private void Reset()
    {
        trayRoot = transform as RectTransform;
    }

    private void Awake()
    {
        ResolveReferences();
        wantShown = false;
        if (trayRoot != null)
        {
            trayRoot.anchoredPosition = hiddenPosition;
        }
    }

    private void OnEnable()
    {
        // Snap to the resting position when shown again (e.g. re-entering a mini game).
        wantShown = false;
        if (trayRoot != null)
        {
            trayRoot.anchoredPosition = hiddenPosition;
        }
    }

    private void Update()
    {
        ResolveReferences();
        if (trayRoot == null)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        UpdateWantShown();

        Vector2 target = active && wantShown ? shownPosition : hiddenPosition;
        float t = slideSpeed > 0f ? 1f - Mathf.Exp(-slideSpeed * deltaTime) : 1f;
        Vector2 next = Vector2.Lerp(trayRoot.anchoredPosition, target, t);

        if (snapToPixelGrid)
        {
            next = new Vector2(Mathf.Round(next.x), Mathf.Round(next.y));
        }

        trayRoot.anchoredPosition = next;
    }

    /// <summary>Enable the hover slide (mini game running) or force the tray to its resting position.</summary>
    public void SetActive(bool value)
    {
        active = value;
        if (!active)
        {
            wantShown = false;
        }
    }

    private void UpdateWantShown()
    {
        if (!active)
        {
            wantShown = false;
            return;
        }

        if (!TryGetPointer(out Vector2 pointer))
        {
            wantShown = false; // no mouse -> rest
            return;
        }

        float distance = DistanceToRevealZone(pointer);

        if (distance <= revealDistance)
        {
            wantShown = true;
        }
        else if (distance >= hideDistance)
        {
            wantShown = false;
        }
        // else: inside the dead-band, keep the previous decision (hysteresis).
    }

    private float DistanceToRevealZone(Vector2 screenPoint)
    {
        RectTransform zone = revealZone != null ? revealZone : trayRoot;
        if (zone == null)
        {
            return float.MaxValue;
        }

        Camera cam = ResolveEventCamera();
        zone.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); // bottom-left
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]); // top-right

        // The reveal zone uses the tray's SHOWN rect, so measure against where it will be when up.
        Vector2 slideOffset = shownPosition - trayRoot.anchoredPosition;
        min += slideOffset;
        max += slideOffset;

        float dx = Mathf.Max(min.x - screenPoint.x, 0f, screenPoint.x - max.x);
        float dy = Mathf.Max(min.y - screenPoint.y, 0f, screenPoint.y - max.y);
        return Mathf.Sqrt(dx * dx + dy * dy); // 0 when inside
    }

    private Camera ResolveEventCamera()
    {
        if (canvas == null)
        {
            return null;
        }

        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    private void ResolveReferences()
    {
        if (trayRoot == null)
        {
            trayRoot = transform as RectTransform;
        }

        if (canvas == null && trayRoot != null)
        {
            canvas = trayRoot.GetComponentInParent<Canvas>();
        }
    }

    private static bool TryGetPointer(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
#endif
        screenPosition = default;
        return false;
    }
}
