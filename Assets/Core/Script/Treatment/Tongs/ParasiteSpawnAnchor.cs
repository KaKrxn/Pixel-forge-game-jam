using UnityEngine;

public sealed class ParasiteSpawnAnchor : MonoBehaviour
{
    [SerializeField] private SpriteMask woundMask;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform pullDirection;
    [SerializeField] private int requiredDirection = 1;

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

    private void OnValidate()
    {
        requiredDirection = requiredDirection < 0 ? -1 : 1;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 spawnPosition = SpawnPosition;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnPosition, 0.06f);
        Gizmos.DrawLine(spawnPosition, spawnPosition + (Vector3)PullDirection * 0.35f);
    }
}
