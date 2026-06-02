using UnityEngine;

// Self-destructs after the Explosion animation clip finishes playing.

[RequireComponent(typeof(Animator))]
public class ExplosionEffect : MonoBehaviour
{
    private const float ClipDuration = 0.9f;

    private void Start()
    {
        Destroy(gameObject, ClipDuration);
    }
}
