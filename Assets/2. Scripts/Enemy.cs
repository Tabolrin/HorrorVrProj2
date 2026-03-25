using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] private Transform targetPosition;

    // ── State ─────────────────────────────────────────────────────────────
    private Transform _player;
    private bool _arrived;
    private bool _moving;
    private float _shootTimer;
    private float _health;

    public bool IsDead { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        // Called both on first activation and when building resets
        ResetEnemy();
    }

    private void ResetEnemy()
    {
        _player   = GameManager.Instance != null ? GameManager.Instance.Player : null;
        _health   = data.maxHealth;
        IsDead    = false;
        _arrived  = false;
        _moving   = false;
        _shootTimer = 0f;
    }

    private void Update()
    {
        if (IsDead || !_moving) return;

        if (!_arrived)
            MoveToTarget();
        else
        {
            FacePlayer();
            HandleShooting();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MoveStart"))
            _moving = true;
    }

    // ── Movement ──────────────────────────────────────────────────────────
    private void MoveToTarget()
    {
        Vector3 dir = targetPosition.position - transform.position;
        dir.y = 0f;

        if (dir.magnitude <= data.arrivedThreshold)
        {
            _arrived = true;
            return;
        }

        transform.position += dir.normalized * data.moveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir.normalized), data.rotationSpeed * Time.deltaTime);
    }

    private void FacePlayer()
    {
        if (_player == null) return;
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0f;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), data.rotationSpeed * Time.deltaTime);
    }

    // ── Shooting ──────────────────────────────────────────────────────────
    private void HandleShooting()
    {
        _shootTimer += Time.deltaTime;
        if (_shootTimer >= data.shootInterval)
        {
            _shootTimer = 0f;
            Shoot();
        }
    }

    private void Shoot()
    {
        // TODO: fire a pooled projectile toward player
        Debug.Log($"[Enemy] {name} shoots at player.");
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
        // Disable self — BuildingUnit will re-enable via ResetEnemy on pool return
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (targetPosition == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPosition.position, 0.3f);
        Gizmos.DrawLine(transform.position, targetPosition.position);

        if (!Application.isPlaying || data == null) return;
        Gizmos.color = Color.Lerp(Color.red, Color.green, _health / data.maxHealth);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
    }
#endif
}
