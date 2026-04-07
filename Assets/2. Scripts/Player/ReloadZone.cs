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

    private void OnTriggerEnter(Collider other)
    {
        if (_pistol == null) return;
        if (!other.CompareTag("Gun")) return;

        // Dot product < threshold means gun is tilted sufficiently downward
        if (Vector3.Dot(other.transform.up, Vector3.up) < _downwardThreshold)
            _pistol.Reload();
    }
}