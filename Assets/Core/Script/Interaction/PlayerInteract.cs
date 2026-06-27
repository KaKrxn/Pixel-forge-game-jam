using UnityEngine;

public sealed class PlayerInteract : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRadius = 1.2f;
    [SerializeField] private LayerMask interactMask = ~0;

    private void Update()
    {
        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRadius, interactMask);
        if (hit == null)
        {
            return;
        }

        Interactable interactable = hit.GetComponent<Interactable>();
        interactable?.Interact();
    }
}
