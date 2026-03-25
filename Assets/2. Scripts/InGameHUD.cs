using UnityEngine;
using TMPro;

/// <summary>
/// Attach to your in-game Canvas. Wire _pistol in inspector.
/// Displays current ammo and plays an empty-mag warning.
/// </summary>
public class InGameHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pistol _pistol;

    [Header("Ammo UI")]
    [SerializeField] private TextMeshProUGUI _ammoText;

    [Header("Empty Mag Feedback")]
    [SerializeField] private GameObject _emptyMagWarning; // e.g. a red "RELOAD" label

    private void OnEnable()
    {
        if (_pistol == null) return;
        _pistol.OnAmmoChanged += HandleAmmoChanged;
        _pistol.OnEmptyMag    += HandleEmptyMag;
    }

    private void OnDisable()
    {
        if (_pistol == null) return;
        _pistol.OnAmmoChanged -= HandleAmmoChanged;
        _pistol.OnEmptyMag    -= HandleEmptyMag;
    }

    private void HandleAmmoChanged(int current, int max)
    {
        if (_ammoText) _ammoText.text = $"{current} / {max}";
        if (_emptyMagWarning) _emptyMagWarning.SetActive(current == 0);
    }

    private void HandleEmptyMag()
    {
        if (_emptyMagWarning) _emptyMagWarning.SetActive(true);
    }
}
