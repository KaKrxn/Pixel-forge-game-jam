using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Pustule : MonoBehaviour
{
    [SerializeField] private PustuleType type = PustuleType.Small;
    [SerializeField, Min(0.01f)] private float pustuleRadius = 0.4f;
    [SerializeField, Min(0.01f)] private float pierceTime = 0.4f;
    [SerializeField, Min(0f)] private float drainSpeed = 0.5f;
    [SerializeField, Min(0f)] private float painPerSecond = 0.22f;
    [SerializeField, Min(0f)] private float painDrainRate = 0.4f;
    [SerializeField, Min(0f)] private float painSpikeAmount = 25f;
    [SerializeField, Min(0f)] private float strayPenaltyPerSecond = 8f;
    [SerializeField, Min(0f)] private float rollbackPerSecond = 0.3f;
    [SerializeField, Min(0f)] private float jitterStrength;
    [SerializeField, Min(0.01f)] private float jitterFrequency = 1.8f;
    [SerializeField] private bool hideWhenCompleted = true;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color normalColor = new Color(0.96f, 0.72f, 0.34f, 1f);
    [SerializeField] private Color piercedColor = new Color(1f, 0.86f, 0.46f, 1f);
    [SerializeField] private Color completedColor = new Color(1f, 1f, 1f, 0.2f);

    private Collider2D hitCollider;
    private float pierceProgress;
    private float drainProgress;
    private float painLevel;
    private bool isPierced;
    private bool completed;
    private float jitterSeed;
    private Vector2 currentTip;
    private Vector2 currentSteerPoint;
    private bool isOutsideRadius;

    public PustuleType Type => type;
    public float PierceProgress => pierceProgress;
    public float DrainProgress => drainProgress;
    public float OverallProgress => isPierced ? 0.5f + drainProgress * 0.5f : pierceProgress * 0.5f;
    public float PainLevel => painLevel;
    public bool IsPierced => isPierced;
    public bool IsCompleted => completed;
    public bool CanPierce => !completed && !isPierced;
    public bool CanSqueeze => !completed && isPierced && type == PustuleType.Small;
    public bool CanDrain => !completed && isPierced && type == PustuleType.Big;

    public event Action<Pustule> Completed;
    public event Action<float> PainChanged;
    public event Action<float> ProgressChanged;

    private void Awake()
    {
        ResolveReferences();
        ResetRuntimeState();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void ResetRuntimeState()
    {
        pierceProgress = 0f;
        drainProgress = 0f;
        painLevel = 0f;
        isPierced = false;
        completed = false;
        isOutsideRadius = false;
        jitterSeed = UnityEngine.Random.value * 1000f;
        currentTip = transform.position;
        currentSteerPoint = transform.position;
        gameObject.SetActive(true);
        ApplyVisualState();
        PainChanged?.Invoke(painLevel);
        ProgressChanged?.Invoke(OverallProgress);
    }

    public bool ContainsPoint(Vector2 worldPoint)
    {
        ResolveReferences();
        return hitCollider != null && hitCollider.OverlapPoint(worldPoint);
    }

    public void SetNeedleTipPreview(Vector2 worldPoint)
    {
        currentTip = worldPoint;
        currentSteerPoint = worldPoint;
    }

    public void TickPierce(float deltaTime)
    {
        if (!CanPierce || deltaTime <= 0f)
        {
            DrainPain(deltaTime);
            return;
        }

        pierceProgress = Mathf.Clamp01(pierceProgress + deltaTime / Mathf.Max(0.01f, pierceTime));
        ProgressChanged?.Invoke(OverallProgress);

        if (pierceProgress >= 1f)
        {
            isPierced = true;
            ApplyVisualState();
        }
    }

    public void TickSqueeze(float deltaTime, Sanity sanity)
    {
        if (!CanSqueeze || deltaTime <= 0f)
        {
            DrainPain(deltaTime);
            return;
        }

        isOutsideRadius = false;
        ApplyPain(painPerSecond, deltaTime, sanity);
        AdvanceDrain(deltaTime);
    }

    public void TickDrain(Vector2 pointerWorldPosition, float deltaTime, Sanity sanity)
    {
        if (!CanDrain || deltaTime <= 0f)
        {
            DrainPain(deltaTime);
            return;
        }

        currentSteerPoint = pointerWorldPosition;
        currentTip = ApplyDrainJitter(pointerWorldPosition);
        ApplyPain(painPerSecond, deltaTime, sanity);

        float distance = Vector2.Distance(currentTip, transform.position);
        isOutsideRadius = distance > pustuleRadius;
        if (isOutsideRadius)
        {
            sanity?.AddSanity(strayPenaltyPerSecond * deltaTime);
            RollBackDrain(deltaTime);
            return;
        }

        AdvanceDrain(deltaTime);
    }

    public void TickIdle(float deltaTime)
    {
        DrainPain(deltaTime);
    }

    private Vector2 ApplyDrainJitter(Vector2 pointerWorldPosition)
    {
        if (type != PustuleType.Big || jitterStrength <= 0f)
        {
            return pointerWorldPosition;
        }

        float frequency = Mathf.Max(0.01f, jitterFrequency);
        float noiseX = Mathf.PerlinNoise((Time.time + jitterSeed) * frequency, jitterSeed);
        float noiseY = Mathf.PerlinNoise(jitterSeed, (Time.time + jitterSeed) * frequency);
        Vector2 jitter = new Vector2(noiseX - 0.5f, noiseY - 0.5f) * (2f * jitterStrength);
        return pointerWorldPosition + jitter;
    }

    private void AdvanceDrain(float deltaTime)
    {
        float previous = drainProgress;
        drainProgress = Mathf.Clamp01(drainProgress + drainSpeed * deltaTime);
        if (!Mathf.Approximately(previous, drainProgress))
        {
            ProgressChanged?.Invoke(OverallProgress);
        }

        if (drainProgress >= 1f)
        {
            CompletePustule();
        }
    }

    private void RollBackDrain(float deltaTime)
    {
        if (rollbackPerSecond <= 0f)
        {
            return;
        }

        float previous = drainProgress;
        drainProgress = Mathf.Clamp01(drainProgress - rollbackPerSecond * deltaTime);
        if (!Mathf.Approximately(previous, drainProgress))
        {
            ProgressChanged?.Invoke(OverallProgress);
        }
    }

    private void ApplyPain(float rate, float deltaTime, Sanity sanity)
    {
        if (rate <= 0f)
        {
            return;
        }

        SetPain(painLevel + rate * deltaTime);
        if (painLevel >= 1f)
        {
            sanity?.AddSanity(painSpikeAmount);
            SetPain(0f);
        }
    }

    private void DrainPain(float deltaTime)
    {
        if (deltaTime <= 0f || painLevel <= 0f || painDrainRate <= 0f)
        {
            return;
        }

        SetPain(painLevel - painDrainRate * deltaTime);
    }

    private void SetPain(float value)
    {
        float previous = painLevel;
        painLevel = Mathf.Clamp01(value);
        if (!Mathf.Approximately(previous, painLevel))
        {
            PainChanged?.Invoke(painLevel);
        }
    }

    private void CompletePustule()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        drainProgress = 1f;
        painLevel = 0f;
        ApplyVisualState();
        ProgressChanged?.Invoke(OverallProgress);
        PainChanged?.Invoke(painLevel);

        if (hideWhenCompleted)
        {
            gameObject.SetActive(false);
        }

        Completed?.Invoke(this);
    }

    private void ResolveReferences()
    {
        if (hitCollider == null)
        {
            hitCollider = GetComponent<Collider2D>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void ApplyVisualState()
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (completed)
        {
            targetRenderer.color = completedColor;
        }
        else if (isPierced)
        {
            targetRenderer.color = piercedColor;
        }
        else
        {
            targetRenderer.color = normalColor;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;
        Gizmos.color = isOutsideRadius ? Color.red : Color.green;
        DrawCircle(center, pustuleRadius, 32);

        if (type == PustuleType.Big && jitterStrength > 0f)
        {
            Gizmos.color = Color.yellow;
            DrawCircle(currentSteerPoint, jitterStrength, 24);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(currentTip, 0.05f);
    }

    private static void DrawCircle(Vector3 center, float radius, int segments)
    {
        if (radius <= 0f || segments < 3)
        {
            return;
        }

        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
