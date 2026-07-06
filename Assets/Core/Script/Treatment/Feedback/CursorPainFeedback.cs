using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Drives the treatment mini game cursor as a real hardware cursor (Cursor.SetCursor) so the pointer tip
/// lines up exactly with the OS click position. Pain feedback: an optional swap to a "pain" cursor texture
/// once pain crosses a threshold (hardware cursors cannot tint/lerp colour, only swap textures), plus an
/// optional floating pain bar near the pointer for smooth, gradual feedback. Place it under the shared mini
/// game overlay so it is only active while a mini game is; OnEnable applies the cursor, OnDisable/OnDestroy
/// restore the default OS cursor.
///
/// Cursor textures MUST be CPU accessible: set Texture Type = Cursor, Read/Write enabled, Compression = None,
/// and keep them small (32 or 64 px). Otherwise Unity logs "texture was not CPU accessible" and the swap fails.
/// </summary>
public sealed class CursorPainFeedback : MonoBehaviour
{
    [SerializeField] private TreatmentFeedback feedback;
    [Header("Hardware Cursor")]
    [SerializeField] private Texture2D normalCursor;                 // default pointer while treating
    [SerializeField] private Texture2D painCursor;                   // optional: swapped in when pain is high
    [SerializeField] private Vector2 hotspot = Vector2.zero;         // pointer tip in texture pixels (0,0 = top-left)
    [SerializeField, Range(0f, 1f)] private float painCursorThreshold = 0.6f;
    [Header("Floating Pain Bar")]
    [SerializeField] private RectTransform painBarRoot;              // sits near the cursor (optional)
    [SerializeField] private Image painFill;                         // Image Type = Filled; fillAmount = pain
    [SerializeField] private Vector2 painBarOffset = new Vector2(28f, -28f);
    [SerializeField, Min(0f)] private float painLerpSpeed = 12f;

    private float displayedPain;
    private bool painCursorActive;
    private bool cursorApplied;

    private void OnEnable()
    {
        ResolveFeedback();
        painCursorActive = false;
        ApplyCursor(false);
        Cursor.visible = true;
    }

    private void OnDisable()
    {
        RestoreDefaultCursor();
    }

    private void OnDestroy()
    {
        // Safety: never leave a custom hardware cursor applied after this object goes away.
        RestoreDefaultCursor();
    }

    private void Update()
    {
        ResolveFeedback();

        if (TryGetPointer(out Vector2 screenPosition) && painBarRoot != null)
        {
            painBarRoot.position = screenPosition + painBarOffset;
        }

        float targetPain = feedback != null ? feedback.Pain : 0f;
        displayedPain = painLerpSpeed > 0f
            ? Mathf.Lerp(displayedPain, targetPain, Mathf.Clamp01(painLerpSpeed * Time.unscaledDeltaTime))
            : targetPain;

        if (painFill != null)
        {
            painFill.fillAmount = displayedPain;
        }

        UpdatePainCursor(targetPain);
    }

    private void UpdatePainCursor(float pain)
    {
        // Only meaningful when a dedicated pain cursor exists; otherwise the normal cursor stays applied.
        if (painCursor == null)
        {
            return;
        }

        bool wantPain = pain >= painCursorThreshold;
        if (wantPain == painCursorActive && cursorApplied)
        {
            return; // avoid re-issuing SetCursor every frame
        }

        painCursorActive = wantPain;
        ApplyCursor(wantPain);
    }

    private void ApplyCursor(bool usePainCursor)
    {
        Texture2D texture = usePainCursor && painCursor != null ? painCursor : normalCursor;

        // A null texture is valid: it means "use the default OS cursor".
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        cursorApplied = true;
    }

    private void RestoreDefaultCursor()
    {
        if (!cursorApplied)
        {
            return;
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
        cursorApplied = false;
        painCursorActive = false;
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

    private void ResolveFeedback()
    {
        if (feedback == null)
        {
            feedback = TreatmentFeedback.Instance;
        }
    }
}
