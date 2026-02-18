using UnityEngine;

[CreateAssetMenu(fileName = "GunProperties", menuName = "Scriptable Objects/GunProperties")]
public class GunProperties : ScriptableObject
{
    [SerializeField] public float _range;
    [SerializeField] public int _maxAmmo;
    [SerializeField] public LayerMask enemyLayer;
    [SerializeField] public float _fireRate;
}
