using UnityEngine;

public sealed class CameraSway : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 amplitude = new Vector2(0.125f, 0.0625f);
    [SerializeField] private Vector2 frequency = new Vector2(0.35f, 0.27f);
    [SerializeField] private bool snapToPixelGrid = true;
    [SerializeField] private float pixelsPerUnit = 16f;
    [SerializeField] private CameraShake cameraShake;

    private Vector3 basePosition;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        basePosition = target.position;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float x = Mathf.Sin(Time.time * Mathf.PI * 2f * frequency.x) * amplitude.x;
        float y = Mathf.Sin(Time.time * Mathf.PI * 2f * frequency.y) * amplitude.y;
        Vector3 shakeOffset = cameraShake != null ? cameraShake.CurrentOffset : Vector3.zero;
        Vector3 nextPosition = basePosition + new Vector3(x, y, 0f) + shakeOffset;
        nextPosition.z = basePosition.z;

        target.position = snapToPixelGrid ? Snap(nextPosition) : nextPosition;
    }

    public void Recenter()
    {
        if (target == null)
        {
            target = transform;
        }

        basePosition = target.position;
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
