using System.Collections;
using UnityEngine;

// Singleton that controls all music and sound effects.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music — Title Screen")]
    [SerializeField] private AudioClip titleMusic;

    [Header("Music — Gameplay")]
    [SerializeField] private AudioClip worldIntro;
    [SerializeField] private AudioClip worldLoop;

    [Header("Music — Defeat Screen")]
    [SerializeField] private AudioClip defeatMusic;

    [Header("Music — Victory Screen")]
    [SerializeField] private AudioClip victoryIntro;
    [SerializeField] private AudioClip victoryLoop;

    [Header("SFX — Player")]
    [SerializeField] private AudioClip playerAttackSFX;
    [SerializeField] private AudioClip playerDeathSFX;

    [Header("SFX — Goblin")]
    [SerializeField] private AudioClip goblinAttackSFX;
    [SerializeField] private AudioClip goblinDeathSFX;
    [SerializeField] private AudioClip goblinLaughSFX;

    [Header("SFX — Boss")]
    [SerializeField] private AudioClip bossLaughSFX;
    [SerializeField] private AudioClip dynamiteExplosionSFX;
    [SerializeField] private AudioClip barrelExplosionSFX;
    [Tooltip("Volume multiplier for the barrel explosion. >1 = louder.")]
    [Range(1f, 3f)] [SerializeField] private float barrelExplosionVolumeMult = 2f;
    [Tooltip("Pitch for the barrel explosion. <1 = deeper.")]
    [Range(0.5f, 1f)] [SerializeField] private float barrelExplosionPitch = 0.72f;

    [Header("SFX — World")]
    [SerializeField] private AudioClip bridgeAppearSFX;

    [Header("Timing")]
    [Tooltip("Delay in seconds before the goblin laughs after the player dies.")]
    [SerializeField] private float goblinLaughDelay = 2.5f;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume   = 1f;

    [Header("Pitch")]
    [Tooltip("Title music speed. 1 = normal, >1 = faster.")]
    [Range(0.5f, 2f)] [SerializeField] private float titleMusicPitch = 1.15f;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private Coroutine   _musicRoutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _musicSource = CreateSource("Music", loop: true,  musicVolume);
        _sfxSource   = CreateSource("SFX",   loop: false, sfxVolume);
    }

    private AudioSource CreateSource(string label, bool loop, float volume)
    {
        var go  = new GameObject(label);
        go.transform.SetParent(transform);
        var src         = go.AddComponent<AudioSource>();
        src.loop        = loop;
        src.volume      = volume;
        src.playOnAwake = false;
        return src;
    }

    // ── Music ─────────────────────────────────────────────────────────────────

    public void PlayTitleMusic()
    {
        _musicSource.pitch = titleMusicPitch;
        StartMusicLoop(titleMusic);
    }

    public void PlayWorldMusic()
    {
        _musicSource.pitch = 1f;
        StartMusicSequence(worldIntro, worldLoop);
    }

    public void PlayDefeatMusic()
    {
        _musicSource.pitch = 1f;
        StartMusicLoop(defeatMusic);
    }

    public void PlayVictoryMusic()
    {
        _musicSource.pitch = 1f;
        StartMusicSequence(victoryIntro, victoryLoop);
    }

    public void StopMusic()
    {
        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        _musicSource.Stop();
    }

    private void StartMusicLoop(AudioClip clip)
    {
        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        if (clip == null) return;
        _musicSource.loop = true;
        _musicSource.clip = clip;
        _musicSource.Play();
    }

    private void StartMusicSequence(AudioClip intro, AudioClip loop)
    {
        if (_musicRoutine != null) StopCoroutine(_musicRoutine);
        _musicRoutine = StartCoroutine(PlayIntroThenLoop(intro, loop));
    }

    // Plays the intro clip once, then switches to the looping track.
    // Uses WaitForSecondsRealtime so it works even when timeScale = 0.
    private IEnumerator PlayIntroThenLoop(AudioClip intro, AudioClip loop)
    {
        if (intro != null)
        {
            _musicSource.loop = false;
            _musicSource.clip = intro;
            _musicSource.Play();
            yield return new WaitForSecondsRealtime(intro.length);
        }

        if (loop != null)
        {
            _musicSource.loop = true;
            _musicSource.clip = loop;
            _musicSource.Play();
        }
    }

    // ── SFX ───────────────────────────────────────────────────────────────────

    public void PlayPlayerAttack() => PlaySFX(playerAttackSFX);

    // Plays the death sound then queues a delayed goblin laugh.
    public void PlayPlayerDeath()
    {
        PlaySFX(playerDeathSFX);
        StartCoroutine(DelayedGoblinLaugh());
    }

    public void PlayGoblinAttack()      => PlaySFX(goblinAttackSFX);
    public void PlayGoblinDeath()       => PlaySFX(goblinDeathSFX);
    public void PlayBridgeAppear()      => PlaySFX(bridgeAppearSFX);
    public void PlayBossLaugh()         => PlaySFX(bossLaughSFX);
    public void PlayDynamiteExplosion() => PlaySFX(dynamiteExplosionSFX);

    // Plays the barrel explosion at a lower pitch and higher volume than dynamite.
    public void PlayBarrelExplosion()
    {
        if (barrelExplosionSFX == null || _sfxSource == null) return;
        float prevPitch  = _sfxSource.pitch;
        _sfxSource.pitch = barrelExplosionPitch;
        _sfxSource.PlayOneShot(barrelExplosionSFX, sfxVolume * barrelExplosionVolumeMult);
        _sfxSource.pitch = prevPitch;
    }

    private IEnumerator DelayedGoblinLaugh()
    {
        yield return new WaitForSecondsRealtime(goblinLaughDelay);
        PlaySFX(goblinLaughSFX);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
