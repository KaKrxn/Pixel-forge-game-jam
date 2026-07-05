using System;
using UnityEngine;

/// <summary>
/// Shared feedback bus for the treatment mini games. It carries the current <see cref="Pain"/> (0..1) for
/// the cursor feedback and raises <see cref="Hurt"/> (intensity 0..1) when the customer is hurt, for the
/// camera shake (and future SFX / screen-flash). Mini games push through the static helpers so they do not
/// need a reference; consumers (cursor, camera) subscribe to the instance events.
/// </summary>
public sealed class TreatmentFeedback : MonoBehaviour
{
    public static TreatmentFeedback Instance { get; private set; }

    [Tooltip("Sanity amount that maps to a full-intensity hurt (e.g. the pain-full spike amount).")]
    [SerializeField, Min(0.01f)] private float hurtReference = 25f;

    /// <summary>Latest pain value 0..1.</summary>
    public float Pain { get; private set; }

    public event Action<float> PainChanged;
    public event Action<float> Hurt; // intensity 0..1

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetPain(float pain)
    {
        float clamped = Mathf.Clamp01(pain);
        if (Mathf.Approximately(clamped, Pain))
        {
            return;
        }

        Pain = clamped;
        PainChanged?.Invoke(Pain);
    }

    /// <summary>Report a hurt using the raw sanity amount added; converted to a 0..1 intensity.</summary>
    public void ReportHurt(float sanityAmount)
    {
        if (sanityAmount <= 0f)
        {
            return;
        }

        float intensity = Mathf.Clamp01(sanityAmount / hurtReference);
        Hurt?.Invoke(intensity);
    }

    /// <summary>Null-safe push of the current pain from anywhere (no reference needed).</summary>
    public static void PushPain(float pain)
    {
        if (Instance != null)
        {
            Instance.SetPain(pain);
        }
    }

    /// <summary>Null-safe push of a hurt (raw sanity amount) from anywhere.</summary>
    public static void PushHurt(float sanityAmount)
    {
        if (Instance != null)
        {
            Instance.ReportHurt(sanityAmount);
        }
    }
}
