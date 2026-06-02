using UnityEngine;
using UnityEngine.UI;

// Drives the world-space health bar attached to an enemy.
// Subscribes to Health.OnHealthChanged to keep the fill image in sync.
[RequireComponent(typeof(Health))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Bar References")]
    [SerializeField] private Image fillImage;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnHealthChanged.AddListener(UpdateBar);
        UpdateBar(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDisable()
    {
        health.OnHealthChanged.RemoveListener(UpdateBar);
    }

    // Updates the fill amount of the bar. Called by Health.OnHealthChanged.
    private void UpdateBar(int current, int max)
    {
        if (fillImage == null || max <= 0) return;
        fillImage.fillAmount = (float)current / max;
    }
}
