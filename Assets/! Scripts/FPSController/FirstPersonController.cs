using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        
        [SerializeField] public bool m_IsWalking;
        [SerializeField] private bool m_IsCrouched;
        public bool m_IsGrappling;
        [SerializeField] private float m_CrouchHeight;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_CrouchSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField][Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        public Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        public bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        public bool m_Jumping;
        public bool m_IsVaulting;
        private AudioSource m_AudioSource;
        private float speed;
        private float defaultHeight;
        private Vector3 defaultCenter;
        private Vector3 m_CrouchedCameraPosition;

        [Header("Reference (Auto)")]
        public Player playerScript;
        public Volume volume;
        private Vignette vignette;

        private bool isFlashing = false;
        private float flashDuration = 1f; // Flash duration (in seconds)
        private float flashStartTime;

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;
            m_IsVaulting = false;
            m_AudioSource = GetComponent<AudioSource>();
            m_MouseLook.Init(transform, m_Camera.transform);
            defaultHeight = m_CharacterController.height;
            defaultCenter = m_CharacterController.center;
            m_CrouchedCameraPosition.y = m_CrouchHeight;

            //Lex's code
            playerScript = GetComponent<Player>();
            if (playerScript == null) Debug.LogWarning("No playerScript reference!");

            if (volume == null) volume = GameObject.FindGameObjectWithTag("Global Volume").GetComponent<Volume>();
            if (volume == null) Debug.LogWarning("No Global Volume Found!");
            if (volume != null && volume.profile != null)
            {
                // Try to get the Vignette effect
                if (volume.profile.TryGet<Vignette>(out vignette))
                {
                    vignette.active = false; // Ensure it's initially off
                }
                else
                {
                    Debug.LogWarning("Vignette effect not found in the Volume Profile.");
                }
            }
            else
            {
                Debug.LogError("Volume or Volume Profile is missing.");
            }
        }


        // Update is called once per frame
        private void Update()
        {
            GetInput(out speed);

            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && m_CharacterController.isGrounded && !m_IsGrappling)
            {
                m_Jump = Input.GetKeyDown(KeybindManager.Instance.keybinds["Jump"]);
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded && !m_IsVaulting && !m_IsGrappling)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }

            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

            //Lex's Code
            // Handle the red vignette flashing timer
            if (isFlashing)
            {
                vignette.color.value = Color.red;

                float elapsed = Time.time - flashStartTime;

                if (elapsed < flashDuration)
                {
                    // Fade in (alpha from 0 to 1) over the first half of the duration
                    float alpha = Mathf.Lerp(0f, 1f, elapsed / flashDuration);
                    vignette.smoothness.value = alpha;
                }
                else if (elapsed < flashDuration * 1.5f)
                {
                    // Hold the vignette fully visible for a brief moment
                    vignette.smoothness.value = 1f;
                }
                else if (elapsed < flashDuration * 2f)
                {
                    // Fade out (alpha from 1 to 0) over the last part of the duration
                    float alpha = Mathf.Lerp(1f, 0f, (elapsed - flashDuration * 1.5f) / flashDuration);
                    vignette.smoothness.value = alpha;
                }
                else
                {
                    // Finish the flash effect
                    vignette.smoothness.value = 0f;
                    vignette.color.value = Color.black;  // Reset to default color (black)
                    isFlashing = false;

                    // Re-apply crouching vignette if the player is still crouching
                    if (m_IsCrouched)
                    {
                        ToggleCrouchingVignette(true);  // Re-enable the crouching vignette
                    }
                    else
                    {
                        vignette.active = false;  // Disable vignette when not crouching
                    }
                }
            }
        }


        public void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;


            }
           if (!m_IsGrappling && m_CharacterController.enabled!=false)
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            m_MouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        public void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;

            //Lex's Code
            playerScript.FPSControllerNoiseHandler(m_IsWalking, m_IsCrouched); // m_IsWalking false = Running, NO SOUND IF CROUCHED
        }

        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = (m_OriginalCameraPosition.y * (m_IsCrouched ? 0.0f : 1.0f)) + (m_CrouchedCameraPosition.y * (m_IsCrouched ? 1.0f : 0.0f)) - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;

                //branchless programming applied here
                //if not crouched, use original pos else use the crouched one
                newCameraPosition.y = (m_OriginalCameraPosition.y * (m_IsCrouched ? 0.0f : 1.0f)) + (m_CrouchedCameraPosition.y * (m_IsCrouched ? 1.0f : 0.0f)) - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeybindManager.Instance.keybinds["Sprint"]);

            //check if crouch key was pressed to change isCrouched state
            if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Crouch"]))
            {
                m_IsCrouched = !m_IsCrouched;
                ToggleCrouch();
            }
