using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class CandleRefillParticleController : MonoBehaviour
{
    [SerializeField] private Candle candle;
    [SerializeField] private ParticleSystem refillParticles;
    [SerializeField] private bool onlyPlayAtCounter = true;

    private void Awake()
    {
        ResolveReferences();
        RefreshPlayback();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        RefreshPlayback();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopParticles();
    }

    private void Update()
    {
        RefreshPlayback();
    }

    private void Subscribe()
    {
        if (candle == null)
        {
            return;
        }

        candle.StateChanged -= HandleCandleStateChanged;
        candle.StateChanged += HandleCandleStateChanged;
    }

    private void Unsubscribe()
    {
        if (candle == null)
        {
            return;
        }

        candle.StateChanged -= HandleCandleStateChanged;
    }

    private void HandleCandleStateChanged(CandleLightState state)
    {
        RefreshPlayback();
    }

    private void RefreshPlayback()
    {
        if (refillParticles == null)
        {
            return;
        }

        bool shouldPlay = candle != null && candle.IsRefilling;
        if (onlyPlayAtCounter)
        {
            shouldPlay &= candle != null && candle.IsAtCounter;
        }

        if (shouldPlay)
        {
            if (!refillParticles.isPlaying)
            {
                refillParticles.Play();
            }
        }
        else
        {
            StopParticles();
        }
    }

    private void StopParticles()
    {
        if (refillParticles == null || !refillParticles.isPlaying)
        {
            return;
        }

        refillParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void ResolveReferences()
    {
        if (refillParticles == null)
        {
            refillParticles = GetComponent<ParticleSystem>();
        }

        if (candle == null)
        {
            candle = GetComponentInParent<Candle>();
        }

        if (candle == null)
        {
            candle = FindFirstObjectByType<Candle>();
        }
    }
}
