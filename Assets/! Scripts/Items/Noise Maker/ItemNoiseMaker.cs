using UnityEngine;

[CreateAssetMenu(fileName = "NewNoiseMaker", menuName = "Items/NoiseMaker")]
public class ItemNoiseMaker : Item
{
    [Header("NoiseMaker Settings")]
    public GameObject noiseMakerPrefab;

    public override void Use(GameObject player)
    {
        Debug.Log($"{itemName} used!");

        if (noiseMakerPrefab != null && player != null)
        {
            GameObject noiseMaker = Instantiate(noiseMakerPrefab, player.GetComponent<Inventory>().instantitatePosition.position, Quaternion.identity);
            NoiseMaker noiseMakerPrefabScript = noiseMakerPrefab.GetComponent<NoiseMaker>();
            if (noiseMakerPrefabScript != null)
            {
                //Apply velocity
                Rigidbody rb = noiseMaker.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(player.GetComponent<Inventory>().instantitatePosition.forward * currentChargeForce, ForceMode.Impulse);
                }
                else
                {
                    Debug.LogWarning("no RB!");
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
