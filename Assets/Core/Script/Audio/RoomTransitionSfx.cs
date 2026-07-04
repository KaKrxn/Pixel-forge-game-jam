using UnityEngine;

public sealed class RoomTransitionSfx : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private RoomTransition roomTransition;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("To Treatment")]
    [SerializeField] private AudioClip toTreatmentCloseClip;
    [SerializeField] private AudioClip toTreatmentSwitchClip;
    [SerializeField] private AudioClip toTreatmentOpenClip;

    [Header("To Counter")]
    [SerializeField] private AudioClip toCounterCloseClip;
    [SerializeField] private AudioClip toCounterSwitchClip;
    [SerializeField] private AudioClip toCounterOpenClip;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (roomTransition == null)
        {
            return;
        }

        roomTransition.TransitionStarted -= HandleTransitionStarted;
        roomTransition.RoomSwitched -= HandleRoomSwitched;
        roomTransition.TransitionFinished -= HandleTransitionFinished;
        roomTransition.TransitionStarted += HandleTransitionStarted;
        roomTransition.RoomSwitched += HandleRoomSwitched;
        roomTransition.TransitionFinished += HandleTransitionFinished;
    }

    private void Unsubscribe()
    {
        if (roomTransition == null)
        {
            return;
        }

        roomTransition.TransitionStarted -= HandleTransitionStarted;
        roomTransition.RoomSwitched -= HandleRoomSwitched;
        roomTransition.TransitionFinished -= HandleTransitionFinished;
    }

    private void HandleTransitionStarted(RoomTransitionDirection direction)
    {
        Play(direction == RoomTransitionDirection.ToTreatment ? toTreatmentCloseClip : toCounterCloseClip);
    }

    private void HandleRoomSwitched(RoomTransitionDirection direction)
    {
        Play(direction == RoomTransitionDirection.ToTreatment ? toTreatmentSwitchClip : toCounterSwitchClip);
    }

    private void HandleTransitionFinished(RoomTransitionDirection direction)
    {
        Play(direction == RoomTransitionDirection.ToTreatment ? toTreatmentOpenClip : toCounterOpenClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, volume);
    }

    private void ResolveReferences()
    {
        if (roomTransition == null)
        {
            roomTransition = GetComponent<RoomTransition>();
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }
}
