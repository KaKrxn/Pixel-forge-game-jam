using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    [SerializeField] private float refillRate = 8f;
    [SerializeField] private float clickRefillAmount = 3f;
    [SerializeField] private int clickRefillCapPerSecond = 8;
    [SerializeField] private float lowLightThreshold = 60f;
    [SerializeField] private float flickeringThreshold = 25f;
    [SerializeField] private bool drainOnPlay = true;
    [SerializeField] private Collider2D refillHitArea;

    public float CurrentLight => currentLight;
    public float MaxLight => maxLight;
    public float NormalizedLight => maxLight <= 0f ? 0f : Mathf.Clamp01(currentLight / maxLight);
    public bool IsLit => currentLight > 0f;
    public bool IsAtCounter => isAtCounter;
    public bool IsRefilling => isRefilling;
    public CandleLightState CurrentState { get; private set; }

    public event Action<float, float> LightChanged;
    public event Action<CandleLightState> StateChanged;
    public event Action Extinguished;
    public event Action Relit;

    private bool isRefilling;
    private bool isAtCounter = true;
    private int clicksThisSecond;
    private float clickWindowTimer;

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

        UpdateClickWindow(deltaTime);
        HandleCounterRefillInput(deltaTime);

        if (isRefilling && isAtCounter)
        {
            ChangeLight(refillRate * deltaTime);
        }

        if (drainOnPlay)
        {
            ChangeLight(-drainRate * deltaTime);
        }
    }

    public void StartRefill()
    {
        if (!isAtCounter)
        {
            return;
        }

        SetRefilling(true);
    }

    public void StopRefill()
    {
        SetRefilling(false);
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

    public void SetAtCounter(bool atCounter)
    {
        if (isAtCounter == atCounter)
        {
            return;
        }

        isAtCounter = atCounter;

        if (!isAtCounter)
        {
            clicksThisSecond = 0;
            clickWindowTimer = 0f;
            SetRefilling(false);
        }
    }

    private void UpdateClickWindow(float deltaTime)
    {
        clickWindowTimer += deltaTime;
        if (clickWindowTimer < 1f)
        {
            return;
        }

        clickWindowTimer = 0f;
        clicksThisSecond = 0;
    }

    private void HandleCounterRefillInput(float deltaTime)
    {
        if (!isAtCounter || !IsPointerInRefillArea())
        {
            SetRefilling(false);
            return;
        }

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            SetRefilling(false);
            return;
        }

        SetRefilling(mouse.leftButton.isPressed);

        if (mouse.leftButton.wasPressedThisFrame && clicksThisSecond < clickRefillCapPerSecond)
        {
            clicksThisSecond++;
            ChangeLight(clickRefillAmount);
        }
#else
        SetRefilling(false);
#endif
    }

    private bool IsPointerInRefillArea()
    {
        if (refillHitArea == null)
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        Camera camera = Camera.main;
        if (mouse == null || camera == null)
        {
            return false;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        Vector2 worldPosition = camera.ScreenToWorldPoint(screenPosition);
        return refillHitArea.OverlapPoint(worldPosition);
#else
        return false;
#endif
    }

    private void SetRefilling(bool refilling)
    {
        if (isRefilling == refilling)
        {
            return;
        }

        isRefilling = refilling;
        SetState(isRefilling ? CandleLightState.Refilling : EvaluateState(), notify: true);
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
