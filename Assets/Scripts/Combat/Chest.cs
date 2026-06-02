using UnityEngine;

// Activates the win condition when the player walks into the chest.
// The chest stays inactive until the boss is defeated.
public class Chest : MonoBehaviour
{
    private const string PlayerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(PlayerTag)) return;

        GameManager.Instance?.RegisterChestPickup();
        gameObject.SetActive(false);
    }
}
