using UnityEngine;

// Drops loot when the enemy dies. Always guarantees at least one item.
[RequireComponent(typeof(Health))]
public class LootDrop : MonoBehaviour
{
    private const float ScatterRadius = 0.4f;

    [Header("Loot Prefabs")]
    [SerializeField] private GameObject meatPrefab;
    [SerializeField] private GameObject moneyBagPrefab;

    [Header("Drop Chances (0 = never, 1 = always)")]
    [Range(0f, 1f)] [SerializeField] private float meatDropChance  = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float moneyDropChance = 0.7f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()  => health.OnDeath.AddListener(DropLoot);
    private void OnDisable() => health.OnDeath.RemoveListener(DropLoot);

    private void DropLoot()
    {
        Vector3 origin = transform.position;

        bool droppedMeat  = false;
        bool droppedMoney = false;

        if (meatPrefab != null && Random.value <= meatDropChance)
        {
            Instantiate(meatPrefab, origin + RandomOffset(), Quaternion.identity);
            droppedMeat = true;
        }

        if (moneyBagPrefab != null && Random.value <= moneyDropChance)
        {
            Instantiate(moneyBagPrefab, origin + RandomOffset(), Quaternion.identity);
            droppedMoney = true;
        }

        // If both rolls failed, always drop at least money (or meat as fallback).
        if (!droppedMeat && !droppedMoney)
        {
            if (moneyBagPrefab != null)
                Instantiate(moneyBagPrefab, origin + RandomOffset(), Quaternion.identity);
            else if (meatPrefab != null)
                Instantiate(meatPrefab, origin + RandomOffset(), Quaternion.identity);
        }
    }

    private static Vector3 RandomOffset()
    {
        return new Vector3(
            Random.Range(-ScatterRadius, ScatterRadius),
            Random.Range(-ScatterRadius, ScatterRadius),
            0f);
    }
}
