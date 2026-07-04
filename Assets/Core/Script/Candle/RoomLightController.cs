using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public sealed class RoomLightController : MonoBehaviour
{
    [SerializeField] private Candle candle;
    [SerializeField] private Light2D[] globalLights;
    [SerializeField] private Light2D[] candlePointLights;
    [SerializeField] private Graphic darknessOverlay;
    [SerializeField] private AnimationCurve globalIntensityByLight = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);
    [SerializeField] private AnimationCurve pointIntensityByLight = AnimationCurve.Linear(0f, 0f, 1f, 1.2f);
    [SerializeField] private AnimationCurve overlayAlphaByLight = AnimationCurve.Linear(0f, 0.8f, 1f, 0f);
    [SerializeField, Min(0f)] private float smoothingSpeed = 8f;
    [SerializeField] private bool enableFlicker = true;
    [SerializeField, Min(0f)] private float flickerAmplitude = 0.12f;
    [SerializeField, Min(0f)] private float flickerSpeed = 18f;
    [SerializeField, Min(0f)] private float overlayFlickerAmount = 0.05f;

    private float targetGlobalIntensity = 1f;
    private float targetPointIntensity = 1f;
    private float targetOverlayAlpha;
    private CandleLightState currentState = CandleLightState.Bright;

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

        float globalIntensity = Mathf.Max(0f, targetGlobalIntensity);
        float pointIntensity = Mathf.Max(0f, targetPointIntensity + flicker);
        float overlayAlpha = Mathf.Clamp01(targetOverlayAlpha - flicker * overlayFlickerAmount);

        ApplyLightIntensity(globalLights, globalIntensity, smoothing);
        ApplyLightIntensity(candlePointLights, pointIntensity, smoothing);
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
        ApplyLightIntensity(globalLights, targetGlobalIntensity, 1f);
        ApplyLightIntensity(candlePointLights, targetPointIntensity, 1f);
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

    private static void ApplyLightIntensity(Light2D[] lights, float targetIntensity, float smoothing)
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

            lights[i].intensity = Mathf.Lerp(lights[i].intensity, targetIntensity, smoothing);
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
    }

    private static float EvaluateCurve(AnimationCurve curve, float time, float fallback)
    {
        return curve != null && curve.length > 0 ? curve.Evaluate(time) : fallback;
    }
}
