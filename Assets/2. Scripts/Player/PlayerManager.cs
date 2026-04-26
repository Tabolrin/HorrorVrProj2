using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks player HP and exposes Unity Events for health changes and death.
/// Wire OnHealthChanged to HealthBarUI and OnDeath to GameStateManager.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Stats")]
    [SerializeField] private PlayerStats _stats;

    /// <summary>Fired with (currentHp, maxHp) on every damage event.</summary>
    public UnityEvent<float, float> OnHealthChanged;
    /// <summary>Fired once when HP reaches zero.</summary>
    public UnityEvent OnDeath;

    public float CurrentHp { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        CurrentHp = _stats.MaxHp;
        OnHealthChanged?.Invoke(CurrentHp, _stats.MaxHp);
    }

    /// <summary>Reduces HP by amount. Fires OnDeath once if HP reaches zero.</summary>
    public void TakeDamage(float amount)
    {
        if (CurrentHp <= 0f) return;
        CurrentHp = Mathf.Max(CurrentHp - amount, 0f);
        OnHealthChanged?.Invoke(CurrentHp, _stats.MaxHp);
        if (CurrentHp <= 0f) OnDeath?.Invoke();
    }
}