using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Boss AI. Orbits the player, throws Dynamite and GoblinBarrels.
// Disables any co-existing TntGoblin component to take sole control.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyDeath))]
public class Boss : MonoBehaviour
{
    private const string AnimIsMoving = "IsMoving";
    private const string AnimThrow    = "Throw";

    private const float MinWanderRadius = 2f;
    private const float MaxWanderRadius = 5f;
    private const float MinIdleTime     = 0.8f;
    private const float MaxIdleTime     = 1.8f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed  = 2.0f;
    [SerializeField] private float chaseSpeed = 3.0f;
    [SerializeField] private float fleeSpeed  = 3.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float disengageRange = 14f;
    [SerializeField] private float throwRange     = 5.0f;
    [SerializeField] private float minThrowRange  = 2.5f;

    [Header("Attack 1 — Dynamite")]
    [SerializeField] private GameObject dynamitePrefab;
    [SerializeField] private float      dynamiteCooldown   = 2.0f;
    [SerializeField] private float      dynamiteFlightTime = 0.8f;
    [SerializeField] private int        dynamiteDamage     = 2;
    [SerializeField] private float      explosionRadius    = 1.5f;

    [Header("Attack 2 — Barrel")]
    [SerializeField] private GameObject barrelPrefab;
    [SerializeField] private float      barrelCooldown   = 8.0f;
    [SerializeField] private float      barrelFlightTime = 1.0f;

    [Header("Boss Laugh")]
    [Tooltip("Min seconds between laughs during combat.")]
    [SerializeField] private float laughIntervalMin = 6f;
    [Tooltip("Max seconds between laughs during combat.")]
    [SerializeField] private float laughIntervalMax = 12f;

    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject barrelExplosionPrefab;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Animator       anim;
    private Knockback      knockback;

    private Transform player;
    private Health    playerHealth;

    private enum AIState { Idle, Wander, Chase, Exit }
    private AIState state;

    private Vector2 wanderTarget;
    private float   idleTimer;
    private float   attackTimer;
    private float   barrelTimer;
    private bool    facingRight = true;

    private bool isAttacking;
    private bool isChasing;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        sr        = GetComponent<SpriteRenderer>();
        anim      = GetComponent<Animator>();
        knockback = GetComponent<Knockback>();

        rb.gravityScale           = 0f;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;

