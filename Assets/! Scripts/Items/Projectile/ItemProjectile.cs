using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectile", menuName = "Items/Projectile")]
public class ItemProjectile : Item
{
    [Header("Projectile Settings")]
    public GameObject shurikenPrefab;
    public float throwForce = 20f;

    public override void Use(GameObject player)
    {
        Debug.Log($"{itemName} used!");

        if (shurikenPrefab != null && player != null)
        {
            GameObject projectile = Instantiate(shurikenPrefab, player.GetComponent<Inventory>().instantitatePosition.position, Quaternion.identity);

            // Apply velocity
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(player.GetComponent<Inventory>().instantitatePosition.forward * throwForce, ForceMode.Impulse);
            }
        }
    }
}
