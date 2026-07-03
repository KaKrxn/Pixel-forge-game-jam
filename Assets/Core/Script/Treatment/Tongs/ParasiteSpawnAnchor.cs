using UnityEngine;

public sealed class ParasiteSpawnAnchor : MonoBehaviour
{
    [System.Serializable]
    private sealed class SpawnVisualVariant
    {
        [SerializeField] private ParasiteType.Variant parasiteVariant = ParasiteType.Variant.Small;
        [SerializeField] private GameObject visualRoot;

        public ParasiteType.Variant ParasiteVariant => parasiteVariant;
        public GameObject VisualRoot => visualRoot;
        public bool IsValid => visualRoot != null;
    }

    [SerializeField] private SpriteMask woundMask;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform pullDirection;
    [SerializeField] private int requiredDirection = 1;
    [Header("Spawn Visual")]
    [SerializeField] private GameObject defaultVisualRoot;
    [SerializeField] private SpawnVisualVariant[] visualVariants = new SpawnVisualVariant[0];

    public SpriteMask WoundMask => woundMask;
    public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
    public int RequiredDirection => requiredDirection < 0 ? -1 : 1;

    public Vector2 PullDirection
    {
        get
        {
            if (pullDirection == null)
            {
                return Vector2.up;
            }

            Vector2 direction = pullDirection.position - SpawnPosition;
            return direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;
        }
    }

    public void HideSpawnVisuals()
    {
        SetDefaultVisualVisible(false);

        if (visualVariants == null)
        {
            return;
        }

        for (int i = 0; i < visualVariants.Length; i++)
        {
            SpawnVisualVariant visualVariant = visualVariants[i];
            if (visualVariant != null && visualVariant.VisualRoot != null)
            {
                visualVariant.VisualRoot.SetActive(false);
            }
        }
    }

    public void ShowSpawnVisual(ParasiteType parasiteType)
    {
        HideSpawnVisuals();

        if (parasiteType == null || visualVariants == null)
        {
            SetDefaultVisualVisible(true);
            return;
        }

        for (int i = 0; i < visualVariants.Length; i++)
        {
            SpawnVisualVariant visualVariant = visualVariants[i];
            if (visualVariant != null && visualVariant.IsValid && visualVariant.ParasiteVariant == parasiteType.ParasiteVariant)
            {
                visualVariant.VisualRoot.SetActive(true);
                return;
            }
        }

        SetDefaultVisualVisible(true);
    }

    private void OnValidate()
    {
        requiredDirection = requiredDirection < 0 ? -1 : 1;
    }

    private void SetDefaultVisualVisible(bool visible)
    {
        if (defaultVisualRoot != null)
        {
            defaultVisualRoot.SetActive(visible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 spawnPosition = SpawnPosition;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnPosition, 0.06f);
        Gizmos.DrawLine(spawnPosition, spawnPosition + (Vector3)PullDirection * 0.35f);
    }
}
