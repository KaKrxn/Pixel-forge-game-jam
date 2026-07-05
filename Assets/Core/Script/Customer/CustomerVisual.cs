using UnityEngine;

/// <summary>
/// Holds a customer's sick (default) and cured looks — each a <see cref="Sprite"/> and a
/// <see cref="RuntimeAnimatorController"/> — and swaps between them. A freshly spawned customer shows the
/// sick look; <see cref="ShowCured"/> swaps to the cured sprite + controller (and an optional effect) when
/// the case is completed. Sorting stays owned by <c>CustomerLayer</c>; this only changes the sprite/animator.
/// </summary>
public sealed class CustomerVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer; // assign the CustomerLayer renderer
    [SerializeField] private Animator animator;

    [Header("Sick (default)")]
    [SerializeField] private Sprite sickSprite;
    [SerializeField] private RuntimeAnimatorController sickController;

    [Header("Cured")]
    [SerializeField] private Sprite curedSprite;
    [SerializeField] private RuntimeAnimatorController curedController;

    [Header("Optional")]
    [SerializeField] private GameObject curedEffectRoot; // glow / sparkle shown when cured
    [SerializeField] private bool applySickOnAwake = true;

    public bool IsCured { get; private set; }

    private void Awake()
    {
        ResolveReferences();

        if (curedEffectRoot != null)
        {
            curedEffectRoot.SetActive(false);
        }

        if (applySickOnAwake)
        {
            Apply(false);
        }
    }

    public void ShowSick()
    {
        Apply(false);
    }

    public void ShowCured()
    {
        Apply(true);
    }

    public void SetCured(bool cured)
    {
        Apply(cured);
    }

    private void Apply(bool cured)
    {
        ResolveReferences();
        IsCured = cured;

        Sprite sprite = cured ? curedSprite : sickSprite;
        RuntimeAnimatorController controller = cured ? curedController : sickController;

        if (bodyRenderer != null && sprite != null)
        {
            bodyRenderer.sprite = sprite;
        }

        if (animator != null && controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        if (curedEffectRoot != null)
        {
            curedEffectRoot.SetActive(cured);
        }
    }

    private void ResolveReferences()
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }
}
