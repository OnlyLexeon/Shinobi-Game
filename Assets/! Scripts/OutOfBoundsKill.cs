using UnityEngine;

public class OutOfBoundsKill : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Example kill logic: 
            // You can replace this with your game's specific logic.
            Player playerScript = other.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.PlayerDie();
            }
            else
            {
                Debug.LogWarning("Player does not have a PlayerHealth script!");
            }
        }
    }
}
