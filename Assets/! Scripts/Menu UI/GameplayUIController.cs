using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUIController : MonoBehaviour
{
    public static GameplayUIController Instance;

    [Header("Health")]
    public Slider healthSlider;
    public int currentHealth;
    public int maxHealth;

    [Header("Ghost")]
    public Slider ghostSlider;
    public int currentGhost;
    public int maxGhost;

    [Header("Charges")]
    public Slider itemCharge;
    public Slider attackCharge;

    [Header("Util Count")]
    public TextMeshProUGUI itemText;

    [Header("Wheel")]
    public Animator anim;
    public bool isOpen = false;
    private bool weaponWheelSelected = false;
    public Image selectedItem;
    public TextMeshProUGUI selectedText;
    public Sprite noImage;
    public static int weaponID;
    public Button[] buttons;
    public Image[] buttonIcons;

    [Header("Inventory")]
    public List<InventorySlot> currentInventory = new List<InventorySlot>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (buttons.Length == 0) Debug.LogError("No Buttons set! Pls set them.");
        
        if (buttonIcons.Length == 0) Debug.LogError("No Buttons ICONS set! Pls set them.");

        HideItemSlider();
        HideAttackSlider();
    }

    
    public void OpenInventoryWheel(List<InventorySlot> inventory)
    {
        currentInventory = inventory;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttonIcons[i] == null || currentInventory.Count <= i || currentInventory[i].item == null)
            {
                buttonIcons[i].sprite = noImage; // Assign a default "no image" sprite
            }
            else
            {
                buttonIcons[i].sprite = currentInventory[i].item.itemImage;
            }
        }

        isOpen = true;
        weaponWheelSelected = true;
        anim.SetBool("OpenWeaponWheel", true);

        // Unlock and show the cursor while the weapon wheel is open
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseInventoryWheel()
    {
        isOpen = false;
        weaponWheelSelected = false;
        anim.SetBool("OpenWeaponWheel", false);

        // Lock and hide the cursor after the weapon wheel is closed
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UpdateHealthSlider(int currentHealth, int maxHealth)
    {
        healthSlider.minValue = 0;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    public void UpdateGhostSlider(int currentGhost, int maxGhost)
    {
        ghostSlider.minValue = 0;
        ghostSlider.maxValue = maxGhost;
        ghostSlider.value = currentGhost;
    }

    public void UpdateEquipped(Sprite image, string selectedText)
    {
        if (image != null) selectedItem.sprite = image;
        else selectedItem.sprite = noImage;

        this.selectedText.text = selectedText;
    }
    public void UpdateItemText(int currentItems, int maxItems)
    {
        itemText.text = $"{currentItems}/{maxItems}";

        if (currentItems == 0 || maxItems == 0) itemText.text = $"";
    }

    //SLIDERS
    public void UpdateItemCharge(float chargeValue, float maxValue)
    {
        if (itemCharge)
        {
            itemCharge.gameObject.SetActive(true);
            itemCharge.maxValue = maxValue;
            itemCharge.value = chargeValue;
        }
        
    }
    public void HideItemSlider()
    {
        if (itemCharge)
        {
            itemCharge.value = 0;
            itemCharge.gameObject.SetActive(false);
        }
            
    }

    public void UpdateAttackCharge(float chargeValue, float maxValue)
    {
        if (attackCharge)
        {
            attackCharge.gameObject.SetActive(true);
            attackCharge.maxValue = maxValue;
            attackCharge.value = chargeValue;
        }
        
    }
    public void HideAttackSlider()
    {
        if (attackCharge)
        {
            attackCharge.value = 0;
            attackCharge.gameObject.SetActive(false);
        }
        
    }
}
