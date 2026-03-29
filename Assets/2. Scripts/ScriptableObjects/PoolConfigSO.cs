using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pooling/Pool Config", fileName = "PoolConfig")]
public class PoolConfigSO : ScriptableObject
{
    [Serializable]
    public class PoolEntry
    {
        [Tooltip("Unique key used by spawners to request this prefab.")]
        [SerializeField] public string id;

        [SerializeField] public GameObject prefab;

        [Min(0)]
        [Tooltip("How many instances to create immediately at startup.")]
        [SerializeField] public int prewarmCount = 10;

        [Min(0)]
        [Tooltip("Hard limit: maximum number of instances of this id allowed in the scene at once (active + inactive).")]
        [SerializeField] public int maxInScene = 100;

        [Min(0)]
        [Tooltip("Optional: maximum active at the same time. 0 = unlimited (up to maxInScene).")]
        [SerializeField] public int maxActive = 0;
    }

    [Header("Pools")]
    [SerializeField] public List<PoolEntry> entries = new();

    [Header("Organization")]
    [Tooltip("If set, pooled objects will be parented under this Transform at runtime.")]
    [SerializeField] public Transform poolRoot;
}