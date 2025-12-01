using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RendererToggleManager : MonoBehaviour
{
    public UniversalRenderPipelineAsset urpAsset;
    public UniversalRendererData urpData;

    public static RendererToggleManager Instance;

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;  // Set the instance to this object
            DontDestroyOnLoad(gameObject);  // Optionally, persist the singleton across scenes
        }
        else if (Instance != this)
        {
            Destroy(gameObject);  // Destroy this instance if another instance already exists
        }
    }

    void Start()
    {
        if (urpAsset == null) Debug.LogWarning("urpAsset is NULL!!");
        if (urpData == null) Debug.LogWarning("urpData is NULL!!");

        ResetRendererFeatures();
    }

    public void ResetRendererFeatures()
    {
        ToggleRendererFeature("Kurosawa Filter", false);
        ToggleRendererFeature("Highlight Enemies", false);
    }

    public void ToggleRendererFeature(string featureName, bool enable)
    {
        if (urpData == null)
        {
            Debug.LogError("URP Data is not assigned.");
            return;
        }

        // Get the list of renderer features in the renderer data
        var features = urpData.rendererFeatures;

        bool featureFound = false;

        foreach (var feature in features)
        {
            Debug.Log("Feature type: " + feature.GetType().Name);
            // Check the feature name or type to identify the specific feature
            if (feature.name == featureName)
            {
                feature.SetActive(enable);
                Debug.Log($"{featureName} has been {(enable ? "enabled" : "disabled")}.");
                featureFound = true;
                break;
            }
        }

        if (!featureFound)
        {
            Debug.LogWarning($"Feature '{featureName}' not found.");
        }
    }
}
