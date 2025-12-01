using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
public enum DetectionState { Normal, AwarePause, Aware, Investigating, Alerted }

public class DetectionAI : MonoBehaviour
{
    public bool enemyContributesToAlertStat = true;
    private float lastDetectionCheckTime = 0f; // To throttle detection
    private const float detectionInterval = 0.1f; // Run detection every 0.2 seconds

    [Header("Sus Slider")]
    public bool showSlider = false;
    public float sliderHideAfter = 5f;
    public float sliderTimer = 0f;
    public Slider slider;
    public Image sliderColorImage;
    public Color alertColor = new Color(1.0f, 0.259f, 0.212f); //red
    public Color inveColor = new Color(0.851f, 0.620f, 0.341f); //red
    public Color awareColor = new Color(1.0f, 0.992f, 0.459f); // yellow
    public Color normalColor = new Color(0.443f, 0.792f, 0.431f); //green

    [Header("Sound")]
    public AudioClip detectedSound;
    public AudioClip susIncrease;

    [Header("Current Values (Debug/Auto)")]
    [Tooltip("Current suspicion level.\nIncreases when player is within Yellow or Green zones.")]
    public float susMeter = 0f;             // Current suspicion level
    [Tooltip("Display the current state of the AI.\nCan't change the state manually.")]
    public DetectionState currentState = DetectionState.Normal;
    [Tooltip("True: AI's suspicion meter is paused for awarePauseTime amount of time.\nFalse: Suspicion Meter is running like normal.")]
    public bool isSusPause = false;        // Bool to see if suspicion meter is paused or not
    [Tooltip("True:AI is rotating towards the randomDirection generated in LookAround()\nFalse:Not Looking Around or waiting to be idle during Investigation Mode.")]
    public bool isLookingAround = false; // Flag to track if the AI is currently looking around

    [Header("Detection Settings")]
    [Tooltip("Default detection range.\nAffectable by invDetectDistanceInc and alertDetectionDistanceInc.\nDefault: 15")]
    public float detectDistance = 16.5f;   // Detection range
    [Tooltip("Green zone angle.\nAffectable by inveGreenAngleInc and alertGreenAngleInc\nDefault: 110")]
    public float greenAngle = 110f;          // Green zone angle
    [Tooltip("Yellow zone angle.\nAffectable by inveYellowAngleInc and alertYellowAngleInc\nDefault: 60")]
    public float yellowAngle = 65f;         // Yellow zone angle

    [Header("Corpse Detection Settings")]
    [Tooltip("True: Corpse is in Vision\nFalse: No Corpse is in AI Vision.")]
    public bool corpseInDetectionArea = false;
    [Tooltip("Multilply suspicion increment by this amount when a Corpse is in Detection Zone.\nAdds on to alertBuffs\nDefault: 1.4")]
    public float corpseMultiplier = 1.4f;  // Corpse sus multilpier

    [Header("Smoke Detection Settings")]
    [Tooltip("True: Smoke is in Vision\nFalse: No Smoke is in AI Vision.")]
    public bool lockInInveMode = false;
    [Tooltip("Multilply suspicion increment by this amount when a Smoke Bomb is in Detection Zone.\nAdds on to  alertBuffs\nDefault: 1.2")]
    public float smokeMultiplier = 1.2f;    // Smoke sus multiplier
    [Tooltip("Multilply detectDistance by this amount when AI is in a Smoke Collider.\nSmoke Collider Objects must have Smoke LayerMask!!\nDefault: 0.4f (40%)")]
    public float smokeDetectDistanceMult = 0.3f;    // Multiply detection distance when in smoke
    [Tooltip("Multilply greenAngle by this amount when AI is in a Smoke Collider.\nSmoke Collider Objects must have Smoke LayerMask!!\nDefault: 0.4f (40%)")]
    public float smokeGreenAngleMult = 0.3f;        // Multiply green angle when in smoke
    [Tooltip("Multilply yellowAngle by this amount when AI is in a Smoke Collider.\nSmoke Collider Objects must have Smoke LayerMask!!\nDefault: 0.4f (40%)")]
    public float smokeYellowAngleMult = 0.3f;       // Multiply yellow angle when in smoke

    [Header("Suspicion Settings")]
    // Detection Area (Da Cone)
    [Tooltip("Green zone increment.\nAffectable by inveSusMultiplier and alertSusMultiplier.\nIncreases the suspicion meter by this amount.\nDefault: 8")]
    public float greenSusInc = 8f;          // Rate of suspicion INCREASE in the green zone
    [Tooltip("Yellow zone increment.\nAffectable by inveSusMultiplier and alertSusMultiplier.\nIncreases the suspicion meter by this amount.\nDefault: 14")]
    public float yellowSusInc = 14f;        // Rate of suspicion INCREASE in the yellow zone
    [Tooltip("Starting suspicion meter decrement.\nShould decrease after susDecayDelay of seconds.\nExponentially until maxSusDecayValue\nDefault: 8")]
    public float susDec = 8f;               // Rate of suspicion DECREASE during BEFORE Investigation Mode
    // Decay (increases with time)
    [Tooltip("Maximum value suspicion meter can decay at.\n(As long as Investigation was not triggered).\nDefault: 14")]
    public float maxSusDecayValue = 14f;  // Time for faster decay after player goes out of sight
    [Tooltip("Time taken for suspicion meter to start decaying.\n(As long as Investigation was not triggered).\nDefault: 3")]
    public float susDecayDelay = 3f;        // Time before suspicion starts to decrease
    // Distance affects suspicion
    [Tooltip("True: Closer player is from AI, the higher the suspicion increment.\nFalse: Distance between AI and Player do not affect suspicion increment values at all.")]
    public bool susAffectedByDistance = true;
    [Tooltip("The maximum value of which suspicion increments are multiplied by.\nExample:\t100% at max detectionDistance value." +
        "\n\t300% if player is at current position of the AI.\nBeing directly infront would be lesser than 300%.\nDefault: 300")]
    public float maxSusDistancePercent = 300f;  // The percentage increase for all suspicion increments depending on distance between Player and AI
                                                // (100% is current value, 200% would x2 the current value if player is insanely close to the AI)

