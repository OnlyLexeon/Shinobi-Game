using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.VFX;

public class NoiseMaker : MonoBehaviour
{
    [Header("NoiseMaker Settings")]
    public float waitBeforeStart = 3f;
    public float duration = 8f;
    public float areaOfEffect = 24f;
    public float enemySafeDistance = 4f;

    public AudioClip hissingSound;
    public AudioClip fireCrackersSound;
    public AudioSource audioSource;

    [Header("Debug Stats")]
    public float durationRemaining;

    [Header("References (Auto Assign)")]
    public LayerMask enemyLayer;

    // IF SMOKE BOMB HITS PLAYER, MAKE SURE TO EXCLUDE PLAYERMASK IN EXCLUDE LAYER OF BOMB (not smoke)

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) Debug.LogWarning("No AudioSource Found!");

        enemyLayer = LayerMask.GetMask("Enemy");
        if (enemyLayer == 0) Debug.LogWarning("Enemy Layer reference is missing!");

        StartCoroutine(StartTimer());
    }

    public IEnumerator StartTimer()
    {
        audioSource.loop = false; // Ensure looping is enabled
        audioSource.PlayOneShot(hissingSound);
        yield return new WaitForSeconds(waitBeforeStart);

        audioSource.Stop(); //stop hissing
        ActivateNoiseMaker();
    }

    // Call this function to activate the smoke bomb
    public void ActivateNoiseMaker()
    {
        audioSource.loop = true; // Ensure looping is enabled
        audioSource.clip = fireCrackersSound;
        audioSource.Play();

        // Start the smoke deployment
        StartCoroutine(StartMakingNoise());
    }

    // Coroutine to deploy and expand the smoke
    private IEnumerator StartMakingNoise()
    {
        durationRemaining = duration;

        while (durationRemaining > 0)
        {
            // Detect all colliders within the area of effect
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, areaOfEffect, enemyLayer);

            foreach (Collider collider in hitColliders)
            {
                // Check if the object has the EnemyScript component
                Enemy enemyScript = collider.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    // Call the InstantInvestigate method
                    if (enemyScript.detectionScript) enemyScript.detectionScript.InstantInvestigate(transform);
                }
            }

            // Decrease duration over time
            durationRemaining -= Time.deltaTime;

            yield return null;
        }

        audioSource.Stop();
    }

    // Draw Gizmos in the Editor to visualize the area of effect
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f); // Semi-transparent red
        Gizmos.DrawWireSphere(transform.position, areaOfEffect); // Draw the effect radius
    }

    // Draw Gizmos in Play Mode
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.7f); // Slightly orange with more opacity
        Gizmos.DrawSphere(transform.position, areaOfEffect);
    }


}
