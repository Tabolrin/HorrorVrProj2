using UnityEngine;
using UnityEngine.XR;

public class GunMeleeHit : MonoBehaviour
{
    [Header("Haptics")]
    [SerializeField] private XRNode _hand = XRNode.RightHand;

    [Header("Tuning")]
    [Tooltip("Minimum controller speed (m/s) to count as a swing.")]
    [SerializeField] private float _minSwingSpeed = 1.5f;

    [Tooltip("Seconds before this enemy can be melee-hit again.")]
    [SerializeField] private float _hitCooldownPerEnemy = 0.4f;

    // ── Velocity tracking ─────────────────────────────────────────────────
    private Vector3 _prevPosition;
    private float   _currentSpeed;

    // ── Per-enemy cooldown ────────────────────────────────────────────────
    private readonly System.Collections.Generic.Dictionary<int, float> _cooldowns = new();

    private void Update()
    {
        _currentSpeed = (transform.position - _prevPosition).magnitude / Time.deltaTime;
        _prevPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_currentSpeed < _minSwingSpeed) return;

        var enemy = other.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead) return;

        // Cooldown check (keyed by instance ID to avoid stale references after pool reset)
        int id = enemy.GetInstanceID();
        if (_cooldowns.TryGetValue(id, out float cooldownEnd) && Time.time < cooldownEnd)
            return;

        _cooldowns[id] = Time.time + _hitCooldownPerEnemy;

        enemy.TakeHit();
        ScoreManager.Instance?.RegisterHit(enemy.IsDead);
        HapticManager.Instance?.Play(_hand, HapticType.MeleeHit);
    }
}
