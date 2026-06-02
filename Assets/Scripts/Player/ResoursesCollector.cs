using System;
using UnityEngine;

public enum ResourceType { Money, Meat, Wood }

// Tracks the player's resource inventory (money, meat, wood).
// Fires events whenever resources change so the UI can react.
public class ResoursesCollector : MonoBehaviour
{
    private const int MoneyBagMinValue = 3;
    private const int MoneyBagMaxValue = 10;

    private int meat  = 0;
    private int money = 0;
    private int wood  = 0;

    public event Action<int, int, int>    OnResourcesChanged;
    public event Action<ResourceType, int> OnResourceCollected;

    public int Money => money;
    public int Meat  => meat;
    public int Wood  => wood;

    // Removes the given amount of meat from the inventory.
    // Returns true if there was enough; false otherwise.
    public bool ConsumeMeat(int amount)
    {
        if (meat < amount) return false;

        meat -= amount;
        OnResourcesChanged?.Invoke(money, meat, wood);
        OnResourceCollected?.Invoke(ResourceType.Meat, meat);
        return true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Meat"))
        {
            meat++;
            Destroy(collision.gameObject);
            OnResourcesChanged?.Invoke(money, meat, wood);
            OnResourceCollected?.Invoke(ResourceType.Meat, meat);
        }
        else if (collision.gameObject.CompareTag("MoneyBag"))
        {
            int value = UnityEngine.Random.Range(MoneyBagMinValue, MoneyBagMaxValue + 1);
            money += value;
            Destroy(collision.gameObject);
            OnResourcesChanged?.Invoke(money, meat, wood);
            OnResourceCollected?.Invoke(ResourceType.Money, money);
        }
        else if (collision.gameObject.CompareTag("Wood"))
        {
            wood++;
            Destroy(collision.gameObject);
            OnResourcesChanged?.Invoke(money, meat, wood);
            OnResourceCollected?.Invoke(ResourceType.Wood, wood);
        }
    }
}
