using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float arrivedThreshold = 0.2f;
    public float rotationSpeed = 5f;

    [Header("Health")]
    public float maxHealth = 3f;

    [Header("Combat")]
    public float shootInterval = 1.5f;
    public float projectileDamage = 1f;
}
