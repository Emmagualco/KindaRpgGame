using UnityEngine;

// Plays a looping footstep clip while the entity is moving.
// Works for the player and goblins
[RequireComponent(typeof(Rigidbody2D))]
public class FootstepAudio : MonoBehaviour
{
    // ~0.2 units/s — below this the entity is considered still.
    private const float MovingSqrThreshold = 0.04f;

    [SerializeField] private AudioClip footstepClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.4f;

    private Rigidbody2D _rb;
    private AudioSource _audioSource;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _audioSource              = gameObject.AddComponent<AudioSource>();
        _audioSource.clip         = footstepClip;
        _audioSource.loop         = true;
        _audioSource.volume       = volume;
        _audioSource.spatialBlend = 0f;
        _audioSource.playOnAwake  = false;
    }

    private void Update()
    {
        if (_audioSource.clip == null) return;

        // Rigidbody keeps its last velocity when paused, so also check timeScale.
        bool isMoving = Time.timeScale > 0f && _rb.linearVelocity.sqrMagnitude > MovingSqrThreshold;

        if (isMoving && !_audioSource.isPlaying)
            _audioSource.Play();
        else if (!isMoving && _audioSource.isPlaying)
            _audioSource.Stop();
    }
}
