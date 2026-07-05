using System;
using UnityEngine;

public enum SanityState
{
    Stable,
    Warning,
    Critical,
    Transformed
}

public sealed class Sanity : MonoBehaviour
{
    [SerializeField] private Candle candle;
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float currentSanity;
    [SerializeField] private float candleProtectedIncreaseRate = 0.35f;
    [SerializeField] private float candleOutIncreaseRate = 5f;
    [SerializeField] private bool useCandleTierMultipliers = true;
    [SerializeField] private float lowLightSanityMultiplier = 1.5f;
    [SerializeField] private float flickeringSanityMultiplier = 2.5f;
    [SerializeField] private float extinguishedSanityMultiplier = 4f;
    [SerializeField] private float treatmentStressIncreaseRate = 1.5f;
    [SerializeField] private float warningThreshold = 50f;
    [SerializeField] private float criticalThreshold = 80f;
    [SerializeField] private bool monitoring;

    public float CurrentSanity => currentSanity;
    public float MaxSanity => maxSanity;
    public float NormalizedSanity => maxSanity <= 0f ? 0f : Mathf.Clamp01(currentSanity / maxSanity);
    public bool IsTransformed => CurrentState == SanityState.Transformed;
    public SanityState CurrentState { get; private set; }

    public event Action<float, float> SanityChanged;
    public event Action<SanityState> StateChanged;
    public event Action WarningReached;
    public event Action CriticalReached;
    public event Action TransformationReached;

    private bool treatmentStressActive;

    private void Awake()
    {
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
        SetState(EvaluateState(), notify: false);
        SanityChanged?.Invoke(currentSanity, maxSanity);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (!enabled || deltaTime <= 0f)
        {
            return;
        }

        if (!monitoring || IsTransformed)
        {
            return;
        }

        float rate = GetCandlePressureRate();

        if (treatmentStressActive)
        {
            rate += treatmentStressIncreaseRate;
        }

        ChangeSanity(rate * deltaTime);
    }

    public void SetCandle(Candle source)
    {
        candle = source;
    }

    public void BeginMonitoring()
    {
        monitoring = true;
    }

    public void StopMonitoring()
    {
        monitoring = false;
        treatmentStressActive = false;
    }

    public void ResetSanity(float value = 0f)
    {
        currentSanity = Mathf.Clamp(value, 0f, maxSanity);
        treatmentStressActive = false;
        SetState(EvaluateState(), notify: true);
        SanityChanged?.Invoke(currentSanity, maxSanity);
    }

    public void SetTreatmentStress(bool active)
    {
        treatmentStressActive = active;
    }

    public void AddSanity(float amount)
    {
        ChangeSanity(amount);

        // Explicit sanity adds come from treatment mistakes (pain spike, stray/edge). Passive rise goes
        // through Tick/ChangeSanity, so this only fires "hurt" feedback on real mistakes.
        TreatmentFeedback.PushHurt(amount);
    }

    private float GetCandlePressureRate()
    {
        if (candle == null)
        {
            return candleProtectedIncreaseRate;
        }

        if (!useCandleTierMultipliers)
        {
            return candle.IsLit ? candleProtectedIncreaseRate : candleOutIncreaseRate;
        }

        switch (candle.CurrentState)
        {
            case CandleLightState.Low:
                return candleProtectedIncreaseRate * lowLightSanityMultiplier;
            case CandleLightState.Flickering:
                return candleProtectedIncreaseRate * flickeringSanityMultiplier;
            case CandleLightState.Extinguished:
                return candleProtectedIncreaseRate * extinguishedSanityMultiplier;
            case CandleLightState.Bright:
            case CandleLightState.Refilling:
            default:
                return candleProtectedIncreaseRate;
        }
    }

    private void ChangeSanity(float amount)
    {
        if (Mathf.Approximately(amount, 0f))
        {
            return;
        }

        currentSanity = Mathf.Clamp(currentSanity + amount, 0f, maxSanity);
        SanityChanged?.Invoke(currentSanity, maxSanity);
        SetState(EvaluateState(), notify: true);
    }

    private SanityState EvaluateState()
    {
        if (currentSanity >= maxSanity)
        {
            return SanityState.Transformed;
        }

        if (currentSanity >= criticalThreshold)
        {
            return SanityState.Critical;
        }

        if (currentSanity >= warningThreshold)
        {
            return SanityState.Warning;
        }

        return SanityState.Stable;
    }

    private void SetState(SanityState nextState, bool notify)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        CurrentState = nextState;

        if (!notify)
        {
            return;
        }

        StateChanged?.Invoke(CurrentState);

        switch (CurrentState)
        {
            case SanityState.Warning:
                WarningReached?.Invoke();
                break;
            case SanityState.Critical:
                CriticalReached?.Invoke();
                break;
            case SanityState.Transformed:
                monitoring = false;
                treatmentStressActive = false;
                TransformationReached?.Invoke();
                break;
        }
    }
}
