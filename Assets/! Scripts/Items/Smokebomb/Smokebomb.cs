using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.VFX;

public class SmokeBomb : MonoBehaviour
{
    public float duration = 12.5f; // How long the smoke lasts (particle system duration)
    public float areaOfEffect = 8f; // The maximum radius of the smoke (final size)
    public float startRadius = 3f; // The initial radius of the smoke effect
    public float timeForFullRadius = 4f; // Time it takes for the radius to reach the maximum size
    public SphereCollider smokeCollider; // The collider that blocks raycasts
    public VisualEffect effect;

    public AudioClip smokeHissLoop;
    public AudioSource audioSource;
    
    public bool isSmokeReleased = false;

    [Header("Debug Stats")]
    public float durationRemaining;

    [Header("References (Auto Assign)")]
    public LayerMask enemyLayer;

    
    private float currentRadiusTime;

    // IF SMOKE BOMB HITS PLAYER, MAKE SURE TO EXCLUDE PLAYERMASK IN EXCLUDE LAYER OF BOMB (not smoke)

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) Debug.LogWarning("No AudioSource Found!");

        // Ensure the SphereCollider is set up
        if (smokeCollider == null)
        {
            smokeCollider = gameObject.AddComponent<SphereCollider>();
            smokeCollider.isTrigger = true; // Make the collider a trigger so it blocks raycasts but doesn't physically interact
        }

        smokeCollider.radius = startRadius;

        enemyLayer = LayerMask.GetMask("Enemy");
        if (enemyLayer == 0) Debug.LogWarning("Enemy Layer reference is missing!");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isSmokeReleased && collision.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            ActivateSmokeBomb();
            isSmokeReleased = true;
        }
    }

    // Call this function to activate the smoke bomb
    public void ActivateSmokeBomb()
    {
        audioSource.loop = true; // Ensure looping is enabled
        audioSource.clip = smokeHissLoop;
        audioSource.Play();

        effect.enabled = true;

        //VFX Duration
        effect.SetFloat("smokeBombLifetime", duration);

        // Start the smoke deployment
        StartCoroutine(DeploySmoke());
    }

    // Coroutine to deploy and expand the smoke
    private IEnumerator DeploySmoke()
    {
        durationRemaining = duration;
        currentRadiusTime = 0f;

        // Gradually increase the radius of the SphereCollider over time
        while (durationRemaining > 0)
        {
            // Gradually expand the collider's radius over time based on timeForFullRadius
            float normalizedTime = Mathf.Clamp01(currentRadiusTime / timeForFullRadius);
            float smokeRadius = Mathf.Lerp(startRadius, areaOfEffect, Mathf.SmoothStep(0f, 1f, normalizedTime));
            smokeCollider.radius = smokeRadius;
            if (effect != null) effect.SetFloat("radius", smokeRadius);

            // Decrease duration over time
            durationRemaining -= Time.deltaTime;
            currentRadiusTime += Time.deltaTime; // Increase radius expansion time
            yield return null;
        }

        audioSource.Stop();
        enemyNotInSmoke();
        smokeCollider.enabled = false;
    }

    private void enemyNotInSmoke()
    {
        // Notify all enemies in the smoke to reset their inSmoke status
        Collider[] affectedEnemies = Physics.OverlapSphere(transform.position, areaOfEffect, enemyLayer); // Filter by enemy layer
        foreach (Collider enemy in affectedEnemies)
        {
            Enemy enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.OnSmokeDisabled();
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (smokeCollider != null && smokeCollider.enabled)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Semi-transparent gray for the smoke area
            Gizmos.DrawSphere(transform.position, smokeCollider.radius);
        }
    }
}
