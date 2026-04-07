using UnityEngine;

/// <summary>
/// Stationary enemy placed on building prefabs.
/// Idles until the player enters the detection trigger,
/// then faces the player and shoots at set intervals.
/// Resets via OnEnable when its building is recycled by the pool.
/// </summary>
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] public Side side;

    [Header("Shooting")]
    [Tooltip("Must match the EnemyProjectile pool ID in PoolConfigSO.")]
    [SerializeField] private string _projectilePoolId = "EnemyProjectile";
    [Tooltip("Muzzle transform - projectile spawns here. Falls back to root if unassigned.")]
    [SerializeField] private Transform _shootPoint;

    [Header("Animator")]
    [SerializeField] private Animator _animator;

    // Pre-hashed animator parameter IDs for performance
    private static readonly int HashIsActive = Animator.StringToHash("IsActive");
    private static readonly int HashShoot    = Animator.StringToHash("Shoot");

    private Transform _player;
    private bool      _playerInRange;
    private float     _shootTimer;
    private float     _health;

    public bool IsDead { get; private set; }

    // Called on first activation and on every pool reset via BuildingUnit
    private void OnEnable() => ResetEnemy();

    private void ResetEnemy()
    {
        _player        = GameManager.Instance != null ? GameManager.Instance.Player : null;
        _health        = data.maxHealth;
        IsDead         = false;
        _playerInRange = false;
        _shootTimer    = 0f;
        _animator?.SetBool(HashIsActive, false);
    }

    private void Update()
    {
        if (IsDead || !_playerInRange) return;
        FacePlayer();
        HandleShooting();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        _animator?.SetBool(HashIsActive, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        _animator?.SetBool(HashIsActive, false);
    }

    private void FacePlayer()
    {
        if (_player == null) return;
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir.normalized),
            data.rotationSpeed * Time.deltaTime);
    }

    private void HandleShooting()
    {
        _shootTimer += Time.deltaTime;
        if (_shootTimer < data.shootInterval) return;
        _shootTimer = 0f;
        _animator?.SetTrigger(HashShoot);
        // Remove this call and use an Animation Event on the Shoot clip instead for visual sync
        SpawnProjectile();
    }

    /// <summary>
    /// Spawns a projectile aimed at the player's position at the moment of firing.
    /// Can alternatively be called from an Animation Event on the Shoot clip.
    /// If so, remove the direct call in HandleShooting to avoid double-firing.
    /// </summary>
    public void SpawnProjectile()
    {
        if (_player == null || ObjectPoolManager.Instance == null) return;

        Vector3 origin = _shootPoint != null ? _shootPoint.position : transform.position;
        Vector3 dir    = (_player.position - origin).normalized;

        var go = ObjectPoolManager.Instance.Spawn(
            _projectilePoolId, origin, Quaternion.LookRotation(dir));
        if (go == null) return;

        go.GetComponent<EnemyProjectile>()?.Launch(dir, data.projectileDamage);
    }

    /// <summary>Applies damage and triggers death when health reaches zero.</summary>
    public void TakeHit(float damage = 1f)
    {
        if (IsDead) return;
        _health = Mathf.Max(_health - damage, 0f);
        if (_health <= 0f) Die();
    }

    private void Die()
    {
        IsDead = true;
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || data == null) return;
        Gizmos.color = Color.Lerp(Color.red, Color.green, _health / data.maxHealth);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
    }
#endif
}