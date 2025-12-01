using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.Utility;
using static CursorTipsManager;

public class PlayerAttack : MonoBehaviour
{
    [Header("Assign These!!!")]
    public GameObject highlightPrefab;
    public GameObject GhostSwingAffect;  // can be null
    public GameObject SwingAffect;  // can be null
    public GameObject blockParticle;
    public GameObject heavySwingAffect;
    public GameObject heavyGhostSwingAffect;
    public GameObject dashAssSwingAffect;

    [Header("Sounds (Assign pls)")]
    public AudioClip swordsClash;   // Enemy blocked
    public AudioClip enemyHit;      // Enemy hit by attack
    public AudioClip swordSwing;      // Enemy hit by attack
    public AudioClip swordParry;      // Enemy hit by attack
    public AudioClip assSound;
    public AudioClip fallingSound;
    public AudioClip assHighlightSound;
    public AudioClip ghostModeEnabled;
    public AudioClip ghostModeDisabled;

    [Header("Debug References")]
    public GameObject lookingAtEnemy; // Enemy player is looking at
    public int ghostCurrentChargeAmount = 0;
    public float currentAttackCooldown = 0f;

    [Header("Swinging with Sword")]
    public float attackCooldown = 0.6f;           // Time taken to attack + assassinate after Swing
    public bool isSwinging = false;
    public GameObject MainHandObject;

    [Header("Ghost Mode")]
    public bool ghostMode = false;
    public int ghostChargeAmount = 7; // needed to charge
    public float ghostDuration = 10f;
    public float ghostRangeBuff = 1f; // adds to all range
    public float ghostAngleBuff = 20f; // adds to all angles
    public float ghostAttackCooldown = 0.25f; // adds to all angles
    public float ghostHeavyChargeTimePercentage = 0.5f; // charge 0.5 = 50% less time
    public float ghostStunRadius = 12f;
    public float ghostStunDuration = 7f;

    [Header("Highlight Enemy")]
    public float highlightEnemyTimer = 0f;
    public float highlightEnemyCooldown = 4f;
    public float highlightEnemyDuration = 4f;

    [Header("Light")]
    public float meleeRange = 4f;
    public float meleeAngle = 100f;
    public float meleeAnimTime = 0.2f;        // Time taken to Swing after attack is called

    [Header("Heavy")] // Destroys blocks, stuns, damage them if not blocking, no damage if their blocking
    public bool isCharging = false;
    public float heavyRange = 4.25f;
    public float heavyAngle = 40f;
    public float heavyAnimTime = 0.1f;     // Time taken to Swing after attack is called

    public float heavyStunTime = 1.5f;

    public float heavyChargeTime = 1.1f;   // Time taken to complete charge heavy
    public float chargeTime = 0f;
    public float chargeStartTime = 0f;

    [Header("Land Assassinate Settings")]
    public float assRange = 3f;
    public float assScanAngle = 35f;
    public float behindEnemyThreshold = -0.4f; // Negative value = behind,
                                               // so -1: Player must be directly behind
                                               // -0.9: 40° Arc // -0.7: 100° Arc  // -0.5: 120° Arc
                                               // Ass Animation
    public float assAnimTime = 0.4f;                // Time taken to assassinate enemy after reaching it (Animation Length) 
    public float assLerpMinDistance = 0.15f;        // Min. Distance player will be from the AI after assassinating it  // Default: 0.15
    public float assLerpTime = 0.25f;               // Time taken for player to lerp/reach the AI                       // Default: 0.25

    [Header("Air Assassinate")]
    public float airAssRange = 11.5f;                // When falling (playerScript.isFalling = true), distance can ass
    public float airAssScanAngle = 65f;
    public float fallHeightRequired = 3f;           // Fall this long to assassinate
    public float airAssAnimTime = 0.1f;             // Time taken to assassinate enemy after reaching it (Animation Length)
    public float airAssLerpMinDistance = 0.15f;     // Min. Distance player will be from the AI after assassinating it  // Default: 0.15
    public float airAssLerpTime = 0.1f;             // Time taken for player to lerp/reach the AI                       // Default: 0.1

    [Header("Tip")]
    public CursorTipsManager.Tip assTip;
    public CursorTipsManager.Tip ghostTip;