    [Header("Hearing Settings")]
    [Tooltip("Increase Sus Meter by this value whenever Player walks.\nAffectable by awareSusMultiplier and inveSusMultiplier.\nDefault: 6")]
    public float noiseWalkSusInc = 30f;      // Walk
    [Tooltip("Increase Sus Meter by this value whenever Player runs.\nAffectable by awareSusMultiplier and inveSusMultiplier.\nDefault: 12")]
    public float noiseRunSusInc = 60f;      // Run
    [Tooltip("Time taken before suspicion decreases after hearing Suspicios Noises from Player.\nDefault: 2.5")]
    public float noiseHeardDecay = 5f;
    [Tooltip("True: Closer player is from AI, the higher the suspicion increment.\nFalse: Distance between AI and Player do not affect suspicion increment values at all.")]
    public bool noiseAffectedByDistance = true;
    [Tooltip("The maximum value of which suspicion increments are multiplied by.\nExample:\t100% at max detectionDistance value." +
        "\n\t300% if player is at current position of the AI.\nBeing directly infront would be lesser than 300%.\nDefault: 300")]
    public float noiseMaxSusDistancePercent = 700f;

    [Header("Aware Settings")]
    [Tooltip("Value suspicion meter needs to be to trigger Aware Mode.\n(Default: 50)")]
    public float awareMeter = 50f;          // Trigger value for Aware Mode
    // Detection Settings Changes
    [Tooltip("detectionDistance increase after entering Aware Mode.\ndetectionDistance + awareDetectDistanceInc.\nDefault: 3")]
    public float awareDetectDistanceInc = 3f; // Detection range increase during Aware Mode
    [Tooltip("greenAngle increase after entering Aware Mode.\ngreenAngle + awareGreenAngleInc.\nDefault: 10")]
    public float awareGreenAngleInc = 10f;   // INCREASE ANGLE by value of green zone
    [Tooltip("yellowAngle increase after entering Aware Mode.\nyellowAngle + awareYellowAngleInc.\nDefault: 15")]
    public float awareYellowAngleInc = 15f;  // INCREASE ANGLE by value of yellow zone
    // Suspicion Settings Changes
    [Tooltip("Multiplier for suspicion increment values during Aware Mode.\nExample: greenSusInc * awareSusMultiplier.\n(Recommended: 1.1 - 1.4).\nDefault: 1.1")]
    public float awareSusMultiplier = 1.1f;  // MULTIPLIER for suspicion INCREASE if pass awareMeter
    // Decay (linear)
    [Tooltip("Linear decrease of the suspicion meter with time.\nUnlike normal mode, the AI decreases linearly rather than exponentially.\nDefault: 8")]
    public float awareSusDec = 8f;           // Rate of suspicion DECREASE during Aware Mode
    [Tooltip("Time taken for suspicion to start decaying during Aware Mode.\nDefault: 6")]
    public float awareSusDecayDelay = 6f;    // Time before suspicion starts to DECREASE after enter Aware mode
    // Other
    [Tooltip("Multiplier for Rotation Speed in ActivityAI Script during Aware Mode\nDefault: 1.2")]
    public float awareRotationSpeedMult = 1.2f; // Rotation speed MULTIPLIER

    [Header("Investigate Settings")]
    [Tooltip("Value suspicion meter needs to be to trigger Investigation Mode.\n(Default: 100)")]
    public float inveMeter = 100f;          // Trigger value for Investigation Mode
    // Detection Settings Changes
    [Tooltip("detectionDistance increase after entering Investigation Mode.\ndetectionDistance + inveDetectDistanceInc\nDefault: 6")]
    public float inveDetectDistanceInc = 6f; // Detection range increase during Investigate Mode
    [Tooltip("greenAngle increase after entering Investigation Mode.\ngreenAngle + inveGreenAngleInc.\nDefault: 20")]
    public float inveGreenAngleInc = 20f;   // INCREASE ANGLE by value of green zone
    [Tooltip("yellowAngle increase after entering Investigation Mode.\nyellowAngle + inveYellowAngleInc.\nDefault: 25")]
    public float inveYellowAngleInc = 25f;  // INCREASE ANGLE by value of yellow zone
    // Suspicion Settings Changes
    [Tooltip("Multiplier for suspicion increment values during Investigation Mode.\nExample: greenSusInc * inveSusMultiplier.\n(Recommended: 1.1 - 1.4).\nDefault: 1.25")]
    public float inveSusMultiplier = 1.25f;  // MULTIPLIER for suspicion INCREASE if pass investigatingMeter
    // Decay (linear)
    [Tooltip("Linear decrease of the suspicion meter with time.\nUnlike normal mode, the AI decreases linearly rather than exponentially.\nDefault: 4")]
    public float inveSusDec = 5f;           // Rate of suspicion DECREASE during Investigation Mode
    [Tooltip("Time taken for suspicion to start decaying during Investigation Mode.\nDefault: 10")]
    public float inveSusDecayDelay = 10f;    // Time before suspicion starts to DECREASE after enter Investigate mode
    // Investigate Look around
    [Tooltip("Time taken before every LookAround() during Investigation Mode\nwhen AI reaches it's destination (Either the player or lastKnownPosition).\nDefault: 3")]
    public float inveLookInterval = 3f;     // Time to wait before looking around
    [Tooltip("The positive and negative value determining the angle for looking around in Investigation Mode.\nDefault: 180")]
    public float inveLookAngle = 120;       // Value for look around angle 
    // Other
    [Tooltip("Multiplier for Rotation Speed in ActivityAI Script during Investigation Mode\nDefault: 1.5")]
    public float inveRotationSpeedMult = 1.5f; // Rotation speed MULTIPLIER

