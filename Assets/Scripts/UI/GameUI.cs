using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Drives the HUD: kill counter, bridge notification, boss status, and player health bar.
public class GameUI : MonoBehaviour
{
    private const float BossCallSignDuration = 4f;

    [Header("Kill Counter")]
    [SerializeField] private TextMeshProUGUI killCounterText;
    [SerializeField] private Image           killFillBar;

    [Header("Player Health Bar")]
    [SerializeField] private Image healthBarFill;

    [Header("Bridge Notification")]
    [SerializeField] private TextMeshProUGUI bridgeNotificationText;
    [SerializeField] private Image           bridgeNotificationBackground;

    [Header("Boss Call Sign")]
    [SerializeField] private GameObject bossCallSign;

    [Header("Boss Status")]
    [SerializeField] private TextMeshProUGUI bossStatusText;
    [SerializeField] private GameObject      bossStatusRoot;

    private Health playerHealth;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        SetActive(bossStatusText, false);
        if (bossStatusRoot != null) bossStatusRoot.SetActive(false);
        SetActive(bridgeNotificationText,       false);
        SetActive(bridgeNotificationBackground, false);
        if (bossCallSign != null) bossCallSign.SetActive(false);

        // Subscribe in Start() — GameManager.Instance is guaranteed ready here.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyKillCountChanged.AddListener(UpdateKillCounter);
            GameManager.Instance.OnAllEnemiesDefeated.AddListener(HandleBridgeUnlocked);
            GameManager.Instance.OnBossDefeated.AddListener(HandleBossDefeated);
        }

        int required = GameManager.Instance != null ? GameManager.Instance.EnemiesRequired : 0;
        UpdateKillCounter(0, required);

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.AddListener(UpdatePlayerHealth);
                UpdatePlayerHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        if (healthBarFill != null && playerHealth == null)
            healthBarFill.fillAmount = 1f;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged.RemoveListener(UpdatePlayerHealth);

        if (GameManager.Instance == null) return;

        GameManager.Instance.OnEnemyKillCountChanged.RemoveListener(UpdateKillCounter);
        GameManager.Instance.OnAllEnemiesDefeated.RemoveListener(HandleBridgeUnlocked);
        GameManager.Instance.OnBossDefeated.RemoveListener(HandleBossDefeated);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void UpdateKillCounter(int current, int required)
    {
        if (killCounterText != null)
            killCounterText.text = $"Enemigos {current}/{required}";

        if (killFillBar != null && required > 0)
            killFillBar.fillAmount = (float)current / required;
    }

    private void HandleBridgeUnlocked()
    {
        if (bossCallSign != null)
            StartCoroutine(ShowBossCallSignThenHide());

        SetActive(bridgeNotificationText,       false);
        SetActive(bridgeNotificationBackground, false);
    }

    // Shows the "go for the boss" sign for a few seconds, then hides it.
    // Uses WaitForSecondsRealtime so it works even if timeScale is 0.
    private IEnumerator ShowBossCallSignThenHide()
    {
        bossCallSign.SetActive(true);
        yield return new WaitForSecondsRealtime(BossCallSignDuration);
        if (bossCallSign != null)
            bossCallSign.SetActive(false);
    }

    private void HandleBossDefeated()
    {
        if (bossCallSign != null) bossCallSign.SetActive(false);
        SetActive(bridgeNotificationText,       false);
        SetActive(bridgeNotificationBackground, false);

        if (bossStatusRoot != null) bossStatusRoot.SetActive(true);
        if (bossStatusText != null)
        {
            SetActive(bossStatusText, true);
            bossStatusText.text = "¡Boss derrotado!";
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void UpdatePlayerHealth(int current, int max)
    {
        if (healthBarFill != null && max > 0)
            healthBarFill.fillAmount = (float)current / max;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetActive(Component c, bool active)
    {
        if (c != null) c.gameObject.SetActive(active);
    }
}
