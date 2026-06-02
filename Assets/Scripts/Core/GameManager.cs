using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Singleton that tracks kills, bridge unlock, boss death, and game win.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Requirements")]
    [SerializeField] private int enemiesRequiredToKill = 7;

    [Header("References")]
    [SerializeField] private GameObject chest;

    [Header("Bridge Unlock")]
    [SerializeField] private GameObject bridge;
    [SerializeField] private GameObject waterBridge;

    public UnityEvent<int, int> OnEnemyKillCountChanged;
    public UnityEvent           OnAllEnemiesDefeated;
    public UnityEvent           OnBossDefeated;
    public UnityEvent           OnGameWon;

    private int  enemiesKilled;
    private bool allEnemiesDefeated;
    private bool bossDefeated;
    private bool gameWon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (chest != null)       chest.SetActive(false);
        if (bridge != null)      bridge.SetActive(false);
        if (waterBridge != null) waterBridge.SetActive(true);

        EnsureLayerCollisions();
    }

    // Makes sure relevant layers collide with each other.
    private static void EnsureLayerCollisions()
    {
        int defaultLayer = LayerMask.NameToLayer("Default");
        int enemyLayer   = LayerMask.NameToLayer("Enemy");

        if (defaultLayer < 0)
        {
            Debug.LogWarning("[GameManager] 'Default' layer not found.");
            return;
        }

        Physics2D.IgnoreLayerCollision(defaultLayer, defaultLayer, false);

        if (enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(defaultLayer, enemyLayer, false);
            Physics2D.IgnoreLayerCollision(enemyLayer,   enemyLayer, false);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void RegisterEnemyKill()
    {
        if (gameWon) return;

        enemiesKilled++;
        OnEnemyKillCountChanged?.Invoke(enemiesKilled, enemiesRequiredToKill);

        if (!allEnemiesDefeated && enemiesKilled >= enemiesRequiredToKill)
        {
            allEnemiesDefeated = true;

            if (bridge != null)      bridge.SetActive(true);
            if (waterBridge != null) waterBridge.SetActive(false);

            OnAllEnemiesDefeated?.Invoke();
        }
    }

    public void RegisterBossKill()
    {
        if (gameWon || bossDefeated) return;

        bossDefeated = true;
        OnBossDefeated?.Invoke();

        if (chest != null) chest.SetActive(true);

        StartCoroutine(AutoWinRoutine(1f));
    }

    private IEnumerator AutoWinRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        RegisterChestPickup();
    }

    public void RegisterChestPickup()
    {
        if (gameWon) return;
        gameWon = true;
        OnGameWon?.Invoke();
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public bool AreAllEnemiesDefeated() => allEnemiesDefeated;

    public int  EnemiesKilled      => enemiesKilled;
    public int  EnemiesRequired    => enemiesRequiredToKill;
    public bool AllEnemiesDefeated => allEnemiesDefeated;
    public bool BossDefeated       => bossDefeated;
}
