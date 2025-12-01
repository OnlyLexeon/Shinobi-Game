using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 6;
    public bool assingImmunity = true;

    [Header("Abilities")]
    public bool hasGhostSword = false;

    [Header("Sounds")]
    public AudioClip playerGetHit;

    [Header("Debug References")]
    public int currentHealth;
    public bool isBlocking = false;
    public bool isAssassinating = false;
    public bool isDead = false;
    public bool isWalking = false;
    public bool isCrouching = false;
    public bool isFalling = false;
    public bool isGrapplingHookEquipped = false;
    public LayerMask enemyLayer;

    public float fallHeight = 0f;
    private float startFallHeight = 0f;
    private float lastPositionY = 0f;

    [Header("References (Auto)")]
    public Transform cameraFacing; // Important for aiming
    public Animator cameraAnim;
    public Inventory inventoryScript;
    public PlayerAttack attackScript;
    public FirstPersonController fpsController;
    public CharacterController charController;
    public PlayerNoise noiseScript;
    public Rigidbody rb;
    public AudioSource audioSource;

    private void Start()
    {
        //ghost mode check
        if (!hasGhostSword)
        {
            if (Unlocked.Instance.IsGhostModeUnlocked()) hasGhostSword = true;
        }

        if (cameraFacing == null) cameraFacing = GameObject.FindGameObjectWithTag("MainCamera").transform;
        if (cameraFacing == null) Debug.LogWarning("Unable to Find Main Camera!");
        if (cameraAnim == null) cameraAnim = GetComponentInChildren<Animator>();
        if (cameraAnim == null) Debug.LogWarning("Unable to Find cameraAnim!");

        fpsController = GetComponent<FirstPersonController>();
        charController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        // Children
        noiseScript = GetComponentInChildren<PlayerNoise>();
        attackScript = GetComponentInChildren<PlayerAttack>();
        inventoryScript = GetComponentInChildren<Inventory>();

        if (fpsController == null) Debug.LogWarning("No fpsController reference!!");
        if (charController == null) Debug.LogWarning("No charController reference!!");
        if (noiseScript == null) Debug.LogWarning("No noiseScript reference!!");
        if (audioSource == null) Debug.LogWarning("No audioSource reference!!");
        if (attackScript == null) Debug.LogWarning("No attackScript reference!!");
        if (inventoryScript == null) Debug.LogWarning("No inventoryScript reference!!");

        enemyLayer = LayerMask.GetMask("Enemy");
        if (enemyLayer == 0) Debug.LogWarning("Enemy layer reference is missing!");

        currentHealth = maxHealth;
    }

    private void Update()
    {
        CheckFalling();
    }

    public void PlayerHit()
    {
        //either assing or blocking
        if ((assingImmunity && isAssassinating)) return;

        if (isBlocking)
        {
            attackScript.Blocked();
            return;
        }

        if (currentHealth > 0)
        {
            currentHealth--;

            //Sound
            audioSource.PlayOneShot(playerGetHit);

            if (fpsController) fpsController.OnPlayerHit();

            if (GameplayUIController.Instance) GameplayUIController.Instance.UpdateHealthSlider(currentHealth, maxHealth);
            else Debug.LogWarning("No Game UI?");
        }

        if (currentHealth <= 0)
        {
            PlayerDie();
        }
    }

    public void PlayerDie()
    {
        if (isDead == false)
        {
            currentHealth = 0;

            isDead = true;

            fpsController.enabled = false;
            charController.enabled = false;

            MenuController.Instance.GameOver();
        }
    }

    public void PlayerHealing(int amount)
    {
        if (amount <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); //clamping

        GameplayUIController.Instance.UpdateHealthSlider(currentHealth, maxHealth);
    }

    public void DoingAss()
    {
        isAssassinating = true;

        fpsController.enabled = false;
        charController.enabled = false;
    }

    public void DoneAss()
    {
        isAssassinating = false;

        fpsController.enabled = true;
        charController.enabled = true;
    }

    public void FPSControllerNoiseHandler(bool isWalk, bool isCrouched) // Called at FPSController's Sound Play (Footsteps)
    {
        //if isWalk is false, = isRunning
        isWalking = isWalk;
        isCrouching = isCrouched;
        if (!isCrouched) PlayerMadeNoise();
    }

    public void PlayerMadeNoise()
    {
        //Debug.Log("Player Made Noise!");

        noiseScript.NotifyEnemies(isWalking);
    }

    private void CheckFalling()
    {
        float currentY = transform.position.y;

        if (!charController.isGrounded)
        {
            // Check if we are now falling (we started moving down from the peak)
            if (!isFalling && currentY < lastPositionY)
            {
                //Debug.Log("Player started falling!");
                isFalling = true;
                startFallHeight = lastPositionY; // Save the height when fall starts
            }

            // Calculate fall height if falling
            if (isFalling)
            {
                fallHeight = startFallHeight - currentY; // Calculate the height fallen
                //Debug.Log("Height fallen: " + fallHeight);
            }
        }
        else
        {
            // Player has landed
            if (isFalling)
            {
                //Debug.Log("Player landed!");
                isFalling = false;
                fallHeight = 0f; // Reset fall height after landing
            }
        }

        // Update last known Y position
        lastPositionY = currentY;
    }
}

