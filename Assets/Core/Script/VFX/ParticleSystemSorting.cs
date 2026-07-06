using UnityEngine;

[RequireComponent(typeof(ParticleSystemRenderer))]
public sealed class ParticleSystemSorting : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "Customer";
    [SerializeField] private int sortingOrder = 60;

    private ParticleSystemRenderer particleRenderer;

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    public void Apply()
    {
        if (particleRenderer == null)
        {
            particleRenderer = GetComponent<ParticleSystemRenderer>();
        }

        if (particleRenderer == null)
        {
            return;
        }

        particleRenderer.sortingLayerName = sortingLayerName;
        particleRenderer.sortingOrder = sortingOrder;
    }
}
