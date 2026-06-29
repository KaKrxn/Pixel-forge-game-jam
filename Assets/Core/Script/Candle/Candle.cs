using System;
using UnityEngine;

public enum CandleLightState
{
    Bright,
    Low,
    Flickering,
    Extinguished,
    Refilling
}

public sealed class Candle : MonoBehaviour
{
    [SerializeField] private float maxLight = 100f;
    [SerializeField] private float currentLight = 100f;
    [SerializeField] private float drainRate = 2f;
    [SerializeField] private float refillRate = 30f;
    [SerializeField] private float lowLightThreshold = 30f;
    [SerializeField] private float flickeringThreshold = 10f;
    [SerializeField] private bool drainOnPlay = true;

    public float CurrentLight => currentLight;
    public float MaxLight => maxLight;
    public float NormalizedLight => maxLight <= 0f ? 0f : Mathf.Clamp01(currentLight / maxLight);
    public bool IsLit => currentLight > 0f;
    public CandleLightState CurrentState { get; private set; }

    public event Action<float, float> LightChanged;
    public event Action<CandleLightState> StateChanged;
    public event Action Extinguished;
    public event Action Relit;

    private bool isRefilling;

    private void Awake()
    {
        currentLight = Mathf.Clamp(currentLight, 0f, maxLight);
        SetState(EvaluateState(), notify: false);
        LightChanged?.Invoke(currentLight, maxLight);
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

        if (isRefilling)
        {
            ChangeLight(refillRate * deltaTime);
            return;
        }

        if (drainOnPlay)
        {
            ChangeLight(-drainRate * deltaTime);
        }
    }

    public void StartRefill()
    {
        isRefilling = true;
        SetState(CandleLightState.Refilling, notify: true);
    }

    public void StopRefill()
    {
        isRefilling = false;
        SetState(EvaluateState(), notify: true);
    }

    public void AddLight(float amount)
    {
        ChangeLight(amount);
    }

    public void RefillToFull()
    {
        ChangeLight(maxLight - currentLight);
    }

    public void SetDrainEnabled(bool enabled)
    {
        drainOnPlay = enabled;
    }

    private void ChangeLight(float amount)
    {
        if (Mathf.Approximately(amount, 0f))
        {
            return;
        }

        bool wasLit = IsLit;
        currentLight = Mathf.Clamp(currentLight + amount, 0f, maxLight);
        bool isLit = IsLit;

        LightChanged?.Invoke(currentLight, maxLight);

        if (wasLit && !isLit)
        {
            Extinguished?.Invoke();
        }
        else if (!wasLit && isLit)
        {
            Relit?.Invoke();
        }

        SetState(isRefilling ? CandleLightState.Refilling : EvaluateState(), notify: true);
    }

    private CandleLightState EvaluateState()
    {
        if (currentLight <= 0f)
        {
            return CandleLightState.Extinguished;
        }

        if (currentLight <= flickeringThreshold)
        {
            return CandleLightState.Flickering;
        }

        if (currentLight <= lowLightThreshold)
        {
            return CandleLightState.Low;
        }

        return CandleLightState.Bright;
    }

    private void SetState(CandleLightState nextState, bool notify)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        CurrentState = nextState;

        if (notify)
        {
            StateChanged?.Invoke(CurrentState);
        }
    }
}