        if (TryGetComponent(out NavMeshAgent nav))    nav.enabled       = false;
        if (TryGetComponent(out TntGoblin tntGoblin)) tntGoblin.enabled = false;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player       = playerObj.transform;
            playerHealth = playerObj.GetComponent<Health>();
        }

        attackTimer = dynamiteCooldown * 0.5f;
        barrelTimer = barrelCooldown;
        EnterIdle();
        StartCoroutine(BossLaughRoutine());
    }

    public void BeginExitPhase(Vector2 exitWorldPos, System.Action onComplete = null)
    {
        state = AIState.Exit;
        StartCoroutine(ExitRoutine(exitWorldPos, onComplete));
    }

    private IEnumerator ExitRoutine(Vector2 exitWorldPos, System.Action onComplete)
    {
        const float MaxExitTime = 4f;
        float elapsed = 0f;

        while (Vector2.Distance(transform.position, exitWorldPos) > 0.15f)
        {
            MoveToward(exitWorldPos, moveSpeed);
            anim.SetBool(AnimIsMoving, true);
            elapsed += Time.deltaTime;
            if (elapsed >= MaxExitTime) break;
            yield return null;
        }

        onComplete?.Invoke();
        Stop();
        if (state == AIState.Exit) EnterIdle();
    }

    // ── Main loop ─────────────────────────────────────────────────────────────

    private void Update()
    {
        if (state == AIState.Exit) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            Stop();
            anim.SetBool(AnimIsMoving, false);
            return;
        }

        if (knockback != null && knockback.IsActive) return;

        attackTimer -= Time.deltaTime;
        barrelTimer -= Time.deltaTime;

        float dist = player != null
            ? Vector2.Distance(transform.position, player.position)
            : float.MaxValue;

        if (!isChasing && dist <= detectionRange)
            isChasing = true;
        else if (isChasing && dist > disengageRange)
        {
            isChasing = false;
            EnterIdle();
        }

        if (isChasing && player != null)
            HandleCombat(dist);
        else
            HandleWander();

        UpdateFacing();
        anim.SetBool(AnimIsMoving, rb.linearVelocity.sqrMagnitude > 0.04f);
    }

    private void FixedUpdate()
    {
        rb.rotation = 0f;
    }

    // ── Combat ────────────────────────────────────────────────────────────────

    private void HandleCombat(float dist)
    {
        Orbit(dist);

        if (isAttacking) return;

        if (barrelTimer <= 0f && barrelPrefab != null)
        {
            StartCoroutine(ThrowBarrel());
            return;
        }

        if (attackTimer <= 0f && dist <= throwRange * 1.5f)
            StartCoroutine(ThrowDynamite());
    }

    // Orbits the player at the ideal throw range.
    // Flees when too close, closes gap when too far, strafes when in range.
    private void Orbit(float dist)
    {
        if (player == null) return;

        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 perp     = new Vector2(-toPlayer.y, toPlayer.x);
        float   sign     = Mathf.Sin(Time.time * 0.7f) >= 0f ? 1f : -1f;

        Vector2 velocity;

        if (dist < minThrowRange)
            velocity = (-toPlayer + perp * sign * 0.6f).normalized * fleeSpeed;
        else if (dist > throwRange + 1.0f)
            velocity = (toPlayer + perp * sign * 0.4f).normalized * chaseSpeed;
        else
            velocity = perp * sign * moveSpeed;

        rb.linearVelocity = velocity;
    }

    // ── Attack coroutines ─────────────────────────────────────────────────────

    // Laughs at random intervals during combat.
    private IEnumerator BossLaughRoutine()
    {
        while (true)
        {
            float interval = Random.Range(laughIntervalMin, laughIntervalMax);
            yield return new WaitForSeconds(interval);

            Health bossHealth = GetComponent<Health>();
            bool bossAlive   = bossHealth == null || !bossHealth.IsDead;
            bool playerAlive = playerHealth == null || !playerHealth.IsDead;

            if (isChasing && bossAlive && playerAlive)
                AudioManager.Instance?.PlayBossLaugh();
        }
    }

    // Throws dynamite in an arc and deals AoE damage on landing.
    private IEnumerator ThrowDynamite()
    {
        isAttacking = true;
        FacePlayer();
        anim.SetTrigger(AnimThrow);

        Vector3 targetPos = player != null ? player.position : transform.position;

        yield return new WaitForSeconds(0.2f);

        if (dynamitePrefab != null)
            StartCoroutine(ArcProjectile(transform.position, targetPos, dynamiteFlightTime));

        attackTimer = dynamiteCooldown;
        isAttacking = false;

        yield return new WaitForSeconds(dynamiteFlightTime);

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, targetPos, Quaternion.identity);

        AudioManager.Instance?.PlayDynamiteExplosion();

        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                playerHealth?.TakeDamage(dynamiteDamage);
                break;
            }
        }
    }

    // Throws a GoblinBarrel in an arc; on landing it explodes and activates as a melee enemy.
    private IEnumerator ThrowBarrel()
    {
        isAttacking = true;
        FacePlayer();
        anim.SetTrigger(AnimThrow);

        Vector3 targetPos = player != null ? player.position : transform.position;

        yield return new WaitForSeconds(0.25f);

        if (barrelPrefab != null)
            StartCoroutine(ArcBarrel(transform.position, targetPos, barrelFlightTime));

        barrelTimer = barrelCooldown;
        isAttacking = false;
    }

    private IEnumerator ArcProjectile(Vector3 start, Vector3 end, float duration)
    {
        const float ArcHeight = 1.5f;

        GameObject proj = Instantiate(dynamitePrefab, start, Quaternion.identity);
        float elapsed   = 0f;

        while (elapsed < duration && proj != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += ArcHeight * Mathf.Sin(Mathf.PI * t);
            proj.transform.position = pos;
            yield return null;
        }

        if (proj != null) Destroy(proj);
    }

    private IEnumerator ArcBarrel(Vector3 start, Vector3 end, float duration)
    {
        const float ArcHeight    = 2.5f;
        const float GrowDuration = 0.35f;
        const float GrowMult     = 2.5f;

        GameObject barrel = Instantiate(barrelPrefab, start, Quaternion.identity);

        if (barrel.TryGetComponent(out Collider2D barrelCol))  barrelCol.enabled = false;
        if (barrel.TryGetComponent(out Rigidbody2D barrelRb))
        {
            barrelRb.gravityScale  = 0f;
            barrelRb.constraints   = RigidbodyConstraints2D.FreezeAll;
            barrelRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        if (barrel.TryGetComponent(out NavMeshAgent barrelNav)) barrelNav.enabled = false;

        float elapsed = 0f;
        while (elapsed < duration && barrel != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += ArcHeight * Mathf.Sin(Mathf.PI * t);
            barrel.transform.position = pos;
            yield return null;
        }

        if (barrel == null) yield break;

        Vector3 landPos = barrel.transform.position;

        GameObject explosionToUse = barrelExplosionPrefab != null ? barrelExplosionPrefab : explosionPrefab;
        if (explosionToUse != null)
            Instantiate(explosionToUse, landPos, Quaternion.identity);

        AudioManager.Instance?.PlayBarrelExplosion();

        Collider2D[] hits = Physics2D.OverlapCircleAll(landPos, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                playerHealth?.TakeDamage(dynamiteDamage);
                break;
            }
        }

        // Barrel grows then destroys itself.
        elapsed = 0f;
        Vector3 landScale = barrel.transform.localScale;
        while (elapsed < GrowDuration && barrel != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / GrowDuration);
            barrel.transform.localScale = Vector3.Lerp(landScale, landScale * GrowMult, t);
            yield return null;
        }

        if (barrel != null) Destroy(barrel);
    }

    // ── Wander ────────────────────────────────────────────────────────────────

    private void HandleWander()
    {
        switch (state)
        {
            case AIState.Idle:
                Stop();
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f) PickWanderTarget();
                break;

            case AIState.Wander:
                if (Vector2.Distance(transform.position, wanderTarget) > 0.2f)
                    MoveToward(wanderTarget, moveSpeed);
                else
                    EnterIdle();
                break;

            default:
                EnterIdle();
                break;
        }
    }

    private void EnterIdle()
    {
        state     = AIState.Idle;
        idleTimer = Random.Range(MinIdleTime, MaxIdleTime);
        Stop();
    }

    private void PickWanderTarget()
    {
        wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * Random.Range(MinWanderRadius, MaxWanderRadius);
        state        = AIState.Wander;
    }

    // ── Movement helpers ──────────────────────────────────────────────────────

    private void MoveToward(Vector2 target, float speed)
        => rb.linearVelocity = (target - (Vector2)transform.position).normalized * speed;

    private void Stop()
        => rb.linearVelocity = Vector2.zero;

    // ── Visuals ───────────────────────────────────────────────────────────────

    private void FacePlayer()
    {
        if (player == null) return;
        bool left   = player.position.x < transform.position.x;
        facingRight = !left;
        sr.flipX    = left;
    }

    private void UpdateFacing()
    {
        if (rb.linearVelocity.sqrMagnitude < 0.04f) return;
        float vx = rb.linearVelocity.x;
        if (vx >  0.08f)      { facingRight = true;  sr.flipX = false; }
        else if (vx < -0.08f) { facingRight = false; sr.flipX = true;  }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, disengageRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, throwRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minThrowRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
