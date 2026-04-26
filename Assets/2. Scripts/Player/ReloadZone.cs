using UnityEngine;

/// <summary>
/// Trigger zone placed near the player's hip.
/// Reloads the pistol when the gun enters the zone while pointing downward.
/// The gun GameObject must be tagged "Gun".
/// </summary>
public class ReloadZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pistol _pistol;

    [Header("Tuning")]
    [Tooltip("Dot product of gun up vs world up. Negative = gun pointing down. -0.3 is roughly 45 degrees past horizontal.")]
    [SerializeField] [Range(-1f, 0f)] private float _downwardThreshold = -0.3f;

    [Tooltip("Minimum seconds between reload triggers. Prevents multiple colliders on the gun firing Reload() several times per holster gesture.")]
    [SerializeField] private float _reloadCooldown = 0.5f;

    private float _lastReloadTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        if (_pistol == null) return;
        if (!other.CompareTag("Gun")) return;
        if (Time.time - _lastReloadTime < _reloadCooldown) return;

        if (Vector3.Dot(other.transform.up, Vector3.up) < _downwardThreshold)
        {
            _lastReloadTime = Time.time;
            _pistol.Reload();
        }
    }
}