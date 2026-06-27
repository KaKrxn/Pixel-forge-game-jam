using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class CustomerLayer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private string outsideSortingLayer = "Wall";
    [SerializeField] private int outsideSortingOrder = -10;
    [SerializeField] private string insideSortingLayer = "Customer";
    [SerializeField] private int insideSortingOrder;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void SetOutsideLayer()
    {
        ApplyLayer(outsideSortingLayer, outsideSortingOrder);
    }

    public void SetInsideLayer()
    {
        ApplyLayer(insideSortingLayer, insideSortingOrder);
    }

    private void ApplyLayer(string sortingLayer, int sortingOrder)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.sortingLayerName = sortingLayer;
        targetRenderer.sortingOrder = sortingOrder;
    }
}
