using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance { get; private set; }

    public GameObject SubtitleHolder; // Parent object for subtitles
    public GameObject SubtitlePrefab; // Prefab to display subtitles

    private Queue<KeyValuePair<Voiceline, GameObject>> activeSubtitles = new Queue<KeyValuePair<Voiceline, GameObject>>();

    // The maximum number of subtitles to show at once
    public int subtitleLimit = 8;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (SubtitleHolder == null)
        {
            SubtitleHolder = gameObject;
        }
    }

    public void ShowSubtitle(string subtitleText, Voiceline voiceline, float duration)
    {
        if (SubtitlePrefab != null && SubtitleHolder != null)
        {
            // If there is already a subtitle for this voiceline, don't show another
            if (IsSubtitleAlreadyActive(voiceline))
                return;

            // If the subtitle count exceeds the limit, remove the oldest subtitle
            if (activeSubtitles.Count >= subtitleLimit)
            {
                RemoveOldestSubtitle();
            }

            GameObject newSubtitle = Instantiate(SubtitlePrefab, SubtitleHolder.transform);
            TextMeshProUGUI subtitleUI = newSubtitle.GetComponentInChildren<TextMeshProUGUI>();
            RawImage backgroundImage = newSubtitle.GetComponentInChildren<RawImage>();
            if (subtitleUI != null)
            {
                subtitleUI.text = subtitleText;
            }

            // Update the background image width based on the text length
            if (backgroundImage != null)
            {
                UpdateBackgroundWidth(subtitleUI, backgroundImage);
            }

            // Add the new subtitle to the queue
            activeSubtitles.Enqueue(new KeyValuePair<Voiceline, GameObject>(voiceline, newSubtitle));

            // Automatically remove the subtitle after the given duration
            Destroy(newSubtitle, duration);
            StartCoroutine(RemoveSubtitleAfterDuration(voiceline, newSubtitle, duration));
        }
    }

    private IEnumerator RemoveSubtitleAfterDuration(Voiceline voiceline, GameObject subtitle, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (activeSubtitles.Count > 0 && activeSubtitles.Peek().Key == voiceline)
        {
            activeSubtitles.Dequeue();
        }
    }

    private bool IsSubtitleAlreadyActive(Voiceline voiceline)
    {
        // Check if the voiceline is already in the queue
        foreach (var subtitle in activeSubtitles)
        {
            if (subtitle.Key == voiceline)
                return true;
        }
        return false;
    }

    private void RemoveOldestSubtitle()
    {
        // Remove the oldest subtitle (first in the queue)
        var oldestSubtitle = activeSubtitles.Dequeue();
        Destroy(oldestSubtitle.Value);  // Destroy the associated GameObject
    }

    private void UpdateBackgroundWidth(TextMeshProUGUI subtitleText, RawImage backgroundImage)
    {
        // Calculate the width of the text (with some padding)
        float textWidth = subtitleText.preferredWidth;
        float padding = 20f; // You can adjust this padding value as needed

        // Set the width of the background to match the text width
        RectTransform bgRectTransform = backgroundImage.GetComponent<RectTransform>();
        bgRectTransform.sizeDelta = new Vector2(textWidth + padding, bgRectTransform.sizeDelta.y); // Set width and keep current height
    }

    public void RemoveSubtitle(Voiceline voiceline)
    {
        // Remove a specific subtitle by voiceline
        foreach (var subtitle in activeSubtitles)
        {
            if (subtitle.Key == voiceline)
            {
                activeSubtitles = new Queue<KeyValuePair<Voiceline, GameObject>>(activeSubtitles.Where(s => s.Key != voiceline));
                Destroy(subtitle.Value);
                break;
            }
        }
    }

    public void ClearAllSubtitles()
    {
        // Clear all currently active subtitles
        foreach (var subtitle in activeSubtitles)
        {
            if (subtitle.Value != null)
            {
                Destroy(subtitle.Value);
            }
        }
        activeSubtitles.Clear();
    }
}
