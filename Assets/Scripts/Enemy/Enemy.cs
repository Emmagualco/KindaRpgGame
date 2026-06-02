using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Melee enemy AI. States: Idle → Wander → Chase → Attack.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Enemy : MonoBehaviour
{
    private const string IsMovingParam    = "IsMoving";
    private const string IsAttackingParam = "IsAttacking";
    private const string AttackDirParam   = "AttackDir";

    private const float MinWanderRadius = 1f;
    private const float MaxWanderRadius = 4f;
    private const float MinIdleTime     = 1f;
    private const float MaxIdleTime     = 2.5f;

    private const float StuckCheckInterval = 0.45f;
    private const float StuckMoveThreshold = 0.06f;
    private const float MovingSqrThreshold = 0.04f;
    private const float FlipDeadzone       = 0.08f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Header("Detection & Combat")]
    [SerializeField] private float chaseRange     = 6f;
    [SerializeField] private float attackRange    = 1.0f;
    [SerializeField] private float attackCooldown = 1.1f;
    [SerializeField] private int   attackDamage   = 1;

    [Header("Torch Hit Detection")]
    [SerializeField] private float torchLength = 0.7f;
    [SerializeField] private float hitRadius   = 0.3f;

    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Animator       anim;
    private Knockback      knockback;

    private Transform player;
    private Health    playerHealth;

    private enum AIState { Idle, Wander, Chase, Attack, Exit }
    private AIState currentState;

    private Vector2 wanderTarget;
    private float   idleTimer;
    private float   attackTimer;
    private bool    facingRight = true;

    private Vector2 _lastSampledPos;
    private float   _stuckTimer;

    private Vector2 pendingAttackDirection;

    // Prevents double-hits when Invoke and an Animation Event both fire.
    private bool _hitFired;

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

        if (TryGetComponent(out NavMeshAgent nav))
            nav.enabled = false;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player       = playerObj.transform;
            playerHealth = playerObj.GetComponent<Health>();
        }

        // If BeginExitPhase() was called before Start(), don't reset to Idle —
        // the ExitRoutine will do that when it finishes.
        if (currentState != AIState.Exit)
            EnterIdle();
    }

    // Called by SpawnPoint right after instantiation so the enemy walks out of
    // the spawn building before AI activates.
    public void BeginExitPhase(Vector2 exitWorldPos, System.Action onComplete = null)
    {
        currentState = AIState.Exit;
        StartCoroutine(ExitRoutine(exitWorldPos, onComplete));
    }

    private IEnumerator ExitRoutine(Vector2 exitWorldPos, System.Action onComplete)
    {
        const float MaxExitTime = 4f;
        float elapsed = 0f;

        while (Vector2.Distance(transform.position, exitWorldPos) > 0.15f)
        {
            MoveToward(exitWorldPos);
            anim.SetBool(IsMovingParam, true);
            elapsed += Time.deltaTime;
            if (elapsed >= MaxExitTime) break;
            yield return null;
        }

        onComplete?.Invoke();
        Stop();
        EnterIdle();
    }

    private void Update()
    {
        if (currentState == AIState.Exit) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            Stop();
            anim.SetBool(IsMovingParam, false);
            return;
        }

        if (knockback != null && knockback.IsActive) return;

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        float dist = player != null
            ? Vector2.Distance(transform.position, player.position)
            : float.MaxValue;

        if (player != null && dist <= chaseRange)
        {
            if (dist <= attackRange)
                HandleAttack();
            else
                HandleChase();
        }
        else
        {
            HandleWander();
        }

        UpdateFacing();
        anim.SetBool(IsMovingParam, rb.linearVelocity.sqrMagnitude > MovingSqrThreshold);
    }

    private void FixedUpdate()
    {
        rb.rotation = 0f;
    }

    // ── State handlers ────────────────────────────────────────────────────────

    private void HandleChase()
    {
        currentState = AIState.Chase;
        MoveToward(player.position);
    }

    private void HandleAttack()
    {
        currentState = AIState.Attack;
        Stop();
        Debug.Log("HandleAttack");

        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;

            int attackDir = 0;
            if (player != null)
            {
                Vector2 toPlayer = player.position - transform.position;
                if (Mathf.Abs(toPlayer.y) > Mathf.Abs(toPlayer.x) * 1.2f)
                    attackDir = toPlayer.y > 0 ? 1 : -1;

                pendingAttackDirection = attackDir == 1  ? Vector2.up :
                                         attackDir == -1 ? Vector2.down :
                                         (facingRight    ? Vector2.right : Vector2.left);
            }

            anim.SetInteger(AttackDirParam, attackDir);
            anim.SetTrigger(IsAttackingParam);

            _hitFired = false;
            Invoke(nameof(OnEnemyAttackHit), 0.3f);
        }
    }

    // Called by Invoke or Animation Events on the impact frame.
    // The _hitFired guard ensures damage is dealt exactly once per swing.
    public void OnEnemyAttackHit()
    {
        if (_hitFired) return;
        _hitFired = true;

        AudioManager.Instance?.PlayGoblinAttack();

        Vector2 tipPos = (Vector2)transform.position + pendingAttackDirection * torchLength;
        Collider2D[] hits = Physics2D.OverlapCircleAll(tipPos, hitRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                playerHealth?.TakeDamage(attackDamage);
                break;
            }
        }
    }

    // Alias for Animation Events that use the old typo name.
    public void OnEnemyAttackHi() => OnEnemyAttackHit();

    private void HandleWander()
    {
        switch (currentState)
        {
            case AIState.Idle:
                Stop();
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                    PickWanderTarget();
                break;

            case AIState.Wander:
                if (Vector2.Distance(transform.position, wanderTarget) > 0.2f)
                {
                    MoveToward(wanderTarget);

                    _stuckTimer -= Time.deltaTime;
                    if (_stuckTimer <= 0f)
                    {
                        _stuckTimer = StuckCheckInterval;
                        if (Vector2.Distance(transform.position, _lastSampledPos) < StuckMoveThreshold)
                            PickWanderTarget();
                        else
                            _lastSampledPos = transform.position;
                    }
                }
                else
                    EnterIdle();
                break;

            default:
                EnterIdle();
                break;
        }
    }

    // ── Movement helpers ──────────────────────────────────────────────────────

    private void EnterIdle()
    {
        currentState = AIState.Idle;
        idleTimer    = Random.Range(MinIdleTime, MaxIdleTime);
        Stop();
    }

    private void PickWanderTarget()
    {
        Vector2 offset  = Random.insideUnitCircle * Random.Range(MinWanderRadius, MaxWanderRadius);
        wanderTarget    = (Vector2)transform.position + offset;
        currentState    = AIState.Wander;
        _stuckTimer     = StuckCheckInterval;
        _lastSampledPos = transform.position;
    }

    // Pick a new direction when bumping into something that isn't the player.
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (currentState == AIState.Wander && !col.gameObject.CompareTag("Player"))
            PickWanderTarget();
    }

    private void MoveToward(Vector2 target)
    {
        rb.linearVelocity = (target - (Vector2)transform.position).normalized * moveSpeed;
    }

    private void Stop()
    {
        rb.linearVelocity = Vector2.zero;
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    private void UpdateFacing()
    {
        if (rb.linearVelocity.sqrMagnitude < MovingSqrThreshold) return;

        float vx = rb.linearVelocity.x;
        if (vx > FlipDeadzone)
        {
            facingRight = true;
            sr.flipX    = false;
        }
        else if (vx < -FlipDeadzone)
        {
            facingRight = false;
            sr.flipX    = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.magenta;
        Vector2 tipPos = (Vector2)transform.position + pendingAttackDirection * torchLength;
        Gizmos.DrawWireSphere(tipPos, hitRadius);
    }
}
