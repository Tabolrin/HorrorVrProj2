using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool manager. Pools are defined in a PoolConfigSO asset
/// and built on Awake. Supports pre-warming, max active limits, and max scene limits.
/// All pooled objects are tagged with a PooledInstance component for safe return routing.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [SerializeField] private PoolConfigSO config;
    [SerializeField] private bool buildOnAwake = true;

    private readonly Dictionary<string, PoolRuntime> _pools         = new();
    private readonly Dictionary<int, PooledInstance> _instanceCache = new();
    private Transform _root;

    private class PoolRuntime
    {
        public readonly Queue<GameObject> inactive    = new();
        public readonly HashSet<int>      inactiveIds = new();
        public GameObject prefab;
        public int maxInScene;
        public int maxActive;
        public int totalCreated;
        public int activeCount;
    }

    private class PooledInstance : MonoBehaviour
    {
        public string poolId;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (buildOnAwake) BuildPools(config);
    }

    /// <summary>Clears and rebuilds all pools from the provided config.</summary>
    public void BuildPools(PoolConfigSO cfg)
    {
        config = cfg;
        _pools.Clear();
        _instanceCache.Clear();

        _root = config != null && config.poolRoot != null
            ? config.poolRoot
            : new GameObject("[ObjectPoolRoot]").transform;

        if (config == null) { Debug.LogWarning("ObjectPoolManager: No config assigned."); return; }

        foreach (var entry in config.entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.prefab == null)
            {
                Debug.LogWarning("ObjectPoolManager: Skipping invalid pool entry (missing id or prefab).");
                continue;
            }

            if (_pools.ContainsKey(entry.id))
            {
                Debug.LogWarning($"ObjectPoolManager: Duplicate pool id '{entry.id}' - skipping.");
                continue;
            }

            var rt = new PoolRuntime
            {
                prefab       = entry.prefab,
                maxInScene   = Mathf.Max(0, entry.maxInScene),
                maxActive    = Mathf.Max(0, entry.maxActive),
                totalCreated = 0,
                activeCount  = 0
            };

            _pools.Add(entry.id, rt);

            int count = Mathf.Clamp(entry.prewarmCount, 0,
                rt.maxInScene == 0 ? entry.prewarmCount : rt.maxInScene);

            for (int i = 0; i < count; i++)
            {
                var go = CreateInstance(entry.id, rt);
                if (go == null) break;
                EnqueueInactive(go, rt);
            }
        }
    }

    /// <summary>
    /// Retrieves an inactive instance from the pool or creates a new one if within limits.
    /// Returns null if the pool is at capacity.
    /// </summary>
    public GameObject Spawn(string id, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!_pools.TryGetValue(id, out var pool))
        {
            Debug.LogError($"ObjectPoolManager: No pool found with id '{id}'.");
            return null;
        }

        if (pool.maxActive > 0 && pool.activeCount >= pool.maxActive) return null;

        GameObject go = null;
        while (pool.inactive.Count > 0 && go == null)
        {
            go = pool.inactive.Dequeue();
            if (go != null) pool.inactiveIds.Remove(go.GetInstanceID());
        }

        if (go == null)
        {
            if (pool.maxInScene > 0 && pool.totalCreated >= pool.maxInScene) return null;
            go = CreateInstance(id, pool);
            if (go == null) return null;
        }

        pool.activeCount++;
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    /// <summary>Deactivates and re-queues a pooled object. Safe to call with null.</summary>
    public void ReturnToPool(GameObject go)
    {
        if (go == null) return;

        if (!_instanceCache.TryGetValue(go.GetInstanceID(), out var tag))
        {
            Destroy(go);
            return;
        }

        if (string.IsNullOrWhiteSpace(tag.poolId) || !_pools.TryGetValue(tag.poolId, out var pool))
        {
            Destroy(go);
            return;
        }

        if (pool.inactiveIds.Contains(go.GetInstanceID()))
        {
            Debug.LogWarning($"[ObjectPoolManager] ReturnToPool called twice on '{go.name}'. Ignoring.");
            return;
        }

        pool.activeCount = Mathf.Max(0, pool.activeCount - 1);
        go.SetActive(false);
        go.transform.SetParent(_root, worldPositionStays: false);
        EnqueueInactive(go, pool);
    }

    private void EnqueueInactive(GameObject go, PoolRuntime pool)
    {
        pool.inactiveIds.Add(go.GetInstanceID());
        pool.inactive.Enqueue(go);
    }

    private GameObject CreateInstance(string id, PoolRuntime pool)
    {
        if (pool.prefab == null) return null;

        var go  = Instantiate(pool.prefab, _root);
        go.name = $"{pool.prefab.name} (Pooled:{id})";

        var tag = go.GetComponent<PooledInstance>() ?? go.AddComponent<PooledInstance>();
        tag.poolId = id;
        _instanceCache[go.GetInstanceID()] = tag;

        pool.totalCreated++;
        go.SetActive(false);
        return go;
    }
}