    [Header("Alert Settings")]
    [Tooltip("True: AI is currently alert.\nThis bool ensures that AI doesn't get alert buff and perma buffs.")]
    public bool isAlert = false;                // Flag to determine if guard has permanent buffs from being alerted
    [Tooltip("Value suspicion meter needs to be to trigger Alert Mode.\n(Default: 150)")]
    public float alertMeter = 150f;             // Trigger value for alert mode
    // Detection Settings Changes
    [Tooltip("detectionDistance increase after entering Alert Mode.\ndetectionDistance + AlertDetectDistanceInc.\nDefault: 12")]
    public float alertDetectDistanceInc = 10f; // Detection range increase during Alert Mode
    [Tooltip("greenAngle increase after entering Alert Mode.\ngreenAngle + alertGreenAngleInc\nDefault: 50")]
    public float alertGreenAngleInc = 45f;   // INCREASE ANGLE by value of green zone
    // Alert Exclusive
    [Tooltip("Time taken for suspicion to start decaying after Alert Mode is triggered.\nDefault: 20")]
    public float alertModePeriod = 20f;    // Time before suspicion starts to DECREASE after enter Alert mode
    [Tooltip("Multiplier for AI movement speed after the alert mode is triggerd.\nDefault: 1.7")]
    public float alertMoveSpeedMult = 1.7f;    // Multiplies movement speed by this amount
    [Tooltip("Radius which other AI around this one will be instantly alerted as well.\nDefault: 21")]
    public float alertModeSpread = 21f;    // Radius to alert other AI.
    // Alert Look Around
    [Tooltip("Time taken before every LookAround() during Alert Mode\nwhen AI reaches it's destination (Either the player or lastKnownPosition).\nDefault: 1.2")]
    public float alertLookInterval = 1.2f;     // Time to wait before looking around
    [Tooltip("The positive and negative value determining the angle for looking around in Alert Mode.\nDefault: 300")]
    public float alertLookAngle = 200;       // Value for look around angle 
    // Other
    [Tooltip("Multiplier for Rotation Speed in ActivityAI Script during Investigation Mode\nDefault: 1.8")]
    public float alertRotationSpeedMult = 1.8f; // Rotation speed MULTIPLIER

    [Header("Alerted Buffs Settings")]
    // Permanent Buff after entering Alert Mode
    [Tooltip("True: AI has buffed angles, vision range, sus multipier and movement speed after being Alerted.")]
    public bool alertBuffs = false;                // Flag to determine if guard has permanent buffs from being alerted
    [Tooltip("Detection Distance Increment after a AI enters Alert Mode (permanent).\nIncrements other multipliers.\nDefault: 2")]
    public float buffDetectDistanceInc = 3f;    // permanent Detection Distance buff after Alert Mode is triggered.
    [Tooltip("greenAngle increase after entering Alert Mode (permanent).\nIncrements other multipliers.\ngreenAngle + buffGreenAngleInc\nDefault: 10\nRecommended: 10-15 to not go beyond Alert Mode Buffs")]
    public float buffGreenAngleInc = 15f;   // INCREASE ANGLE by value of green zone
    [Tooltip("yellowAngle increase after entering Alert Mode (permanent).\nIncrements other multipliers.\nyellowAngle + buffYellowAngleInc\nDefault: 10\nRecommended: 10-15 to not go beyond Alert Mode Buffs")]
    public float buffYellowAngleInc = 15f;  // INCREASE ANGLE by value of yellow zone
    [Tooltip("Multiplier for suspicion increment after a AI enters Alert Mode (permanent).\nIncrements other multipliers.\nDefault: 1.5")]
    public float buffSusMultiplier = 1.4f;    // permanent Sus Meter MULTIPLIER after Alert Mode is triggered.
    [Tooltip("Multiplier for AI movement speed after alert mode (permanent).\nDefault: 1.2")]
    public float buffMoveSpeedInc = 1.2f;    // Multiplies movement speed by this amount
    [Tooltip("Multiplier for Rotation Speed in ActivityAI Script after Alert Mode is triggered. (Permanent).\nIncrements other multipliers.\nDefault: 1.2")]
    public float buffRotationSpeedMult = 1.2f; // Rotation speed MULTIPLIER
    // Other
    [Tooltip("Radius which other AI around this one will be instantly alerted as well. (permanent)\nDefault: 14")]
    public float alertedSpread = 14f;    // Radius to alert other AI.

    [Header("References (Auto)")]       // Should auto assign itself, but just to be safe, assign it yourself
    public Enemy enemyScript;           // Reference to Enemy Script in AI game Object

    [Header("Other References (Auto Assign)")]
    public float outOfSightTimer = 0f; // Timer for how long the player has been out of sight
    public Vector3 lastKnownPosition;   // Last position where the player was seen
    public float inveIdleTimer = 0f; // Timer to track idle time
    public Quaternion randomLookDirection;
    public float alertTimer = 0f;
    public List<GameObject> corpsesSeen = new List<GameObject>();
    public float noiseHeardTimer = 0f;
    public DetectionState previousState;

    private void Start()
    {
        if (enemyScript == null) enemyScript = GetComponent<Enemy>();
        if (enemyScript == null) Debug.LogWarning("Enemy Script reference is missing!");

        if (slider == null) slider = GetComponentInChildren<Slider>();
        if (slider == null) Debug.LogWarning("slider reference is missing!");
        else //initialize slider
        {
            slider.maxValue = alertMeter;
            sliderColorImage = slider.fillRect.GetComponent<Image>();
            if (sliderColorImage == null) Debug.LogWarning("sliderColorImage reference is missing!");
            sliderColorImage.color = normalColor;
        }
    }

