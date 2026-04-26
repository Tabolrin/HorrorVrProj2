using UnityEngine;

/// <summary>
/// Pooled enemy projectile. Uses a per-frame sweep raycast to prevent tunneling
/// through thin colliders at high speed.
/// Add to PoolConfigSO with id "EnemyProjectile".
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _speed = 8f;

    [Header("Lifetime")]
    [SerializeField] private float _maxLifetime = 5f;

    [Header("Layer")]
    [Tooltip("Layer the Player/XR Rig is on.")]
    [SerializeField] private LayerMask _playerLayer;

    private float   _damage;
    private float   _spawnTime;
    private Vector3 _direction;
    private Vector3 _prevPosition;
    private bool    _hasHit;

    private void OnEnable()
    {
        _spawnTime    = Time.time;
        _direction    = Vector3.zero;
        _hasHit       = false;
        _prevPosition = transform.position;

        if (_playerLayer.value == 0)
            Debug.LogWarning("[EnemyProjectile] Player layer mask not set - projectiles will not hit player.");
    }

    /// <summary>
    /// Sets travel direction and damage. Called by Enemy.SpawnProjectile() after spawning.
    /// Direction is captured at fire time and does not update.
    /// </summary>
    public void Launch(Vector3 direction, float damage)
    {
        _direction = direction.normalized;
        _damage    = damage;
    }

    private void Update()
    {
        if (_hasHit || _direction == Vector3.zero) return;

        if (Time.time - _spawnTime >= _maxLifetime)
        {
            ReturnToPool();
            return;
        }

        float stepDistance = _speed * Time.deltaTime;

        if (Physics.Raycast(_prevPosition, _direction, out RaycastHit hit, stepDistance, _playerLayer))
        {
            _hasHit = true;
            transform.position = hit.point;
            PlayerManager.Instance?.TakeDamage(_damage);

#if UNITY_EDITOR
            Debug.DrawRay(_prevPosition, _direction * stepDistance, Color.red, 0.5f);
#endif
            ReturnToPool();
            return;
        }

#if UNITY_EDITOR
        Debug.DrawRay(_prevPosition, _direction * stepDistance, Color.yellow);
#endif

        Vector3 newPos = _prevPosition + _direction * stepDistance;
        transform.position = newPos;
        _prevPosition      = newPos;
    }

    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.ReturnToPool(gameObject);
        else
            gameObject.SetActive(false);
    }
}