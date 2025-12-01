using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI killText;
    public TextMeshProUGUI alertedText;
    public TextMeshProUGUI suspicionText;
    public TextMeshProUGUI timeText;


    public static StatsUI Instance;

    private void Awake()
    {
        // Check if an instance already exists and ensure there's only one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Optionally, persist this object across scenes
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        alertedText.text = GameStats.Instance.alertedEnemies.ToString();
        killText.text = GameStats.Instance.killCount.ToString();
        suspicionText.text = GameStats.Instance.totalSuspicion.ToString();
        timeText.text = GetFormattedTime();

        if (GameStats.Instance.alertedEnemies <= 0) statusText.text = "Stealthy";
        else statusText.text = "Caught";
    }

    public string GetFormattedTime()
    {
        if (GameStats.Instance)
        {
            int seconds = GameStats.Instance.seconds;

            int hours = seconds / 3600;
            int minutes = (seconds % 3600) / 60;
            int secs = seconds % 60;

            if (hours > 0)
            {
                return $"{Mathf.Min(hours, 99):00}:{minutes:00}:{secs:00}"; // Limit to 99:59:59
            }
            else if (minutes > 0)
            {
                return $"{minutes:00}:{secs:00}";
            }
            else
            {
                return $"{secs:00}";
            }
        }
        else return "Uhh no gamestats?";
    }
}
