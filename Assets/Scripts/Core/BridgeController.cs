using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Removes the blocking wall and opens the bridge when all enemies are defeated.
public class BridgeController : MonoBehaviour
{
    private const float NotificationDuration = 5f;

    [Header("Bridge Objects")]
    [Tooltip("Invisible wall that blocks the bridge entrance.")]
    [SerializeField] private GameObject blockingWall;

    [Tooltip("Optional visual that plays when the bridge opens.")]
    [SerializeField] private GameObject bridgeUnlockEffect;

    [Header("Events")]
    public UnityEvent OnBridgeUnlocked;

    private bool isUnlocked;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Start() runs after all Awake() calls, so GameManager.Instance is safe here.
        if (GameManager.Instance != null)
            GameManager.Instance.OnAllEnemiesDefeated.AddListener(UnlockBridge);

        // Handle the case where enemies were already defeated before this runs.
        if (GameManager.Instance != null && GameManager.Instance.AreAllEnemiesDefeated())
            UnlockBridge();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnAllEnemiesDefeated.RemoveListener(UnlockBridge);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void UnlockBridge()
    {
        if (isUnlocked) return;
        isUnlocked = true;

        AudioManager.Instance?.PlayBridgeAppear();

        if (blockingWall != null)
            blockingWall.SetActive(false);

        if (bridgeUnlockEffect != null)
        {
            bridgeUnlockEffect.SetActive(true);
            StartCoroutine(DeactivateEffectAfterDelay(bridgeUnlockEffect, NotificationDuration));
        }

        OnBridgeUnlocked?.Invoke();
        Debug.Log("[BridgeController] Bridge unlocked.");
    }

    private static IEnumerator DeactivateEffectAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            obj.SetActive(false);
    }
}
