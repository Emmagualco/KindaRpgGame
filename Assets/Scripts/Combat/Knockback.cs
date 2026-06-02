using UnityEngine;

// Applies a brief physics push when an enemy is hit.
// Velocity is set directly in FixedUpdate every physics step so no AI script
// running in Update can override it. Enemy AI checks IsActive
// to skip their own movement while the push is ongoing.
[RequireComponent(typeof(Rigidbody2D))]
public class Knockback : MonoBehaviour
{
    private const float DefaultDuration = 0.18f;

    [Tooltip("Seconds the push lasts.")]
    [SerializeField] private float duration = DefaultDuration;

    private Rigidbody2D rb;
    private Vector2     pushVelocity;
    private float       remainingTime;
    private float       originalDamping;

    // True while the push is still being applied.
    public bool IsActive => remainingTime > 0f;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    // Starts a knockback push in the given direction with the specified force.
    // Calling this again while active immediately overrides the previous push.
    public void Apply(Vector2 direction, float force)
    {
        pushVelocity    = direction.normalized * force;
        originalDamping = rb.linearDamping;
        remainingTime   = duration;

        rb.linearDamping  = 0f;
        // Apply immediately — removes the 1-physics-frame gap of AddForce.
        rb.linearVelocity = pushVelocity;
    }

    private void FixedUpdate()
    {
        if (!IsActive) return;

        remainingTime -= Time.fixedDeltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime     = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.linearDamping  = originalDamping;
        }
        else
        {
            // Re-apply every physics step so Update-based AI cannot override it.
            rb.linearVelocity = pushVelocity;
        }
    }
}
