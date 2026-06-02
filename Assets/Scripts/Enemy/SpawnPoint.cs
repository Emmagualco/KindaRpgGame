using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Self-contained spawn point. After an initial delay it spawns one enemy from a
// weighted variant list, then waits a configurable interval before spawning again.
// Enemies appear at the door-offset position, as if exiting through the house entrance.
public class SpawnPoint : MonoBehaviour
{
    // ── Prefab variants ──────────────────────────────────────────────────────

    [System.Serializable]
    public class SpawnVariant
    {
        [Tooltip("Enemy prefab to spawn.")]
        public GameObject prefab;

        [Tooltip("Relative weight. Higher values make this variant more likely.")]
        [Min(1)]
        public int weight = 1;
    }

    [Header("Prefab Variants (up to 5)")]
    [Tooltip("Enemy prefab variants. One is chosen at random using weights.")]
    [SerializeField] private SpawnVariant[] variants = new SpawnVariant[1];

    // ── Timing ───────────────────────────────────────────────────────────────

    [Header("Timing")]
    [Tooltip("Seconds after Play before ANY spawn activates (shared initial delay).")]
    [SerializeField] private float globalDelay = 5f;

    [Tooltip("Extra random delay per spawn point so they don't all fire at once.")]
    [SerializeField] private float randomExtraDelay = 2f;

    [Tooltip("Minimum seconds between consecutive spawns at this point.")]
    [SerializeField] private float minRespawnInterval = 14f;

    [Tooltip("Maximum seconds between consecutive spawns at this point.")]
    [SerializeField] private float maxRespawnInterval = 24f;

    [Tooltip("Maximum number of enemies alive at a time from this spawn point (0 = unlimited).")]
    [SerializeField] private int maxAlive = 1;

    [Tooltip("Maximum total enemies ever spawned from this point (0 = unlimited). Default 1 means spawn once and stop.")]
    [SerializeField] private int maxTotalSpawns = 1;

    // ── Door offset ──────────────────────────────────────────────────────────

    [Header("Door Position")]
    [Tooltip("Local offset from this transform to the house door.")]
    [SerializeField] private Vector2 doorOffset = new Vector2(0f, -1.2f);

    [Tooltip("How many units past the door the enemy walks before its AI activates.")]
    [SerializeField] private float exitDistance = 2.0f;

    // ─────────────────────────────────────────────────────────────────────────

    private int _aliveCount;
    private int _totalSpawned;

    private void Start()
    {
        float delay = globalDelay + Random.Range(0f, randomExtraDelay);
        StartCoroutine(SpawnLoop(delay));
    }

    private IEnumerator SpawnLoop(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        while (maxTotalSpawns <= 0 || _totalSpawned < maxTotalSpawns)
        {
            if (maxAlive <= 0 || _aliveCount < maxAlive)
            {
                SpawnEnemy();

                // If we've now hit the total cap, stop immediately.
                if (maxTotalSpawns > 0 && _totalSpawned >= maxTotalSpawns)
                    yield break;
            }

            float interval = Random.Range(minRespawnInterval, maxRespawnInterval);
            yield return new WaitForSeconds(interval);
        }
    }

    // Radius around the spawn point used to detect surrounding environment colliders.
    // Should be large enough to cover the full building footprint.
    private const float NearbyColliderSearchRadius = 8f;

