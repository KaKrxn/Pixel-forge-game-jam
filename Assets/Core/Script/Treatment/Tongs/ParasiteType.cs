using UnityEngine;

[CreateAssetMenu(fileName = "ParasiteType", menuName = "Pixel Forge/Treatment/Parasite Type")]
public sealed class ParasiteType : ScriptableObject
{
    public enum Variant
    {
        Small,
        Long,
        Big
    }

    [SerializeField] private Variant variant = Variant.Small;
    [SerializeField, Min(0.01f)] private float requiredDistance = 1f;
    [SerializeField, Min(0f)] private float pullSpeed = 1.5f;
    [SerializeField] private bool needsDirection;
    [SerializeField, Min(0f)] private float painPerSecond = 0.25f;
    [SerializeField, Min(0f)] private float painDrainRate = 0.4f;
    [SerializeField, Min(0f)] private float painSpikeAmount = 25f;
    [SerializeField, Min(0f)] private float edgePenaltyPerSecond = 8f;
    [SerializeField, Min(0f)] private float channelHalfWidth = 0.4f;
    [SerializeField, Min(0f)] private float jitterStrength;
    [SerializeField, Min(0f)] private float jitterFrequency = 1.5f;

    public Variant ParasiteVariant => variant;
    public float RequiredDistance => requiredDistance;
    public float PullSpeed => pullSpeed;
    public bool NeedsDirection => needsDirection;
    public float PainPerSecond => painPerSecond;
    public float PainDrainRate => painDrainRate;
    public float PainSpikeAmount => painSpikeAmount;
    public float EdgePenaltyPerSecond => edgePenaltyPerSecond;
    public float ChannelHalfWidth => channelHalfWidth;
    public float JitterStrength => jitterStrength;
    public float JitterFrequency => jitterFrequency;
}
