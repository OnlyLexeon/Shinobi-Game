using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public Item item;
    public int count;
}

public class Inventory : MonoBehaviour
{
    [Header("Inventory Wheel Settings")]
    public float openWheelKeyPressTime = 0.25f; // Hold inventory button for this long to open
    public float inventoryPressStartTime = 0f;
    public float inventoryPressTime = 0f;

    [Header("Item Throw Transform")]
    public Transform instantitatePosition;

    [Header("Hand Positions")]
    public Transform utilHandPos;
    public Transform mainHandPos;

    [Header("Debug (Reference)")]
    public float chargeStartTime;
    public Item utilHand; // swaps with Inventory Items
    public GameObject mainHand; // For holding sword or swapping with back items
    public GameObject onBack; // Item currently stored on the back
    public int holdingAmount;
    public int stackAmount;

    [Header("References (Auto)")]
    public GameObject player; // This game object
    public Player playerScript; // This game object
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
   

    private void Start()
    {
        player = gameObject ?? GameObject.FindWithTag("Player");
        if (playerScript == null) playerScript = player.GetComponentInParent<Player>();
        else if (playerScript == null)  Debug.LogWarning("No playerScript!! Can't get script!");


        if (utilHandPos == null) utilHandPos = GameObject.FindWithTag("Util Hand").transform;
        if (mainHandPos == null) mainHandPos = GameObject.FindWithTag("Main Hand").transform;

        UpdateItemPositions();
    }

    private void Update()
    {
        HandleInput();
    }

    // Input
    void HandleInput()
    {
        // Check for holding right click
        //CHARGING ONLY
        if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Use Utility"]) && utilHand != null && utilHand.canHoldCharge)
        {
            chargeStartTime = Time.time; // Record when charging started
        }
        if (Input.GetKey(KeybindManager.Instance.keybinds["Use Utility"]) && utilHand != null && utilHand.canHoldCharge)
        {
            float chargeTime = Time.time - chargeStartTime;
            utilHand.Charge(player, chargeTime); // charging - change values within smoke bomb

            if (GameplayUIController.Instance) GameplayUIController.Instance.UpdateItemCharge(utilHand.currentChargeForce, utilHand.maxCharge);
        }

        //Lift Use Util key
        if (Input.GetKeyUp(KeybindManager.Instance.keybinds["Use Utility"]))
        {
            if (utilHand != null)
            {
                UseEquippedUtilItem();

                if (GameplayUIController.Instance) GameplayUIController.Instance.HideItemSlider();
            }
        }

        // Check if unequipUtilHand key is pressed
        if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Unequip Offhand"]))
        {
            UnequipUtilHand();
        }

        // INVENTORY HANDLING
        // Wheel or Swap??
        // Quick swap?
        if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Inventory"]))
        {
            inventoryPressStartTime = Time.time;
        }
        if (Input.GetKey(KeybindManager.Instance.keybinds["Inventory"]))
        {
            inventoryPressTime = Time.time - inventoryPressStartTime;

            // Open wheel
            if (inventoryPressTime >= openWheelKeyPressTime)
            {
                GameplayUIController.Instance.OpenInventoryWheel(inventorySlots);
            }
        }
        //Swap Back w Hand / Hand w Back
        if (Input.GetKeyUp(KeybindManager.Instance.keybinds["Inventory"]))
        {
            if (inventoryPressTime < openWheelKeyPressTime) SwapBackAndMain();
            
            // Close Wheel
            if (GameplayUIController.Instance.isOpen) GameplayUIController.Instance.CloseInventoryWheel();

            UpdateItemPositions(); // Update Model Hand
        }

        // Equip Items (keys: 1-4)
        if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Inventory Slot 1"]))
        {
            EquipFromInventorySlot(0);
        }
        if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Inventory Slot 2"]))
        {
            EquipFromInventorySlot(1);
        }
        if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Inventory Slot 3"]))
        {
            EquipFromInventorySlot(2);
        }
        if (Input.GetKeyDown(KeybindManager.Instance.keybinds["Inventory Slot 4"]))
        {
            EquipFromInventorySlot(3);
            
        }
    }
    void SwapBackAndMain()
    {
        GameObject temp;
        temp = mainHand;

        mainHand = onBack;
        onBack = temp;
    }

