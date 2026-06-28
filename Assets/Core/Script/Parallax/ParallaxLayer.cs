using UnityEngine;

public sealed class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector2 parallaxStrength = new Vector2(0.08f, 0.04f);
    [SerializeField] private bool lockZ = true;
    [SerializeField] private bool snapToPixelGrid = true;
    [SerializeField] private float pixelsPerUnit = 16f;

    private Vector3 startPosition;
    private Vector3 cameraStartPosition;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (cameraTransform == null)
        {
            return;
        }

        Vector3 cameraDelta = cameraTransform.position - cameraStartPosition;
        Vector3 nextPosition = startPosition + new Vector3(
            cameraDelta.x * parallaxStrength.x,
            cameraDelta.y * parallaxStrength.y,
            0f);

        if (!lockZ)
        {
            nextPosition.z = startPosition.z + cameraDelta.z;
        }
        else
        {
            nextPosition.z = startPosition.z;
        }

        transform.position = snapToPixelGrid ? Snap(nextPosition) : nextPosition;
    }

    public void Recenter()
    {
        initialized = false;
        Initialize();
    }

    private void Initialize()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        startPosition = transform.position;
        cameraStartPosition = cameraTransform != null ? cameraTransform.position : Vector3.zero;
        initialized = true;
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
