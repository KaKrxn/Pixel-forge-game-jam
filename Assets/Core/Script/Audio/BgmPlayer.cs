using System.Collections;
using UnityEngine;

public sealed class BgmPlayer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip mainMenuClip;
    [SerializeField] private AudioClip gameplayClip;

    [Header("Startup")]
    [SerializeField] private BgmTrack startTrack = BgmTrack.None;
    [SerializeField] private bool playOnStart = true;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.75f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine fadeRoutine;
    private BgmTrack currentTrack = BgmTrack.None;

    public BgmTrack CurrentTrack => currentTrack;

    private void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        if (playOnStart && startTrack != BgmTrack.None)
        {
            Play(startTrack);
        }
    }

    public void PlayMainMenu()
    {
        Play(BgmTrack.MainMenu);
    }

    public void PlayGameplay()
    {
        Play(BgmTrack.Gameplay);
    }

    public void Play(BgmTrack track)
    {
        AudioClip clip = GetClip(track);
        if (musicSource == null || clip == null)
        {
            currentTrack = BgmTrack.None;
            return;
        }

        if (currentTrack == track && musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        currentTrack = track;

        if (fadeDuration <= 0f)
        {
            musicSource.clip = clip;
            musicSource.volume = 1f;
            musicSource.Play();
            return;
        }

        fadeRoutine = StartCoroutine(FadeToClip(clip));
    }

    public void FadeOut()
    {
        if (musicSource == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeOutRoutine());
    }

    public void StopBgm()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        currentTrack = BgmTrack.None;

        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private IEnumerator FadeToClip(AudioClip clip)
    {
        if (musicSource.isPlaying && musicSource.volume > 0f)
        {
            yield return FadeVolume(musicSource.volume, 0f, fadeDuration * 0.5f);
        }

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        yield return FadeVolume(0f, 1f, fadeDuration * 0.5f);
        fadeRoutine = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return FadeVolume(musicSource.volume, 0f, fadeDuration);
        musicSource.Stop();
        currentTrack = BgmTrack.None;
        fadeRoutine = null;
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            musicSource.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            musicSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        musicSource.volume = to;
    }

    private AudioClip GetClip(BgmTrack track)
    {
        switch (track)
        {
            case BgmTrack.MainMenu:
                return mainMenuClip;
            case BgmTrack.Gameplay:
                return gameplayClip;
            case BgmTrack.None:
            default:
                return null;
        }
    }
}
