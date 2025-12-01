using UnityEngine;

public class CanDespawn : MonoBehaviour
{
    public float seconds = 300f;

    void Start()
    {
        Destroy(gameObject, seconds);
    }
}
