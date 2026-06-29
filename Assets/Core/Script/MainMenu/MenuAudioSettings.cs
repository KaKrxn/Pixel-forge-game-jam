using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public sealed class MenuAudioSettings : MonoBehaviour
{
    public const string MusicVolumeKey = "Settings.MusicVolume";
    public const string GameVolumeKey = "Settings.GameVolume";

    private const float MinimumLinearVolume = 0.0001f;
    private const float MutedDecibels = -80f;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider gameSlider;

    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [SerializeField] private string gameVolumeParameter = "GameVolume";

    [Header("Fallback Audio Sources")]
    [SerializeField] private AudioSource musicSource;

    [Header("Defaults")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 0.75f;
    [Range(0f, 1f)]
    [SerializeField] private float defaultGameVolume = 0.65f;

    private bool isInitializing;

    public static float SavedMusicVolume => PlayerPrefs.GetFloat(MusicVolumeKey, 0.75f);
    public static float SavedGameVolume => PlayerPrefs.GetFloat(GameVolumeKey, 0.65f);

    private void OnEnable()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (gameSlider != null)
        {
            gameSlider.onValueChanged.AddListener(SetGameVolume);
        }
    }

    private void Start()
    {
        LoadAndApply();
    }

    private void OnDisable()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        }

        if (gameSlider != null)
        {
            gameSlider.onValueChanged.RemoveListener(SetGameVolume);
        }
    }

    public void LoadAndApply()
    {
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        float gameVolume = PlayerPrefs.GetFloat(GameVolumeKey, defaultGameVolume);

        isInitializing = true;
        SetupSlider(musicSlider, musicVolume);
        SetupSlider(gameSlider, gameVolume);
        isInitializing = false;

        ApplyMusicVolume(musicVolume);
        ApplyGameVolume(gameVolume);
    }

    public void SetMusicVolume(float value)
    {
        float volume = Mathf.Clamp01(value);
        ApplyMusicVolume(volume);
        SaveVolume(MusicVolumeKey, volume);
    }

    public void SetGameVolume(float value)
    {
        float volume = Mathf.Clamp01(value);
        ApplyGameVolume(volume);
        SaveVolume(GameVolumeKey, volume);
    }

    public static float LinearToDecibels(float linearVolume)
    {
        if (linearVolume <= MinimumLinearVolume)
        {
            return MutedDecibels;
        }

        return Mathf.Log10(Mathf.Clamp01(linearVolume)) * 20f;
    }

    private void ApplyMusicVolume(float volume)
    {
        SetMixerVolume(musicVolumeParameter, volume);

        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }

    private void ApplyGameVolume(float volume)
    {
        SetMixerVolume(gameVolumeParameter, volume);
    }

    private void SetMixerVolume(string parameterName, float linearVolume)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        bool parameterFound = audioMixer.SetFloat(parameterName, LinearToDecibels(linearVolume));
        if (!parameterFound)
        {
            Debug.LogWarning($"AudioMixer parameter '{parameterName}' was not found. Check the exposed parameter name.");
        }
    }

    private void SaveVolume(string key, float value)
    {
        if (isInitializing)
        {
            return;
        }

        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    private static void SetupSlider(Slider slider, float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }
}