#endif
            // set the desired speed to be walking or running
            if (!m_IsCrouched)
            {
                if (m_IsWalking)
                    speed = m_WalkSpeed;
                else
                    speed = m_RunSpeed;
            }
            else
            {
                speed = m_CrouchSpeed;
            }


            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }

        private void ToggleCrouch()
        {

            if (m_IsCrouched)
            {
                // Code to enable crouch
                m_Camera.transform.localPosition = m_CrouchedCameraPosition;
                ShrinkCollider(defaultHeight / 1.5f);
                ToggleCrouchingVignette(true);
                Debug.Log("Crouched");
            }
            else
            {
                // Code to disable crouch
                m_Camera.transform.localPosition = m_OriginalCameraPosition;
                EnlargeCollider(defaultHeight);
                ToggleCrouchingVignette(false);
                Debug.Log("Uncrouched");
            }
            Debug.Log(m_Camera.transform.localPosition);
        }

        private void ToggleCrouchingVignette(bool isOn)
        {
            // Check if the Volume has a profile
            if (volume != null && volume.profile != null)
            {
                // Check if the Vignette effect exists in the profile
                if (volume.profile.TryGet<Vignette>(out vignette))
                {
                    // Enable the Vignette effect
                    vignette.active = isOn;

                }
                else
                {
                    Debug.LogWarning("Vignette effect not found in the Volume Profile.");
                }
            }
            else
            {
                Debug.LogError("Volume or Volume Profile is missing.");
            }
        }

        // Call this method when the player is hit
        public void OnPlayerHit()
        {
            // Start the flash effect if it's not already active
            if (!isFlashing)
            {
                isFlashing = true;
                flashStartTime = Time.time;
                vignette.active = true; // Ensure vignette is active during the flash

                // Set the vignette to red
                if (vignette != null)
                {
                    vignette.color.value = Color.red;  // Set color to red
                    vignette.intensity.value = 0.5f;  // Adjust the intensity as needed
                }

                // Begin flashing effect
                vignette.smoothness.value = 0f;  // Start with no vignette smoothness (fully transparent)
            }
        }

        private void ShrinkCollider(float targetHeight)
        {
            if (m_CharacterController == null) return;

            float heightDifference = m_CharacterController.height - targetHeight;
            Vector3 newCenter = m_CharacterController.center;
            newCenter.y -= heightDifference / 2;

            m_CharacterController.height = targetHeight;
            m_CharacterController.center = newCenter;

            // Temporarily reset the parent position to avoid any movement during the resize
            Vector3 originalParentPosition = transform.position;

            // Move the GameObject downward
            transform.position -= new Vector3(0, heightDifference / 2, 0);

            // Optionally re-align parent position
            transform.position = originalParentPosition;
        }

        private void EnlargeCollider(float targetHeight)
        {
            if (m_CharacterController == null) return;

            float heightDifference = targetHeight - m_CharacterController.height;
            Vector3 newCenter = m_CharacterController.center;
            newCenter.y += heightDifference / 2;

            m_CharacterController.height = targetHeight;
            m_CharacterController.center = newCenter;

            // Temporarily reset the parent position to avoid any movement during the resize
            Vector3 originalParentPosition = transform.position;

            // Move the GameObject upward
            transform.position += new Vector3(0, heightDifference / 2, 0);

            // Optionally re-align parent position
            transform.position = originalParentPosition;
        }
    

        private void RotateView()
        {
            m_MouseLook.LookRotation(transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
