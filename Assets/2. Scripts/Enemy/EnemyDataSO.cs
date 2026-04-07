using UnityEngine;

/// <summary>
/// ScriptableObject defining per-enemy-type stats.
/// Create via Assets - Enemies - Enemy Data.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Facing")]
    public float rotationSpeed = 5f;

    [Header("Health")]
    public float maxHealth = 3f;

    [Header("Combat")]
    public float shootInterval    = 2.5f;
    public float projectileDamage = 1f;
}