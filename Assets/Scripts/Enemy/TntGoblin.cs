using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// TNT Goblin enemy: wanders randomly, detects the player within range,
// stops and throws a TNT projectile that explodes on arrival.
// Uses Rigidbody2D physics — water and terrain colliders block movement automatically.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyDeath))]
public class TntGoblin : MonoBehaviour
{
    // ── Animator parameter names ───────────────────────────────────────────────
    private const string AnimIsMoving = "IsMoving";
    private const string AnimThrow    = "Throw";

    // ── Wander constants ──────────────────────────────────────────────────────
    private const float MinWanderRadius = 1f;
    private const float MaxWanderRadius = 3.5f;
    private const float MinIdleTime     = 1.5f;
    private const float MaxIdleTime     = 3.0f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.2f;

    [Header("Attack")]
    [SerializeField] private float detectionRange   = 6f;
    [SerializeField] private float attackRange      = 4f;
    [SerializeField] private float attackCooldown   = 2.5f;
    [SerializeField] private int   tntDamage        = 1;
    [SerializeField] private float tntThrowDuration = 0.6f;

    [Header("Projectile")]
    [SerializeField] private GameObject tntProjectilePrefab;
    [SerializeField] private float      explosionRadius = 0.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private Rigidbody2D    rb;
    private SpriteRenderer spriteRenderer;
    private Animator       animator;
    private Transform      playerTransform;
    private Health         playerHealth;
    private Knockback      knockback;

    private Vector2 wanderTarget;
    private float   idleTimer;
    private float   attackTimer;
    private bool    isIdle      = true;
    private bool    isThrowing  = false;
    private bool    isExiting   = false;
    private bool    facingRight = true;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator       = GetComponent<Animator>();
        knockback      = GetComponent<Knockback>();

        rb.gravityScale           = 0f;
        rb.freezeRotation         = true;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Disable any stray NavMeshAgent — movement is driven by Rigidbody2D.
        if (TryGetComponent(out NavMeshAgent navAgent))
        {
            navAgent.enabled = false;
            Debug.LogWarning($"[TntGoblin] NavMeshAgent found on '{name}' and has been disabled. " +
                             "Remove it from the prefab to avoid this warning.", this);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth    = playerObj.GetComponent<Health>();
        }
    }

    private void Start()
    {
        EnterIdle();
        attackTimer = attackCooldown * 0.5f;
    }

    // Called by SpawnPoint after instantiation. The goblin walks straight to
    // exitWorldPos before activating normal AI, ensuring it clears the building collider cleanly.
    // onComplete is invoked right when the walk finishes so the SpawnPoint
    // can re-enable environment collisions at the exact moment.
    public void BeginExitPhase(Vector2 exitWorldPos, System.Action onComplete = null)
    {
        isExiting    = true;
        wanderTarget = exitWorldPos;
        isIdle       = false;
        StartCoroutine(ExitRoutine(exitWorldPos, onComplete));
    }

    private IEnumerator ExitRoutine(Vector2 exitWorldPos, System.Action onComplete)
    {
        while (Vector2.Distance(transform.position, exitWorldPos) > 0.15f)
        {
            Vector2 dir = (exitWorldPos - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;
            UpdateFacing();
            animator.SetBool(AnimIsMoving, true);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isExiting         = false;

        // Notify SpawnPoint immediately so collisions are restored before EnterIdle
        // picks a wander target — preventing the goblin from walking back inside.
        onComplete?.Invoke();

        EnterIdle();
    }

    private void Update()
    {
        // Suppress normal AI while the goblin is walking out of the building.
        if (isExiting || isThrowing) return;

        // Stop all AI when the player is dead.
        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Pause AI during knockback — Rigidbody2D handles the push.
        if (knockback != null && knockback.IsActive) return;

        attackTimer -= Time.deltaTime;

        float distToPlayer = playerTransform != null
            ? Vector2.Distance(transform.position, playerTransform.position)
            : float.MaxValue;

        // ── Attack check ──────────────────────────────────────────────────────
        if (distToPlayer <= attackRange && attackTimer <= 0f)
        {
            StartCoroutine(ThrowTnt());
            return;
        }

        // ── Chase / wander ────────────────────────────────────────────────────
        if (distToPlayer <= detectionRange && playerTransform != null)
        {
            if (distToPlayer > attackRange)
            {
                Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * moveSpeed;
                isIdle = false;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            Wander();
        }

        UpdateFacing();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        rb.rotation = 0f;
    }

    // ── Wander ────────────────────────────────────────────────────────────────

    private void Wander()
    {
        if (isIdle)
        {
            rb.linearVelocity = Vector2.zero;
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
                PickWanderTarget();
            return;
        }

        float dist = Vector2.Distance(transform.position, wanderTarget);
        if (dist > 0.2f)
        {
            Vector2 dir = (wanderTarget - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            EnterIdle();
        }
    }

    private void PickWanderTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * Random.Range(MinWanderRadius, MaxWanderRadius);
        wanderTarget = (Vector2)transform.position + randomOffset;
        isIdle = false;
    }

    private void EnterIdle()
    {
        rb.linearVelocity = Vector2.zero;
        idleTimer = Random.Range(MinIdleTime, MaxIdleTime);
        isIdle    = true;
    }

    // ── Attack coroutine ──────────────────────────────────────────────────────

    // Plays the throw animation, spawns an optional projectile arc, then
    // applies damage to the player if they are still within the explosion radius.
    private IEnumerator ThrowTnt()
    {
        isThrowing        = true;
        rb.linearVelocity = Vector2.zero;

        if (playerTransform != null)
        {
            bool playerIsLeft    = playerTransform.position.x < transform.position.x;
            facingRight          = !playerIsLeft;
            spriteRenderer.flipX = playerIsLeft;
        }

        animator.SetBool(AnimIsMoving, false);
        animator.SetTrigger(AnimThrow);

        Vector3 targetPos = playerTransform != null
            ? playerTransform.position
            : transform.position;

        if (tntProjectilePrefab != null)
            StartCoroutine(ArcProjectile(transform.position, targetPos, tntThrowDuration));

        yield return new WaitForSeconds(tntThrowDuration);

        if (playerHealth != null && playerTransform != null)
        {
            float dist = Vector2.Distance(playerTransform.position, targetPos);
            if (dist <= explosionRadius)
                playerHealth.TakeDamage(tntDamage);
        }

        attackTimer = attackCooldown;
        isThrowing  = false;
    }

    // Moves a projectile instance in a simple arc from start to end.
    private IEnumerator ArcProjectile(Vector3 start, Vector3 end, float duration)
    {
        GameObject proj     = Instantiate(tntProjectilePrefab, start, Quaternion.identity);
        float      elapsed  = 0f;
        const float arcHeight = 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float   t   = elapsed / duration;
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += arcHeight * Mathf.Sin(Mathf.PI * t);
            proj.transform.position = pos;
            yield return null;
        }

        Destroy(proj);
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    private void UpdateFacing()
    {
        const float MinSpeedSqr   = 0.04f;
        const float FlipThreshold = 0.08f;

        if (rb.linearVelocity.sqrMagnitude < MinSpeedSqr) return;

        float vx = rb.linearVelocity.x;
        if (vx > FlipThreshold)
        {
            facingRight          = true;
            spriteRenderer.flipX = false;
        }
        else if (vx < -FlipThreshold)
        {
            facingRight          = false;
            spriteRenderer.flipX = true;
        }
    }

    private void UpdateAnimator()
    {
        bool moving = !isIdle && rb.linearVelocity.sqrMagnitude > 0.04f;
        animator.SetBool(AnimIsMoving, moving);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