    private void Update()
    {
        if (enemyScript && enemyScript.player) // canEnemyPerform - isDead? isChoking? isStunned?
        {
            HandleDetection();
            HandleCurrentState();
        }
    }

    private void HandleDetection()
    {
        //SLIDER
        float distanceToPlayer = Vector3.Distance(transform.position, enemyScript.player.position);
        if (enemyScript && enemyScript.playerInDetectionArea && !showSlider && !enemyScript.isDead) showSlider = true;
        else if (distanceToPlayer >= GetCurrentDetectionDistance() || enemyScript.isDead) showSlider = false;

        if (slider)
        {
            if (showSlider && currentState != DetectionState.Alerted)
            {
                //Slider
                if (slider) slider.value = susMeter;

                showSlider = true;
                sliderTimer = 0f;
                slider.gameObject.SetActive(true);
            }
            else if (!showSlider)
            {
                sliderTimer += Time.deltaTime;
                if (sliderTimer > sliderHideAfter)
                {
                    slider.gameObject.SetActive(false);
                    sliderTimer = 0f;
                }
            }
        }

        // Only run detection logic periodically to save CPU
        if (Time.time - lastDetectionCheckTime >= detectionInterval)
        {
            lastDetectionCheckTime = Time.time;

            // Detect entities
            DetectCorpses();
            DetectSmokebomb();

            if (distanceToPlayer <= GetCurrentDetectionDistance()) // Player in range
            {
                DetectPlayer();

                if (enemyScript.playerInDetectionArea) outOfSightTimer = 0f;

                // In range but not detected
                if (!enemyScript.combatMode && !enemyScript.playerInDetectionArea && susMeter > 0)
                {
                    DecreaseSuspicion();
                }
            }
            else // Player not in range
            {
                enemyScript.playerInDetectionArea = false;
                if (susMeter > 0 && !enemyScript.combatMode) DecreaseSuspicion();
            }
        }
    }

    private void HandleCurrentState()
    {
        DoCurrentState();

        //BEHAVIORS
        // Rotate towards the player during Aware if they are in the green or yellow zones
        if (!enemyScript.combatMode && susMeter >= awareMeter && enemyScript.playerInDetectionArea && enemyScript.HasLineOfSight(enemyScript.player))
        {
            RotateTowardsPlayer(); //Aware ++
        }
        else if (!enemyScript.combatMode && susMeter >= awareMeter && susMeter <= inveMeter && !enemyScript.playerInDetectionArea)
        {
            RotateTowardsLastKnown(); //Aware, but no sight of Player
        }
        else if (enemyScript.combatMode)
        {
            RotateTowardsPlayer();
        }


        //Tell other AI to be alert too, give the other AIs lastKnownLocation, alertBuff true, isAlert true, susMeter 150
        if (alertBuffs) //alertbuffs means has been alerted which is also isAlert
        {
            AlertOtherAI();
        }

        // Handle looking around during investigation
        if (isLookingAround)
        {
            RotateTowardsLookDirection();
        }
    }

    public void DoCurrentState()
    {
        // NORMAL 0
        // Reset if below 50 and state isnt normal
        if (susMeter < awareMeter && currentState != DetectionState.Normal)
        {
            SetCurrentState(DetectionState.Normal);
        }
        // AWARE 50
        else if (susMeter >= awareMeter && susMeter < inveMeter)
        {
            SetCurrentState(DetectionState.Aware);
        }
        // INVESTIGATE 100
        // If suspicion is high enough, either follow or investigate the player
        else if (susMeter >= inveMeter && susMeter < alertMeter)
        {
            SetCurrentState(DetectionState.Investigating);
        }
        // ALERT/AGGRO 150
        // Gains perma buffs, for a short duration, gain a lot of buffs
        else if (susMeter >= alertMeter)
        {
            if (!lockInInveMode || enemyScript.playerInDetectionArea || corpseInDetectionArea)
            {
                SetCurrentState(DetectionState.Alerted);
            }
        }
    }

