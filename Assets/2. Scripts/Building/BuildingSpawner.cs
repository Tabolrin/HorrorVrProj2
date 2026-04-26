using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages one lane of building spawning.
/// Spawns buildings from the pool, moves them toward the player,
/// and returns them when they pass the return threshold.
/// Controlled by BuildingSpawnCoordinator.
/// </summary>
public class BuildingSpawner : MonoBehaviour
{
    [Header("Pool")]
    [Tooltip("Pool IDs to pick from at random - must match PoolConfigSO entries.")]
    [SerializeField] private string[] _poolIds;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("Return Trigger")]
    [Tooltip("Building is returned to pool when its Z position passes this transform's Z.")]
    [SerializeField] private Transform _returnThreshold;

    [SerializeField] private Side SpawnSide;

    private struct ActiveBuilding
    {
        public GameObject   go;
        public BuildingUnit unit;
    }

    private readonly List<ActiveBuilding> _active = new();

    private void Update()
    {
        if (ObjectPoolManager.Instance == null || _returnThreshold == null) return;

        float thresholdZ = _returnThreshold.position.z;
        float moveDelta  = _moveSpeed * Time.deltaTime;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ab = _active[i];
            if (ab.go == null) { _active.RemoveAt(i); continue; }

            Vector3 pos = ab.go.transform.position;
            pos.z -= moveDelta;
            ab.go.transform.position = pos;

            if (pos.z <= thresholdZ)
            {
                ObjectPoolManager.Instance.ReturnToPool(ab.go);
                _active.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Spawns the next building. Called by BuildingSpawnCoordinator on each interval.
    /// Picks a random pool ID, resets the building's enemies, and applies lane rotation.
    /// </summary>
    public void SpawnNext()
    {
        if (_poolIds == null || _poolIds.Length == 0) return;
        if (ObjectPoolManager.Instance == null) return;

        string id = _poolIds[Random.Range(0, _poolIds.Length)];
        var go = ObjectPoolManager.Instance.Spawn(id, transform.position, transform.rotation);
        if (go == null) return;

        var unit = go.GetComponent<BuildingUnit>();
        if (unit != null)
        {
            unit.SpawnSide = SpawnSide;
            unit.ResetBuilding();
        }

        switch (SpawnSide)
        {
            case Side.Left:
                if (unit != null && unit.ShouldRotateAtSpawn)
                    go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                break;
            case Side.Right:
                go.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;
        }

        _active.Add(new ActiveBuilding { go = go, unit = unit });
    }

    /// <summary>Returns all active buildings to pool.</summary>
    public void ReturnAllToPool()
    {
        if (ObjectPoolManager.Instance == null) return;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (_active[i].go != null)
                ObjectPoolManager.Instance.ReturnToPool(_active[i].go);
        _active.Clear();
    }

    private void OnDisable() => ReturnAllToPool();
}