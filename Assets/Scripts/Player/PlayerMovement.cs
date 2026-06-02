using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    private const string AnimatorIsMoving = "IsMoving";

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    private Rigidbody2D    rb;
    private SpriteRenderer spriteRenderer;
    private Animator       animator;

    private Vector2 movementInput;
    private float   movementLockTimer;
    private bool    newInputDuringLock;

    // Last non-zero normalized input direction. Defaults to right.
    public Vector2 LastDirection { get; private set; } = Vector2.right;

    // Freezes movement for the given number of seconds.
    // Cancelled the moment the player sends any new directional input.
    public void LockMovement(float duration)
    {
        movementLockTimer  = duration;
        newInputDuringLock = false;
    }

    private void Awake()
    {
        rb             = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator       = GetComponent<Animator>();

        // Continuous detection prevents the player from tunneling through enemies
        // when using MovePosition at normal movement speeds.
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        rb.gravityScale           = 0f;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
    }

    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();

        if (movementInput != Vector2.zero)
        {
            LastDirection = movementInput.normalized;
            // A new directional press during the lock cancels it.
            if (movementLockTimer > 0f)
                newInputDuringLock = true;
        }
    }

    private void FixedUpdate()
    {
        // Tick the attack movement lock.
        if (movementLockTimer > 0f)
        {
            if (newInputDuringLock)
            {
                movementLockTimer  = 0f;
                newInputDuringLock = false;
            }
            else
            {
                movementLockTimer -= Time.fixedDeltaTime;
            }
        }

        bool locked = movementLockTimer > 0f;

        // Velocity-based movement so the physics engine resolves contacts
        // with enemy Rigidbody2Ds correctly (MovePosition teleports the body
        // and bypasses velocity-based contact resolution).
        rb.linearVelocity = (movementInput != Vector2.zero && !locked)
            ? movementInput * speed
            : Vector2.zero;
    }

    private void Update()
    {
        UpdateFacingDirection();
        UpdateAnimator();
    }

    private void UpdateFacingDirection()
    {
        if (movementInput.x > 0f)
            spriteRenderer.flipX = false;
        else if (movementInput.x < 0f)
            spriteRenderer.flipX = true;
    }

    private void UpdateAnimator()
    {
        animator.SetBool(AnimatorIsMoving, movementInput != Vector2.zero && movementLockTimer <= 0f);
    }
}