    [Header("References (Auto)")]
    public GameObject currentHighlight; // Only highlights if currentHighlight = null, currentHighlight changes parents and follows above them
    public Player playerScript;
    public float maxRaycastRange = 10f;
    public bool drawSwingGizmo = false; // Flag to draw swing gizmo
    public float meleeTimer = 0f;
    public float ghostTimer = 0f;
    [Space(5f)]
    public GameObject assEnemyTarget; // Enemy within assRange & looking at
    public bool canAssassinate = false; // use this to change UI (add ass UI on screen)
    public GameObject currentAssEnemyTarget;    // Different from assEnemyTarget, what if during animation assEnemyTarget moves away/changes target
    [Space(5f)]
    public bool isPlayingFallingAudio = false;
    public bool startedCharging = false;

    void Start()
    {
        currentAttackCooldown = attackCooldown;

        if (MainHandObject == null) MainHandObject = GameObject.FindGameObjectWithTag("Main Hand");
        if (MainHandObject == null) Debug.LogWarning("Unable to Find Main HAND!!");

        if (!playerScript) playerScript = GetComponentInParent<Player>();
        if (!playerScript) Debug.LogWarning("Player Script reference is missing!");
    }

    // Update is called once per frame
    void Update()
    {
        canAssassinate = isEnemyAssable(); // Check for any assable enemies
        if (canAssassinate && assEnemyTarget && !playerScript.isAssassinating)
        {
            HighlightEnemy(assEnemyTarget);

            if (CursorTipsManager.Instance != null)
            {
                assTip.key = KeybindManager.Instance.keybinds["Attack"];
                assTip.tipMessage = "Assassinate";
                CursorTipsManager.Instance.MakeTip(assTip);
            }
                
        }
        else
        {
            RemoveHighlight();

            if (CursorTipsManager.Instance != null)
            {
                assTip.key = KeybindManager.Instance.keybinds["Attack"];
                assTip.tipMessage = "Assassinate";
                CursorTipsManager.Instance.RemoveTip(assTip);
            }
               
        }


        //Ghost Mode
        if (ghostCurrentChargeAmount >= ghostChargeAmount && playerScript.hasGhostSword)
        {
            ghostTip.key = KeybindManager.Instance.keybinds["Ghost Ultimate"];
            ghostTip.tipMessage = "Ghost Mode";
            CursorTipsManager.Instance.MakeTip(ghostTip);
        }
        if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Ghost Ultimate"]) && !playerScript.isDead)
        {
            if (!ghostMode && ghostCurrentChargeAmount >= ghostChargeAmount && playerScript.hasGhostSword)
            {
                ghostTip.key = KeybindManager.Instance.keybinds["Ghost Ultimate"];
                ghostTip.tipMessage = "Ghost Mode";
                CursorTipsManager.Instance.RemoveTip(ghostTip);

                GhostMode(true);
            }
        }
        //Ghost Timer - Disabler
        if (ghostMode)
        {
            GhostMode(false);
        }

        // !! Set attackCooldown to 0 in Frenzy, or just change attackCooldown for anything
        if (meleeTimer < currentAttackCooldown && !isSwinging && !playerScript.isAssassinating && !playerScript.isDead && !playerScript.isBlocking) meleeTimer += Time.deltaTime;
        // Check for holding left click
        //CHARGING ONLY
        if (meleeTimer >= currentAttackCooldown && !playerScript.isDead && !playerScript.isBlocking && !isSwinging)
        {
            if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Attack"]))
            { //start charge
                chargeTime = 0f;
                chargeStartTime = Time.time; // Record when charging started
                startedCharging = true;

            }
            if (Input.GetKey(KeybindManager.Instance.keybinds["Attack"]))
            {
                //start charge
                if (startedCharging == false) // bug fix chargeStartTime doesn't execute if you charge before attackcooldown
                {
                    chargeTime = 0f;
                    chargeStartTime = Time.time; // Record when charging started
                    startedCharging = true;
                }

                //Charging
                if (startedCharging)
                {
                    chargeTime = Time.time - chargeStartTime;
                    isCharging = true;
                    if (GameplayUIController.Instance) GameplayUIController.Instance.UpdateAttackCharge(chargeTime, heavyChargeTime);
                }

                //Animation checker
                if (chargeTime >= 0.125f && isCharging && startedCharging)
                {
                    Animator anim = MainHandObject.GetComponentInChildren<Animator>();
                    if (anim != null) anim.SetBool("isCharging", true);
                    else Debug.LogWarning("No Sword Anim detected!");
                }
                if (chargeTime >= heavyChargeTime && isCharging && startedCharging)
                {
                    //CHARGE DONE
                    Animator anim = MainHandObject.GetComponentInChildren<Animator>();
                    if (anim != null) anim.SetTrigger("ChargeDone");
                }

                //if (chargeTime < heavyChargeTime) Debug.Log($"Charging... {chargeTime}");
                //else Debug.Log($"Heavy Ready!");
            }
            if (Input.GetKeyUp(KeybindManager.Instance.keybinds["Attack"]))
            {
                startedCharging = false;
                isCharging = false;

                //Slider
                if (GameplayUIController.Instance) GameplayUIController.Instance.HideAttackSlider();

                Animator anim = MainHandObject.GetComponentInChildren<Animator>();
                if (anim != null) anim.SetBool("isCharging", false);
                else Debug.LogWarning("No Sword Anim detected!");

                //ATTACK
                if (canAssassinate && assEnemyTarget != null && chargeTime < heavyChargeTime)
                {
                    Assassinate(isAirAss(), playerScript.fpsController.m_IsWalking);
                }
                else if (chargeTime >= heavyChargeTime * (ghostMode ? ghostHeavyChargeTimePercentage : 1f)) // Heavy
                {
                    Swing(true); //isHeavy
                }
                else
                {
                    Swing(false);
                    
                }

                chargeTime = 0f;
                meleeTimer = 0f;
            }
        }

        //BLOCKING
        if (!isSwinging && !playerScript.isAssassinating && !playerScript.isDead && !isCharging)
        {   
            if (Input.GetKey(KeybindManager.Instance.keybinds["Block"]))
            {
                playerScript.isBlocking = true;

                Animator anim = MainHandObject.GetComponentInChildren<Animator>();
                if (anim != null) anim.SetBool("isBlocking", true);
                else Debug.LogWarning("No Sword Anim detected!");
            }

            if (Input.GetKeyUp(KeybindManager.Instance.keybinds["Block"]))
            {
                playerScript.isBlocking = false;

                Animator anim = MainHandObject.GetComponentInChildren<Animator>();
                if (anim != null) anim.SetBool("isBlocking", false);
                else Debug.LogWarning("No Sword Anim detected!");
            }
        }
    }

