using UnityEngine;

// When the player leaves an elevated area, re-enables the mountain wall colliders
// and disables the boundary colliders so the normal ground layout is restored.
// Resets the sprite sorting order back to its default value.
public class ElevationsExit : MonoBehaviour
{
    public Collider2D[] mountainCollider;
    public Collider2D[] boudaryCollider;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        foreach (Collider2D mountain in mountainCollider)
            mountain.enabled = true;

        foreach (Collider2D boudary in boudaryCollider)
        {
            boudary.enabled = false;
            boudary.gameObject.SetActive(false);
        }

        collision.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 5;
    }
}
