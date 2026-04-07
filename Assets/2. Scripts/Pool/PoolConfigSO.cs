using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines all object pools for the game.
/// Assign to ObjectPoolManager. Create via Assets - Pooling - Pool Config.
/// </summary>
[CreateAssetMenu(menuName = "Pooling/Pool Config", fileName = "PoolConfig")]
public class PoolConfigSO : ScriptableObject
{
    [Serializable]
    public class PoolEntry
    {
        [Tooltip("Unique string key used by spawners to request this prefab.")]
        public string id;

        public GameObject prefab;

        [Min(0)]
        [Tooltip("Instances created at startup and placed in the inactive queue.")]
        public int prewarmCount = 10;

        [Min(0)]
        [Tooltip("Hard limit: total instances allowed in the scene (active + inactive). 0 = unlimited.")]
        public int maxInScene = 100;

        [Min(0)]
        [Tooltip("Max simultaneously active instances. 0 = unlimited up to maxInScene.")]
        public int maxActive = 0;
    }

    [Header("Pool Entries")]
    public List<PoolEntry> entries = new();

    [Header("Organization")]
    [Tooltip("Parent transform for all pooled objects. Auto-created if left empty.")]
    public Transform poolRoot;
}