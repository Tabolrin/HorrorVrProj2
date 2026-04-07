using UnityEngine;
using TMPro;

/// <summary>
/// Subscribes to Pistol events and updates the ammo display and empty-mag warning.
/// Attach to the Canvas root or any persistent UI GameObject.
/// </summary>
public class InGameHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pistol _pistol;

    [Header("Ammo UI")]
    [SerializeField] private TextMeshProUGUI _ammoText;

    [Header("Empty Mag Warning")]
    [SerializeField] private GameObject _emptyMagWarning;

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
        if (_ammoText)       _ammoText.text = $"{current} / {max}";
        if (_emptyMagWarning) _emptyMagWarning.SetActive(current == 0);
    }

    private void HandleEmptyMag()
    {
        if (_emptyMagWarning) _emptyMagWarning.SetActive(true);
    }
}