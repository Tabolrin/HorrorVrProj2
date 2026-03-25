using UnityEngine;
using UnityEngine.XR;

public class Pistol : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunProperties _gunProperties;
    [SerializeField] private Transform _firePoint;

    [Header("Haptics")]
    [SerializeField] private XRNode _hand = XRNode.RightHand;

    [Header("Ammo")]
    [SerializeField] private int _magazineSize = 6;

    [Header("Laser Shot Pool")]
    [Tooltip("Must match the pool ID in PoolConfigSO for the laser shot prefab.")]
    [SerializeField] private string _laserShotPoolId = "LaserShot";

    // ── State ─────────────────────────────────────────────────────────────
    private float _lastFireTime;
    private int   _currentAmmo;
    private bool  _isReloading;

    // ── Events ────────────────────────────────────────────────────────────
    public event System.Action<int, int> OnAmmoChanged;   // (current, max)
    public event System.Action           OnEmptyMag;

    private void Start()
    {
        _currentAmmo = _magazineSize;
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
    }

    public void Shoot()
    {
        if (_isReloading) return;

        // Fire rate gate
        if (Time.time - _lastFireTime < 1f / _gunProperties._fireRate) return;

        if (_currentAmmo <= 0)
        {
            OnEmptyMag?.Invoke();
            HapticManager.Instance?.Play(_hand, HapticType.Miss);
            return;
        }

        _lastFireTime = Time.time;
        _currentAmmo--;
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);

        // Spawn pooled laser shot
        // LaserShot handles movement, hit detection, scoring, and hit haptic
        var shot = ObjectPoolManager.Instance?.Spawn(
            _laserShotPoolId, _firePoint.position, _firePoint.rotation);

        // Light "fire" haptic on every trigger pull
        HapticManager.Instance?.Play(_hand, HapticType.Miss);

        if (shot == null)
            Debug.LogWarning("[Pistol] LaserShot pool exhausted or pool ID not found.");
    }

    public void Reload()
    {
        if (_isReloading || _currentAmmo == _magazineSize) return;
        _isReloading = true;

        // Instant reload — if you want a delay, use a Coroutine here
        _currentAmmo = _magazineSize;
        _isReloading = false;
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
        Debug.Log("[Pistol] Reloaded.");
    }
}
