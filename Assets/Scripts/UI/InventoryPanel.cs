using System.Collections;
using TMPro;
using UnityEngine;

// Drives the resource panel (HUD/Resources).
// On pickup: shows only the collected resource entry for a few seconds, then hides the panel.
// When manually opened via UIManager (I key): shows all entries at once.
public class InventoryPanel : MonoBehaviour
{
    private const float DefaultDisplayDuration = 2.5f;
    private const int   BlinkCount             = 4;
    private const float BlinkInterval          = 0.15f;

    [Header("Resource Entry Containers")]
    [SerializeField] private GameObject moneyEntry;
    [SerializeField] private GameObject meatEntry;
    [SerializeField] private GameObject woodEntry;

    [Header("Resource Texts")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI meatText;
    [SerializeField] private TextMeshProUGUI woodText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = DefaultDisplayDuration;

    private ResoursesCollector collector;
    private Coroutine           hideCoroutine;
    private bool                isFlashVisible;

    private void Start()
    {
        // Fallback: subscribe if Initialize() was never called externally.
        if (collector == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                Initialize(player.GetComponent<ResoursesCollector>());
        }

        if (!isFlashVisible)
        {
            SetAllEntriesVisible(true);
            RefreshCurrent();
        }
    }

    private void OnDestroy()
    {
        if (collector == null) return;
        collector.OnResourcesChanged  -= RefreshAll;
        collector.OnResourceCollected -= HandleResourceCollected;
    }

    private void OnEnable()
    {
        // When opened manually (not by a flash), restore all entries.
        if (!isFlashVisible)
        {
            SetAllEntriesVisible(true);
            RefreshCurrent();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Connects this panel to a ResoursesCollector and subscribes to its events.
    // Called from UIManager.Start() so the panel works even when it starts inactive.
    public void Initialize(ResoursesCollector col)
    {
        if (collector != null)
        {
            collector.OnResourcesChanged  -= RefreshAll;
            collector.OnResourceCollected -= HandleResourceCollected;
        }

        collector = col;

        if (collector != null)
        {
            collector.OnResourcesChanged  += RefreshAll;
            collector.OnResourceCollected += HandleResourceCollected;
        }
    }

    // Cancels any active flash and restores all entries. Called by UIManager on manual toggle.
    public void CancelFlash()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        isFlashVisible = false;
        SetAllEntriesVisible(true);
        RefreshCurrent();
    }

    // Shows only the given resource entry blinking to signal the player doesn't have enough.
    // Ignored when the inventory is already manually open.
    public void FlashInsufficient(ResourceType type)
    {
        if (UIManager.Instance != null && UIManager.Instance.IsInventoryOpen && !isFlashVisible)
            return;

        SetAllEntriesVisible(false);
        SetEntryVisible(type, true);
        UpdateText(type, GetCount(type));

        isFlashVisible = true;
        UIManager.Instance?.SetInventoryVisible(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(BlinkAndHide(type));
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleResourceCollected(ResourceType type, int newCount)
    {
        // If the inventory is already manually open, just let RefreshAll update all texts normally.
        if (UIManager.Instance != null && UIManager.Instance.IsInventoryOpen && !isFlashVisible)
            return;

        // Show only the collected resource entry.
        SetAllEntriesVisible(false);
        SetEntryVisible(type, true);
        UpdateText(type, newCount);

        // Mark as flash BEFORE activating the panel so OnEnable respects it.
        isFlashVisible = true;

        // Open the panel temporarily.
        UIManager.Instance?.SetInventoryVisible(true);

        // Reset the auto-hide timer.
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        isFlashVisible = false;
        SetAllEntriesVisible(true);
        UIManager.Instance?.SetInventoryVisible(false);
        hideCoroutine = null;
    }

    private IEnumerator BlinkAndHide(ResourceType type)
    {
        for (int i = 0; i < BlinkCount; i++)
        {
            SetEntryVisible(type, false);
            yield return new WaitForSeconds(BlinkInterval);
            SetEntryVisible(type, true);
            yield return new WaitForSeconds(BlinkInterval);
        }

        isFlashVisible = false;
        SetAllEntriesVisible(true);
        UIManager.Instance?.SetInventoryVisible(false);
        hideCoroutine = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RefreshAll(int money, int meat, int wood)
    {
        if (moneyText != null) moneyText.text = money.ToString("D2");
        if (meatText  != null) meatText.text  = meat.ToString("D2");
        if (woodText  != null) woodText.text  = wood.ToString("D2");
    }

    private void RefreshCurrent()
    {
        if (collector != null)
            RefreshAll(collector.Money, collector.Meat, collector.Wood);
    }

    private void UpdateText(ResourceType type, int value)
    {
        switch (type)
        {
            case ResourceType.Money: if (moneyText != null) moneyText.text = value.ToString("D2"); break;
            case ResourceType.Meat:  if (meatText  != null) meatText.text  = value.ToString("D2"); break;
            case ResourceType.Wood:  if (woodText  != null) woodText.text  = value.ToString("D2"); break;
        }
    }

    private void SetAllEntriesVisible(bool visible)
    {
        if (moneyEntry != null) moneyEntry.SetActive(visible);
        if (meatEntry  != null) meatEntry.SetActive(visible);
        if (woodEntry  != null) woodEntry.SetActive(visible);
    }

    private void SetEntryVisible(ResourceType type, bool visible)
    {
        switch (type)
        {
            case ResourceType.Money: if (moneyEntry != null) moneyEntry.SetActive(visible); break;
            case ResourceType.Meat:  if (meatEntry  != null) meatEntry.SetActive(visible);  break;
            case ResourceType.Wood:  if (woodEntry  != null) woodEntry.SetActive(visible);  break;
        }
    }

    private int GetCount(ResourceType type)
    {
        if (collector == null) return 0;
        return type switch
        {
            ResourceType.Money => collector.Money,
            ResourceType.Meat  => collector.Meat,
            ResourceType.Wood  => collector.Wood,
            _                  => 0,
        };
    }
}
