using UnityEngine;
using UnityEngine.XR;

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

    private float _spawnTime;

    private void OnEnable()
    {
        _spawnTime = Time.time;
    }

    private void Update()
    {
        transform.position += transform.forward * (_speed * Time.deltaTime);

        if (Time.time - _spawnTime >= _maxLifetime)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Layer check
        if ((_enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        var enemy = other.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead) return;

        enemy.TakeHit(_damage);
        ScoreManager.Instance?.RegisterHit(enemy.IsDead);
        HapticManager.Instance?.Play(_hand, HapticType.Hit);

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
