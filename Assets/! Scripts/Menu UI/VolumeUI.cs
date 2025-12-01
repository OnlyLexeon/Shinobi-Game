using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeUI : MonoBehaviour
{
    public static VolumeUI Instance;

    [Header("Master")]
    public Slider masterSlider;
    public TextMeshProUGUI masterText;

    [Header("Enemy")]
    public Slider enemySlider;
    public TextMeshProUGUI enemyText;

    [Header("Ambience")]
    public Slider ambienceSlider;
    public TextMeshProUGUI ambienceText;

    [Header("Music")]
    public Slider musicSlider;
    public TextMeshProUGUI musicText;

    [Header("Player")]
    public Slider playerSlider;
    public TextMeshProUGUI playerText;

    [Header("UI")]
    public Slider uiSlider;
    public TextMeshProUGUI uiText;

    [Header("NPC")]
    public Slider npcSlider;
    public TextMeshProUGUI npcText;

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
    }

    private void Start()
    {
        // Initialize sliders and text with current values from AudioManager
        InitializeSlider(masterSlider, masterText, AudioManager.Instance.GetCurrentMasterVolume(), AudioManager.Instance.SetMasterVolume);
        InitializeSlider(enemySlider, enemyText, AudioManager.Instance.GetCurrentEnemyVolume(), AudioManager.Instance.SetEnemyVolume);
        InitializeSlider(ambienceSlider, ambienceText, AudioManager.Instance.GetCurrentAmbienceVolume(), AudioManager.Instance.SetAmbienceVolume);
        InitializeSlider(musicSlider, musicText, AudioManager.Instance.GetCurrentMusicVolume(), AudioManager.Instance.SetMusicVolume);
        InitializeSlider(playerSlider, playerText, AudioManager.Instance.GetCurrentPlayerVolume(), AudioManager.Instance.SetPlayerVolume);
        InitializeSlider(uiSlider, uiText, AudioManager.Instance.GetCurrentUIVolume(), AudioManager.Instance.SetUIVolume);
        InitializeSlider(npcSlider, npcText, AudioManager.Instance.GetCurrentNPCVolume(), AudioManager.Instance.SetNPCVolume);
    }

    public void RefreshSliders()
    {
        UpdateSlider(masterSlider, masterText, AudioManager.Instance.GetCurrentMasterVolume());
        UpdateSlider(enemySlider, enemyText, AudioManager.Instance.GetCurrentEnemyVolume());
        UpdateSlider(ambienceSlider, ambienceText, AudioManager.Instance.GetCurrentAmbienceVolume());
        UpdateSlider(musicSlider, musicText, AudioManager.Instance.GetCurrentMusicVolume());
        UpdateSlider(playerSlider, playerText, AudioManager.Instance.GetCurrentPlayerVolume());
        UpdateSlider(uiSlider, uiText, AudioManager.Instance.GetCurrentUIVolume());
        UpdateSlider(npcSlider, npcText, AudioManager.Instance.GetCurrentNPCVolume());
    }

    private void UpdateSlider(Slider slider, TextMeshProUGUI text, float dbValue)
    {
        float percentageValue = DbToPercentage(dbValue);
        slider.value = percentageValue;
        UpdateVolumeText(text, percentageValue);
    }

    private void InitializeSlider(Slider slider, TextMeshProUGUI text, float currentValue, System.Action<float> onValueChanged)
    {
        slider.minValue = 0f;   // Percentage minimum
        slider.maxValue = 100f; // Percentage maximum
        slider.value = DbToPercentage(currentValue);

        // Update the text display
        UpdateVolumeText(text, slider.value);

        // Add listener to update the volume and text dynamically
        slider.onValueChanged.AddListener((percentageValue) =>
        {
            float dbValue = PercentageToDb(percentageValue);
            onValueChanged(dbValue);
            UpdateVolumeText(text, percentageValue);
        });
    }

    private void UpdateVolumeText(TextMeshProUGUI text, float percentageValue)
    {
        text.text = $"{percentageValue:0}%"; // Display the volume as a percentage
    }

    private float DbToPercentage(float dbValue)
    {
        // Convert dB (-80 to 20) to a percentage (0 to 100)
        return Mathf.Clamp01((dbValue + 80f) / 100f) * 100f;
    }

    private float PercentageToDb(float percentageValue)
    {
        // Convert a percentage (0 to 100) to dB (-80 to 20)
        return Mathf.Lerp(-80f, 20f, percentageValue / 100f);
    }
}
