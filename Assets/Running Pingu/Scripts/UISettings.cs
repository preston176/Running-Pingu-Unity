using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class UISettings : UIWindow
{
    private const string MIXER_MASTER = "MasterVolume";
    private const string MIXER_MUSIC = "MusicVolume";

    private const string PREF_MASTER_VOLUME = "MasterVolume";
    private const string PREF_MUSIC_VOLUME = "MusicVolume";

    [Header("References")]
    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public TMP_Text masterPercentageText;
    public TMP_Text musicPercentageText;

    [Header("Settings")]
    public float defaultMasterVolume = 1f;
    public float defaultMusicVolume = 0.5f;

    private void Start()
    {
        Close();
        LoadVolume();
    }

    public void UpdateMasterVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.001f, 1f);
        audioMixer.SetFloat(MIXER_MASTER, Mathf.Log10(volume) * 20);

        masterPercentageText.text = $"{masterSlider.value * 100:N0}%";
    }

    public void UpdateMusicVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.001f, 1f);
        audioMixer.SetFloat(MIXER_MUSIC, Mathf.Log10(volume) * 20);

        musicPercentageText.text = $"{musicSlider.value * 100:N0}%";
    }

    public void SaveVolume()
    {
        audioMixer.GetFloat(MIXER_MASTER, out float masterVolume);
        var masterMixerToSliderValue = Mathf.Pow(10, masterVolume / 20);
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterMixerToSliderValue);

        audioMixer.GetFloat(MIXER_MUSIC, out float musicVolume);
        var musicMixerToSliderValue = Mathf.Pow(10, musicVolume / 20);
        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicMixerToSliderValue);

        PlayerPrefs.Save();
    }

    public void LoadVolume()
    {
        var savedMasterVolume = Mathf.Clamp(PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, defaultMasterVolume), 0.001f, 1f);
        masterSlider.value = savedMasterVolume;
    }
}
