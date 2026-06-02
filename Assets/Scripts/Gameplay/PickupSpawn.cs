using System.Collections;
using UnityEngine;

// Disables all Collider2D components on this pickup until the spawn animation
// finishes, preventing collection before the item is fully visible.

public class PickupSpawn : MonoBehaviour
{
    private const float FallbackDelay = 0.5f;

    private void Start()
    {
        StartCoroutine(EnableColliderAfterSpawn());
    }

    private IEnumerator EnableColliderAfterSpawn()
    {
        // Disable colliders immediately so the pickup can't be grabbed mid-animation.
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
            col.enabled = false;

        // Wait one frame so the Animator has initialised its first state.
        yield return null;

        float delay = FallbackDelay;
        if (TryGetComponent(out Animator anim) && anim.isActiveAndEnabled)
            delay = anim.GetCurrentAnimatorStateInfo(0).length;

        yield return new WaitForSeconds(delay);

        foreach (Collider2D col in colliders)
            col.enabled = true;
    }
}
