using UnityEngine;

public sealed class Door : MonoBehaviour
{
    private static readonly Vector2 DefaultPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Visual")]
    [SerializeField] private GameObject openVisual;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private bool startClosed = true;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip[] openClips;
    [SerializeField] private AudioClip[] closeClips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 randomPitchRange = DefaultPitchRange;
    [SerializeField] private bool playOnlyOnStateChanged = true;

    public bool IsOpen { get; private set; }

    private float defaultPitch = 1f;

    private void Awake()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (sfxSource != null)
        {
            defaultPitch = sfxSource.pitch;
        }

        SetDoorState(!startClosed, playAudio: false);
    }

    public void Open()
    {
        SetDoorState(true, playAudio: true);
    }

    public void Close()
    {
        SetDoorState(false, playAudio: true);
    }

    private void SetDoorState(bool open, bool playAudio)
    {
        bool changed = IsOpen != open;
        IsOpen = open;
        SetVisuals(open);

        if (!playAudio)
        {
            return;
        }

        if (playOnlyOnStateChanged && !changed)
        {
            return;
        }

        PlayStateSound(open);
    }

    private void SetVisuals(bool open)
    {
        if (openVisual != null)
        {
            openVisual.SetActive(open);
        }

        if (closedVisual != null)
        {
            closedVisual.SetActive(!open);
        }
    }

    private void PlayStateSound(bool open)
    {
        if (sfxSource == null)
        {
            return;
        }

        AudioClip clip = GetRandomClip(open ? openClips : closeClips);
        if (clip == null)
        {
            return;
        }

        sfxSource.pitch = GetRandomPitch();
        sfxSource.PlayOneShot(clip, volume);
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int validCount = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int selected = Random.Range(0, validCount);
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
            {
                continue;
            }

            if (selected == 0)
            {
                return clips[i];
            }

            selected--;
        }

        return null;
    }

    private float GetRandomPitch()
    {
        float min = Mathf.Min(randomPitchRange.x, randomPitchRange.y);
        float max = Mathf.Max(randomPitchRange.x, randomPitchRange.y);

        if (Mathf.Approximately(min, 0f) && Mathf.Approximately(max, 0f))
        {
            return defaultPitch;
        }

        return Random.Range(min, max);
    }
}
