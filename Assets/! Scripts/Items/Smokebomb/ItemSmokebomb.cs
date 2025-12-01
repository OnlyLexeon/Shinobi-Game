using UnityEngine;

[CreateAssetMenu(fileName = "NewSmokeBomb", menuName = "Items/SmokeBomb")]
public class ItemSmokebomb : Item
{
    [Header("SmokeBomb Settings")]
    public GameObject smokeEffectPrefab;

    public override void Use(GameObject player)
    {
        Debug.Log($"{itemName} used!");

        if (smokeEffectPrefab != null && player != null)
        {
            GameObject smokeBomb = Instantiate(smokeEffectPrefab, player.GetComponent<Inventory>().instantitatePosition.position, Quaternion.identity);
            SmokeBomb smokeBombScript = smokeBomb.GetComponent<SmokeBomb>();
            if (smokeBombScript != null)
            {
                //Apply velocity
                Rigidbody rb = smokeBomb.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(player.GetComponent<Inventory>().instantitatePosition.forward * currentChargeForce, ForceMode.Impulse);
                }
            }

            // Reset chargeForce after use
            currentChargeForce = minCharge;
        }
    }

    public override void Charge(GameObject player, float chargeTime)
    {
        if (!canHoldCharge) return;

        // Calculate and clamp chargeForce
        currentChargeForce = Mathf.Clamp(chargeTime * chargePerSecond, minCharge, maxCharge);
        Debug.Log($"Charging {itemName}. Current force: {currentChargeForce}");
    }
}
