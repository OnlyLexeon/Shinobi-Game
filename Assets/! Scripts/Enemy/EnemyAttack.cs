using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAttack : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 3.5f;
    public float attackAngle = 80f;
    public float attackAnimTime = 0.5f; // Wind Up before the swing time
    public float minAttackInterval = 3f; // Random time between attacks
    public float maxAttackInterval = 8f; // Random time between attacks

    public float blockTime = 2f; // Block duration, how long the player's attack is deflected
    // NO BLOCK ANIM TIME - supposed to react to player's attack, so i dont think we should put a anim Time here

    public float minRepositionInterval = 10f; // How often should enemies attempt to reposition?
    public float maxRepositionInterval = 20f; // How often should enemies attempt to reposition?

    public float minStrafeInterval = 2f; // How often should enemies attempt to reposition?
    public float maxStrafeInterval = 5f; // How often should enemies attempt to reposition?

    [Header("Smart Position Settings")]
    public bool smartReposition = true;
    public float enemySpacing = 3f;
    [Space(5f)]
    public bool canStrafe = true;
    public float strafeSpeed = 5f;

    [Header("Debug Reference")]
    public bool attackAvailable = false; // True: attack cooldown is complete
    public float attackWaitTime = 0f;
    public float attackWaitTimer = 0f;

    public bool canRepositionNow = false; // True: reposition cooldown is complete
    public float repositionTime = 0f;
    public float repositionTimer = 0f;

    public bool canStrafeNow = false; // True: strafe cooldown is complete
    public float strafeTime = 0f;
    public float strafeTimer = 0f;

    [Header("References (Auto)")]
    public Enemy enemyScript;
    public Vector3 attackPosition;
    public float attackGoToTimer = 0f;
    public bool playerIsKilled = false;
    
    private Coroutine attackCoroutine;

    private void Start()
    {
        enemyScript = GetComponent<Enemy>();
        if (enemyScript == null) Debug.LogWarning("No enemyScript detected!");

        attackWaitTime = GetAttackWait();
        repositionTime = GetRepositionWait();

    }

    private void Update()
    {
        if (enemyScript.combatMode && enemyScript.canEnemyPerform() && !playerIsKilled) // not dead, not stunned, not choking, not attacking, not blocking
        {
            //ATTACK TIMER MANAGER
            if (!attackAvailable)
            {
                if (attackWaitTimer < attackWaitTime) attackWaitTimer += Time.deltaTime;
                else attackAvailable = true;
            }

            //REPOSITION TIMER MANAGER
            if (smartReposition && !canRepositionNow)
            {
                if (repositionTimer < repositionTime) repositionTimer += Time.deltaTime;
                else canRepositionNow = true;
            }

            //STRAFE TIMER MANAGER
            if (canStrafe && !canStrafeNow)
            {
                if (strafeTimer < strafeTime) strafeTimer += Time.deltaTime;
                else canStrafeNow = true;
            }

            if (attackAvailable && !enemyScript.player.GetComponent<Player>().isAssassinating)
            {
                //ATTACK
                enemyScript.isAttacking = true;
                attackWaitTimer = 0f;
                attackGoToTimer = 0f;

                CalculateAttackPosition();
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(PerformAttack());
            }
            else if (canRepositionNow)
            {
                //POSITIONING AMONGST OTHER ENEMIES
                // If can't attack, try to move positions
                Reposition();

                canRepositionNow = false;
                repositionTimer = 0f;
                repositionTime = GetRepositionWait();
            }
            else if (canStrafeNow)
            {
                //STRAFE L/R
                Strafe();

                canStrafeNow = false;
                strafeTimer = 0f;
                strafeTime = GetStrafeWait();
            }
        }
    }

    public void CalculateAttackPosition()
    {
        // Feature: moves a little forward to attack
        float distanceFromPlayer = Vector3.Distance(enemyScript.player.transform.position, transform.position);
        Vector3 directionToPlayer = (enemyScript.player.transform.position - transform.position).normalized;
        Vector3 destination = transform.position + directionToPlayer * ((distanceFromPlayer + 1f) - attackRange); // + 1f for player radius and enemy radius

        attackPosition = destination;
    }

    private IEnumerator PerformAttack()
    {
        while (Vector3.Distance(attackPosition, transform.position) > attackRange)
        {
            if (!enemyScript.agent.isActiveAndEnabled)
                break;

            Vector3 validatedPosition = ValidateNavMeshPosition(attackPosition);
            if (validatedPosition == Vector3.zero)
            {
                Debug.LogWarning("Invalid target position on NavMesh!");
                break; // Exit the loop to prevent an infinite loop
            }

            enemyScript.agent.SetDestination(validatedPosition);

            //MOVEMENT ANIMATION
            enemyScript.SetMovementAnimation();

            // Safeguard: Timeout after 4 seconds
            attackGoToTimer += Time.deltaTime;
            if (attackGoToTimer >= 4f) break;

            // Yield control to avoid freezing Unity
            yield return null;

            if (!enemyScript.agent.pathPending && enemyScript.agent.remainingDistance <= attackRange)
                break;
        }

        //ANIMATE
        enemyScript.anim.SetTrigger("Attack");
        // Wait for the attack animation time
        yield return new WaitForSeconds(attackAnimTime);

        Attack();
    }

    private void Attack()
    {
        // Check for players within range and angle
        Collider[] hitPlayers = Physics.OverlapSphere(transform.position, attackRange, enemyScript.playerLayer);
        foreach (Collider player in hitPlayers)
        {
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer <= attackAngle / 2)
            {
                Player playerScript = player.GetComponent<Player>();
                playerScript.PlayerHit();
                Debug.Log($"Hit player: {player.name}");

                // dance partyyyy!!
                if (playerScript.isDead)
                {
                    playerIsKilled = true;
                    enemyScript.anim.SetBool("Twerk", true);
                }
            };
        }

        enemyScript.isAttacking = false;
        attackAvailable = false;
        attackWaitTime = GetAttackWait(); // Get Random Wait Time
    }
    public void onEnemyHit()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
            enemyScript.isAttacking = false;
            attackAvailable = false;
            attackWaitTimer = 0f;
            attackGoToTimer = 0f;

        }
        Debug.Log("Attack interrupted because the enemy was hit!");
    }

    private Vector3 ValidateNavMeshPosition(Vector3 targetPosition)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 1.0f, NavMesh.AllAreas))
        {
            return hit.position; // Return the closest valid NavMesh position
        }
        return Vector3.zero; // Return invalid position if none found
    }


    public float GetAttackWait()
    {
        return Random.Range(minAttackInterval, maxAttackInterval);
    }
    public float GetRepositionWait()
    {
        return Random.Range(minRepositionInterval, maxRepositionInterval);
    }
    public float GetStrafeWait()
    {
        return Random.Range(minStrafeInterval, maxStrafeInterval);
    }

    public void TryToBlock()
    {
        if (enemyScript.canBlock && enemyScript.canEnemyPerform())
        {
            StartCoroutine(PerformBlock());
        }
    }

    private IEnumerator PerformBlock()
    {
        enemyScript.isBlocking = true;

        //BLOCKING ANIMATION
        enemyScript.anim.SetBool("isBlocking", true);

        // Block for the specified duration
        Debug.Log("Enemy is blocking");

        yield return new WaitForSeconds(blockTime);

        if (enemyScript.isBlocking) StopBlocking();
    }

    public void StopBlocking()
    {
        enemyScript.isBlocking = false;
        enemyScript.anim.SetBool("isBlocking", false);

        Debug.Log("Enemy stopped blocking");
    }

    // COMBAT REPOSITIONING ====================================================================

    public void Reposition()
    {
        List<Vector3> enemyPositions = GetNearbyEnemyPositions();
        Vector3 freePosition = Vector3.zero;
        int maxAttempts = 5;
        int attempts = 0;

        // Try multiple times to find a free position
        while (freePosition == Vector3.zero && attempts < maxAttempts)
        {
            freePosition = FindFreePosition(enemyPositions);
            if (freePosition != Vector3.zero && enemyScript.HasClearLineOfSightForRepositionOrStrafe(freePosition))
            {
                Debug.Log("Repositioning!");

                if (enemyScript.agent.isActiveAndEnabled) enemyScript.agent.SetDestination(freePosition);
                //MOVEMENT ANIMATION
                enemyScript.SetMovementAnimation();

                return;
            }
            attempts++;
        }

        Debug.Log("Failed to Reposition!");
    }

    private List<Vector3> GetNearbyEnemyPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        // Use OverlapSphere to find nearby enemies
        Collider[] colliders = Physics.OverlapSphere(enemyScript.player.position, enemyScript.combatModeProximity, enemyScript.enemyLayer);
        foreach (var collider in colliders)
        {
            // Exclude this enemy
            if (collider.gameObject != gameObject)
            {
                positions.Add(collider.transform.position);
            }
        }

        return positions;
    }

    private Vector3 FindFreePosition(List<Vector3> enemyPositions)
    {
        // Try multiple positions around the player
        int numSamples = 36; // Divide the circle into 36 segments
        for (int i = 0; i < numSamples; i++)
        {
            float angle = (360f / numSamples) * i;
            Vector3 candidatePosition = CalculatePositionOnCircle(enemyScript.player.position, enemyScript.combatModeProximity, angle);

            // Check if this position is far enough from other enemies
            if (IsPositionFree(candidatePosition, enemyPositions))
            {
                return candidatePosition;
            }
        }

        // No valid position found
        return Vector3.zero;
    }

    private Vector3 CalculatePositionOnCircle(Vector3 center, float radius, float angle)
    {
        // Convert the angle from degrees to radians
        float radians = angle * Mathf.Deg2Rad;

        // Calculate the position on the circle
        float x = center.x + radius * Mathf.Cos(radians);
        float z = center.z + radius * Mathf.Sin(radians);
        return new Vector3(x, center.y, z); // Assume y-coordinate remains the same
    }

    private bool IsPositionFree(Vector3 position, List<Vector3> enemyPositions)
    {
        foreach (var enemyPos in enemyPositions)
        {
            if (Vector3.Distance(position, enemyPos) < enemySpacing)
            {
                return false;
            }
        }
        return true;
    }

    // STRAFING
    public void Strafe()
    {
        Debug.Log("Attempting to Strafe!");

        // Calculate the direction perpendicular to the player
        Vector3 directionToPlayer = (enemyScript.player.position - transform.position).normalized;
        Vector3 strafeDirection = Vector3.Cross(directionToPlayer, Vector3.up).normalized;

        // Decide whether to strafe left or right
        if (!enemyScript.strafingRight) strafeDirection = -strafeDirection;

        // Calculate the target position
        Vector3 targetPosition = transform.position + strafeDirection * strafeSpeed;

        // Validate strafing target
        Vector3 validatedPosition = ValidateNavMeshPosition(targetPosition);

        if (validatedPosition != Vector3.zero && enemyScript.HasClearLineOfSightForRepositionOrStrafe(validatedPosition))
        {
            if (enemyScript.agent.isActiveAndEnabled) enemyScript.agent.SetDestination(validatedPosition);
            enemyScript.isStrafing = true;
            enemyScript.SetMovementAnimation();
            ToggleStrafeDirection();
        }
        else
        {
            Debug.LogWarning("Strafe path blocked or invalid!");
        }
    }

    public void ToggleStrafeDirection()
    {
        // Toggle strafing direction
        enemyScript.strafingRight = !enemyScript.strafingRight;
    }

    //private void OnDrawGizmosSelected()
    //{
    //    // Draw the attack range sphere in the editor for visualization
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, attackRange);

    //    // Draw the attack angle arc
    //    Gizmos.color = Color.yellow;
    //    Vector3 forward = transform.forward * attackRange;
    //    Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle / 2, 0) * forward;
    //    Vector3 rightBoundary = Quaternion.Euler(0, attackAngle / 2, 0) * forward;
    //    Gizmos.DrawRay(transform.position, leftBoundary);
    //    Gizmos.DrawRay(transform.position, rightBoundary);
    //}
}
