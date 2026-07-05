using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public sealed class RoomLightController : MonoBehaviour
{
    [SerializeField] private Candle candle;
    [SerializeField] private Light2D[] globalLights;
    [SerializeField] private Light2D[] candlePointLights;
    [SerializeField] private Graphic darknessOverlay;
    [Tooltip("Multiplier (0..1) applied to each light's authored intensity, keyed by candle light (0..1). At 1 it keeps the intensity you set in the Inspector.")]
    [SerializeField] private AnimationCurve globalIntensityByLight = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);
    [SerializeField] private AnimationCurve pointIntensityByLight = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve overlayAlphaByLight = AnimationCurve.Linear(0f, 0.8f, 1f, 0f);
    [SerializeField, Min(0f)] private float smoothingSpeed = 8f;
    [SerializeField] private bool enableFlicker = true;
    [SerializeField, Min(0f)] private float flickerAmplitude = 0.12f;
    [SerializeField, Min(0f)] private float flickerSpeed = 18f;
    [SerializeField, Min(0f)] private float overlayFlickerAmount = 0.05f;
    [Header("Setup")]
    [Tooltip("If the candle point-light array is empty, auto-collect Light2D found under the Candle's children.")]
    [SerializeField] private bool autoCollectCandleChildLights = true;
    [SerializeField] private bool warnWhenNoLights = true;

    private float targetGlobalIntensity = 1f;
    private float targetPointIntensity = 1f;
    private float targetOverlayAlpha;
    private CandleLightState currentState = CandleLightState.Bright;
    private bool referencesResolved;
    private float[] globalBaseIntensities;
    private float[] candlePointBaseIntensities;

    private void Awake()
    {
        ResolveReferences();
        RefreshTargets();
        ApplyInstant();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        RefreshTargets();
        ApplyInstant();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        float smoothing = smoothingSpeed <= 0f ? 1f : 1f - Mathf.Exp(-smoothingSpeed * deltaTime);
        float flicker = GetFlickerOffset();

        float globalMultiplier = Mathf.Max(0f, targetGlobalIntensity);
        float pointMultiplier = Mathf.Max(0f, targetPointIntensity + flicker);
        float overlayAlpha = Mathf.Clamp01(targetOverlayAlpha - flicker * overlayFlickerAmount);

        ApplyLightMultiplier(globalLights, globalBaseIntensities, globalMultiplier, smoothing);
        ApplyLightMultiplier(candlePointLights, candlePointBaseIntensities, pointMultiplier, smoothing);
        ApplyOverlayAlpha(overlayAlpha, smoothing);
    }

    private void HandleLightChanged(float current, float max)
    {
        RefreshTargets();
    }

    private void HandleStateChanged(CandleLightState state)
    {
        currentState = state;
        RefreshTargets();
    }

    private void RefreshTargets()
    {
        float normalizedLight = candle != null ? candle.NormalizedLight : 1f;
        currentState = candle != null ? candle.CurrentState : CandleLightState.Bright;

        targetGlobalIntensity = EvaluateCurve(globalIntensityByLight, normalizedLight, 1f);
        targetPointIntensity = EvaluateCurve(pointIntensityByLight, normalizedLight, 1f);
        targetOverlayAlpha = EvaluateCurve(overlayAlphaByLight, normalizedLight, 0f);

        if (currentState == CandleLightState.Extinguished)
        {
            targetPointIntensity = 0f;
        }
    }

    private void ApplyInstant()
    {
        ApplyLightMultiplier(globalLights, globalBaseIntensities, targetGlobalIntensity, 1f);
        ApplyLightMultiplier(candlePointLights, candlePointBaseIntensities, targetPointIntensity, 1f);
        ApplyOverlayAlpha(targetOverlayAlpha, 1f);
    }

    private float GetFlickerOffset()
    {
        if (!enableFlicker || currentState != CandleLightState.Flickering)
        {
            return 0f;
        }

        float noise = Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0.37f) - 0.5f;
        return noise * flickerAmplitude;
    }

    private static void ApplyLightMultiplier(Light2D[] lights, float[] baseIntensities, float multiplier, float smoothing)
    {
        if (lights == null)
        {
            return;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
            {
                continue;
            }

            float baseIntensity = baseIntensities != null && i < baseIntensities.Length
                ? baseIntensities[i]
                : lights[i].intensity;
            float target = baseIntensity * multiplier;
            lights[i].intensity = Mathf.Lerp(lights[i].intensity, target, smoothing);
        }
    }

    private void ApplyOverlayAlpha(float targetAlpha, float smoothing)
    {
        if (darknessOverlay == null)
        {
            return;
        }

        Color color = darknessOverlay.color;
        color.a = Mathf.Lerp(color.a, targetAlpha, smoothing);
        darknessOverlay.color = color;
    }

    private void Subscribe()
    {
        if (candle == null)
        {
            return;
        }

        candle.LightChanged -= HandleLightChanged;
        candle.StateChanged -= HandleStateChanged;
        candle.LightChanged += HandleLightChanged;
        candle.StateChanged += HandleStateChanged;
    }

    private void Unsubscribe()
    {
        if (candle == null)
        {
            return;
        }

        candle.LightChanged -= HandleLightChanged;
        candle.StateChanged -= HandleStateChanged;
    }

    private void ResolveReferences()
    {
        if (candle == null)
        {
            candle = FindFirstObjectByType<Candle>();
        }

        if (autoCollectCandleChildLights && (candlePointLights == null || candlePointLights.Length == 0) && candle != null)
        {
            candlePointLights = candle.GetComponentsInChildren<Light2D>(true);
        }

        if (referencesResolved)
        {
            return;
        }

        referencesResolved = true;
        CaptureBaseIntensities();

        if (warnWhenNoLights && !HasAnyLight())
        {
            Debug.LogWarning(
                "[RoomLightController] No Light2D references assigned (globalLights and candlePointLights are both empty). " +
                "The candle value will not drive any light. Assign the room/candle Light2D in the Inspector.",
                this);
        }
    }

    private void CaptureBaseIntensities()
    {
        // Snapshot the intensity you authored in the Inspector so the candle scales from it
        // (intensity = authoredIntensity * multiplier) instead of overwriting it.
        globalBaseIntensities = BuildBaseIntensityArray(globalLights);
        candlePointBaseIntensities = BuildBaseIntensityArray(candlePointLights);
    }

    private static float[] BuildBaseIntensityArray(Light2D[] lights)
    {
        if (lights == null || lights.Length == 0)
        {
            return System.Array.Empty<float>();
        }

        float[] bases = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            bases[i] = lights[i] != null ? lights[i].intensity : 0f;
        }

        return bases;
    }

    private bool HasAnyLight()
    {
        return (globalLights != null && globalLights.Length > 0)
            || (candlePointLights != null && candlePointLights.Length > 0);
    }

    private static float EvaluateCurve(AnimationCurve curve, float time, float fallback)
    {
        return curve != null && curve.length > 0 ? curve.Evaluate(time) : fallback;
    }
}
