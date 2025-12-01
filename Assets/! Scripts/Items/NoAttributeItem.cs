using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/EmptyItem")]
public class NoAttributeItem : Item
{
    public override void Use(GameObject player)
    {
        Debug.Log($"{itemName} used!");
    }
}
