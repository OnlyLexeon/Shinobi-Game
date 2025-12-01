//using UnityEngine;
//using UnityEngine.UI;

//public class ResolutionDropdown : MonoBehaviour
//{
//    public MainMenuController mainMenuController;

//    public void OnResolutionChanged(int option)
//    {
//        Debug.Log($"Resolution dropdown value changed to option: {option}");

//        if (mainMenuController == null)
//        {
//            Debug.LogError("MainMenuController reference is not assigned in the ResolutionDropdown script.");
//            return;
//        }

//        switch (option)
//        {
//            case 0:
//                mainMenuController.ChangeResolution(1920, 1080);
//                Debug.Log("Resolution option selected: 1920 x 1080");
//                break;
//            case 1:
//                mainMenuController.ChangeResolution(1280, 720);
//                Debug.Log("Resolution option selected: 1280 x 720");
//                break;
//            case 2:
//                mainMenuController.ChangeResolution(800, 600);
//                Debug.Log("Resolution option selected: 800 x 600");
//                break;
//            default:
//                Debug.LogWarning("Invalid resolution option selected.");
//                break;
//        }
//    }
//}