    public void Blocked()
    {
        playerScript.audioSource.PlayOneShot(swordsClash);
        Instantiate(blockParticle, transform.position, Quaternion.identity, transform);
    }

    public void GhostMode(bool state)
    {
        if (state == true)
        {
            //Enter ghost mode
            GhostModeStun();

            ghostMode = true;
            ghostTimer = 0f;
            ghostCurrentChargeAmount = 0;
            currentAttackCooldown = ghostAttackCooldown;

            GameplayUIController.Instance.UpdateGhostSlider(ghostCurrentChargeAmount, ghostChargeAmount);
            RendererToggleManager.Instance.ToggleRendererFeature("Kurosawa Filter", true);

            // Sound
            playerScript.audioSource.PlayOneShot(ghostModeEnabled);
        }
        else
        {
            ghostTimer += Time.deltaTime;
            if (ghostTimer >= ghostDuration)
            {
                ghostMode = false;
                ghostTimer = 0f;
                currentAttackCooldown = attackCooldown;

                GameplayUIController.Instance.UpdateGhostSlider(ghostCurrentChargeAmount, ghostChargeAmount);
                RendererToggleManager.Instance.ToggleRendererFeature("Kurosawa Filter", false);
                
                //Sound
                playerScript.audioSource.PlayOneShot(ghostModeDisabled);
            }
        }
    }
    public void IncreaseGhostCharge()
    {
        if (!ghostMode && ghostCurrentChargeAmount < ghostChargeAmount)
        {
            ghostCurrentChargeAmount++;

            GameplayUIController.Instance.UpdateGhostSlider(ghostCurrentChargeAmount, ghostChargeAmount);
        }
    }

