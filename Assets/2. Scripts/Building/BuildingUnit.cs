using System;
using System.Collections.Generic;
using UnityEngine;

public enum Side { Left, Middle, Right }

/// <summary>
/// Attached to each building prefab root.
/// Manages which enemies are active based on the lane this building spawned in.
/// Called by BuildingSpawner after pulling from the pool.
/// </summary>
public class BuildingUnit : MonoBehaviour
{
    [Serializable]
    public class EnemyEntry
    {
        [Tooltip("Which lane this enemy belongs to.")]
        [SerializeField] public Side side;
        [SerializeField] public Enemy Object;
    }

    [Header("Enemies")]
    [SerializeField] private List<EnemyEntry> Enemies = new();

    [Header("Settings")]
    public Side SpawnSide;
    public bool ShouldRotateAtSpawn;

    /// <summary>
    /// Re-enables enemies that belong to this building's spawn side or the middle.
    /// Called by BuildingSpawner.SpawnNext() before the building goes active.
    /// </summary>
    public void ResetBuilding()
    {
        foreach (var e in Enemies)
        {
            if (e == null) continue;
            // Skip enemies assigned to the opposite lane
            if (e.side != SpawnSide && e.side != Side.Middle) continue;
            e.Object.gameObject.SetActive(true);
        }
    }
}