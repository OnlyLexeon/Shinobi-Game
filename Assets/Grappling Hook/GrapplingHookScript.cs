using Obi;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class GrapplingHookScript : MonoBehaviour
{
    [Header("Game Objects")]
    public FirstPersonController fpsController;
    public CharacterController characterController;
    [Header("Values")]
    public float hookReachTime = 2f; // Time for the hook to reach the target
    public float hookRange = 10f;
    public float swingJumpForce = 300f;
    public float climbSpeed = 2f;
    public float maxSwingSpeed = 10f;
    public float grapplingHookCooldown = .5f;
    [Tooltip("Force applied when moving swing direction")]
    public float swingControlForce = 1f;
    [Tooltip("Used to detect collisions to turn back on Character Controller")]
    public CapsuleCollider grapplingCollider;
    [Header("Aim Prediction")]
    public float predictionSphereCastRadius;

    [Header("Sounds")]
    public AudioClip shootSound;
    public AudioClip hookHit;
    public AudioClip grapplingAscentDecent;
    public bool grapplingClimbTriggered = false;

    [Header("Tip")]
    public CursorTipsManager.Tip tip;

    [Header("References (Auto)")]
    public ObiRope rope; // Reference to the Obi Rope for rendering
    public GameObject grapplingHook; // Reference to the grappling hook
    public Player playerScript;

    private string grappleTag = "Grappable";
    private Camera cam;
    private Vector3 startPosition;
    private ConfigurableJoint joint;
    private float currentRopeLength;
    private bool inPhysicsMovementState = false;
    private RaycastHit predictionHit;
    private Vector3 predictionPoint;
    private bool isHooking;
    private float timer;

    void Start()
    {
        cam = Camera.main;
        isHooking = false;
        timer = 0f;

        if (rope == null) rope = GameObject.FindGameObjectWithTag("ObiSolver").GetComponentInChildren<ObiRope>();
        if (rope == null) Debug.LogWarning("Failed to find Rope!!");

        if (grapplingHook == null) grapplingHook = GameObject.FindGameObjectWithTag("GrapplingHook");
        if (grapplingHook == null) Debug.LogWarning("Failed to find grapplingHook!!");

        if (playerScript == null) playerScript = GetComponent<Player>();
        if (playerScript == null) Debug.LogWarning("No playerScript found!");

        if (rope && grapplingHook) MoveObiSolverAndTheUhhhWhatsitCalledAgain();
    }

    void MoveObiSolverAndTheUhhhWhatsitCalledAgain()
    {
        //Step 1 : copy the position of the player to ObiSolver's position
        Transform ropeTransform = rope.transform;
        ropeTransform.position = playerScript.transform.position;

        //Step 2 : set ObiSolver to 0,-90,0 rotation
        ropeTransform.rotation = Quaternion.Euler(0, -90, 0);

        //Step 3 : copy the position of the player
        //Step 4 : paste that into the grappling hook position
        Transform grapplingHookTransform = grapplingHook.transform;
        grapplingHookTransform.position = playerScript.transform.position;

        // Step 5: Rotate the grappling hook to -75° of the player's Y-axis rotation
        float newYRotation = playerScript.transform.rotation.eulerAngles.y - 75;
        grapplingHookTransform.rotation = Quaternion.Euler(
            grapplingHookTransform.rotation.eulerAngles.x, // Keep the current X rotation
            newYRotation,                                 // Adjust the Y rotation
            grapplingHookTransform.rotation.eulerAngles.z  // Keep the current Z rotation
        );

        //Step 5 : subtract 2 from the grappling hook z position
        Vector3 grapplingHookPosition = grapplingHookTransform.position;
        grapplingHookPosition.z -= 2;
        grapplingHookTransform.position = grapplingHookPosition;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > grapplingHookCooldown)
            timer = grapplingHookCooldown;  //  to prevent incrementing over max range of float

        if (playerScript.isGrapplingHookEquipped)
        {
            CheckForSwingPoints();  // todo : do when grappling hook is equipped or put it to to the start of ThrowGrapplingHook()
            // Cursor Tip (can grapple or not)
            if (predictionHit.point != Vector3.zero && predictionHit.collider.gameObject.tag.Equals(grappleTag) && !isHooking) // taken from ThrowGrapplingHook()
            {
                tip.key = KeybindManager.Instance.keybinds["Use Utility"];
                tip.tipMessage = "Grapple";
                CursorTipsManager.Instance.MakeTip(tip);
            }
            else
            {
                tip.key = KeybindManager.Instance.keybinds["Use Utility"];
                tip.tipMessage = "Grapple";
                CursorTipsManager.Instance.RemoveTip(tip);
            }

            if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Use Utility"]) && timer >= grapplingHookCooldown) ThrowGrapplingHook();
            if (Input.GetKeyUp(KeybindManager.Instance.keybinds["Use Utility"])) StopSwing();

            if (joint != null)
            {
                HandleClimbing();
                ApplySwingControl();
            }
        }

        var rb = fpsController.GetComponent<Rigidbody>();
        //if (rb.linearVelocity.y == 0 && !fpsController.m_IsGrappling)
        //Debug.DrawRay(fpsController.transform.position, Vector3.down, Color.cyan, characterController.height / 2 + 0.1f);
        if (Physics.Raycast(fpsController.transform.position, Vector3.down, characterController.height / 2 + 0.1f) && !fpsController.m_IsGrappling)
        {
            inPhysicsMovementState = false;

            ReactivateCharacterController();
        }
    }

    private void ReactivateCharacterController()
    {
        // Re-enable CharacterController and disable Rigidbody physics
        characterController.enabled = true;
        fpsController.GetComponent<Rigidbody>().isKinematic = true; // Stop Rigidbody physics
        ToggleGrapplingCollider(false);
    }

    public void StopSwing()
    {
        if (fpsController.m_IsGrappling)
        {
            RenderGrapplingHook(false);
            Destroy(joint);
            joint = null;

            if (!isHooking) 
            {
                var rb = fpsController.GetComponent<Rigidbody>();
                rb.AddForce(cam.transform.forward.normalized * swingJumpForce);
            }


            fpsController.m_IsGrappling = false;
            timer = 0f;
        }
    }

    private void StartSwing(Vector3 swingPoint)
    {
        isHooking = false;

        if (joint != null) // in case of rogue joints
        {
            Destroy(joint);
            joint = null;
        }

        fpsController.m_IsGrappling = true;
        var rb = fpsController.GetComponent<Rigidbody>();

        // Disable CharacterController and enable Rigidbody physics
        rb.isKinematic = false;
        rb.linearVelocity = characterController.velocity;
        characterController.enabled = false;
        inPhysicsMovementState = true;

        joint = fpsController.gameObject.AddComponent<ConfigurableJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        // Configure the joint
        float distanceFromPoint = Vector3.Distance(fpsController.transform.position, swingPoint);
        currentRopeLength = distanceFromPoint;

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        SoftJointLimit limit = new SoftJointLimit { limit = distanceFromPoint };
        joint.linearLimit = limit;

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        ToggleGrapplingCollider(true);
    }

    private void ThrowGrapplingHook()
    {
        if (predictionHit.point != Vector3.zero && predictionHit.collider.gameObject.tag.Equals(grappleTag) && !isHooking)
        {
            startPosition = grapplingHook.transform.position = fpsController.transform.position;

            //SOUND
            playerScript.audioSource.PlayOneShot(shootSound);

            StartCoroutine(LerpGrapplingHookTravel(predictionHit.point, hookReachTime));
            RenderGrapplingHook(true);
        }
    }

    private void CheckForSwingPoints() 
    {
        if (joint != null) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.transform.position, predictionSphereCastRadius, cam.transform.forward, out sphereCastHit, hookRange);
        
        RaycastHit raycastHit;
        Physics.Raycast(cam.transform.position, cam.transform.forward, out raycastHit, hookRange);

        Vector3 realHitPoint;

        //opt 1 - direct hit
        if (raycastHit.point != Vector3.zero && raycastHit.collider.gameObject.tag.Equals(grappleTag))
        {
            realHitPoint = raycastHit.point;
        }
        //opt 2 - indirect (predict) hit
        else if (sphereCastHit.point != Vector3.zero && sphereCastHit.collider.gameObject.tag.Equals(grappleTag))
        {
            realHitPoint = sphereCastHit.point;
        }
        //opt 3 - miss
        else
            realHitPoint = Vector3.zero;

        if (realHitPoint != Vector3.zero) 
            predictionPoint = realHitPoint;

        predictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
    }

    IEnumerator LerpGrapplingHookTravel(Vector3 targetPosition, float duration)
    {
        float timePassed = 0;
        isHooking = true;
        while (timePassed < duration)
        {
            float t = timePassed / duration;

            // Move the hook
            grapplingHook.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            timePassed += Time.deltaTime;
            yield return null;
        }
        // Ensure final position
        grapplingHook.transform.position = targetPosition;

        // Once hooked, start swinging
        //SOUND
        playerScript.audioSource.PlayOneShot(hookHit);
        StartSwing(targetPosition);
    }

    void RenderGrapplingHook(bool isRendered)
    {
        rope.GetComponent<ObiRopeExtrudedRenderer>().enabled = isRendered;
        grapplingHook.GetComponent<Renderer>().enabled = isRendered;
    }

    void ToggleGrapplingCollider(bool isEnable)
    {
        grapplingCollider.enabled = isEnable;

        grapplingCollider.height = characterController.height;
        grapplingCollider.center = characterController.center;
        grapplingCollider.radius = characterController.radius;
    }

    private void ApplySwingControl()
    {
        if (!fpsController.m_IsGrappling) return;

        // Get input for horizontal and vertical movement
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float verticalInput = Input.GetAxis("Vertical");     // W/S or Up/Down Arrow

        // Calculate movement direction relative to the camera
        Vector3 moveDirection = (cam.transform.right * horizontalInput + cam.transform.forward * verticalInput).normalized;

        // Apply force to the Rigidbody
        Rigidbody rb = fpsController.GetComponent<Rigidbody>();
        rb.AddForce(moveDirection * swingControlForce, ForceMode.Acceleration);
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSwingSpeed);
    }

    private void HandleClimbing()
    {
        if (Input.GetKey(KeybindManager.Instance.keybinds["Jump"]))
        {
            AdjustRopeLength(-climbSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeybindManager.Instance.keybinds["Crouch"]))
        {
            AdjustRopeLength(climbSpeed * Time.deltaTime);
        }
        else
        {
            //dont play sound
            if (grapplingClimbTriggered)
            {
                playerScript.audioSource.Stop();
                grapplingClimbTriggered = false;
            }
        }
    }

    private void AdjustRopeLength(float adjustment)
    {
        //SOUND
        if (!grapplingClimbTriggered) playerScript.audioSource.PlayOneShot(grapplingAscentDecent);
        grapplingClimbTriggered = true;
        
        currentRopeLength = Mathf.Clamp(currentRopeLength + adjustment, 1f, hookRange);

        SoftJointLimit limit = joint.linearLimit;
        limit.limit = currentRopeLength;
        joint.linearLimit = limit;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!fpsController.m_IsGrappling && !inPhysicsMovementState)
        {
            ReactivateCharacterController();
        }
    }
}