    public bool isFacingEnemy() // Closest/First rayhit enemy is Target
    {
        //is falling? use wider angle
        float scanAngle = (isAirAss() ? airAssScanAngle : assScanAngle) + (ghostMode ? ghostAngleBuff : 0);

        Collider[] hitColliders = Physics.OverlapSphere(playerScript.cameraFacing.position, maxRaycastRange, playerScript.enemyLayer);
        float closestDistance = float.MaxValue;
        GameObject closestEnemy = null;

        foreach (Collider collider in hitColliders)
        {
            Vector3 closestPointOnCollider = collider.ClosestPoint(playerScript.cameraFacing.position);
            Vector3 directionToPoint = (closestPointOnCollider - playerScript.cameraFacing.position).normalized;

            float angleToPoint = Vector3.Angle(playerScript.cameraFacing.forward, directionToPoint);
            if (angleToPoint <= scanAngle / 2)
            {
                float distanceToPoint = Vector3.Distance(playerScript.cameraFacing.position, closestPointOnCollider);

                if (distanceToPoint < closestDistance)
                {
                    closestDistance = distanceToPoint;
                    closestEnemy = collider.gameObject;
                }
            }
        }

        if (closestEnemy != null)
        {
            lookingAtEnemy = closestEnemy;
            return true;
        }

        lookingAtEnemy = null;
        assEnemyTarget = null;
        canAssassinate = false;
        return false;
    }

    private bool IsPlayerBehindEnemy(Transform enemyTransform)
    {
        Vector3 toPlayer = (transform.position - enemyTransform.position).normalized;
        return Vector3.Dot(enemyTransform.forward, toPlayer) < behindEnemyThreshold; // Adjust threshold for "behind"
    }

    public bool isEnemyAssable() // Ass-ass-inatable?
    {
        if (!isFacingEnemy() || lookingAtEnemy == null) return false;

        Enemy enemyScript = lookingAtEnemy.GetComponent<Enemy>();

        if (!enemyScript.canBeAssed) return false;

        if (isAirAss())
        {
            // AIR ASSASSINATION
            if (IsValidTarget(enemyScript) && // not dead
                Vector3.Distance(transform.position, lookingAtEnemy.transform.position) <= airAssRange + (ghostMode ? ghostRangeBuff : 0) && //within AIR ass range
                (!enemyScript.playerInDetectionArea || enemyScript.isChoking || enemyScript.isStunned)) // not in view OR enemy is busy choking
            {
                assEnemyTarget = lookingAtEnemy;
                //Debug.Log("AIR ASS");
                Debug.DrawLine(transform.position, lookingAtEnemy.transform.position, Color.magenta, 1f);
                return true;
            }
        }
        else // NOT FALLING
        {
            // LAND ASSASSINATION
            if (IsValidTarget(enemyScript) && // not dead
            Vector3.Distance(transform.position, lookingAtEnemy.transform.position) <= assRange + (ghostMode ? ghostRangeBuff : 0) && //within ass range
            (IsPlayerBehindEnemy(lookingAtEnemy.transform) && !enemyScript.playerInDetectionArea || enemyScript.isChoking || enemyScript.isStunned)) // is behind & not in view OR enemy is busy choking
            {
                assEnemyTarget = lookingAtEnemy;
                //Debug.Log("LAND ASS");
                Debug.DrawLine(transform.position, lookingAtEnemy.transform.position, Color.magenta, 1f);
                return true;
            }
        }
            

        assEnemyTarget = null;
        return false;
    }

    private bool IsValidTarget(Enemy enemyScript)
    {
        // NOT DEAD
        return enemyScript != null && !enemyScript.isDead;
    }

    
    
    public bool isAirAss()
    {
        if (playerScript.isFalling && playerScript.fallHeight >= fallHeightRequired)
        {
            if (!isPlayingFallingAudio)
            {
                isPlayingFallingAudio = true;
                playerScript.audioSource.PlayOneShot(fallingSound);
            }
            return true;
        }
        else
        {
            if (isPlayingFallingAudio)
            {
                isPlayingFallingAudio = false;
                playerScript.audioSource.Stop();
                playerScript.fpsController.PlayLandingSound();
            }
            return false;
        }
    }

