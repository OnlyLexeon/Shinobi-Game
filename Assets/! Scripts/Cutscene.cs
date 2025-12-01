using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Cutscene : MonoBehaviour
{
    public static Cutscene Instance;

    public float waitTimeAfterStart = 2f;
    public GameObject cutsceneCamera;
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip cutsceneMusic;
    public string cutsceneName;

    [Header("References (Auto)")]
    public GameObject player;
    public Camera playerCamera;
    public FirstPersonController fpsController;
    public CharacterController characterController;
    public PlayerAttack playerAttack;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) Debug.LogWarning("No Player Found!");

        if (playerCamera == null) playerCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        else if (playerCamera == null) playerCamera = player.GetComponentInChildren<Camera>();
        else if (playerCamera == null) Debug.LogWarning("No playerCamera Found!");

        if (fpsController == null) fpsController = player.GetComponent<FirstPersonController>();
        if (fpsController == null) Debug.LogWarning("No fpsController Found!");

        if (characterController == null) characterController = player.GetComponent<CharacterController>();
        if (characterController == null) Debug.LogWarning("No characterController Found!");

        if (playerAttack == null) playerAttack = player.GetComponentInChildren<PlayerAttack>();
        if (playerAttack == null) Debug.LogWarning("No playerAttack Found!");

        if (cutsceneCamera == null) Debug.LogWarning("No CAMERA!!");
        StartCoroutine(DoCutscene());
    }

    public IEnumerator DoCutscene()
    {
        yield return new WaitForSeconds(waitTimeAfterStart);

        cutsceneCamera.SetActive(true);

        //Prevent player from moving
        fpsController.enabled = false;
        characterController.enabled = false;
        playerCamera.enabled = false;
        playerAttack.enabled = false;

        //Play Cutscene
        animator.SetTrigger("PlayCutscene");

        //Song
        audioSource.PlayOneShot(cutsceneMusic);

        //Get cutscene duration
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        while (stateInfo.IsName(cutsceneName) && stateInfo.normalizedTime < 1.0f)
        {
            yield return null; // Wait for the next frame
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Switch back to the player camera
        cutsceneCamera.SetActive(false);
        fpsController.enabled = true;
        characterController.enabled = true;
        playerCamera.enabled = true;
        playerAttack.enabled = true;
    }
}

