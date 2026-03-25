using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place TWO of these in the scene (left lane, right lane).
/// Wire both to a single BuildingSpawnCoordinator which calls SpawnNext().
/// Each spawner manages its own active building list and pool return.
/// </summary>
public class BuildingSpawner : MonoBehaviour
{
    [Header("Pool IDs (must match PoolConfigSO entries)")]
    [Tooltip("All building pool IDs this spawner can pick from at random.")]
    [SerializeField] private string[] _poolIds;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("Return Trigger")]
    [Tooltip("When a building's Z passes this transform's Z it is returned to pool.")]
    [SerializeField] private Transform _returnThreshold;

    // ── Runtime ───────────────────────────────────────────────────────────
    private struct ActiveBuilding
    {
        public GameObject go;
        public BuildingUnit unit;
    }

    private readonly List<ActiveBuilding> _active = new();

    private void Update()
    {
        if (ObjectPoolManager.Instance == null || _returnThreshold == null) return;

        float thresholdZ = _returnThreshold.position.z;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ab = _active[i];
            if (ab.go == null) { _active.RemoveAt(i); continue; }

            ab.go.transform.position += Vector3.back * (_moveSpeed * Time.deltaTime);

            if (ab.go.transform.position.z <= thresholdZ)
            {
                ObjectPoolManager.Instance.ReturnToPool(ab.go);
                _active.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Called by BuildingSpawnCoordinator. Picks a random pool ID and spawns.
    /// </summary>
    public void SpawnNext()
    {
        if (_poolIds == null || _poolIds.Length == 0) return;
        if (ObjectPoolManager.Instance == null) return;

        string id = _poolIds[Random.Range(0, _poolIds.Length)];
        var go = ObjectPoolManager.Instance.Spawn(id, transform.position, transform.rotation);
        if (go == null) return;

        var unit = go.GetComponent<BuildingUnit>();
        unit?.ResetBuilding();

        _active.Add(new ActiveBuilding { go = go, unit = unit });
    }

    private void OnDisable()
    {
        if (ObjectPoolManager.Instance == null) return;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (_active[i].go != null)
                ObjectPoolManager.Instance.ReturnToPool(_active[i].go);
        _active.Clear();
    }
}
