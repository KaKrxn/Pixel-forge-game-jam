using System;
using System.Collections;
using UnityEngine;

public sealed class RoomTransition : MonoBehaviour
{
    [SerializeField] private GameObject counterRoomRoot;
    [SerializeField] private GameObject treatmentRoomRoot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Vector3 counterCameraPosition = new Vector3(0f, 0f, -10f);
    [SerializeField] private Vector3 treatmentCameraPosition = new Vector3(0f, 0f, -10f);
    [SerializeField] private float closeEyeDuration = 0.18f;
    [SerializeField] private float closedEyeHoldDuration = 0.08f;
    [SerializeField] private float openEyeDuration = 0.22f;
    [SerializeField] private bool useUnscaledTime = true;

    public bool IsInTreatmentRoom { get; private set; }

    public event Action<RoomTransitionDirection> TransitionStarted;
    public event Action<RoomTransitionDirection> RoomSwitched;
    public event Action<RoomTransitionDirection> TransitionFinished;

    private Coroutine transitionRoutine;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        SetFade(0f, blocksRaycasts: false);
        ApplyImmediate(counterRoomVisible: true, counterCameraPosition);
    }

    public void ShowCounterRoom()
    {
        ShowCounterRoom(null);
    }

    public void ShowCounterRoom(Action completed)
    {
        StartBlinkTransition(counterRoomVisible: true, counterCameraPosition, completed);
    }

    public void ShowTreatmentRoom()
    {
        ShowTreatmentRoom(null);
    }

    public void ShowTreatmentRoom(Action completed)
    {
        StartBlinkTransition(counterRoomVisible: false, treatmentCameraPosition, completed);
    }

    private void StartBlinkTransition(bool counterRoomVisible, Vector3 cameraPosition, Action completed)
    {
        RoomTransitionDirection direction = counterRoomVisible
            ? RoomTransitionDirection.ToCounter
            : RoomTransitionDirection.ToTreatment;

        if (!isActiveAndEnabled || fadeCanvasGroup == null)
        {
            TransitionStarted?.Invoke(direction);
            ApplyImmediate(counterRoomVisible, cameraPosition);
            RoomSwitched?.Invoke(direction);
            TransitionFinished?.Invoke(direction);
            completed?.Invoke();
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(BlinkTransition(counterRoomVisible, cameraPosition, direction, completed));
    }

    private IEnumerator BlinkTransition(bool counterRoomVisible, Vector3 cameraPosition, RoomTransitionDirection direction, Action completed)
    {
        TransitionStarted?.Invoke(direction);
        SetFade(0f, blocksRaycasts: true);
        yield return Fade(0f, 1f, closeEyeDuration);

        ApplyImmediate(counterRoomVisible, cameraPosition);
        RoomSwitched?.Invoke(direction);

        if (closedEyeHoldDuration > 0f)
        {
            yield return Wait(closedEyeHoldDuration);
        }

        yield return Fade(1f, 0f, openEyeDuration);
        SetFade(0f, blocksRaycasts: false);
        transitionRoutine = null;
        TransitionFinished?.Invoke(direction);
        completed?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetFade(to, blocksRaycasts: true);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFade(Mathf.Lerp(from, to, t), blocksRaycasts: true);
            yield return null;
        }

        SetFade(to, blocksRaycasts: true);
    }

    private IEnumerator Wait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void ApplyImmediate(bool counterRoomVisible, Vector3 cameraPosition)
    {
        IsInTreatmentRoom = !counterRoomVisible;
        ApplyRoomState(counterRoomVisible);
        MoveCamera(cameraPosition);
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

    private void SetFade(float alpha, bool blocksRaycasts)
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha = alpha;
        fadeCanvasGroup.blocksRaycasts = blocksRaycasts;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.gameObject.SetActive(alpha > 0f || blocksRaycasts);
    }
}
