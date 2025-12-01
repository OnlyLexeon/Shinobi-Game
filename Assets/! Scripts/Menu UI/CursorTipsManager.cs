using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CursorTipsManager : MonoBehaviour
{
    [System.Serializable]
    public class Tip
    {
        public KeyCode key;
        public string tipMessage;
    }

    public Transform contentPanel;     // Parent panel where tips will be added
    public GameObject tipPrefab;       // Prefab containing an Image and a TextMeshProUGUI

    private readonly Dictionary<Tip, GameObject> activeTipsDict = new Dictionary<Tip, GameObject>();

    public static CursorTipsManager Instance { get; private set; } // Singleton instance

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    public GameObject MakeTip(Tip tipData)
    {
        if (tipData == null)
        {
            Debug.LogWarning("Invalid tip data");
            return null;
        }

        // Check if a tip with the same data already exists
        if (activeTipsDict.ContainsKey(tipData))
        {
            return activeTipsDict[tipData]; // Return the existing tip
        }

        // Instantiate the tip prefab
        GameObject newTip = Instantiate(tipPrefab, contentPanel);

        // Set the key and text in the prefab
        TextMeshProUGUI keyTextComponent = newTip.transform.Find("KeyText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI messageTextComponent = newTip.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();

        if (keyTextComponent != null)
        {
            // Retrieve the keybind name or fallback to the KeyCode
            string keyName = KeybindManager.Instance.keybinds
                .FirstOrDefault(pair => pair.Value == tipData.key).Key;

            // Format the text as "Press [Key]"
            keyTextComponent.text = $"Press [{tipData.key}] to ";
        }
        else
        {
            Debug.LogWarning("KeyText component not found in tip prefab.");
        }

        if (messageTextComponent != null)
        {
            messageTextComponent.text = tipData.tipMessage;
        }
        else
        {
            Debug.LogWarning("MessageText component not found in tip prefab.");
        }

        // Add the new tip to the list of active tips
        activeTipsDict[tipData] = newTip;
        return newTip;
    }

    public void RemoveTip(Tip tipData)
    {
        if (tipData != null && activeTipsDict.ContainsKey(tipData))
        {
            Destroy(activeTipsDict[tipData]);
            activeTipsDict.Remove(tipData);
        }
    }

    public void ClearTips()
    {
        foreach (var tip in activeTipsDict.Values)
        {
            Destroy(tip);
        }
        activeTipsDict.Clear();
    }
}