    public void SetCurrentState(DetectionState state)
    {
        // Manages buffs, states, etc
        currentState = state;

        //Previous state not current state?
        // Play Sound
        // Update game stats
        if (currentState != previousState)
        {
            previousState = currentState;
            switch (currentState)
            {
                case DetectionState.Alerted:
                    if (enemyScript.soundScript) enemyScript.soundScript.PlayAlerted();
                    if (GameStats.Instance && enemyContributesToAlertStat) GameStats.Instance.IncreaseAlerted();
                    break;
                case DetectionState.Investigating:
                    if (enemyScript.soundScript) enemyScript.soundScript.PlayInvestigate();
                    break;
                case DetectionState.Aware:
                    if (enemyScript.soundScript)
                    {
                        //Sound
                        enemyScript.audioSource.PlayOneShot(detectedSound);
                        enemyScript.soundScript.PlayAware();
                    }
                    break;

                case DetectionState.Normal:
                    if (enemyScript.soundScript) enemyScript.soundScript.PlayBackToNormal();
                    break;
            }
        }
        
        // Speed/Rotation change depending on state
        float speedMult = 1;
        float rotationMult = 1;

        if (currentState != DetectionState.Alerted) isAlert = false;
        
        switch (currentState)
        {
            case DetectionState.Alerted:
                enemyScript.PauseActivity();
                sliderColorImage.color = alertColor; //Slider
                if (alertTimer < alertModePeriod) AlertMode();
                speedMult += (alertMoveSpeedMult - 1);
                rotationMult += alertRotationSpeedMult;
                float distanceFromPlayer = Vector3.Distance(transform.position, enemyScript.player.position);
                if (distanceFromPlayer <= enemyScript.combatModeProximity && enemyScript.HasLineOfSight(enemyScript.player) && !enemyScript.combatMode) enemyScript.EnterCombatMode(); // Combat Proximity?
                else if (distanceFromPlayer > enemyScript.combatModeProximity && enemyScript.combatMode) // Player Exit Combat Proximity
                {
                    enemyScript.combatModeTimer += Time.deltaTime;
                    if (enemyScript.combatModeTimer >= enemyScript.combatModeOutDecayTime)
                    {
                        enemyScript.DisableCombatMode();
                    }
                }
                else if (enemyScript.combatMode) // Player within proximity
                {
                    if (!enemyScript.playerInDetectionArea) // Player not in sight
                    {
                        lastKnownPosition = enemyScript.player.transform.position;
                    }
                }
                else if (!enemyScript.combatMode) // No combat mode / Lost player, Go investigate
                {
                    if (enemyScript.playerInDetectionArea && enemyScript.HasLineOfSight(enemyScript.player)) GoTo(enemyScript.player.position); // Follow player if in vision
                    else GoTo(lastKnownPosition); // If player disappears, go to last known location
                }
                break;
            case DetectionState.Investigating:
                enemyScript.PauseActivity();
                sliderColorImage.color = inveColor; //Slider
                rotationMult += (inveRotationSpeedMult - 1);
                if (enemyScript.playerInDetectionArea && enemyScript.HasLineOfSight(enemyScript.player)) GoTo(enemyScript.player.position); // Follow player if in vision
                else if (!enemyScript.inSmoke) GoTo(lastKnownPosition); // If player disappears, go to last known location
                break;
            case DetectionState.Aware:
                enemyScript.PauseActivity();
                sliderColorImage.color = awareColor; //Slider
                if (enemyScript.agent.isActiveAndEnabled) enemyScript.agent.isStopped = true; // bug fix: AI still moving after entering aware mode
                rotationMult += (awareRotationSpeedMult - 1);
                break;
            case DetectionState.Normal:
                sliderColorImage.color = normalColor; //Slider
                enemyScript.ContinueActivity(); // Resume normal activities
                break;
        }

        if (alertBuffs)
        {
            speedMult += (buffMoveSpeedInc - 1);
            rotationMult += (buffRotationSpeedMult - 1);
        }

        enemyScript.SpeedMult(speedMult);
        enemyScript.RotationMult(rotationMult);
    }

    public void AlertMode() // Start Alert Period
    {
        // Alert Timer resets if player is seen agian btw
        susMeter = alertMeter;
        isAlert = true;
        alertBuffs = true;
        isSusPause = true;

        alertTimer += Time.deltaTime;

        if (alertTimer >= alertModePeriod)
        {
            isSusPause = false;
            susMeter--; //Just in case alert mode loop
            isAlert = false;
            alertTimer = 0f;
        }
    }

    // CALCULATIONS for Distance and Angles
    public float GetCurrentDetectionDistance()
    {
        float currentDetectionDistance = 0;

        switch (currentState) // STATE INCREMENTS
        {
            case DetectionState.Investigating:
                currentDetectionDistance = detectDistance + inveDetectDistanceInc;
                break;
            case DetectionState.Aware:
                currentDetectionDistance = detectDistance + awareDetectDistanceInc;
                break;
            case DetectionState.Alerted:
                currentDetectionDistance = detectDistance + alertDetectDistanceInc;
                break;
            case DetectionState.Normal:
                currentDetectionDistance = detectDistance;
                break;
        }

        if (alertBuffs && !isAlert) // PERMA ALERT BUFF
        {
            currentDetectionDistance += buffDetectDistanceInc;
        }

        if (enemyScript && enemyScript.inSmoke) // SMOKE MULT
        {
            currentDetectionDistance *= smokeDetectDistanceMult;
        }

        return currentDetectionDistance;
    }
    public float GetCurrentGreenAngle()
    {
        float currentGreenAngle = 0;

        switch (currentState) // STATE INCREMENTS
        {
            case DetectionState.Investigating:
                currentGreenAngle = greenAngle + inveGreenAngleInc;
                break;
            case DetectionState.Aware:
                currentGreenAngle = greenAngle + awareGreenAngleInc;
                break;
            case DetectionState.Alerted:
                currentGreenAngle = greenAngle + alertGreenAngleInc;
                break;
            case DetectionState.Normal:
                currentGreenAngle = greenAngle;
                break;
        }

        if (alertBuffs && !isAlert) // PERMA ALERT BUFF
        {
            currentGreenAngle += buffGreenAngleInc;
        }

        if (enemyScript && enemyScript.inSmoke) // SMOKE MULT
        {
            currentGreenAngle *= smokeGreenAngleMult;
        }

        return currentGreenAngle;
    }
    public float GetCurrentYellowAngle()
    {
        float currentYellowAngle = 0;

        switch (currentState) // STATE INCREMENT
        {
            case DetectionState.Investigating:
                currentYellowAngle = yellowAngle + inveYellowAngleInc;
                break;
            case DetectionState.Aware:
                currentYellowAngle = yellowAngle + awareYellowAngleInc;
                break;
            case DetectionState.Normal:
                currentYellowAngle = yellowAngle;
                break;
        }

        if (alertBuffs && !isAlert) // PERMA ALERT BUFF
        {
            currentYellowAngle += buffYellowAngleInc;
        }

        if (enemyScript && enemyScript.inSmoke) // SMOKE MULT
        {
            currentYellowAngle *= smokeYellowAngleMult;
        }

        return currentYellowAngle;
    }
    private float CurrentDecayDelay =>
    currentState switch
    {
        DetectionState.Investigating => inveSusDecayDelay,
        DetectionState.Aware => awareSusDecayDelay,
        _ => susDecayDelay // Default decay delay for Normal state
    };
    private float CurrentSusDecrement =>
    currentState switch
    {
        DetectionState.Investigating => inveSusDec,
        DetectionState.Aware => awareSusDec,
        _ => susDec // Default decrement for Normal state
    };
    private float GetCurrentAlertSpreadDistance()
    {
        float distance = 0;

        if (isAlert) distance = alertModeSpread;
        if (!isAlert && alertBuffs) distance = alertedSpread;

        return distance;
    }
    private float CurrentLookInterval =>
    currentState switch
    {
        DetectionState.Investigating => inveLookInterval,
        DetectionState.Alerted => alertLookInterval,
        _ => 3f // Default to 3 cuz why not
    };
    private float CurrentLookAngle =>
    currentState switch
    {
        DetectionState.Investigating => inveLookAngle,
        DetectionState.Alerted => alertLookAngle,
        _ => 200f // Default to 200 cuz why not
    };

