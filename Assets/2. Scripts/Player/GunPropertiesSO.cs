using UnityEngine;

/// <summary>
/// ScriptableObject holding gun stat values.
/// Create via Assets - Scriptable Objects - GunProperties.
/// </summary>
[CreateAssetMenu(fileName = "GunProperties", menuName = "Scriptable Objects/GunProperties")]
public class GunProperties : ScriptableObject
{
    public float     _range;
    public int       _maxAmmo;
    public LayerMask enemyLayer;
    public float     _fireRate;
}