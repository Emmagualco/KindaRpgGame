using UnityEngine;
using UnityEngine.InputSystem;

// Handles player melee attacks.
// Left click  → light attack (fast, low damage), alternates with a heavy swing.
// Right click → heavy attack (slow, high damage), always heavy.
// Hit detection fires from Animation Events on the impact frame.
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAttack : MonoBehaviour
{
    private const string AnimatorAttack    = "Attack";
    private const string AnimatorAttack2   = "Attack2";
    private const string AnimatorAttackDir = "AttackDir";
    private const string EnemyTag          = "Enemy";

    [Header("Attack 1 — Left Click (fast)")]
    [SerializeField] private int   damage           = 1;
    [SerializeField] private float attackRange      = 0.6f;
    [SerializeField] private float attackCooldown   = 0.4f;
    [SerializeField] private float knockbackForce   = 5f;
    [SerializeField] private float moveLockDuration = 0.25f;

    [Header("Attack 2 — Right Click (strong)")]
    [SerializeField] private int   damage2           = 3;
    [SerializeField] private float attackRange2      = 0.7f;
    [SerializeField] private float attackCooldown2   = 1.0f;
    [SerializeField] private float knockbackForce2   = 10f;
    [SerializeField] private float moveLockDuration2 = 0.45f;

    // Hit data stored at attack start, consumed by the Animation Event.
    private int     pendingDamage;
    private float   pendingRange;
    private float   pendingKnockbackForce;
    private Vector2 pendingDirection;

    // Alternates the left-click combo between light and heavy.
    private bool _nextIsHeavy;

    private SpriteRenderer spriteRenderer;
    private Animator       animator;
    private PlayerMovement movement;

    private float cooldownTimer;
    private float cooldownTimer2;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator       = GetComponent<Animator>();
        movement       = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (cooldownTimer  > 0f) cooldownTimer  -= Time.deltaTime;
        if (cooldownTimer2 > 0f) cooldownTimer2 -= Time.deltaTime;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            TryAttack2();
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed || cooldownTimer > 0f) return;

        cooldownTimer = attackCooldown;

        if (animator != null)
        {
            animator.SetInteger(AnimatorAttackDir, GetAttackDir());
            animator.SetTrigger(_nextIsHeavy ? AnimatorAttack2 : AnimatorAttack);
        }

        if (_nextIsHeavy)
        {
            movement?.LockMovement(moveLockDuration2);
            StorePendingHit(damage2, attackRange2, knockbackForce2);
        }
        else
        {
            movement?.LockMovement(moveLockDuration);
            StorePendingHit(damage, attackRange, knockbackForce);
        }

        _nextIsHeavy = !_nextIsHeavy;
    }

    private void TryAttack2()
    {
        if (cooldownTimer2 > 0f) return;

        cooldownTimer2 = attackCooldown2;

        if (animator != null)
        {
            animator.SetInteger(AnimatorAttackDir, GetAttackDir());
            animator.SetTrigger(AnimatorAttack2);
        }

        movement?.LockMovement(moveLockDuration2);
        StorePendingHit(damage2, attackRange2, knockbackForce2);
    }

    // Stores hit data at attack start so direction stays correct even if the player moves before impact.
    private void StorePendingHit(int dmg, float range, float kbForce)
    {
        pendingDamage         = dmg;
        pendingRange          = range;
        pendingKnockbackForce = kbForce;
        pendingDirection      = GetAttackDirection();
    }

    // Called by Animation Events on the impact frame.
    public void OnAttackHit()
    {
        AudioManager.Instance?.PlayPlayerAttack();
        PerformHit(pendingDamage, pendingRange, pendingKnockbackForce, pendingDirection);
    }

    // Returns 1 (up), -1 (down), or 0 (horizontal) based on last movement direction.
    private int GetAttackDir()
    {
        if (movement == null) return 0;
        float y = movement.LastDirection.y;
        if (y >  0.5f) return  1;
        if (y < -0.5f) return -1;
        return 0;
    }

    private Vector2 GetAttackDirection()
    {
        if (movement != null)
        {
            Vector2 dir = movement.LastDirection;
            if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
                return dir.y > 0f ? Vector2.up : Vector2.down;
        }
        return spriteRenderer.flipX ? Vector2.left : Vector2.right;
    }

    private void PerformHit(int dmg, float range, float kbForce, Vector2 direction)
    {
        Vector2 origin = (Vector2)transform.position + direction * (range * 0.5f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range * 0.5f);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag(EnemyTag)) continue;

            hit.GetComponentInParent<Health>()?.TakeDamage(dmg);
            hit.GetComponentInParent<Knockback>()?.Apply(direction, kbForce);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null) return;
        Vector2 direction = GetAttackDirection();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + direction * (attackRange  * 0.5f), attackRange  * 0.5f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere((Vector2)transform.position + direction * (attackRange2 * 0.5f), attackRange2 * 0.5f);
    }
}
