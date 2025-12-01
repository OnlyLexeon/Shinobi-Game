using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

public class LookAtPlayer : MonoBehaviour
{
    public Transform player;
    public float rotateYOffset = 0f;
    public float updateInterval = 0.3f;

    private void Start()
    {
        StartCoroutine(LookAtPlayerRoutine());
    }

    private IEnumerator LookAtPlayerRoutine()
    {
        while (true)
        {
            if (player != null)
            {
                transform.LookAt(player);

                // Apply the Y-axis rotation offset
                Vector3 eulerRotation = transform.eulerAngles;
                eulerRotation.y += rotateYOffset; // Add the Y-axis offset
                transform.eulerAngles = eulerRotation;
            }
            else
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
                else
                {
                    Debug.LogWarning("No Player Tagged!");
                }
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }
}
