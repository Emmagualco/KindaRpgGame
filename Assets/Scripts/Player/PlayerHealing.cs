using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player consume meat from their inventory to restore health.
// Press F when the inventory contains at least one meat and the player is not at full health.
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(ResoursesCollector))]
public class PlayerHealing : MonoBehaviour
{
    private const int MeatPerHeal       = 1;
    private const int HpRestoredPerMeat = 1;

    [Header("Settings")]
    [Tooltip("Amount of HP restored per meat consumed.")]
    [SerializeField] private int healAmount = HpRestoredPerMeat;

    private Health             health;
    private ResoursesCollector collector;

    private void Awake()
    {
        health    = GetComponent<Health>();
        collector = GetComponent<ResoursesCollector>();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame) return;

        TryHeal();
    }

    // Attempts to consume one meat from the inventory and heal the player.
    // Flashes the meat entry if there is none left.
    // Does nothing if the player is dead or already at full health.
    private void TryHeal()
    {
        if (health.IsDead)                            return;
        if (health.CurrentHealth >= health.MaxHealth) return;

        if (!collector.ConsumeMeat(MeatPerHeal))
        {
            UIManager.Instance?.FlashInsufficient(ResourceType.Meat);
            return;
        }

        health.Heal(healAmount);
    }
}
