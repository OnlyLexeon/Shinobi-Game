using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class RebindKey : MonoBehaviour
{
    public TextMeshProUGUI actionText;    // Displays the action name (e.g., "Attack")
    public TextMeshProUGUI statusText;   // Displays the currently bound key
    public Button rebindButton;          // Rebind button

    private string actionName;           // The name of the action being rebound

    public void Setup(string action, KeyCode currentKey)
    {
        actionName = action;
        actionText.text = action;
        statusText.text = currentKey.ToString();

        KeybindUIManager.Instance.RegisterRebindButton(rebindButton);

        rebindButton.onClick.AddListener(StartRebinding);
    }

    private void OnDestroy()
    {
        rebindButton.onClick.RemoveAllListeners();
    }

    public void StartRebinding()
    {
        StartCoroutine(RebindKeyCoroutine());
    }

    private IEnumerator RebindKeyCoroutine()
    {
        statusText.text = "Press a key...";
        rebindButton.GetComponentInChildren<TextMeshProUGUI>().text = "Rebinding...";

        KeybindUIManager.Instance.SetAllRebindButtonsInteractable(false);

        yield return null;

        bool keyAssigned = false;
        while (!keyAssigned)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    // Cancel rebinding if key is already bound
                    if (IsKeyAlreadyBound(key))
                    {
                        statusText.text = $"Key {key} is already bound!";
                        yield return new WaitForSeconds(1f); // Show warning for 1 second
                        statusText.text = "Press a key...";
                        break;
                    }

                    if (key != KeyCode.Escape)
                    {
                        // Assign new key
                        KeybindManager.Instance.keybinds[actionName] = key;
                        statusText.text = key.ToString();

                        // Save the new keybind
                        KeybindManager.Instance.SaveKeybinds();
                        keyAssigned = true;
                        break;
                    }
                    
                }
            }

            // Allow users to cancel rebinding by pressing Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                statusText.text = KeybindManager.Instance.keybinds[actionName].ToString();
                break; // Exit the rebinding process
            }

            yield return null;
        }

        // Reset button UI and functionality
        yield return new WaitForSeconds(0.1f);
        KeybindUIManager.Instance.SetAllRebindButtonsInteractable(true);
        rebindButton.GetComponentInChildren<TextMeshProUGUI>().text = "Rebind";
    }

    private bool IsKeyAlreadyBound(KeyCode key)
    {
        foreach (var boundKey in KeybindManager.Instance.keybinds.Values)
        {
            if (boundKey == key)
                return true;
        }
        return false;
    }
}
