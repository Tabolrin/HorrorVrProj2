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

    private readonly System.Collections.Generic.Dictionary<int, float> _cooldowns   = new();
    private readonly System.Collections.Generic.List<int>              _pruneBuffer = new();

    private void OnEnable()
    {
        _prevPosition = transform.position;
        _currentSpeed = 0f;
    }

    private void Update()
    {
        _currentSpeed = (transform.position - _prevPosition).magnitude / Time.deltaTime;
        _prevPosition = transform.position;

        _pruneTimer += Time.deltaTime;
        if (_pruneTimer >= PruneInterval)
        {
            _pruneTimer = 0f;
            _pruneBuffer.Clear();
            foreach (var kvp in _cooldowns)
                if (kvp.Value < Time.time) _pruneBuffer.Add(kvp.Key);
            foreach (int k in _pruneBuffer)
                _cooldowns.Remove(k);
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

        bool wasDeadBefore = enemy.IsDead;
        enemy.TakeHit();
        bool justKilled = !wasDeadBefore && enemy.IsDead;

        ScoreManager.Instance?.RegisterHit(justKilled);
        HapticManager.Instance?.Play(_hand, HapticType.MeleeHit);
    }
}