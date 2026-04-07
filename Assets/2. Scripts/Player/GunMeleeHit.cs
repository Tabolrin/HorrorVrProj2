using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Detects melee hits when the gun is swung fast enough to contact an enemy.
/// Attach to the gun or a child melee collider object.
/// Requires a Trigger Collider on the same GameObject.
/// </summary>
public class GunMeleeHit : MonoBehaviour
{
    [Header("Haptics")]
    [SerializeField] private XRNode _hand = XRNode.RightHand;

    [Header("Tuning")]
    [Tooltip("Minimum controller speed (m/s) required to register a melee hit.")]
    [SerializeField] private float _minSwingSpeed = 1.5f;

    [Tooltip("Cooldown per enemy in seconds to prevent one swing registering multiple hits.")]
    [SerializeField] private float _hitCooldownPerEnemy = 0.4f;

    private Vector3 _prevPosition;
    private float   _currentSpeed;
    private float   _pruneTimer;
    private const float PruneInterval = 5f;

    // Keyed by enemy instance ID to avoid stale references after pool resets
    private readonly System.Collections.Generic.Dictionary<int, float> _cooldowns = new();

    private void OnEnable() => _prevPosition = transform.position;

    private void Update()
    {
        _currentSpeed = (transform.position - _prevPosition).magnitude / Time.deltaTime;
        _prevPosition = transform.position;

        // Periodically remove expired cooldown entries to prevent unbounded growth
        _pruneTimer += Time.deltaTime;
        if (_pruneTimer >= PruneInterval)
        {
            _pruneTimer = 0f;
            var keys = new System.Collections.Generic.List<int>(_cooldowns.Keys);
            foreach (int k in keys)
                if (_cooldowns[k] < Time.time) _cooldowns.Remove(k);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_currentSpeed < _minSwingSpeed) return;

        var enemy = other.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead) return;

        int id = enemy.GetInstanceID();
        if (_cooldowns.TryGetValue(id, out float cooldownEnd) && Time.time < cooldownEnd) return;

        _cooldowns[id] = Time.time + _hitCooldownPerEnemy;

        enemy.TakeHit();
        ScoreManager.Instance?.RegisterHit(enemy.IsDead);
        HapticManager.Instance?.Play(_hand, HapticType.MeleeHit);
    }
}