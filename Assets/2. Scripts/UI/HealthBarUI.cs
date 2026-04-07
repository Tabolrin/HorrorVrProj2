using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a UI Slider as a health bar.
/// Wire PlayerManager.OnHealthChanged to OnHealthChanged in the inspector.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    /// <summary>
    /// Receives (currentHp, maxHp) from PlayerManager.OnHealthChanged.
    /// Normalizes to 0-1 for the slider value.
    /// </summary>
    public void OnHealthChanged(float current, float max)
    {
        if (_slider == null) return;
        _slider.value = max > 0f ? current / max : 0f;
    }
}