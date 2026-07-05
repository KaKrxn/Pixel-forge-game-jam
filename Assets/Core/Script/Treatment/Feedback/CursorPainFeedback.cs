using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Custom software cursor for the treatment mini games. While enabled it hides the OS cursor, follows the
/// mouse, tints toward red as pain rises, and carries a small pain bar/ring near the cursor. Place it under
/// the shared mini game overlay (Screen Space - Overlay canvas) so it is only active while a mini game is —
/// its OnEnable/OnDisable restore the OS cursor automatically. Set the cursor Image's Raycast Target to off.
/// </summary>
public sealed class CursorPainFeedback : MonoBehaviour
{
    [SerializeField] private TreatmentFeedback feedback;
    [Header("Cursor")]
    [SerializeField] private RectTransform cursorRoot;    // follows the mouse
    [SerializeField] private Image cursorImage;           // tinted normal -> red by pain
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color painColor = new Color(0.9f, 0.12f, 0.12f, 1f);
    [SerializeField, Min(0f)] private float colorLerpSpeed = 12f;
    [Header("Floating Pain Bar")]
    [SerializeField] private RectTransform painBarRoot;   // sits near the cursor
    [SerializeField] private Image painFill;              // Image Type = Filled; fillAmount = pain
    [SerializeField] private Vector2 painBarOffset = new Vector2(28f, -28f);
    [Header("Rules")]
    [SerializeField] private bool hideSystemCursor = true;

    private float displayedPain;

    private void OnEnable()
    {
        ResolveFeedback();
        ApplySystemCursor(false);
    }

    private void OnDisable()
    {
        ApplySystemCursor(true);
    }

    private void OnDestroy()
    {
        // Safety: never leave the OS cursor stuck hidden (e.g. scene unload / object destroyed).
        ApplySystemCursor(true);
    }

    private void Update()
    {
        ResolveFeedback();

        if (!TryGetPointer(out Vector2 screenPosition))
        {
            return;
        }

        if (cursorRoot != null)
        {
            cursorRoot.position = screenPosition;
        }

        if (painBarRoot != null)
        {
            painBarRoot.position = screenPosition + painBarOffset;
        }

        float targetPain = feedback != null ? feedback.Pain : 0f;
        displayedPain = colorLerpSpeed > 0f
            ? Mathf.Lerp(displayedPain, targetPain, Mathf.Clamp01(colorLerpSpeed * Time.unscaledDeltaTime))
            : targetPain;

        if (cursorImage != null)
        {
            cursorImage.color = Color.Lerp(normalColor, painColor, displayedPain);
        }

        if (painFill != null)
        {
            painFill.fillAmount = displayedPain;
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

    private void ResolveFeedback()
    {
        if (feedback == null)
        {
            feedback = TreatmentFeedback.Instance;
        }
    }

    private void ApplySystemCursor(bool visible)
    {
        if (hideSystemCursor)
        {
            Cursor.visible = visible;
        }
    }
}
