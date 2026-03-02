// Enemy.cs
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] private Vector3 targetPosition;

    private Transform _player;
    private bool _arrived;
    private float _shootTimer;
    private float _health;
    private bool _isDead;

    private void Start()
    {
        _player = GameManager.Instance.Player;
        _health = data.maxHealth;
    }

    private void Update()
    {
        if (_isDead) return;

        if (!_arrived)
            MoveToTarget();
        else
        {
            FacePlayer();
            HandleShooting();
        }
    }

    private void MoveToTarget()
    {
        Vector3 dir = targetPosition - transform.position;
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
        // TODO: implement shooting (e.g. instantiate projectile, raycast, etc.)
        Debug.Log("Enemy shoots at player!");
    }

    public void TakeHit(float damage = 1f)
    {
        if (_isDead) return;

        _health = Mathf.Max(_health - damage, 0f);

        if (_health <= 0f)
            Die();
    }

    private void Die()
    {
        _isDead = true;
        // TODO: play death animation / VFX / drop loot before disabling
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPosition, 0.3f);
        Gizmos.DrawLine(transform.position, targetPosition);

        if (!Application.isPlaying || data == null) return;
        Gizmos.color = Color.Lerp(Color.red, Color.green, _health / data.maxHealth);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
    }
#endif
}