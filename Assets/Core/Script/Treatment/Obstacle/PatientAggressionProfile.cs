using UnityEngine;

[CreateAssetMenu(fileName = "PatientAggressionProfile", menuName = "Pixel Forge/Treatment/Patient Aggression Profile")]
public sealed class PatientAggressionProfile : ScriptableObject
{
    [SerializeField] private bool aggressionEnabled = true;
    [SerializeField, Min(0f)] private float baseChancePerSecond = 0.08f;
    [SerializeField, Min(0f)] private float stableChanceMultiplier;
    [SerializeField, Min(0f)] private float warningChanceMultiplier = 1f;
    [SerializeField, Min(0f)] private float criticalChanceMultiplier = 2f;
    [SerializeField, Min(0f)] private float burstDurationMin = 0.5f;
    [SerializeField, Min(0f)] private float burstDurationMax = 1.25f;
    [SerializeField, Min(0f)] private float cooldownMin = 3f;
    [SerializeField, Min(0f)] private float cooldownMax = 7f;
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float shakeFrequency = 18f;
    [SerializeField, Min(0f)] private float lurchAmplitude = 0.12f;
    [SerializeField, Range(0f, 1f)] private float lurchChancePerBurst = 0.35f;

    public bool AggressionEnabled => aggressionEnabled;
    public float BaseChancePerSecond => baseChancePerSecond;
    public float BurstDurationMin => burstDurationMin;
    public float BurstDurationMax => Mathf.Max(burstDurationMin, burstDurationMax);
    public float CooldownMin => cooldownMin;
    public float CooldownMax => Mathf.Max(cooldownMin, cooldownMax);
    public float ShakeAmplitude => shakeAmplitude;
    public float ShakeFrequency => shakeFrequency;
    public float LurchAmplitude => lurchAmplitude;
    public float LurchChancePerBurst => lurchChancePerBurst;

    public float GetChanceMultiplier(SanityState state)
    {
        switch (state)
        {
            case SanityState.Stable:
                return stableChanceMultiplier;
            case SanityState.Warning:
                return warningChanceMultiplier;
            case SanityState.Critical:
                return criticalChanceMultiplier;
            case SanityState.Transformed:
            default:
                return 0f;
        }
    }
}
