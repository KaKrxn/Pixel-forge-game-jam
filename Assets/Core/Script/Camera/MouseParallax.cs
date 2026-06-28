using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class MouseParallax : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 maxOffset = new Vector2(0.5f, 0.28f);
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private bool snapToPixelGrid = true;
    [SerializeField] private float pixelsPerUnit = 16f;

    private Vector3 basePosition;
    private Vector3 velocity;
    private Vector3 smoothedPosition;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        basePosition = target.position;
        smoothedPosition = basePosition;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector2 normalizedMouse = GetNormalizedMousePosition();
        Vector3 desiredPosition = basePosition + new Vector3(
            normalizedMouse.x * maxOffset.x,
            normalizedMouse.y * maxOffset.y,
            0f);

        desiredPosition.z = basePosition.z;
        smoothedPosition = Vector3.SmoothDamp(smoothedPosition, desiredPosition, ref velocity, smoothTime);
        Vector3 nextPosition = smoothedPosition;
        target.position = snapToPixelGrid ? Snap(nextPosition) : nextPosition;
    }

    public void Recenter()
    {
        if (target == null)
        {
            target = transform;
        }

        basePosition = target.position;
        smoothedPosition = basePosition;
        velocity = Vector3.zero;
    }

    private Vector2 GetNormalizedMousePosition()
    {
        Vector2 mousePosition;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePosition = Mouse.current.position.ReadValue();
        }
        else
        {
            mousePosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
#else
        mousePosition = Input.mousePosition;
#endif

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return Vector2.zero;
        }

        float x = Mathf.Clamp01(mousePosition.x / Screen.width) * 2f - 1f;
        float y = Mathf.Clamp01(mousePosition.y / Screen.height) * 2f - 1f;
        return new Vector2(x, y);
    }

    private Vector3 Snap(Vector3 value)
    {
        if (pixelsPerUnit <= 0f)
        {
            return value;
        }

        float unit = 1f / pixelsPerUnit;
        value.x = Mathf.Round(value.x / unit) * unit;
        value.y = Mathf.Round(value.y / unit) * unit;
        return value;
    }
}
