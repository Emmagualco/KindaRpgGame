using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Handles the player death sequence: animation, input disable, defeat screen.
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
{
    private const string AnimatorIsDead = "IsDead";

    [Tooltip("Seconds to wait after death before showing the defeat screen.")]
    [SerializeField] private float deathScreenDelay = 1.8f;

    private Health      health;
    private Animator    animator;
    private Rigidbody2D rb;

    private void Awake()
    {
        health   = GetComponent<Health>();
        animator = GetComponent<Animator>();
        rb       = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()  => health.OnDeath.AddListener(HandleDeath);
    private void OnDisable() => health.OnDeath.RemoveListener(HandleDeath);

    private void HandleDeath()
    {
        AudioManager.Instance?.PlayPlayerDeath();

        rb.linearVelocity = Vector2.zero;
        rb.simulated      = false;

        animator.SetBool(AnimatorIsDead, true);

        if (TryGetComponent(out PlayerMovement movement)) movement.enabled = false;
        if (TryGetComponent(out PlayerAttack attack))     attack.enabled   = false;
        if (TryGetComponent(out PlayerInput playerInput)) playerInput.enabled = false;

        StartCoroutine(ShowDefeatScreenAfterDelay());
    }

    private IEnumerator ShowDefeatScreenAfterDelay()
    {
        yield return new WaitForSeconds(deathScreenDelay);
        GameScreenManager.Instance?.ShowDefeatScreen();
    }
}
