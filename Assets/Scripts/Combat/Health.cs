using UnityEngine;
using UnityEngine.Events;

// Generic health component shared by the player, enemies, and the boss.
public class Health : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 3;

    public UnityEvent<int, int> OnHealthChanged;  // (current, max)
    public UnityEvent           OnDeath;

    private int  currentHealth;
    private bool isDead;

    public int  CurrentHealth => currentHealth;
    public int  MaxHealth     => maxHealth;
    public bool IsDead        => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // Applies damage and returns true if the hit was lethal.
    public bool TakeDamage(int amount)
    {
        if (isDead) return false;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            isDead = true;
            OnDeath?.Invoke();
            return true;
        }

        return false;
    }

    // Heals by the given amount, capped at MaxHealth.
    public void Heal(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
