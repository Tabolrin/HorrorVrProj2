using UnityEngine;

/// <summary>
/// Attach to the enemy projectile prefab.
/// Add to PoolConfigSO with a matching id (e.g. "EnemyProjectile").
/// Requires a Trigger Collider on the prefab.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _speed = 8f;

    [Header("Lifetime")]
    [SerializeField] private float _maxLifetime = 5f;

    [Header("Layer")]
    [Tooltip("The layer your Player/XR Rig is on.")]
    [SerializeField] private LayerMask _playerLayer;

    private float _damage = 1f;
    private float _spawnTime;
    private Vector3 _direction;

    private void OnEnable()
    {
        _spawnTime = Time.time;
    }
    
    public void Launch(Vector3 direction, float damage)
    {
        _direction = direction.normalized;
        _damage    = damage;
    }

    private void Update()
    {
        transform.position += _direction * (_speed * Time.deltaTime);

        if (Time.time - _spawnTime >= _maxLifetime)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((_playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        PlayerManager.Instance?.TakeDamage(_damage);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        else
            gameObject.SetActive(false);
    }
}
