using UnityEngine;

// Attached at runtime to spawned enemies to notify their origin SpawnPoint
// when they are destroyed, so the alive-count stays accurate.
public class SpawnPointTracker : MonoBehaviour
{
    private SpawnPoint _origin;

    // Links this tracker to its parent spawn point.
    public void Initialize(SpawnPoint origin) => _origin = origin;

    private void OnDestroy()
    {
        if (_origin != null)
            _origin.OnEnemyDestroyed();
    }
}
