using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Central UI manager. Handles showing and hiding UI panels.
// Toggle the inventory panel by pressing the I key.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject inventoryPanel;
    public TextMeshProUGUI invTextMoney;
    public TextMeshProUGUI invTextWoods;
    public TextMeshProUGUI invTextMeat;

    private bool           inventoryOpen;
    private InventoryPanel inventoryPanelComponent;

    // Returns true if the resource panel is currently active.
    public bool IsInventoryOpen => inventoryOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanelComponent = inventoryPanel.GetComponent<InventoryPanel>();

        // Initialize the panel from here so it receives events even when it starts inactive.
        if (inventoryPanelComponent != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                inventoryPanelComponent.Initialize(player.GetComponent<ResoursesCollector>());
        }

        SetInventoryVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            ToggleInventory();
    }

    // Toggles the inventory panel open or closed. Cancels any active flash first.
    public void ToggleInventory()
    {
        inventoryPanelComponent?.CancelFlash();
        SetInventoryVisible(!inventoryOpen);
    }

    // Shows or hides the inventory panel explicitly.
    public void SetInventoryVisible(bool visible)
    {
        inventoryOpen = visible;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(visible);
    }

    // Shows the resource panel with the given entry blinking to signal insufficient stock.
    public void FlashInsufficient(ResourceType type)
    {
        inventoryPanelComponent?.FlashInsufficient(type);
    }

    // Updates the money counter text in the inventory panel.
    public void UpdateMoney(int value)
    {
        if (invTextMoney != null) invTextMoney.text = value.ToString();
    }

    // Updates the wood counter text in the inventory panel.
    public void UpdateWoods(int value)
    {
        if (invTextWoods != null) invTextWoods.text = value.ToString();
    }

    // Updates the meat counter text in the inventory panel.
    public void UpdateMeat(int value)
    {
        if (invTextMeat != null) invTextMeat.text = value.ToString();
    }
}
