using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;


public class Enemy : MonoBehaviour
{
    public float minPlayerDistanceToRun = 45f;

    [Header("Particles")]
    public GameObject blockParticle;
    public GameObject parriedParticle;

    [Header("Enemy Settings")]
    public int baseHealth = 4;
    public float baseMovementSpeed = 5f;
    public float baseRotationSpeed = 120f;
    public float maxDistanceFromNodes = 2.5f; //Default: 3
    public float despawnTime = 300f;
    public bool canBeAssed = true;
    public bool canBeInstaKilled = true;

    [Header("Combat Settings")]
    public bool hitCauseAlert = true; //melee hits cause aggro
    public bool canBlock = true;
    public bool isBlocking = false;
    public bool isAttacking = false;

    public float combatModeProximity = 8f; // Prefered distance from player
    public float combatModeOutDecayTime = 0.5f; // If player steps out, wait for this amount of time before continuing chase
    public bool combatMode = false;

    [Header("Smoked Settings")]
    public bool inSmoke = false;
    public float smokeChokeAfterDisappear = 2f; // Extra choking time after smoke disappears

    [Header("Debug Stats (Auto)")]
    public int currentHealth = 0;
    public float movementSpeed = 5f;
    public float rotationSpeed = 120f;
    [Space(5f)]
    public bool isWalking = false;
    public bool isRunning = false;
    public bool isStrafing = false;
    public bool strafingRight = false;
    [Space(5f)]
    public bool isChoking = false; // Stunned - Choking
    public float chokingDuration = 0f;
    public float chokingTimer = 0f;
    public bool isStunned = false; // Stunned - Confused?
    public float stunnedDuration = 0f;
    public float stunnedTimer = 0f;
    [Space(5f)]
    public bool isDead = false; //Should stop ALL functions (KILL SWITCH)
    [Space(5f)]
    public bool isExecutingActivity = false; // Prevents multiple activities from running simultaneously
    public bool isActivityPaused = false; // Pause activity when player is detected/chased/investigated
    [Space(5f)]
    public bool playerInDetectionArea = false;

    [Header("References (Auto Assign)")]
    public Transform player;            // Reference to the player
    public NavMeshAgent agent;
    public Animator anim;
    public CapsuleCollider capsuleCollider;

    public AudioSource audioSource;

    public DetectionAI detectionScript;
    public ActivityAI activityScript;
    public EnemyAttack attackScript;
    public EnemySounds soundScript;

    public LayerMask playerLayer;       // Layer mask for detecting players
    public LayerMask enemyLayer;       // Layer mask for detecting enemies
    public LayerMask smokeLayer;
    public LayerMask surfacesLayer;

    public int overlappingSmokes = 0; // Fix bug with overlapping smokes,
                                      // if 1 ends, shouldn't "unsmoke" the enemy
    public float combatModeTimer = 0f;

    private float updateTimer = 0f; // Tracks time since the last update
    private const float updateInterval = 0.1f; // Interval in seconds

