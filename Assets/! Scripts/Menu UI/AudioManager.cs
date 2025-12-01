using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer audioMixer; // Reference to the AudioMixer
    public static AudioManager Instance { get; private set; } // Singleton instance

    // Volume variables
    private float currentMasterVolume = 0f;
    private float currentEnemyVolume = 0f;
    private float currentAmbienceVolume = 0f;
    private float currentMusicVolume = 0f;
    private float currentPlayerVolume = 0f;
    private float currentUIVolume = 0f;
    private float currentNPCVolume = 0f;

    // Constants for PlayerPrefs keys
    private const string MasterVolumeKey = "MasterVolume";
    private const string EnemyVolumeKey = "EnemyVolume";
    private const string AmbienceVolumeKey = "AmbienceVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string PlayerVolumeKey = "PlayerVolume";
    private const string UIVolumeKey = "UIVolume";
    private const string NPCVolumeKey = "NPCVolume";

    // Initialize the Singleton and load settings
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // Set the singleton instance
            DontDestroyOnLoad(gameObject); // Keep the AudioManager persistent across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicates of the singleton
        }

        LoadVolumeSettings(); // Load volume settings when the game starts
    }

    private void Start()
    {
        // Apply the loaded settings to the audio mixer
        SetMasterVolume(currentMasterVolume);
        SetEnemyVolume(currentEnemyVolume);
        SetAmbienceVolume(currentAmbienceVolume);
        SetMusicVolume(currentMusicVolume);
        SetPlayerVolume(currentPlayerVolume);
        SetUIVolume(currentUIVolume);
    }

    public float GetCurrentMasterVolume() => currentMasterVolume;
    public float GetCurrentEnemyVolume() => currentEnemyVolume;
    public float GetCurrentAmbienceVolume() => currentAmbienceVolume;
    public float GetCurrentMusicVolume() => currentMusicVolume;
    public float GetCurrentPlayerVolume() => currentPlayerVolume;
    public float GetCurrentUIVolume() => currentUIVolume;
    public float GetCurrentNPCVolume() => currentNPCVolume;

    // Set and save the master volume
    public void SetMasterVolume(float volume)
    {
        currentMasterVolume = Mathf.Clamp(volume, -80f, 20f); // Unity mixes in dB, so -80 is the minimum
        audioMixer.SetFloat("Master", currentMasterVolume);
        PlayerPrefs.SetFloat(MasterVolumeKey, currentMasterVolume); // Save to PlayerPrefs
    }

    // Set and save the enemy volume
    public void SetEnemyVolume(float volume)
    {
        currentEnemyVolume = Mathf.Clamp(volume, -80f, 20f);
        audioMixer.SetFloat("Enemy", currentEnemyVolume);
        PlayerPrefs.SetFloat(EnemyVolumeKey, currentEnemyVolume); // Save to PlayerPrefs
    }
    public void SetNPCVolume(float volume)
    {
        currentNPCVolume = Mathf.Clamp(volume, -80f, 20f);
        audioMixer.SetFloat("NPC", currentNPCVolume);
        PlayerPrefs.SetFloat(NPCVolumeKey, currentNPCVolume); // Save to PlayerPrefs
    }
    // Set and save the ambience volume
    public void SetAmbienceVolume(float volume)
    {
        currentAmbienceVolume = Mathf.Clamp(volume, -80f, 20f);
        audioMixer.SetFloat("Ambience", currentAmbienceVolume);
        PlayerPrefs.SetFloat(AmbienceVolumeKey, currentAmbienceVolume); // Save to PlayerPrefs
    }

    // Set and save the music volume
    public void SetMusicVolume(float volume)
    {
        currentMusicVolume = Mathf.Clamp(volume, -80f, 20f);
        audioMixer.SetFloat("Music", currentMusicVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, currentMusicVolume); // Save to PlayerPrefs
    }

    // Set and save the player volume
    public void SetPlayerVolume(float volume)
    {
        currentPlayerVolume = Mathf.Clamp(volume, -80f, 20f);
        audioMixer.SetFloat("Player", currentPlayerVolume);
        PlayerPrefs.SetFloat(PlayerVolumeKey, currentPlayerVolume); // Save to PlayerPrefs
    }

    // Set and save the UI volume
    public void SetUIVolume(float volume)
    {
        currentUIVolume = Mathf.Clamp(volume, -80f, 20f);
        audioMixer.SetFloat("UI", currentUIVolume);
        PlayerPrefs.SetFloat(UIVolumeKey, currentUIVolume); // Save to PlayerPrefs
    }

    // Load volume settings from PlayerPrefs
    private void LoadVolumeSettings()
    {
        // Load saved volume settings, if available; otherwise, use default values
        currentMasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 0f); // Default: 0 (normal volume)
        currentEnemyVolume = PlayerPrefs.GetFloat(EnemyVolumeKey, 0f); // Default: 0
        currentAmbienceVolume = PlayerPrefs.GetFloat(AmbienceVolumeKey, 0f); // Default: 0
        currentMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0f); // Default: 0
        currentPlayerVolume = PlayerPrefs.GetFloat(PlayerVolumeKey, 0f); // Default: 0
        currentUIVolume = PlayerPrefs.GetFloat(UIVolumeKey, 0f); // Default: 0
        currentNPCVolume = PlayerPrefs.GetFloat(NPCVolumeKey, 0f); // Default: 0
    }

    // Reset all volume settings to default (0) and save them
    public void ResetToDefaults()
    {
        // Reset volumes to default (0)
        SetMasterVolume(0f);
        SetEnemyVolume(0f);
        SetAmbienceVolume(0f);
        SetMusicVolume(0f);
        SetPlayerVolume(0f);
        SetUIVolume(0f);
        SetNPCVolume(0f);

        // Optionally, reset PlayerPrefs for all volume settings
        PlayerPrefs.SetFloat(MasterVolumeKey, 0f);
        PlayerPrefs.SetFloat(EnemyVolumeKey, 0f);
        PlayerPrefs.SetFloat(AmbienceVolumeKey, 0f);
        PlayerPrefs.SetFloat(MusicVolumeKey, 0f);
        PlayerPrefs.SetFloat(NPCVolumeKey, 0f);
        PlayerPrefs.SetFloat(PlayerVolumeKey, 0f);
        PlayerPrefs.SetFloat(UIVolumeKey, 0f);

        VolumeUI.Instance.RefreshSliders();
    }
}