    // Using
    void UseEquippedUtilItem()
    {
        if (utilHand != null)
        {
            try
            {
                utilHand.Use(player); // Call the Use method on the equipped item
                
                //Sound
                playerScript.audioSource.PlayOneShot(utilHand.useSound);

                RemoveUtilHandObject();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error while using {utilHand.itemName}: {ex.Message}\nCould be player null reference.");
            }
        }
        else
        {
            Debug.Log("No item equipped!");
        }

        UpdateItemPositions(); // Update Model Hand
        UpdateItem(); // Update UI
    }
    void RemoveUtilHandObject()
    {
        if (utilHand != null && utilHand.removeOnUse)
        {
            holdingAmount--; // Decrease the amount being held by 1
            if (holdingAmount <= 0) // If no more items are being held
            {
                utilHand = null; // Clear the utilHand
                holdingAmount = 0; // Ensure holdingAmount does not go below 0

                UpdateItem();
            }
        }
    }

    // Equip
    void EquipFromInventorySlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        InventorySlot slot = inventorySlots[slotIndex];
        if (slot.item == null || slot.count <= 0) return;

        // Put the current utilHand item back into inventory
        if (utilHand != null)
        {
            bool addedToInventory = AddToInventory(utilHand, holdingAmount);
            if (!addedToInventory)
            {
                Debug.LogWarning("No space in inventory for the current utilHand item. Unable to equip new item.");
                return;
            }
        }

        // Equip the entire stack from the selected slot
        utilHand = slot.item;
        holdingAmount = slot.count; // Set the holdingAmount to the number of items in the stack
        stackAmount = utilHand.maxStackCount; // Set stackAmount to the maximum stack size of the item

        //Grappling Hook???
        if (slot.item.itemType == ItemType.GrapplingHook)
        {
            playerScript.isGrapplingHookEquipped = true;
        }
        else
        {
            playerScript.isGrapplingHookEquipped = false;
        }

        // Clear the slot
        slot.item = null;
        slot.count = 0;

        UpdateItemPositions(); // Update Model Hand
        UpdateItem(); // Update UI
        Debug.Log($"Equipped {holdingAmount}/{stackAmount} {utilHand.itemName}(s)");
    }
    bool AddToInventory(Item item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // Try adding to an existing stack
        foreach (var slot in inventorySlots)
        {
            if (slot.item == item && slot.count < item.maxStackCount)
            {
                int spaceAvailable = item.maxStackCount - slot.count;
                int toAdd = Mathf.Min(spaceAvailable, amount);

                slot.count += toAdd;
                amount -= toAdd;

                if (amount <= 0) return true; // All items added successfully
            }
        }

        // Add to empty slots
        foreach (var slot in inventorySlots)
        {
            if (slot.item == null)
            {
                int toAdd = Mathf.Min(item.maxStackCount, amount);

                slot.item = item;
                slot.count = toAdd;
                amount -= toAdd;

                if (amount <= 0) return true; // All items added successfully
            }
        }

        Debug.LogWarning("Not enough space in inventory to add all items!");
        return amount <= 0;
    }
    void UnequipUtilHand()
    {
        if (utilHand != null)
        {
            // Attempt to add the utilHand item back into the inventory
            bool addedToInventory = AddToInventory(utilHand, holdingAmount);

            if (addedToInventory)
            {
                utilHand = null; // Unequip the item if successfully added to the inventory
            }
            else
            {
                Debug.Log("No space in inventory to unequip the item!");
            }
        }
        else
        {
            Debug.Log("No item equipped in the util hand!");
        }

        UpdateItemPositions(); // Update Model Hand
        UpdateItem(); // Update UI
    }

    // MODELS
    void UpdateItemPositions()
    {
        //Clean hand
        foreach (Transform child in utilHandPos)
        {
            Destroy(child.gameObject); // Destroy old item model
        }
        //UTIL HAND
        if (utilHand != null && utilHand.itemHoldModel != null)
        {
            // Spawning
            GameObject itemModel = Instantiate(utilHand.itemHoldModel, utilHandPos.position, utilHandPos.rotation);
            itemModel.transform.SetParent(utilHandPos);
        }

        //Clean hand
        foreach (Transform child in mainHandPos)
        {
            Destroy(child.gameObject);
        }
        //MAIN HAND
        if (mainHand != null && mainHand != null)
        {
            // Spawning
            GameObject itemModel = Instantiate(mainHand, mainHandPos.position, mainHandPos.rotation);
            itemModel.transform.SetParent(mainHandPos);
        }
    }

    // UI
    void UpdateItem()
    {
        if (GameplayUIController.Instance.isActiveAndEnabled)
        {
            // Wheel
            if (holdingAmount > 0 && utilHand) GameplayUIController.Instance.UpdateEquipped(utilHand.itemImage, utilHand.itemName);
            else GameplayUIController.Instance.UpdateEquipped(null, null);

            //Bottom left ammo count
            if (utilHand != null) GameplayUIController.Instance.UpdateItemText(holdingAmount, stackAmount);
            else GameplayUIController.Instance.UpdateItemText(0, 0);
        }
    }

}
