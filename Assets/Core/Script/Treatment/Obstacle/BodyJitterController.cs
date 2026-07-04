using UnityEngine;

public sealed class BodyJitterController : MonoBehaviour
{
    private Transform target;
    private Vector3 originalLocalPosition;
    private Vector2 lurchOffset;
    private PatientAggressionProfile activeProfile;
    private float burstDuration;
    private float burstElapsed;
    private bool isBursting;
    private bool hasTarget;

    public bool IsBursting => isBursting;

    public void Bind(Transform nextTarget)
    {
        StopAndReset();
        target = nextTarget;
        hasTarget = target != null;
        originalLocalPosition = hasTarget ? target.localPosition : Vector3.zero;
    }

    public void StartBurst(PatientAggressionProfile profile)
    {
        if (!hasTarget || profile == null || !profile.AggressionEnabled)
        {
            return;
        }

        activeProfile = profile;
        burstDuration = Random.Range(profile.BurstDurationMin, profile.BurstDurationMax);
        burstElapsed = 0f;
        isBursting = true;

        lurchOffset = Vector2.zero;
        if (Random.value <= profile.LurchChancePerBurst)
        {
            lurchOffset = Random.insideUnitCircle.normalized * profile.LurchAmplitude;
        }
    }

    public void Tick(float deltaTime)
    {
        if (!isBursting || !hasTarget || activeProfile == null)
        {
            return;
        }

        burstElapsed += deltaTime;
        float normalized = burstDuration <= 0f ? 1f : Mathf.Clamp01(burstElapsed / burstDuration);
        float envelope = Mathf.Sin(normalized * Mathf.PI);
        float time = burstElapsed * activeProfile.ShakeFrequency;
        Vector2 shake = new Vector2(Mathf.Sin(time), Mathf.Sin(time * 0.71f + 1.37f));
        shake *= activeProfile.ShakeAmplitude * envelope;
        Vector2 lurch = Vector2.Lerp(lurchOffset, Vector2.zero, normalized) * envelope;
        Vector2 offset = shake + lurch;

        target.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);

        if (burstElapsed >= burstDuration)
        {
            StopAndReset();
        }
    }

    public void StopAndReset()
    {
        if (hasTarget && target != null)
        {
            target.localPosition = originalLocalPosition;
        }

        isBursting = false;
        activeProfile = null;
        burstElapsed = 0f;
        burstDuration = 0f;
        lurchOffset = Vector2.zero;
    }
}
