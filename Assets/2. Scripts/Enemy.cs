using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] public Side side;

    [Header("Shooting")]
    [Tooltip("Must match the EnemyProjectile pool ID in PoolConfigSO.")]
    [SerializeField] private string _projectilePoolId = "EnemyProjectile";
    [Tooltip("Empty child transform at the gun/hand muzzle position.")]
    [SerializeField] private Transform _shootPoint;

    [Header("Animator")]
    [SerializeField] private Animator _animator;

    // ── Animator parameter hashes ─────────────────────────────────────────
    private static readonly int HashIsActive = Animator.StringToHash("IsActive");
    private static readonly int HashShoot    = Animator.StringToHash("Shoot");

    // ── State ─────────────────────────────────────────────────────────────
    private Transform _player;
    private bool      _playerInRange;
    private float     _shootTimer;
    private float     _health;

    public bool IsDead { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        ResetEnemy();
    }

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

    // ── Detection ─────────────────────────────────────────────────────────
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

    // ── Facing ────────────────────────────────────────────────────────────
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

    // ── Shooting ──────────────────────────────────────────────────────────
    private void HandleShooting()
    {
        _shootTimer += Time.deltaTime;
        if (_shootTimer < data.shootInterval) return;

        _shootTimer = 0f;
        _animator?.SetTrigger(HashShoot);
        
        //SpawnProjectile();
    }
    
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

    // ── Damage ────────────────────────────────────────────────────────────
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