using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Handles shooting logic: fire rate gating, ammo tracking, and laser shot spawning.
/// Exposes events for HUD updates. Reload is triggered externally by ReloadZone.
/// </summary>
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

    private float _lastFireTime;
    private int   _currentAmmo;
    private bool  _isReloading;

    /// <summary>Fired with (currentAmmo, maxAmmo) on every ammo change.</summary>
    public event System.Action<int, int> OnAmmoChanged;
    /// <summary>Fired when trigger is pulled with an empty magazine.</summary>
    public event System.Action OnEmptyMag;

    private void Start()
    {
        _currentAmmo = _magazineSize;
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
    }

    /// <summary>
    /// Attempts to fire. Respects fire rate, reload state, and ammo count.
    /// Called by InputManager on trigger press.
    /// </summary>
    public void Shoot()
    {
        if (_isReloading) return;
        if (_gunProperties == null) return;
        if (_gunProperties.FireRate <= 0) return;
        if (Time.time - _lastFireTime < 1f / _gunProperties.FireRate) return;

        if (_currentAmmo <= 0)
        {
            OnEmptyMag?.Invoke();
            HapticManager.Instance?.Play(_hand, HapticType.Miss);
            return;
        }

        _lastFireTime = Time.time;
        _currentAmmo--;
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);

        // LaserShot handles movement, hit detection, scoring, and hit haptic
        var shot = ObjectPoolManager.Instance?.Spawn(
            _laserShotPoolId, _firePoint.position, _firePoint.rotation);

        HapticManager.Instance?.Play(_hand, HapticType.Miss);

        if (shot == null)
            Debug.LogWarning("[Pistol] LaserShot pool exhausted or pool ID not found.");
    }
   

    /// <summary>Refills magazine. Called by ReloadZone when gun is holstered downward.</summary>
    public void Reload()
    {
        if (_isReloading || _currentAmmo == _magazineSize) return;
        _isReloading = true;
        _currentAmmo = _magazineSize;
        _isReloading = false;
        OnAmmoChanged?.Invoke(_currentAmmo, _magazineSize);
    }
}