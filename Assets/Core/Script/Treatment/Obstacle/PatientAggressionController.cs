using System;
using UnityEngine;

[RequireComponent(typeof(BodyJitterController))]
public sealed class PatientAggressionController : MonoBehaviour
{
    [SerializeField] private PatientAggressionProfile profile;
    [SerializeField] private Sanity sanity;
    [SerializeField] private bool useUnscaledTime;

    public event Action AggressionStarted;
    public event Action AggressionEnded;
    public event Action AggressionSuppressed;

    private BodyJitterController jitter;
    private Transform bodyMotionRoot;
    private float cooldownRemaining;
    private float suppressRemaining;
    private bool isRunning;
    private bool isPaused;
    private bool wasBursting;

    private void Awake()
    {
        jitter = GetComponent<BodyJitterController>();
    }

    private void Update()
    {
        if (!isRunning || isPaused)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Tick(deltaTime);
    }

    public void Begin(Sanity nextSanity, Transform nextBodyMotionRoot, PatientAggressionProfile nextProfile)
    {
        sanity = nextSanity;
        profile = nextProfile;
        bodyMotionRoot = nextBodyMotionRoot;

        EnsureJitter();
        jitter.Bind(bodyMotionRoot);
        isRunning = profile != null && profile.AggressionEnabled && bodyMotionRoot != null;
        isPaused = false;
        wasBursting = false;
        suppressRemaining = 0f;
        cooldownRemaining = isRunning ? UnityEngine.Random.Range(profile.CooldownMin, profile.CooldownMax) : 0f;
    }

    public void Stop()
    {
        EnsureJitter();
        jitter.StopAndReset();
        isRunning = false;
        isPaused = false;
        wasBursting = false;
        suppressRemaining = 0f;
        cooldownRemaining = 0f;
        bodyMotionRoot = null;
    }

    public void Pause()
    {
        if (!isRunning)
        {
            return;
        }

        isPaused = true;
        EnsureJitter();
        jitter.StopAndReset();
        wasBursting = false;
    }

    public void Resume()
    {
        if (!isRunning)
        {
            return;
        }

        isPaused = false;
    }

    public void Suppress(float duration)
    {
        suppressRemaining = Mathf.Max(suppressRemaining, duration);
        EnsureJitter();
        jitter.StopAndReset();
        AggressionSuppressed?.Invoke();
    }

    private void Tick(float deltaTime)
    {
        if (profile == null || sanity == null || bodyMotionRoot == null)
        {
            return;
        }

        if (suppressRemaining > 0f)
        {
            suppressRemaining -= deltaTime;
            return;
        }

        EnsureJitter();
        bool wasActive = jitter.IsBursting;
        jitter.Tick(deltaTime);
        bool isActive = jitter.IsBursting;

        if (!wasActive && isActive)
        {
            AggressionStarted?.Invoke();
        }
        else if (wasActive && !isActive)
        {
            AggressionEnded?.Invoke();
            cooldownRemaining = UnityEngine.Random.Range(profile.CooldownMin, profile.CooldownMax);
        }

        wasBursting = isActive;
        if (isActive)
        {
            return;
        }

        cooldownRemaining -= deltaTime;
        if (cooldownRemaining > 0f)
        {
            return;
        }

        float chance = profile.BaseChancePerSecond * profile.GetChanceMultiplier(sanity.CurrentState) * deltaTime;
        if (chance > 0f && UnityEngine.Random.value <= chance)
        {
            jitter.StartBurst(profile);
            if (!wasBursting && jitter.IsBursting)
            {
                AggressionStarted?.Invoke();
            }
        }
    }

    private void EnsureJitter()
    {
        if (jitter == null)
        {
            jitter = GetComponent<BodyJitterController>();
        }

        if (jitter == null)
        {
            jitter = gameObject.AddComponent<BodyJitterController>();
        }
    }
}
