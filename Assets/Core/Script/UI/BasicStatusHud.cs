using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class BasicStatusHud : MonoBehaviour
{
    [SerializeField] private Candle candle;
    [SerializeField] private Sanity sanity;
    [SerializeField] private Slider candleSlider;
    [SerializeField] private Slider sanitySlider;
    [SerializeField] private TMP_Text candleLabel;
    [SerializeField] private TMP_Text sanityLabel;

    private void OnEnable()
    {
        Subscribe();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetSources(Candle candleSource, Sanity sanitySource)
    {
        Unsubscribe();
        candle = candleSource;
        sanity = sanitySource;
        Subscribe();
        RefreshAll();
    }

    private void Subscribe()
    {
        if (candle != null)
        {
            candle.LightChanged += HandleCandleChanged;
            candle.StateChanged += HandleCandleStateChanged;
        }

        if (sanity != null)
        {
            sanity.SanityChanged += HandleSanityChanged;
            sanity.StateChanged += HandleSanityStateChanged;
        }
    }

    private void Unsubscribe()
    {
        if (candle != null)
        {
            candle.LightChanged -= HandleCandleChanged;
            candle.StateChanged -= HandleCandleStateChanged;
        }

        if (sanity != null)
        {
            sanity.SanityChanged -= HandleSanityChanged;
            sanity.StateChanged -= HandleSanityStateChanged;
        }
    }

    private void RefreshAll()
    {
        if (candle != null)
        {
            HandleCandleChanged(candle.CurrentLight, candle.MaxLight);
            HandleCandleStateChanged(candle.CurrentState);
        }

        if (sanity != null)
        {
            HandleSanityChanged(sanity.CurrentSanity, sanity.MaxSanity);
            HandleSanityStateChanged(sanity.CurrentState);
        }
    }

    private void HandleCandleChanged(float current, float max)
    {
        if (candleSlider != null)
        {
            candleSlider.value = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }
    }

    private void HandleCandleStateChanged(CandleLightState state)
    {
        if (candleLabel != null)
        {
            candleLabel.text = $"Candle: {state}";
        }
    }

    private void HandleSanityChanged(float current, float max)
    {
        if (sanitySlider != null)
        {
            sanitySlider.value = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }
    }

    private void HandleSanityStateChanged(SanityState state)
    {
        if (sanityLabel != null)
        {
            sanityLabel.text = $"Sanity: {state}";
        }
    }
}
