using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeybindUIManager : MonoBehaviour
{
    // Attach to keybind settings
    // Script talks to RebindKey,

    public static KeybindUIManager Instance;

    public GameObject keybindRowPrefab; // Prefab for a row containing action name, key, and rebind button
    public Transform keybindListParent; // Parent transform for the keybind rows

    private List<Button> rebindButtons = new List<Button>();

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        PopulateKeybindList();
    }

    public void PopulateKeybindList()
    {
        foreach (var binding in KeybindManager.Instance.keybinds)
        {
            GameObject row = Instantiate(keybindRowPrefab, keybindListParent);
            RebindKey rebindKey = row.GetComponent<RebindKey>();
            rebindKey.Setup(binding.Key, binding.Value);
        }
    }

    public void ResetKeybinds()
    {
        KeybindManager.Instance.ResetToDefaults();
        foreach (Transform child in keybindListParent)
        {
            Destroy(child.gameObject);
        }
        rebindButtons.Clear();
        PopulateKeybindList();
    }

    public void RegisterRebindButton(Button button)
    {
        rebindButtons.Add(button);
    }

    public void SetAllRebindButtonsInteractable(bool interactable)
    {
        foreach (var button in rebindButtons)
        {
            button.interactable = interactable;
        }
    }
}
