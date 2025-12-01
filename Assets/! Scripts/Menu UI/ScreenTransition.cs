using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenTransition : MonoBehaviour
{
    public float fadeDuration = 1f; // Duration of the fade
    private Image fadeImage;

    private void Awake()
    {
        fadeImage = GetComponent<Image>();
    }

    public void FadeIn()
    {
        StartCoroutine(Fade(1, 0)); // Fade from black to clear
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(0, 1)); // Fade from clear to black
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;
    }
}
