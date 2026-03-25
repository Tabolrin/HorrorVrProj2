using UnityEngine;

/// <summary>
/// Place this on a GameObject with a Trigger Collider at the reload position
/// (e.g. near the player's hip/holster). When the gun enters and is pointing
/// downward past the threshold, it reloads.
/// </summary>
public class ReloadZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pistol _pistol;

    [Header("Tuning")]
    [Tooltip("Dot product of gun's up vector vs World up. " +
             "Negative = gun pointing down. -0.3 ≈ 45° past horizontal.")]
    [SerializeField] [Range(-1f, 0f)] private float _downwardThreshold = -0.3f;

    private void OnTriggerEnter(Collider other)
    {
        if (_pistol == null) return;
        if (!other.CompareTag("Gun")) return;

        float dot = Vector3.Dot(other.transform.up, Vector3.up);
        if (dot < _downwardThreshold)
            _pistol.Reload();
    }
}
