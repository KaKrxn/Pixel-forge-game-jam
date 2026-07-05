using UnityEngine;

public sealed class GameResolutionController : MonoBehaviour
{
    [SerializeField] private GameFlow flow;
    [SerializeField] private GameResultPanel resultPanel;
    [SerializeField] private bool pauseTimeOnResolution = true;
    [SerializeField] private FailReason defaultGameOverReason = FailReason.SanityMaxed;
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private bool resolved;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (flow != null)
        {
            flow.StateChanged -= HandleStateChanged;
            flow.StateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (flow != null)
        {
            flow.StateChanged -= HandleStateChanged;
        }
    }

    public void ResetResolution()
    {
        resolved = false;
        resultPanel?.Hide();
        Time.timeScale = 1f;
    }

    private void HandleStateChanged(ClinicFlowState state)
    {
        if (resolved)
        {
            return;
        }

        switch (state)
        {
            case ClinicFlowState.AllCustomersComplete:
                ResolveWin();
                break;
            case ClinicFlowState.GameOver:
                ResolveLose(defaultGameOverReason);
                break;
        }
    }

    private void ResolveWin()
    {
        resolved = true;
        PlaySfx(winClip);
        resultPanel?.ShowWin();
        PauseTimeIfNeeded();
    }

    private void ResolveLose(FailReason reason)
    {
        resolved = true;
        PlaySfx(loseClip);
        resultPanel?.ShowLose(reason);
        PauseTimeIfNeeded();
    }

    private void PauseTimeIfNeeded()
    {
        if (pauseTimeOnResolution)
        {
            Time.timeScale = 0f;
        }
    }

    private void ResolveReferences()
    {
        if (flow == null)
        {
            flow = FindFirstObjectByType<GameFlow>();
        }

        if (resultPanel == null)
        {
            resultPanel = FindFirstObjectByType<GameResultPanel>(FindObjectsInactive.Include);
        }

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
            return;
        }

        AudioSource.PlayClipAtPoint(clip, transform.position, sfxVolume);
    }
}
