using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Pooled player projectile. Uses a per-frame raycast sweep to prevent
/// tunneling through thin colliders at high speed. No collider needed on this prefab.
/// Add to PoolConfigSO with id "LaserShot".
/// </summary>
public class LaserShot : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _speed = 20f;

    [Header("Lifetime")]
    [SerializeField] private float _maxLifetime = 3f;

    [Header("Damage")]
    [SerializeField] private float _damage = 1f;

    [Header("Layer")]
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Haptics")]
    [SerializeField] private XRNode _hand = XRNode.RightHand;

    private float   _spawnTime;
    private Vector3 _prevPosition;

    private void OnEnable()
    {
        _spawnTime    = Time.time;
        _prevPosition = transform.position;

        if (_enemyLayer.value == 0)
            Debug.LogWarning("[LaserShot] Enemy layer mask not set - shots will not register hits.");
    }

    private void Update()
    {
        if (Time.time - _spawnTime >= _maxLifetime)
        {
            ReturnToPool();
            return;
        }

        float stepDistance = _speed * Time.deltaTime;

        if (Physics.Raycast(_prevPosition, transform.forward, out RaycastHit hit, stepDistance, _enemyLayer))
        {
            var enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                transform.position = hit.point;

                bool wasDeadBefore = enemy.IsDead;
                enemy.TakeHit(_damage);
                bool justKilled = !wasDeadBefore && enemy.IsDead;

                ScoreManager.Instance?.RegisterHit(justKilled);
                HapticManager.Instance?.Play(_hand, HapticType.Hit);

#if UNITY_EDITOR
                Debug.DrawRay(_prevPosition, transform.forward * stepDistance, Color.green, 0.5f);
#endif
                ReturnToPool();
                return;
            }
        }

#if UNITY_EDITOR
        Debug.DrawRay(_prevPosition, transform.forward * stepDistance, Color.cyan);
#endif

        Vector3 newPos = _prevPosition + transform.forward * stepDistance;
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