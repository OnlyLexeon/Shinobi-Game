using UnityEngine;

public enum ItemType
{
    Normal,
    GrapplingHook,
}


public abstract class Item : ScriptableObject
{
    public ItemType itemType = ItemType.Normal;
    public string itemName;
    public int maxStackCount = 0;
    public bool removeOnUse = true;
    public bool isDoubleHanded = false; //never ysed
    
    [Header("If can Charge:")]
    public bool canHoldCharge = false;
    public float minCharge = 1f;
    public float maxCharge = 20f;
    public float chargePerSecond = 20f;
    public float currentChargeForce = 0f;

    [Header("UI")]
    public GameObject itemHoldModel;
    public Sprite itemImage;
    public AudioClip useSound;

    public abstract void Use(GameObject player);
    public virtual void Charge(GameObject player, float chargeTime)
    {

    }
}
