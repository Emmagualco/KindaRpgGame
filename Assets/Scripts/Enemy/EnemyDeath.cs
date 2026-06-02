using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Plays the death animation and destroys the enemy when health reaches zero.
[RequireComponent(typeof(Health))]
public class EnemyDeath : MonoBehaviour
{
    private const string DeathStateName      = "Dead";
    private const float  FallbackDestroyDelay = 0.6f;

    [Header("Boss")]
    [SerializeField] private bool isBoss = false;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDeath.AddListener(HandleDeath);
    }

    private void HandleDeath()
    {
        AudioManager.Instance?.PlayGoblinDeath();

        if (TryGetComponent(out NavMeshAgent agent))
            agent.isStopped = true;

        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic    = true;
        }

        if (TryGetComponent(out Enemy enemyAI)) enemyAI.enabled = false;
        if (TryGetComponent(out TntGoblin tntAI)) tntAI.enabled = false;
        if (TryGetComponent(out Boss bossAI)) bossAI.enabled    = false;

        if (isBoss)
            GameManager.Instance?.RegisterBossKill();
        else
            GameManager.Instance?.RegisterEnemyKill();

        StartCoroutine(DeathRoutine());
    }

    // Plays the "Dead" animation state if it exists, then destroys the GameObject.
    private IEnumerator DeathRoutine()
    {
        float delay = FallbackDestroyDelay;

        if (TryGetComponent(out Animator animator) && animator.isActiveAndEnabled)
        {
            animator.SetBool("IsMoving", false);

            int stateHash = Animator.StringToHash(DeathStateName);
            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
                yield return null;
                delay = animator.GetCurrentAnimatorStateInfo(0).length;
            }
        }

        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
