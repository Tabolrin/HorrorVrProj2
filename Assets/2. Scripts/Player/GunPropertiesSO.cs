using UnityEngine;

/// <summary>
/// ScriptableObject holding gun stat values.
/// Create via Assets - Scriptable Objects - GunProperties.
/// </summary>
[CreateAssetMenu(fileName = "GunProperties", menuName = "Scriptable Objects/GunProperties")]
public class GunProperties : ScriptableObject
{
    // FIX: Public serialized fields renamed to PascalCase to match Unity SO conventions
    // and the rest of the codebase. Underscore prefix is reserved for private fields.
    // If Pistol.cs references these by old names (_range, _maxAmmo, _fireRate),
    // update those references accordingly.
    public float     Range;
    public int       MaxAmmo;
    public LayerMask EnemyLayer;
    public float     FireRate;
}