using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public sealed class MenuButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 0.8f;
    [SerializeField] private bool playHoverOnKeyboardSelect = true;
    [SerializeField] private bool playClickOnKeyboardSubmit = true;

    [Header("Hover Motion")]
    [SerializeField] private bool animateHover = true;
    [SerializeField] private float hoverScale = 1.04f;
    [SerializeField] private float hoverTiltAngle = 2.5f;
    [SerializeField] private float motionSpeed = 16f;

    private Selectable selectable;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private Vector3 targetScale;
    private Quaternion targetRotation;
    private bool hasCachedTransform;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        CacheBaseTransform();
    }

    private void OnEnable()
    {
        CacheBaseTransform();
        ResetHoverMotion(immediate: true);
    }

    private void OnDisable()
    {
        ResetHoverMotion(immediate: true);
    }

    private void Update()
    {
        if (!animateHover || !hasCachedTransform)
        {
            return;
        }

        float t = 1f - Mathf.Exp(-motionSpeed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
        ApplyHoverMotion();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverMotion(immediate: false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClick();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (playHoverOnKeyboardSelect)
        {
            PlayHover();
            ApplyHoverMotion();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ResetHoverMotion(immediate: false);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (playClickOnKeyboardSubmit)
        {
            PlayClick();
        }
    }

    public void PlayHover()
    {
        PlayClip(hoverClip, hoverVolume);
    }

    public void PlayClick()
    {
        PlayClip(clickClip, clickVolume);
    }

    private void PlayClip(AudioClip clip, float volumeScale)
    {
        if (clip == null || uiAudioSource == null || selectable == null || !selectable.IsInteractable())
        {
            return;
        }

        float gameVolume = PlayerPrefs.GetFloat(MenuAudioSettings.GameVolumeKey, 0.65f);
        uiAudioSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * Mathf.Clamp01(gameVolume));
    }

    private void ApplyHoverMotion()
    {
        if (!animateHover || selectable == null || !selectable.IsInteractable())
        {
            return;
        }

        CacheBaseTransform();
        float tiltDirection = Random.value < 0.5f ? -1f : 1f;
        targetScale = baseScale * Mathf.Max(1f, hoverScale);
        targetRotation = baseRotation * Quaternion.Euler(0f, 0f, hoverTiltAngle * tiltDirection);
    }

    private void ResetHoverMotion(bool immediate)
    {
        if (!hasCachedTransform)
        {
            return;
        }

        targetScale = baseScale;
        targetRotation = baseRotation;

        if (immediate)
        {
            transform.localScale = baseScale;
            transform.localRotation = baseRotation;
        }
    }

    private void CacheBaseTransform()
    {
        if (hasCachedTransform)
        {
            return;
        }

        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        targetScale = baseScale;
        targetRotation = baseRotation;
        hasCachedTransform = true;
    }
}
