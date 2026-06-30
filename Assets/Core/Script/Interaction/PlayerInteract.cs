using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerInteract : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRadius = 1.2f;
    [SerializeField] private LayerMask interactMask = ~0;

    private void Update()
    {
        if (!WasInteractPressedThisFrame())
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

    private bool WasInteractPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        Key key = ToInputSystemKey(interactKey);
        return key != Key.None && Keyboard.current[key].wasPressedThisFrame;
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static Key ToInputSystemKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Space:
                return Key.Space;
            case KeyCode.Return:
                return Key.Enter;
            case KeyCode.Escape:
                return Key.Escape;
            case KeyCode.LeftShift:
                return Key.LeftShift;
            case KeyCode.RightShift:
                return Key.RightShift;
            case KeyCode.LeftControl:
                return Key.LeftCtrl;
            case KeyCode.RightControl:
                return Key.RightCtrl;
            case KeyCode.LeftAlt:
                return Key.LeftAlt;
            case KeyCode.RightAlt:
                return Key.RightAlt;
            case KeyCode.Alpha0:
                return Key.Digit0;
            case KeyCode.Alpha1:
                return Key.Digit1;
            case KeyCode.Alpha2:
                return Key.Digit2;
            case KeyCode.Alpha3:
                return Key.Digit3;
            case KeyCode.Alpha4:
                return Key.Digit4;
            case KeyCode.Alpha5:
                return Key.Digit5;
            case KeyCode.Alpha6:
                return Key.Digit6;
            case KeyCode.Alpha7:
                return Key.Digit7;
            case KeyCode.Alpha8:
                return Key.Digit8;
            case KeyCode.Alpha9:
                return Key.Digit9;
        }

        return System.Enum.TryParse(keyCode.ToString(), out Key key) ? key : Key.None;
    }
#endif
}
