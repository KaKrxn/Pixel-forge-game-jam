using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class BodyPartButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private BodyArea area;
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Range(0f, 1f)] private float alphaHitTestThreshold = 0.1f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.86f, 0.42f, 1f);
    [SerializeField] private Color healthyColor = new Color(0.82f, 0.82f, 0.82f, 1f);
    [SerializeField] private Color treatedColor = new Color(0.48f, 0.66f, 0.58f, 1f);
    [Header("Debug")]
    [SerializeField] private bool debugTreatmentFlow = true;

    private Image image;
    private RectTransform rectTransform;
    private Vector3 baseScale = Vector3.one;
    private AnatomyController controller;
    private PartState state = PartState.Untouched;
    private bool isHovering;

    public BodyArea Area => area;

    private void Awake()
    {
        CacheReferences();
        ApplyAlphaHitTest();
        ApplyVisualState();
    }

    private void OnEnable()
    {
        CacheReferences();
        baseScale = rectTransform != null ? rectTransform.localScale : transform.localScale;
        ApplyVisualState();
    }

    private void OnValidate()
    {
        CacheReferences();
        ApplyAlphaHitTest();
        ApplyVisualState();
    }

    public void Bind(AnatomyController owner)
    {
        controller = owner;
    }

    public void SetState(PartState nextState)
    {
        state = nextState;
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsSelectable())
        {
            return;
        }

        isHovering = true;
        SetScale(baseScale * hoverScale);
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        SetScale(baseScale);
        ApplyVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        bool selectable = IsSelectable();
        if (debugTreatmentFlow)
        {
            Debug.Log($"[TreatmentFlow][BodyPartButton] Click object={name} area={area} selectable={selectable} controller={(controller != null ? controller.name : "null")} state={state}", this);
        }

        if (!selectable)
        {
            return;
        }

        controller.EnterArea(area);
    }

    private bool IsSelectable()
    {
        return controller != null && controller.CanSelectArea(area);
    }

    private void CacheReferences()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (controller == null)
        {
            controller = GetComponentInParent<AnatomyController>(true);
        }
    }

    private void ApplyAlphaHitTest()
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = image.overrideSprite != null ? image.overrideSprite : image.sprite;
        if (sprite == null || sprite.texture == null || alphaHitTestThreshold <= 0f)
        {
            SetAlphaHitTestThreshold(0f);
            return;
        }

        try
        {
            image.alphaHitTestMinimumThreshold = alphaHitTestThreshold;
        }
        catch (InvalidOperationException)
        {
            SetAlphaHitTestThreshold(0f);
        }
    }

    private void SetAlphaHitTestThreshold(float threshold)
    {
        try
        {
            image.alphaHitTestMinimumThreshold = threshold;
        }
        catch (InvalidOperationException)
        {
            // Non-readable placeholder textures must fall back to normal rectangular UI raycasts.
        }
    }

    private void ApplyVisualState()
    {
        if (image == null)
        {
            return;
        }

        if (isHovering && IsSelectable())
        {
            image.color = hoverColor;
            return;
        }

        image.color = state switch
        {
            PartState.Healthy => healthyColor,
            PartState.Treated => treatedColor,
            _ => normalColor
        };
    }

    private void SetScale(Vector3 scale)
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = scale;
            return;
        }

        transform.localScale = scale;
    }
}
