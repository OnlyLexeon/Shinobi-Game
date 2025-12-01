using UnityEngine;
using UnityEngine.UI;

public class Unlocked : MonoBehaviour
{
    private const string GhostModeKey = "GhostModeUnlocked";
    public static Unlocked Instance;

    public GameObject ContentHolder;
    public GameObject imagePrefab;

    [Header("GhostMode")]
    public Sprite ghostImage;

    public bool isGhostMode = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Ensure there's only one instance
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: keeps the singleton alive across scenes
    }

    private void Start()
    {
        if (IsGhostModeUnlocked()) isGhostMode = true;

        LoadUnlocked();
    }

    public void LoadUnlocked()
    {
        //destroy all child objects first
        foreach (Transform child in ContentHolder.transform)
        {
            Destroy(child.gameObject);
        }

        if (IsGhostModeUnlocked())
        {
            // Instantiate the image prefab as a child of the ContentHolder
            GameObject newImage = Instantiate(imagePrefab, ContentHolder.transform);

            // Set the sprite of the Image component to ghostImage
            Image imageComponent = newImage.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.sprite = ghostImage;
            }
            else
            {
                Debug.LogError("The prefab does not have an Image component!");
            }
        }
    }

    public void UnlockGhostMode()
    {
        isGhostMode = true;

        PlayerPrefs.SetInt(GhostModeKey, 1); // 1 means true
        PlayerPrefs.Save(); // Save PlayerPrefs to ensure the change persists
        Debug.Log("Ghost Mode has been unlocked!");
    }

    public bool IsGhostModeUnlocked()
    {
        return PlayerPrefs.GetInt(GhostModeKey, 0) == 1; // 0 (default) means false
    }

    public void ResetAllUnlocks()
    {
        PlayerPrefs.DeleteKey(GhostModeKey); // Remove the GhostModeUnlocked key
        PlayerPrefs.Save(); // Save changes

        isGhostMode = false; // Reset the local variable
        Debug.Log("All unlocks have been reset!");

        LoadUnlocked();
    }
}
