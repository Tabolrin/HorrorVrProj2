using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [SerializeField] private PoolConfigSO config;
    [SerializeField] private bool buildOnAwake = true;

    private readonly Dictionary<string, PoolRuntime> _pools = new();
    private Transform _root;

    private class PoolRuntime
    {
        public readonly Queue<GameObject> inactive = new();
        public GameObject prefab;

        public int maxInScene;
        public int maxActive; // 0 = unlimited
        public int totalCreated;
        public int activeCount;
    }

    // Helper component so instances know which pool they belong to 
    private class PooledInstance : MonoBehaviour
    {
        public string poolId;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (buildOnAwake)
            BuildPools(config);
    }

    public void BuildPools(PoolConfigSO cfg)
    {
        config = cfg;

        _pools.Clear();

        _root = (config != null && config.poolRoot != null)
            ? config.poolRoot
            : new GameObject("[ObjectPoolRoot]").transform;

        if (config == null)
        {
            Debug.LogWarning("ObjectPoolManager: No config assigned.");
            return;
        }

        foreach (var entry in config.entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.prefab == null)
            {
                Debug.LogWarning("ObjectPoolManager: Skipping invalid pool entry (missing id/prefab).");
                continue;
            }

            if (_pools.ContainsKey(entry.id))
            {
                Debug.LogWarning($"ObjectPoolManager: Duplicate pool id '{entry.id}' - skipping duplicate.");
                continue;
            }

            var rt = new PoolRuntime
            {
                prefab = entry.prefab,
                maxInScene = Mathf.Max(0, entry.maxInScene),
                maxActive = Mathf.Max(0, entry.maxActive),
                totalCreated = 0,
                activeCount = 0
            };

            _pools.Add(entry.id, rt);

            // Prewarm
            int count = Mathf.Clamp(entry.prewarmCount, 0, rt.maxInScene == 0 ? entry.prewarmCount : rt.maxInScene);
            for (int i = 0; i < count; i++)
            {
                var go = CreateInstance(entry.id, rt);
                if (go == null) break;
                ReturnToPool(go);
            }
        }
    }

    public GameObject Spawn(string id, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!_pools.TryGetValue(id, out var pool))
        {
            Debug.LogError($"ObjectPoolManager: No pool found with id '{id}'.");
            return null;
        }

        // Enforce maxActive if set
        if (pool.maxActive > 0 && pool.activeCount >= pool.maxActive)
            return null;

        GameObject go = null;

        // Reuse inactive if possible
        while (pool.inactive.Count > 0 && go == null)
            go = pool.inactive.Dequeue();

        // If none available, create if allowed
        if (go == null)
        {
            if (pool.maxInScene > 0 && pool.totalCreated >= pool.maxInScene)
                return null;

            go = CreateInstance(id, pool);
            if (go == null) return null;
        }

        pool.activeCount++;

        var t = go.transform;
        t.SetParent(parent, worldPositionStays: false);
        t.SetPositionAndRotation(position, rotation);

        go.SetActive(true);
        return go;
    }

    public void ReturnToPool(GameObject go)
    {
        if (go == null) return;

        var tag = go.GetComponent<PooledInstance>();
        if (tag == null || string.IsNullOrWhiteSpace(tag.poolId))
        {
            // Not a pooled object - destroy to avoid leaking junk.
            Destroy(go);
            return;
        }

        if (!_pools.TryGetValue(tag.poolId, out var pool))
        {
            Destroy(go);
            return;
        }

        // Decrease active count safely (avoid going negative if user double-returns)
        pool.activeCount = Mathf.Max(0, pool.activeCount - 1);

        go.SetActive(false);
        
        go.transform.SetParent(_root, worldPositionStays: false);

        pool.inactive.Enqueue(go);
    }

    private GameObject CreateInstance(string id, PoolRuntime pool)
    {
        if (pool.prefab == null) return null;

        var go = Instantiate(pool.prefab, _root);
        go.name = $"{pool.prefab.name} (Pooled:{id})";

        var tag = go.GetComponent<PooledInstance>();
        if (tag == null) tag = go.AddComponent<PooledInstance>();
        tag.poolId = id;

        pool.totalCreated++;
        go.SetActive(false);
        return go;
    }
}