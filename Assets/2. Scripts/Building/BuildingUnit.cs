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
    /// Re-enables enemies that belong to this building's spawn side or the middle,
    /// and explicitly disables all others so stale state from a previous pool cycle
    /// never carries over.
    ///
    /// FIX: ResetEnemy() is now called BEFORE SetActive(true).
    /// This ensures _player is assigned before OnEnable fires, preventing the
    /// broken-state that occurred when SetActive triggered OnEnable -> ResetEnemy
    /// before state was ready.
    /// </summary>
    public void ResetBuilding()
    {
        foreach (var e in Enemies)
        {
            if (e == null || e.Object == null) continue;

            bool shouldBeActive = (e.side == SpawnSide || e.side == Side.Middle);

            if (shouldBeActive)
            {
                // FIX: Reset state first, then activate.
                // OnEnable fires on SetActive(true), but _player is already set by ResetEnemy.
                e.Object.ResetEnemy();
                e.Object.gameObject.SetActive(true);
            }
            else
            {
                // FIX: Explicitly disable enemies from other lanes so they don't
                // carry over active state from a previous spawn on a different side.
                e.Object.gameObject.SetActive(false);
            }
        }
    }
}