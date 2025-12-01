using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MissionManager : MonoBehaviour
{
    [System.Serializable]
    public class Mission
    {
        public string missionName;      
        public string missionDescription; 
        public Sprite missionImage;    
        public string sceneToLoad;  
    }

    public Mission[] missions; 
    public Transform contentPanel; 
    public GameObject missionButtonPrefab;   // Prefab for mission buttons

    public Image selectedMissionImage;       // UI Image for the selected mission
    public TextMeshProUGUI selectedMissionDescription;  // UI Text for the selected mission description
    public TextMeshProUGUI selectedMissionName;         // UI Text for the selected mission name
    public Button confirmButton;             // Confirm button to load the mission scene

    private Mission selectedMission;         // Currently selected mission

    void Start()
    {
        LoadMissions();
        confirmButton.interactable = false; // Disable confirm button until a mission is selected
    }

    // Load missions into the ScrollView
    public void LoadMissions()
    {
        foreach (Mission mission in missions)
        {
            GameObject missionButton = Instantiate(missionButtonPrefab, contentPanel);
            missionButton.GetComponentInChildren<TextMeshProUGUI>().text = mission.missionName;

            // Add button click event
            missionButton.GetComponent<Button>().onClick.AddListener(() => SelectMission(mission));
        }
    }

    // When a mission is selected
    public void SelectMission(Mission mission)
    {
        selectedMission = mission;

        // Update UI with the selected mission's details
        if (mission.missionImage) selectedMissionImage.sprite = mission.missionImage;
        if (mission.missionDescription.Length > 0) selectedMissionDescription.text = mission.missionDescription;
        if (mission.missionName.Length > 0) selectedMissionName.text = mission.missionName;

        confirmButton.interactable = true; // Enable confirm button
    }

    // Load the selected mission's scene
    public void ConfirmMission()
    {
        if (selectedMission != null)
        {
            MenuController.Instance.LoadScene(selectedMission.sceneToLoad);
            Debug.Log("Loading Scene!");
        }
    }
}
