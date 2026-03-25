using UnityEngine;

/// <summary>
/// Attach to each building prefab root.
/// Holds refs to all Enemy children and re-enables them when the building
/// is returned to the pool and re-spawned.
/// </summary>
public class BuildingUnit : MonoBehaviour
{
    // Populated automatically in Awake so the prefab needs no manual wiring.
    private Enemy[] _enemies;

    private void Awake()
    {
        _enemies = GetComponentsInChildren<Enemy>(includeInactive: true);
    }

    /// <summary>
    /// Called by BuildingSpawner after pulling from pool, before activation.
    /// Re-enables all enemies so they're alive for the next run.
    /// </summary>
    public void ResetBuilding()
    {
        foreach (var e in _enemies)
        {
            if (e == null) continue;
            e.gameObject.SetActive(true);
            // Enemy.OnEnable handles the full stat reset
        }
    }
}
