using UnityEngine;

/// <summary>
/// Produces a decaying camera shake **offset** on "hurt" events. It does not write the camera transform
/// itself — <see cref="CameraSway"/> (the single position writer) reads <see cref="CurrentOffset"/> and
/// adds it, so nothing fights over the camera position. Subscribe to <see cref="TreatmentFeedback.Hurt"/>.
/// </summary>
public sealed class CameraShake : MonoBehaviour
{
    [SerializeField] private TreatmentFeedback feedback;
    [SerializeField, Min(0f)] private float maxTranslation = 0.18f; // world units at full trauma
    [SerializeField, Min(0f)] private float decayPerSecond = 3.5f;
    [SerializeField, Min(0f)] private float frequency = 26f;
    [SerializeField, Range(0f, 1f)] private float minHurtTrauma = 0.15f; // floor so light hurts still register
    [SerializeField] private bool useUnscaledTime;

    private float trauma;
    private float seedX;
    private float seedY;

    public Vector3 CurrentOffset { get; private set; }

    private void Awake()
    {
        seedX = Random.value * 100f;
        seedY = Random.value * 100f + 50f;
    }

    private void OnEnable()
    {
        ResolveFeedback();
        Subscribe();
    }

    private void Start()
    {
        // The hub may Awake after this component's OnEnable; resolve + (idempotently) subscribe here too.
        ResolveFeedback();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        trauma = 0f;
        CurrentOffset = Vector3.zero;
    }

    private void Update()
    {
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (trauma > 0f)
        {
            trauma = Mathf.Max(0f, trauma - decayPerSecond * deltaTime);
        }

        if (trauma <= 0f)
        {
            CurrentOffset = Vector3.zero;
            return;
        }

        float shake = trauma * trauma; // squared feels punchier
        float time = (useUnscaledTime ? Time.unscaledTime : Time.time) * frequency;
        float offsetX = (Mathf.PerlinNoise(seedX, time) - 0.5f) * 2f;
        float offsetY = (Mathf.PerlinNoise(seedY, time) - 0.5f) * 2f;
        CurrentOffset = new Vector3(offsetX, offsetY, 0f) * (maxTranslation * shake);
    }

    /// <summary>Add trauma (0..1). Stronger hurts stack toward a full jolt; light hurts get a floor.</summary>
    public void Shake(float intensity)
    {
        float add = Mathf.Max(minHurtTrauma, Mathf.Clamp01(intensity));
        trauma = Mathf.Clamp01(trauma + add);
    }

    private void HandleHurt(float intensity)
    {
        Shake(intensity);
    }

    private void ResolveFeedback()
    {
        if (feedback == null)
        {
            feedback = TreatmentFeedback.Instance;
        }
    }

    private void Subscribe()
    {
        if (feedback != null)
        {
            feedback.Hurt -= HandleHurt;
            feedback.Hurt += HandleHurt;
        }
    }

    private void Unsubscribe()
    {
        if (feedback != null)
        {
            feedback.Hurt -= HandleHurt;
        }
    }
}
