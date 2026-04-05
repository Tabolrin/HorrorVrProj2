using System;
using System.Collections.Generic;
using UnityEngine;

public enum Side { Left, Middle, Right }

public class BuildingUnit : MonoBehaviour
{
    [Serializable]
    public class EnemyEntry
    {
        [Tooltip("Unique key used by spawners to request this prefab.")]
        [SerializeField] public Side side;

        [SerializeField] public Enemy Object;
    }
    
    [Header("Pools")] 
    [SerializeField] private List<EnemyEntry> Enemies = new();
    
    [SerializeField] public Side SpawnSide;
    
    
    public void ResetBuilding()
    {
        foreach (var e in Enemies)
        {
            if (e == null || (e.side == SpawnSide && e.side != Side.Middle)) continue;
            e.Object.gameObject.SetActive(true);
        }
    }
}