    public void GhostModeStun()
    {
        // Find all colliders within the noise radius on the enemy layer
        Collider[] hitColliders = Physics.OverlapSphere(playerScript.transform.position, ghostStunRadius, playerScript.enemyLayer);

        foreach (Collider collider in hitColliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && !enemy.isDead)
            {
                enemy.Stun(ghostStunDuration);
            }
        }
    }

    public void Assassinate(bool isAirAssassinate, bool isWalking)
    {
        if (assEnemyTarget != null)
        {
            currentAssEnemyTarget = assEnemyTarget;

            Enemy enemyScript = currentAssEnemyTarget.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                //Ass
                Debug.Log("ASS");
                playerScript.isAssassinating = true;
                playerScript.DoingAss(); // Disables fpscontroller and charcontroller
                IncreaseGhostCharge();

                enemyScript.PauseActivity();
                enemyScript.agent.isStopped = true;
                enemyScript.agent.ResetPath();

                //Sounds
                playerScript.audioSource.PlayOneShot(assSound);

                //Animations
                Animator anim = MainHandObject.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    if (isAirAssassinate)
                    {
                        anim.SetTrigger("Air Ass"); //air ass
                        //camera animation
                        playerScript.cameraAnim.SetTrigger("AirAss");
                    }
                    else if (!isWalking)
                    {
                        anim.SetTrigger("Dash Ass"); // Running dash ass
                        GameObject swingEffect = Instantiate(dashAssSwingAffect, playerScript.cameraFacing.transform.position, Quaternion.identity, playerScript.cameraFacing.transform);
                        swingEffect.transform.localRotation = Quaternion.identity;
                        //camera animation
                        playerScript.cameraAnim.SetTrigger("DashAss");
                    }
                    else
                    {
                        anim.SetTrigger("Ass"); // normal ass
                        //camera animation
                        playerScript.cameraAnim.SetTrigger("Ass");
                    }
                }
                else Debug.LogWarning("No Sword Anim detected!");
            }
            else Debug.LogWarning("NO ENEMY SCRIPT!!");

            //mving to Enemy Animation
            StartCoroutine(MoveToEnemy(isAirAssassinate, isWalking));
        }
        else Debug.LogWarning("NO assEnemyTarget!!");
    }

    public IEnumerator MoveToEnemy(bool isAirAssassinate, bool isWalking)
    {
        Vector3 startPosition = playerScript.transform.position;
        Vector3 endPosition = currentAssEnemyTarget.transform.position;

        if (isAirAssassinate)
        {
            // Air assassination uses air-specific parameters
            Vector3 directionToEnemy = (endPosition - startPosition).normalized;
            Vector3 targetPosition = endPosition - directionToEnemy * (airAssLerpMinDistance + 1f); //+value cuz of playerRadius and enemyRadius

            // Calculate the distance to cover and speed based on air parameters
            float journeyLength = Vector3.Distance(startPosition, targetPosition);
            float startTime = Time.time;

            while (Vector3.Distance(playerScript.transform.position, targetPosition) > airAssLerpMinDistance)
            {
                float distanceCovered = (Time.time - startTime) * (journeyLength / airAssLerpTime);
                float fractionOfJourney = distanceCovered / journeyLength;

                playerScript.transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

                yield return null;
            }
        }
        else // NON AIR (dash / normal)
        {
            //Distance/Position Player will be
            Vector3 directionToEnemy = (endPosition - startPosition).normalized;
            Vector3 targetPosition;
            if (!isWalking) targetPosition = endPosition + directionToEnemy * (assLerpMinDistance + 1.5f); //+value cuz of playerRadius and enemyRadius
            else targetPosition = endPosition - directionToEnemy * (assLerpMinDistance + 0.75f); //+value cuz of playerRadius and enemyRadius

            // Calculate distance to cover and speed (we'll use the time to get there)
            float journeyLength = Vector3.Distance(startPosition, targetPosition);  // Total distance to target position
            float startTime = Time.time;

            while (Vector3.Distance(playerScript.transform.position, targetPosition) > assLerpMinDistance)
            {
                // Calculate the distance moved so far as a fraction of the total journey
                float distanceCovered = (Time.time - startTime) * (journeyLength / assLerpTime);
                float fractionOfJourney = distanceCovered / journeyLength;

                // Lerp player position towards enemy
                playerScript.transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);

                yield return null;
            }
        }

        // Reached, stab
        StartCoroutine(AssassinateStab());
    }

    public IEnumerator AssassinateStab()
    {
        yield return new WaitForSeconds(assAnimTime);

        playerScript.isAssassinating = false;
        playerScript.DoneAss();

        Enemy enemyScript = currentAssEnemyTarget.GetComponent<Enemy>();
        if (enemyScript)
        {
            enemyScript.Die(true);
        }
        else Debug.LogWarning("NO ENEMY SCRIPT!!");
    }


    public void Swing(bool isHeavy)
    {
        //Debug.Log($"Swing! Heavy: {isHeavy}");
        // Animate swinging here
        if (isHeavy) // heavy
        {
            GameObject swingEffect;
            if (heavySwingAffect && !ghostMode) //slash effect
            {
                swingEffect = Instantiate(heavySwingAffect, playerScript.cameraFacing.transform.position, Quaternion.identity, playerScript.cameraFacing.transform);
                swingEffect.transform.localRotation = Quaternion.identity;
            }
            else if (heavyGhostSwingAffect)
            {
                swingEffect = Instantiate(heavyGhostSwingAffect, playerScript.cameraFacing.transform.position, Quaternion.identity, playerScript.cameraFacing.transform);
                swingEffect.transform.localRotation = Quaternion.identity;
            }

            //animate heavy
            Animator anim = MainHandObject.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger("Attack");
            else Debug.LogWarning("No Sword Anim detected!");

            //camera animation
            playerScript.cameraAnim.SetTrigger("Heavy");

            //heavy sound
            playerScript.audioSource.PlayOneShot(swordSwing);
        }
        else if (!isHeavy) // light 
        {
            //Light sound
            playerScript.audioSource.PlayOneShot(swordSwing);

            GameObject swingEffect;
            if (SwingAffect && !ghostMode) //slash effect
            {
                swingEffect = Instantiate(SwingAffect, playerScript.cameraFacing.transform.position, Quaternion.identity, playerScript.cameraFacing.transform);
                swingEffect.transform.localRotation = Quaternion.identity;
            }
            else if (GhostSwingAffect)
            {
                swingEffect = Instantiate(GhostSwingAffect, playerScript.cameraFacing.transform.position, Quaternion.identity, playerScript.cameraFacing.transform);
                swingEffect.transform.localRotation = Quaternion.identity;
            }

            // light animation
            Animator anim = MainHandObject.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger("Attack");
            else Debug.LogWarning("No Sword Anim detected!");

            //camera animation
            playerScript.cameraAnim.SetTrigger("Attack");
        }

        isSwinging = true;
        StartCoroutine(StartSwing(isHeavy));
    }
    public IEnumerator StartSwing(bool isHeavy)
    {
        
        float waitTime = isHeavy ? heavyAnimTime : meleeAnimTime;
        float attackRange = (isHeavy ? heavyRange : meleeRange) + (ghostMode ? ghostRangeBuff : 0);
        float attackAngle = (isHeavy ? heavyAngle : meleeAngle) + (ghostMode ? ghostAngleBuff : 0);

        NotifyEnemiesOfAttack(attackAngle, attackRange);

        yield return new WaitForSeconds(waitTime);

        //ATTACK IS HERE

        isSwinging = false;

        // Use the player's camera direction to calculate the swing area
        Vector3 cameraForward = playerScript.cameraFacing.forward;
        Vector3 swingPosition = transform.position; // The origin of the swing

        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, playerScript.enemyLayer);
        HashSet<Enemy> hitEnemySet = new HashSet<Enemy>(); // Prevent duplicate hits

        foreach (Collider enemy in hitEnemies)
        {
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null && !hitEnemySet.Contains(enemyScript) && !enemyScript.isDead)
            {
                Vector3 toEnemy = (enemy.transform.position - swingPosition).normalized;
                if (Vector3.Angle(cameraForward, toEnemy) <= attackAngle / 2)
                {
                    hitEnemySet.Add(enemyScript);

                    if (isHeavy)
                    {
                        //SOUND BEFORE HIT (to determine if blocking or not)
                        if (enemyScript.isBlocking) playerScript.audioSource.PlayOneShot(swordParry);
                        else playerScript.audioSource.PlayOneShot(enemyHit);

                        enemyScript.HitByHeavyMelee(1, heavyStunTime); // HIT AND STUN
                    }
                    else if (!isHeavy)
                    {
                        //SOUND BEFORE HIT (to determine if blocking or not)
                        if (enemyScript.isBlocking) playerScript.audioSource.PlayOneShot(swordsClash);
                        else playerScript.audioSource.PlayOneShot(enemyHit);

                        enemyScript.HitByMelee(); // HIT
                    }

                    //GHOST MODE CHARGE
                    if (enemyScript.isDead)
                    {
                        IncreaseGhostCharge();
                    }

                    Debug.Log($"Hit enemy: {enemyScript.name}");
                }
            }
        }

        drawSwingGizmo = true;
        //StartCoroutine(ResetSwingGizmo()); //DISABLE-ABLE disableable disable
    }
    public void NotifyEnemiesOfAttack(float angle, float range)
    {
        // Find all colliders within the noise radius on the enemy layer
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range + 1f, playerScript.enemyLayer); // +1f to include radius of colliders
        foreach (Collider collider in hitColliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && !enemy.isDead && enemy.canBlock) // CAN BLOCK ONLY
            {
                Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);

                if (angleToEnemy <= (angle + 10f) / 2 && enemy.playerInDetectionArea)
                {
                    enemy.SeeAttack(); // Enemies react to attacks
                }
            }
        }
    }


    public void HighlightEnemy(GameObject target)
    {
        if (currentHighlight == null)
        {
            currentHighlight = Instantiate(highlightPrefab, target.transform.position, Quaternion.identity);
            playerScript.audioSource.PlayOneShot(assHighlightSound);
        }

        AssassinateIndicatorHighlight highlightScript = currentHighlight.GetComponent<AssassinateIndicatorHighlight>();
        highlightScript.SetTarget(target);
    }

    public void RemoveHighlight()
    {
        Destroy(currentHighlight);
    }

    //private IEnumerator ResetSwingGizmo() //DISABLE-ABLE disableable disable
    //{
    //    yield return new WaitForSeconds(0.25f);
    //    drawSwingGizmo = false;
    //}

    //private void OnDrawGizmos() //DISABLE-ABLE disableable disable
    //{
    //    drawFacingGizmos();

    //    if (drawSwingGizmo)
    //    {
    //        Gizmos.color = Color.yellow;
    //        Gizmos.DrawWireSphere(transform.position, meleeRange);

    //        // Draw angle cone
    //        Vector3 leftBoundary = Quaternion.Euler(0, -meleeAngle / 2, 0) * transform.forward * meleeRange;
    //        Vector3 rightBoundary = Quaternion.Euler(0, meleeAngle / 2, 0) * transform.forward * meleeRange;

    //        Gizmos.color = Color.gray;
    //        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
    //        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

    //    }
    //}

    //private void drawFacingGizmos() //DISABLE-ABLE disableable disable
    //{
    //    if (playerScript.cameraFacing == null) return;

    //    Gizmos.color = Color.blue;
    //    Gizmos.DrawWireSphere(playerScript.cameraFacing.position, maxRaycastRange); // Draw the detection sphere

    //    // Draw cone boundaries
    //    Vector3 leftBoundary = Quaternion.Euler(0, -assScanAngle / 2, 0) * playerScript.cameraFacing.forward * maxRaycastRange;
    //    Vector3 rightBoundary = Quaternion.Euler(0, assScanAngle / 2, 0) * playerScript.cameraFacing.forward * maxRaycastRange;

    //    Gizmos.color = Color.cyan;
    //    Gizmos.DrawLine(playerScript.cameraFacing.position, playerScript.cameraFacing.position + leftBoundary);
    //    Gizmos.DrawLine(playerScript.cameraFacing.position, playerScript.cameraFacing.position + rightBoundary);

    //    // Draw a line to the closest enemy, if any
    //    if (lookingAtEnemy != null)
    //    {
    //        Gizmos.color = Color.black;
    //        Gizmos.DrawLine(playerScript.cameraFacing.position, lookingAtEnemy.transform.position); // Line to closest enemy
    //        Gizmos.DrawSphere(lookingAtEnemy.transform.position, 0.2f); // Mark closest enemy
    //    }
    //}

}