    private void Awake()
    {
        // Priority: Layers
        if (playerLayer == 0) playerLayer = LayerMask.GetMask("Player");
        if (playerLayer == 0) Debug.LogWarning("Player Layer reference is missing!");
        if (enemyLayer == 0) enemyLayer = LayerMask.GetMask("Enemy");
        if (enemyLayer == 0) Debug.LogWarning("Enemy Layer reference is missing!");
        if (smokeLayer == 0) smokeLayer = LayerMask.GetMask("Smoke");
        if (smokeLayer == 0) Debug.LogWarning("Smoke Layer reference is missing!");
        if (surfacesLayer == 0) surfacesLayer = LayerMask.GetMask("Surfaces");
        if (surfacesLayer == 0) Debug.LogWarning("Surfaces Layer reference is missing!");
        // Find Player w Layer
        if (player == null) FindPlayer();
        if (player == null) Debug.LogWarning("Player reference is missing! Unable to FindPlayer()");
        // Others
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null) Debug.LogWarning("This Enemy is Missing capsuleCollider!");
        if (detectionScript == null) detectionScript = GetComponent<DetectionAI>();
        if (detectionScript == null) Debug.LogWarning("This Enemy is Missing detectionScript!");
        if (activityScript == null) activityScript = GetComponent<ActivityAI>();
        if (activityScript == null) Debug.LogWarning("This Enemy is Missing activityScript!");
        if (attackScript == null) attackScript = GetComponent<EnemyAttack>();
        if (attackScript == null) Debug.LogWarning("This Enemy is Missing attackScript!");
        if (soundScript == null) soundScript = GetComponentInChildren<EnemySounds>(); // CHILD OBJECT
        if (soundScript == null) Debug.LogWarning("This Enemy is Missing soundScript!");
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogWarning("This Enemy is Missing NavMeshAgent! Other AI Scripts will not be able to run!");
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim == null) Debug.LogWarning("This Enemy is Missing anim!");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = baseHealth;
        movementSpeed = baseMovementSpeed;
        rotationSpeed = baseRotationSpeed;

        agent.speed = movementSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = maxDistanceFromNodes;

        if (detectionScript != null) detectionScript.enabled = true;
        if (activityScript != null) activityScript.enabled = true;

        //Sound
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) Debug.LogWarning("No AudioSource Found!!");
    }

    private void Update()
    {
        if (player == null) { FindPlayer(); return; }

        updateTimer += Time.deltaTime;

        // GET DIRECTION 
        if (HasReachedDestination())
        {
            isWalking = false;
            isRunning = false;
            anim.SetBool("isWalking", false);
            anim.SetBool("isRunning", false);
            anim.SetBool("isBackingUp", false);
        }

        if (isChoking)
        {
            chokingTimer += Time.deltaTime;
            if (chokingTimer >= chokingDuration)
            {
                isChoking = false;
                anim.SetBool("isChoke", false);
                chokingTimer = 0f;
                chokingDuration = 0f;
            }
        }
        
        if (isStunned)
        {
            stunnedTimer += Time.deltaTime;
            if (stunnedTimer >= stunnedDuration)
            {
                isStunned = false;
                anim.SetBool("isStun", false);
                if (agent.isActiveAndEnabled) agent.isStopped = false;

                stunnedTimer = 0f;
                stunnedDuration = 0f;
            }
        }
    }

    //Animation
    public void SetMovementAnimation()
    {
        if (!canEnemyPerform()) return;

        // WALKING OR RUNNING?
        if (movementSpeed <= baseMovementSpeed + 1f) // walking
        {
            if (isWalking == false) anim.SetTrigger("Walk");

            isWalking = true;
            isRunning = false;
            anim.SetBool("isWalking", true);
            anim.SetBool("isRunning", false);
            anim.SetBool("isBackingUp", false);
        }
        else // running
        {
            if (isRunning == false) anim.SetTrigger("Run");

            isWalking = false;
            isRunning = true;
            anim.SetBool("isWalking", false);
            anim.SetBool("isRunning", true);
            anim.SetBool("isBackingUp", false);
        }

        Vector3 toDestination = agent.destination - transform.position;
        Vector3 localDirection = transform.InverseTransformDirection(toDestination.normalized);
        if (localDirection.z < -0.5f) // back
        {
            anim.SetBool("isBackingUp", true);
            anim.SetBool("isWalking", false);
            anim.SetBool("isRunning", false);
            anim.SetTrigger("Back");
        }
    }

    // Blocking
    public void SeeAttack()
    {
        attackScript.TryToBlock();
    }

    // Combat
    public void EnterCombatMode()
    {
        Debug.Log("Combat ON");
        if (agent.isActiveAndEnabled)
        {
            agent.isStopped = true; //Stop moving
            agent.updateRotation = false;
        }
        
        detectionScript.isLookingAround = false; //bug fix 

        if (agent.isActiveAndEnabled) agent.isStopped = false; //Reenable to reposition

        combatMode = true;
        combatModeTimer = 0f;

        if (canEnemyPerform()) attackScript.Reposition();
    }
    public void DisableCombatMode()
    {
        Debug.Log("Combat OFF");
        combatMode = false;
        if (agent.isActiveAndEnabled) agent.updateRotation = true;
    }
    public void DamangeTaken(int hitPoints, bool ignoresBlocks, bool isHeavy)
    {
        // No Minus if blocking
        if (!isBlocking || ignoresBlocks)
        {
            // IMPACT ANIMATION
            anim.SetTrigger("TakeDamage");
                
            currentHealth -= hitPoints;

            //SOUND
            if (currentHealth == 1) soundScript.PlayFinalHitSound(); // in pain, boutto die
            else soundScript.PlayHitSound(); //neutral

            attackScript.onEnemyHit();
        }
        else // success block
        {
            // BLOCK IMPACT
            if (!isHeavy)
            {
                anim.SetTrigger("BlockHit");
                Instantiate(blockParticle, transform.position, Quaternion.identity, transform);
            }
            if (isHeavy)
            {
                anim.SetTrigger("TakeDamage");
                Instantiate(parriedParticle, transform.position, Quaternion.identity, transform);
            }
        }

        // Death check
        if (currentHealth <= 0)
        {
            Die(false);
        }
    }
    // Light
    public void HitByMelee(int hitPoints)
    {
        if (hitCauseAlert && currentHealth > 0) detectionScript.InstantAggroMelee(); //pass player location and alert

        DamangeTaken(hitPoints, false, false); //take damage before stop blocking

        if (isBlocking) attackScript.StopBlocking();
    }
    public void HitByMelee()
    {
        HitByMelee(1);
    }
    // Heavy
    public void HitByHeavyMelee(int hitPoints, float stunDuration) //+ STUNNED
    {
        if (hitCauseAlert && currentHealth > 0) detectionScript.InstantAggroMelee(); //pass player location and alert
        
        Stun(stunDuration);
        DamangeTaken(hitPoints, false, true);

        if (isBlocking) attackScript.StopBlocking(); //isBlocking false
    }
    public void HitByHeavyMelee()
    {
        HitByHeavyMelee(1, 1);
    }
    // Range
    public void HitByRange(int hitPoints, bool canInstaKill)
    {
        if (canInstaKill && canBeInstaKilled)
        {
            if (detectionScript.currentState == DetectionState.Alerted) DamangeTaken(hitPoints, true, false); //range is penetraing (not affected by block)
            else Die(false);
        }
        else
        {
            DamangeTaken(hitPoints, true, false); //range is penetraing (not affected by block)
        }

        //InstantAggroRange alerts enemies but doesn't pass player info
        if (hitCauseAlert && currentHealth > 0) detectionScript.InstantAggroRange(); // AggroRange is different
    }
    public void HitByRange()
    {
        HitByRange(1, false);
    }

    // Stunning
    public void Stun(float duration)
    {
        PauseActivity();
        if (agent.isActiveAndEnabled) agent.isStopped = true; // fix while moving, they get stunned, stops them from moving

        isStunned = true;
        anim.SetBool("isStun", true);
        anim.SetTrigger("Stun");

        if (duration >= stunnedDuration) stunnedDuration = duration; // Overwrite if its a stronger stun
        
        stunnedTimer = 0f;
    }

    // Dying
    public void Die(bool assed)
    {
        //game stats
        if (GameStats.Instance) GameStats.Instance.IncreaseKill();

        //disable slider
        detectionScript.slider.gameObject.SetActive(false);

        //Sound
        if (!assed) soundScript.PlayDyingSound();
        else if (assed) soundScript.PlayAssedSound();

        //ANIMATION
        anim.SetBool("isDead", true);
        anim.SetTrigger("Dieded");

        currentHealth = 0; // Make sure health is ded
        isDead = true;

        if (agent.isActiveAndEnabled) agent.isStopped = true;
        isActivityPaused = true;
        isExecutingActivity = false;

        if (agent.isActiveAndEnabled) agent.enabled = false;

        //Disable collider
        capsuleCollider.excludeLayers = LayerMask.GetMask("Everything");
        capsuleCollider.includeLayers = LayerMask.GetMask("Enemy");

        //GameStats
        CheckAndReportIfEnemyTarget();

        Destroy(gameObject, despawnTime);
    }

    //Speed & Rotation Agent
    public void SpeedMult(float rate)
    {
        movementSpeed = baseMovementSpeed * rate;

        if (agent == null && agent.isActiveAndEnabled) Debug.LogWarning("No NavMesh Agent found! Can't update Agent Speed");
        else if (agent.isActiveAndEnabled) agent.speed = movementSpeed;
    }
    public void RotationMult(float rate)
    {
        rotationSpeed = baseRotationSpeed * rate;

        if (agent == null && agent.isActiveAndEnabled) Debug.LogWarning("No NavMesh Agent found! Can't update Agent Rotation Speed");
        else if (agent.isActiveAndEnabled) agent.angularSpeed = rotationSpeed;
    }

    // Activity
    public void ContinueActivity()
    {
        if (agent.pathPending && agent.isActiveAndEnabled) agent.isStopped = true;

        if (agent.isActiveAndEnabled) agent.isStopped = false;
        isExecutingActivity = false;
        isActivityPaused = false;
    }
    public void PauseActivity()
    {
        isActivityPaused = true;
    }

    //Smoke
    private void OnTriggerEnter(Collider collider)
    {
        if ((smokeLayer.value & (1 << collider.gameObject.layer)) != 0) // Code for checking if collider layer is smoke layer
        {
            EnteredSmoke(collider);
            Debug.Log("Enemy entered smoke.");
        }
    }
    public void EnteredSmoke(Collider collider)
    {
        //Sound
        soundScript.PlayChokingSound();

        PauseActivity();
        if (agent.isActiveAndEnabled) agent.isStopped = true; // fix while moving, they get somked

        inSmoke = true;
        isChoking = true; // start choke in smoke
        anim.SetBool("isChoke", true);
        anim.SetTrigger("Choke");
        chokingTimer = 0f; // Reset timer jic

        // choke until smoke ends + after smoke effects
        SmokeBomb smokeScript = collider.GetComponent<SmokeBomb>();
        if (smokeScript != null)
        {
            chokingDuration = Mathf.Max(chokingDuration, smokeScript.durationRemaining + smokeChokeAfterDisappear); // if a weaker smoke is entered,
                                                                                                                    // won't decrease stronger smoke time
        }
        else
        {
            Debug.LogWarning("SmokeBomb script not found on smoke collider!");
        }

        overlappingSmokes++;
    }
    private void OnTriggerExit(Collider collider)
    {
        if ((smokeLayer.value & (1 << collider.gameObject.layer)) != 0) // Code for checking if collider layer is smoke layer
        {
            OnSmokeDisabled();
        }
    }
    public void OnSmokeDisabled()
    {
        if (agent.enabled) agent.isStopped = false; // fix while moving, they get smoked
        overlappingSmokes--;

        if (overlappingSmokes <= 0)
        {
            inSmoke = false;

            //Fix smoke moved
            if (isChoking && !inSmoke) chokingDuration = smokeChokeAfterDisappear;
        }
    }

    //Noise Hearing
    public void HeardNoise(Transform playerTransform, bool isWalking)
    {
        detectionScript.HeardNoise(playerTransform, isWalking);
    }

    // Has
    public bool HasLineOfSight(Transform target)
    {
        RaycastHit hit;
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, directionToTarget, out hit, detectionScript.GetCurrentDetectionDistance()))
        {
            Debug.DrawRay(transform.position, directionToTarget * detectionScript.GetCurrentDetectionDistance(), Color.red, 0.01f); // DISABLE-ABLE disable disablable
            return hit.transform == target;
        }
        return false;
    }
    public bool HasClearLineOfSightForRepositionOrStrafe(Vector3 targetPosition)
    {
        RaycastHit hit;
        Vector3 directionToTarget = (targetPosition - transform.position).normalized; // Direction to the target position

        // Perform a raycast in the direction the enemy wants to move
        if (Physics.Raycast(transform.position, directionToTarget, out hit, detectionScript.GetCurrentDetectionDistance(), surfacesLayer))
        {
            // Visualize the raycast (optional, for debugging)
            Debug.DrawRay(transform.position, directionToTarget * detectionScript.GetCurrentDetectionDistance(), Color.green, 0.1f);

            // If we hit something, return false (meaning the path is blocked)
            return false;
        }

        // If no obstacles are detected, return true (line-of-sight is clear)
        return true;
    }
    public bool HasReachedDestination()
    {
        if (agent.isActiveAndEnabled && !agent.pathPending && !isDead) // Ensure the path is ready
        {
            // Check if the agent is close enough to its destination
            float remainingDistance = agent.remainingDistance;
            return remainingDistance != Mathf.Infinity && remainingDistance <= agent.stoppingDistance;
        }
        return false;
    }
    public bool canEnemyPerform()
    {
        float distanceFromPlayer = 0f;
        if (player) distanceFromPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceFromPlayer >= minPlayerDistanceToRun) return false;
        if (isDead || isChoking || isStunned || isBlocking || isAttacking) return false;
        else return true;
    }
    private void FindPlayer()
    {
        //find the player within range
        Collider[] colliders = Physics.OverlapSphere(transform.position, Mathf.Infinity, playerLayer); // Since it's a singleplayer game, it uses Mathf.Inf to find the player!!!

        if (colliders.Length > 0)
        {
            player = colliders[0].transform; //get first player found
            //Debug.Log($"Player found: {player.name}");
        }
        else
        {
            //Debug.Log("No player detected within the specified range.");
        }
    }

    // On death, check if im a target
    public void CheckAndReportIfEnemyTarget()
    {
        // Check if the GameObject is tagged as "EnemyTarget"
        if (gameObject.CompareTag("EnemyTarget"))
        {
            // Call the TargetKilled function in GameStats
            GameStats.Instance.TargetKilled();
            Debug.Log($"Target killed: {gameObject.name}");
        }
        else
        {
            Debug.Log($"{gameObject.name} is not tagged as EnemyTarget.");
        }
    }
}
