using System.Collections.Generic;
using UnityEngine;

public class KeybindManager : MonoBehaviour // Saving, Loading Keybinds
{
    // PUT THIS SCRIPT AS A SINGLETON
    // (Separate game object upon game open)

    // if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Attack"]))
    // USE THIS TO DETECT INPUT NOW

    public static KeybindManager Instance;

    // Default keybindings
    private Dictionary<string, KeyCode> defaultKeybinds = new Dictionary<string, KeyCode>
    {
        { "Attack", KeyCode.Mouse0 },  // Left Click
        { "Use Utility", KeyCode.Mouse1 },  // Right Click
        { "Jump", KeyCode.Space },
        { "Sprint", KeyCode.LeftShift },
        { "Interact", KeyCode.E },
        { "Crouch", KeyCode.LeftControl },
        { "Ghost Ultimate", KeyCode.Z },
        { "Block", KeyCode.Q },
        { "Inventory", KeyCode.Tab },
        { "Unequip Offhand", KeyCode.X },
        { "Close", KeyCode.Escape},
        { "Inventory Slot 1", KeyCode.Alpha1 },
        { "Inventory Slot 2", KeyCode.Alpha2 },
        { "Inventory Slot 3", KeyCode.Alpha3 },
        { "Inventory Slot 4", KeyCode.Alpha4 },
    };

    // Current keybindings
    public Dictionary<string, KeyCode> keybinds = new Dictionary<string, KeyCode>();

    private void Awake()
    {
        // Singleton pattern for global access
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Load keybindings or use defaults
        LoadKeybinds();
    }

    public void LoadKeybinds()
    {
        keybinds.Clear();

        foreach (var binding in defaultKeybinds)
        {
            // Attempt to load a saved keybinding from PlayerPrefs
            string savedKey = PlayerPrefs.GetString(binding.Key, string.Empty);

            // If no saved key exists, use the default key
            if (string.IsNullOrEmpty(savedKey))
            {
                keybinds[binding.Key] = binding.Value;
            }
            else
            {
                // Parse the saved key and add it to the keybinds
                keybinds[binding.Key] = (KeyCode)System.Enum.Parse(typeof(KeyCode), savedKey);
            }
        }
    }

    public void SaveKeybinds()
    {
        foreach (var binding in keybinds)
        {
            PlayerPrefs.SetString(binding.Key, binding.Value.ToString());
        }
        PlayerPrefs.Save();
    }

    public void ResetToDefaults()
    {
        keybinds = new Dictionary<string, KeyCode>(defaultKeybinds);
        SaveKeybinds();
    }
}
