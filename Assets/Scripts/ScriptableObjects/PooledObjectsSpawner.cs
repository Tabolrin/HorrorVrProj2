using System.Collections.Generic;
using UnityEngine;

public class PooledObjectsSpawner : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private string poolId;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 0.25f;
    [SerializeField] private int maxAliveFromThisSpawner = 30;

    [Header("Spawn Area (local space)")]
    [SerializeField] private Vector3 randomOffsetMin = new Vector3(-2f, 0f, 0f);
    [SerializeField] private Vector3 randomOffsetMax = new Vector3( 2f, 0f, 0f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private Transform returnWhenBehindThis; 
    [Tooltip("If true: return when object's Z <= target Z. If false: return when object's Z >= target Z.")]
    [SerializeField] private bool targetIsBehindSpawner = true;

    private float _timer;

    private struct ActiveItem
    {
        public GameObject go;
        public float speed;
    }

    private readonly List<ActiveItem> _active = new();

    private void Update()
    {
        if (ObjectPoolManager.Instance == null) return;
        if (returnWhenBehindThis == null) return;

        // Spawn
        _timer += Time.deltaTime;
        while (_timer >= spawnInterval)
        {
            _timer -= spawnInterval;

            if (_active.Count >= maxAliveFromThisSpawner) break;

            Vector3 localOffset = new Vector3(
                Random.Range(randomOffsetMin.x, randomOffsetMax.x),
                Random.Range(randomOffsetMin.y, randomOffsetMax.y),
                Random.Range(randomOffsetMin.z, randomOffsetMax.z)
            );

            Vector3 spawnPos = transform.TransformPoint(localOffset);

            var go = ObjectPoolManager.Instance.Spawn(poolId, spawnPos, transform.rotation);
            if (go == null) break;

            _active.Add(new ActiveItem
            {
                go = go,
                speed = moveSpeed
            });
        }

        // Move + Return
        float targetZ = returnWhenBehindThis.position.z;
        Vector3 backDir = Vector3.back; // (0,0,-1)

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var item = _active[i];

            if (item.go == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            var t = item.go.transform;
            t.position += backDir * (item.speed * Time.deltaTime);

            bool shouldReturn = targetIsBehindSpawner
                ? (t.position.z <= targetZ)
                : (t.position.z >= targetZ);

            if (shouldReturn)
            {
                ObjectPoolManager.Instance.ReturnToPool(item.go);
                _active.RemoveAt(i);
            }
        }
    }

    private void OnDisable()
    {
        // Cleanly return anything still active if the spawner is disabled.
        if (ObjectPoolManager.Instance == null) return;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].go != null)
                ObjectPoolManager.Instance.ReturnToPool(_active[i].go);
        }
        _active.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize spawn region
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 center = (randomOffsetMin + randomOffsetMax) * 0.5f;
        Vector3 size = new Vector3(
            Mathf.Abs(randomOffsetMax.x - randomOffsetMin.x),
            Mathf.Abs(randomOffsetMax.y - randomOffsetMin.y),
            Mathf.Abs(randomOffsetMax.z - randomOffsetMin.z)
        );

        Gizmos.DrawWireCube(center, size);
    }
}