    private void DetectPlayer()
    {
        // Check all potential hits in the detection range
        Collider[] hits = Physics.OverlapSphere(transform.position, GetCurrentDetectionDistance(), enemyScript.playerLayer);
        foreach (var hit in hits)
        {
            Vector3 directionToPlayer = (hit.transform.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            // Check for yellow zone
            if (angleToPlayer <= GetCurrentYellowAngle() / 2 && enemyScript.HasLineOfSight(hit.transform))
            {
                IncreaseSuspicion(yellowSusInc, false);
                PlayerDetected(hit);
            }
            // Check for green zone
            else if (angleToPlayer <= GetCurrentGreenAngle() / 2 && enemyScript.HasLineOfSight(hit.transform))
            {
                IncreaseSuspicion(greenSusInc, false);
                PlayerDetected(hit);
            }
            else enemyScript.playerInDetectionArea = false;
        }
    }
    private void PlayerDetected(Collider hit)
    {
        lastKnownPosition = hit.transform.position;
        enemyScript.playerInDetectionArea = true;
        alertTimer = 0f;
    }

    private void AlertOtherAI()
    {
        // Check all potential hits in the detection range
        Collider[] hits = Physics.OverlapSphere(transform.position, GetCurrentAlertSpreadDistance(), enemyScript.enemyLayer);
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            //1) Not this gameobject itself
            //2) Not dead
            //3) Not in combat mode already
            if (hit.gameObject != gameObject && !enemy.isDead && !enemy.combatMode)
            {
                DetectionAI detectionScript = enemy.detectionScript;
                if (detectionScript != null)
                {
                    //Player Chase Behavior - When this AI is alert, alert other AI
                    if (enemyScript.playerInDetectionArea && susMeter >= alertMeter)
                    {
                        detectionScript.susMeter = detectionScript.alertMeter;
                        //pass on knowledge
                        detectionScript.lastKnownPosition = enemy.player.position;
                        detectionScript.GoTo(lastKnownPosition);
                    }
                    else //Not alert, but has been alerted
                    {
                        //pass on knowledge
                        detectionScript.alertBuffs = true;
                    }
                }
            }
        }
    }