    // Picks a random variant (weighted) and instantiates it at the door position.
    // Collision between the enemy and any nearby environment collider is ignored
    // until the enemy's exit walk completes, then immediately restored.
    private void SpawnEnemy()
    {
        GameObject prefab = PickVariant();
        if (prefab == null)
        {
            Debug.LogWarning($"[SpawnPoint] {name}: no valid prefab — check the variants list.", this);
            return;
        }

        Vector3 spawnPos   = transform.position + (Vector3)doorOffset;
        Vector3 exitTarget = spawnPos + (Vector3)(doorOffset.normalized * exitDistance);

        // Capture surrounding environment colliders BEFORE instantiation so the
        // new enemy's own colliders are not included in the query.
        // IMPORTANT: filter out the player — we only need to ignore building walls,
        // never the player's collider (otherwise hit-detection queries may miss the enemy).
        Collider2D[] allNearby    = Physics2D.OverlapCircleAll(transform.position, NearbyColliderSearchRadius);
        Collider2D[] envColliders = System.Array.FindAll(allNearby, c => !c.CompareTag("Player"));

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        _aliveCount++;
        _totalSpawned++;
        enemy.AddComponent<SpawnPointTracker>().Initialize(this);

        Collider2D[] enemyColliders = enemy.GetComponentsInChildren<Collider2D>(includeInactive: true);

        // Disable collisions between the enemy and all nearby environment colliders.
        SetCollisionIgnore(envColliders, enemyColliders, ignore: true);

        // Re-enable collisions as soon as the exit walk finishes.
        System.Action onExitComplete = () =>
        {
            if (enemy != null)
            {
                Collider2D[] currentEnemyColliders = enemy.GetComponentsInChildren<Collider2D>(includeInactive: true);
                SetCollisionIgnore(envColliders, currentEnemyColliders, ignore: false);
            }
        };

        Enemy melee = enemy.GetComponent<Enemy>();
        if (melee != null) { melee.BeginExitPhase(exitTarget, onExitComplete); return; }

        TntGoblin tnt = enemy.GetComponent<TntGoblin>();
        if (tnt != null) { tnt.BeginExitPhase(exitTarget, onExitComplete); return; }

        // Fallback: no exit phase component — re-enable after a short delay.
        StartCoroutine(ReenableAfterDelay(onExitComplete, 2f));
    }

    private IEnumerator ReenableAfterDelay(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    // Sets Physics2D.IgnoreCollision between every pair of environment and enemy colliders.
    // Skips pairs where either collider is null.
    private static void SetCollisionIgnore(Collider2D[] envColliders, Collider2D[] enemyColliders, bool ignore)
    {
        foreach (Collider2D env in envColliders)
        {
            if (env == null) continue;
            foreach (Collider2D ec in enemyColliders)
            {
                if (ec == null || env == ec) continue;
                Physics2D.IgnoreCollision(env, ec, ignore);
            }
        }
    }

    // Called by SpawnPointTracker when its enemy is destroyed.
    public void OnEnemyDestroyed() => _aliveCount = Mathf.Max(0, _aliveCount - 1);

    // Weighted random selection over the variants array.
    private GameObject PickVariant()
    {
        int totalWeight = 0;
        foreach (SpawnVariant v in variants)
        {
            if (v?.prefab != null)
                totalWeight += v.weight;
        }

        if (totalWeight == 0) return null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (SpawnVariant v in variants)
        {
            if (v?.prefab == null) continue;
            cumulative += v.weight;
            if (roll < cumulative)
                return v.prefab;
        }

        return null;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 doorWorld = transform.position + (Vector3)doorOffset;
        Vector3 exitWorld = doorWorld + (Vector3)(doorOffset.normalized * exitDistance);
        Gizmos.DrawWireSphere(doorWorld, 0.25f);
        Gizmos.DrawLine(transform.position, doorWorld);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(exitWorld, 0.2f);
        Gizmos.DrawLine(doorWorld, exitWorld);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f);
        Vector3 doorWorld = transform.position + (Vector3)doorOffset;
        Vector3 exitWorld = doorWorld + (Vector3)(doorOffset.normalized * exitDistance);
        Gizmos.DrawSphere(doorWorld, 0.2f);
        Gizmos.DrawSphere(exitWorld, 0.15f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(doorWorld + Vector3.up * 0.3f, $"Door | Alive: {_aliveCount}/{maxAlive}");
        UnityEditor.Handles.Label(exitWorld + Vector3.up * 0.3f, "Exit target");
#endif
    }
}
