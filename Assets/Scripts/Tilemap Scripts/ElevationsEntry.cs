using UnityEngine;

// When the player enters an elevated area, disables the mountain wall colliders
// and enables the boundary colliders so the player can walk on top of the elevation.
// Also bumps the sprite sorting order so the player renders above the terrain.
public class ElevationsEntry : MonoBehaviour
{
    public Collider2D[] mountainCollider;
    public Collider2D[] boudaryCollider;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        foreach (Collider2D mountain in mountainCollider)
            mountain.enabled = false;

        foreach (Collider2D boudary in boudaryCollider)
        {
            boudary.gameObject.SetActive(true);
            boudary.enabled = true;
        }

        collision.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 16;
    }
}