    private void DetectCorpses()
    {
        float detectionDistance = GetCurrentDetectionDistance();
        float yellowAngle = GetCurrentYellowAngle() / 2;
        float greenAngle = GetCurrentGreenAngle() / 2;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionDistance, enemyScript.enemyLayer);
        foreach (var hit in hits)
        {
            if (ProcessPotentialCorpse(hit, yellowAngle, greenAngle)) CorpseDetected(hit);
            else corpseInDetectionArea = false;
        }
    }
    private bool ProcessPotentialCorpse(Collider hit, float yellowAngle, float greenAngle)
{
    Enemy hitEnemyScript = hit.GetComponent<Enemy>();
    if (hitEnemyScript == null || !hitEnemyScript.isDead || corpsesSeen.Contains(hit.gameObject))
        return false;

    Vector3 directionToCorpse = (hit.transform.position - transform.position).normalized;
    float angleToCorpse = Vector3.Angle(transform.forward, directionToCorpse);

    if (angleToCorpse <= yellowAngle && enemyScript.HasLineOfSight(hit.transform))
    {
        IncreaseSuspicion(yellowSusInc * corpseMultiplier, false);
        return true;
    }
    else if (angleToCorpse <= greenAngle && enemyScript.HasLineOfSight(hit.transform))
    {
        IncreaseSuspicion(greenSusInc * corpseMultiplier, false);
        return true;
    }

    return false;
}
    private void CorpseDetected(Collider hit)
    {        
        lastKnownPosition = hit.transform.position;
        corpseInDetectionArea = true;
        alertTimer = 0f;

        // Only add corpse to corpse seen if alerted
        if (susMeter >= alertMeter) corpsesSeen.Add(hit.gameObject);
    }
    public void ClearCorpsesSeen()
    {
        corpsesSeen.Clear();
    }

    private void DetectSmokebomb()
    {
        float detectionDistance = GetCurrentDetectionDistance();
        float yellowAngle = GetCurrentYellowAngle() / 2;
        float greenAngle = GetCurrentGreenAngle() / 2;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionDistance, enemyScript.smokeLayer);
        foreach (var hit in hits)
        {
            if (ProcessPotentialSmoke(hit, yellowAngle, greenAngle)) SmokeDetected(hit);
            else lockInInveMode = false;
        }
    }
    private bool ProcessPotentialSmoke(Collider hit, float yellowAngle, float greenAngle)
    {
        Vector3 directionToSmoke = (hit.transform.position - transform.position).normalized;
        float angleToSmoke = Vector3.Angle(transform.forward, directionToSmoke);

        if (angleToSmoke <= yellowAngle && enemyScript.HasLineOfSight(hit.transform))
        {
            if (susMeter < alertMeter - 1f) IncreaseSuspicion(yellowSusInc * corpseMultiplier, false);
            return true;
        }
        else if (angleToSmoke <= greenAngle && enemyScript.HasLineOfSight(hit.transform))
        {
            if (susMeter < alertMeter - 1f) IncreaseSuspicion(greenSusInc * corpseMultiplier, false);
            return true;
        }

        return false;
    }
    public void SmokeDetected(Collider hit)
    {
        SmokeBomb smokeScript = hit.GetComponent<SmokeBomb>();
        if (smokeScript)
        {
            Vector3 smokePosition = hit.transform.position;
            Vector3 directionToSmoke = (smokePosition - transform.position).normalized;
            float safeDistance = smokeScript.areaOfEffect + 1f;
            lastKnownPosition = smokePosition - (directionToSmoke * safeDistance);
        }
        else
        {
            lastKnownPosition = hit.transform.position;
        }

        lockInInveMode = true;
        //Debug.Log($"Smoke seen! {susMeter}");
    }


    public void HeardNoise(Transform playerTransform, bool isWalking) // Called by Enemy Script
    {
        //show slider
        showSlider = true;

        float baseRate = isWalking ? noiseWalkSusInc : noiseRunSusInc;

        // If player isn't spotted, but hears the player, increase
        // Fixes: Doubling suspicion increase if player is getting chased
        if (!enemyScript.playerInDetectionArea) IncreaseSuspicion(baseRate, true);
        
        noiseHeardTimer = 0f;

        // Only pass Player Position
        // 1) if Player isn't already being seen 
        // 2) AI is > Aware
        if (!enemyScript.playerInDetectionArea && susMeter >= awareMeter)
        {
            lastKnownPosition = playerTransform.position;
        }

        //Debug.Log($"Heard noise from player! Walking: {isWalking}, Base Rate: {baseRate}");
    }

    private void IncreaseSuspicion(float rate, bool isTypeNoise)
    {
        if (!isSusPause)
        {
            float multiplier = 1;

            if (alertBuffs && !isAlert) multiplier += (buffSusMultiplier - 1);

            switch (currentState)
            {
                case DetectionState.Investigating:
                    multiplier += (inveSusMultiplier - 1);
                    //Debug.Log("Suspicion Increased! Investigating Mode");
                    break;
                case DetectionState.Aware:
                    multiplier += (awareSusMultiplier - 1);
                    //Debug.Log("Suspicion Increased! Aware Mode");
                    break;
            }

            if (!isTypeNoise) IncreaseSuspicionLooking(multiplier, rate);
            else if (isTypeNoise) IncreaseSuspicionNoise(multiplier, rate);

            //Sound
            float pitch = Mathf.Lerp(1f, 2f, Mathf.InverseLerp(0f, 150f, susMeter));
            enemyScript.soundScript.pitchChangingAudioSource.pitch = pitch;
            enemyScript.soundScript.pitchChangingAudioSource.PlayOneShot(susIncrease);

            //Debug.Log($"Suspicion Increased: {susMeter} | Rate: {rate} | Multiplier: {multiplier}");
        }
    }
    private void IncreaseSuspicionNoise(float multiplier, float rate)
    {
        if (noiseAffectedByDistance)
        {
            // Distance
            float distanceToPlayer = Vector3.Distance(transform.position, enemyScript.player.position);
            // Clamp
            float clampedDistance = Mathf.Clamp(distanceToPlayer, 0, GetCurrentDetectionDistance());
            // Calculate the distance-based multiplier (1.0 at max distance, 2.0 at the AI's position)
            float distanceMultiplier = 1 + (noiseMaxSusDistancePercent / 100f - 1) * (1 - clampedDistance / GetCurrentDetectionDistance());

            multiplier += (distanceMultiplier - 1);   
        }

        rate *= multiplier;
        IncreaseSusMeter(rate);
    }
    private void IncreaseSuspicionLooking(float multiplier, float rate)
    {
        if (susAffectedByDistance && enemyScript.playerInDetectionArea)
        {
            // Distance
            float distanceToPlayer = Vector3.Distance(transform.position, enemyScript.player.position);
            // Clamp
            float clampedDistance = Mathf.Clamp(distanceToPlayer, 0, GetCurrentDetectionDistance());
            // Calculate the distance-based multiplier (1.0 at max distance, 2.0 at the AI's position)
            float distanceMultiplier = 1 + (maxSusDistancePercent / 100f - 1) * (1 - clampedDistance / GetCurrentDetectionDistance());

            multiplier += (distanceMultiplier - 1);
        }
        else if (susAffectedByDistance && corpseInDetectionArea)
        {
            // Distance
            float distanceToPlayer = Vector3.Distance(transform.position, lastKnownPosition);
            // Clamp
            float clampedDistance = Mathf.Clamp(distanceToPlayer, 0, GetCurrentDetectionDistance());
            // Calculate the distance-based multiplier (1.0 at max distance, 2.0 at the AI's position)
            float distanceMultiplier = 1 + (maxSusDistancePercent / 100f - 1) * (1 - clampedDistance / GetCurrentDetectionDistance());

            multiplier += (distanceMultiplier - 1);
        }

        rate *= multiplier;
        IncreaseSusMeter(rate);

        //Debug.Log($"Increased Sus By {rate * Time.deltaTime} | Mutliplier: {multiplier}");
    }
    private void IncreaseSusMeter(float rate)
    {
        float realRate = rate * Time.deltaTime * (1 + detectionInterval);
        if (GameStats.Instance) GameStats.Instance.IncreaseSuspicion(realRate);
        susMeter = Mathf.Min(susMeter + realRate, alertMeter);
    }
    private void DecreaseSuspicion()
    {
        if (!isSusPause)
        {
            outOfSightTimer += Time.deltaTime;
            noiseHeardTimer += Time.deltaTime;

            if (outOfSightTimer >= CurrentDecayDelay && noiseHeardTimer >= noiseHeardDecay && susMeter > 0)
            {
                float decrementAmount = 0f;

                switch (currentState)
                {
                    case DetectionState.Investigating:
                    case DetectionState.Aware:
                        decrementAmount = CurrentSusDecrement * Time.deltaTime;
                        susMeter = Mathf.Max(susMeter - decrementAmount, 0);

                        break;
                    case DetectionState.Normal:
                    default:
                        decrementAmount = susDec * Time.deltaTime * Mathf.Clamp01(outOfSightTimer / maxSusDecayValue) * (1 + detectionInterval);
                        // If suspicion is below investigateMeter (and not in alert), use normal suspicion decay
                        susMeter = Mathf.Max(susMeter - decrementAmount, 0);

                        break;
                }
                //Debug.Log($"Suspicion decreased: {susMeter} | Rate: {decrementAmount}");
            }
        }
    }

    private void GoTo(Vector3 target)
    {
        if (enemyScript.canEnemyPerform() == false) return;

        enemyScript.agent.isStopped = false; //unpause activity after aware pause
        enemyScript.agent.SetDestination(target);

        //MOVEMENT ANIMATION
        enemyScript.SetMovementAnimation();

        // If already at the target position, trigger LookAround behavior
        if (enemyScript.HasReachedDestination() && !enemyScript.HasLineOfSight(enemyScript.player))
        {
            inveIdleTimer += Time.deltaTime;

            if (inveIdleTimer >= CurrentLookInterval && !isLookingAround)
            {
                LookAround();
            }
        }
        else // Not at target, go to target
        {
            // Move towards the target
            inveIdleTimer = 0f; // Reset the timer if moving
            enemyScript.agent.SetDestination(target);
        }
    }
    private void LookAround()
    {
        if (enemyScript.canEnemyPerform() == false) return;

        isLookingAround = true;

        // Calculate a random angle to look around
        float randomAngle = Random.Range(-CurrentLookAngle, CurrentLookAngle);
        randomLookDirection = Quaternion.Euler(0, randomAngle, 0) * transform.rotation;

        Debug.Log($"Looking around. Direction: {randomLookDirection}");
    }

    private void RotateTowardsLookDirection()
    {
        if (enemyScript.canEnemyPerform() == false) return;

        if (isLookingAround)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, randomLookDirection, enemyScript.rotationSpeed * Time.deltaTime);

            // Check if the AI has finished rotating
            if (Quaternion.Angle(transform.rotation, randomLookDirection) < 1f)
            {
                Debug.Log("Finished looking around.");
                isLookingAround = false;
                inveIdleTimer = 0f; // Reset idle timer after looking around
            }
        }
    }
    public void RotateTowardsPlayer()
    {
        // Allow rotation if the enemy is attacking, even if canEnemyPerform() returns false
        if (enemyScript.isDead || !enemyScript.isAttacking && !enemyScript.canEnemyPerform()) return;

        Vector3 directionToPlayer = (enemyScript.player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, enemyScript.rotationSpeed * Time.deltaTime); // Smooth rotation
    }

    private void RotateTowardsLastKnown()
    {
        if (enemyScript.canEnemyPerform() == false) return;

        Vector3 direction = (lastKnownPosition - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, enemyScript.rotationSpeed * Time.deltaTime); // Smooth rotation
    }

    public void InstantAggroMelee()
    {
        lastKnownPosition = enemyScript.player.position;
        susMeter = alertMeter;

    }
    public void InstantAggroRange()
    {
        lastKnownPosition = transform.position;
        susMeter = alertMeter;

    }

    public void InstantInvestigate(Transform targetToInvestigtate)
    {
        if (susMeter <= inveMeter && !enemyScript.playerInDetectionArea)
        {
            susMeter = inveMeter;
            lastKnownPosition = targetToInvestigtate.position;
        }
    }

    //private void OnDrawGizmosSelected() // DISABLE-ABLE disable disablable
    //{
    //    //Draw green detection zone
    //    Gizmos.color = new Color(0, 1, 0, 0.2f);
    //    DrawDetectionCone(GetCurrentGreenAngle());

    //    //Draw yellow detection zone
    //    Gizmos.color = new Color(1, 1, 0, 0.2f);
    //    DrawDetectionCone(GetCurrentYellowAngle());

    //    //Draw the last known position during investigation
    //    if (currentState == DetectionState.Investigating)
    //    {
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
    //    }

    //    //Set Gizmos color based on alert state
    //    Gizmos.color = isAlert ? Color.red : Color.yellow;
    //    //Draw the detection sphere
    //    Gizmos.DrawWireSphere(transform.position, GetCurrentAlertSpreadDistance());
    //}
    //private void DrawDetectionCone(float angle)
    //{
    //    Vector3 forward = transform.forward * GetCurrentDetectionDistance();
    //    Vector3 rightBoundary = Quaternion.Euler(0, angle / 2, 0) * forward;
    //    Vector3 leftBoundary = Quaternion.Euler(0, -angle / 2, 0) * forward;

    //    Gizmos.DrawRay(transform.position, rightBoundary);
    //    Gizmos.DrawRay(transform.position, leftBoundary);
    //    Gizmos.DrawWireSphere(transform.position, GetCurrentDetectionDistance());

    //    Vector3 coneBaseCenter = transform.position + forward;
    //    Gizmos.DrawWireSphere(coneBaseCenter, 0.2f); // Optional: Mark the furthest point
    //}
}
