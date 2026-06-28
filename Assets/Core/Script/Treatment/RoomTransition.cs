using UnityEngine;

public sealed class RoomTransition : MonoBehaviour
{
    [SerializeField] private GameObject counterRoomRoot;
    [SerializeField] private GameObject treatmentRoomRoot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector3 counterCameraPosition = new Vector3(0f, 0f, -10f);
    [SerializeField] private Vector3 treatmentCameraPosition = new Vector3(0f, 0f, -10f);

    public bool IsInTreatmentRoom { get; private set; }

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        ShowCounterRoom();
    }

    public void ShowCounterRoom()
    {
        IsInTreatmentRoom = false;
        ApplyRoomState(counterRoomVisible: true);
        MoveCamera(counterCameraPosition);
    }

    public void ShowTreatmentRoom()
    {
        IsInTreatmentRoom = true;
        ApplyRoomState(counterRoomVisible: false);
        MoveCamera(treatmentCameraPosition);
    }

    private void ApplyRoomState(bool counterRoomVisible)
    {
        if (counterRoomRoot != null)
        {
            counterRoomRoot.SetActive(counterRoomVisible);
        }

        if (treatmentRoomRoot != null)
        {
            treatmentRoomRoot.SetActive(!counterRoomVisible);
        }
    }

    private void MoveCamera(Vector3 targetPosition)
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.transform.position = targetPosition;

        MouseParallax mouseParallax = targetCamera.GetComponent<MouseParallax>();
        if (mouseParallax != null)
        {
            mouseParallax.Recenter();
        }

        CameraSway cameraSway = targetCamera.GetComponent<CameraSway>();
        if (cameraSway != null)
        {
            cameraSway.Recenter();
        }
    }
}
