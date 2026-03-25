using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks player HP. Enemies call TakeDamage() when their projectile hits.
/// Subscribe to OnDeath for game-over logic.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Stats")]
    [SerializeField] private PlayerStats _stats;

    public UnityEvent OnDeath;
    public UnityEvent<float, float> OnHealthChanged; // (current, max)

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

    public void TakeDamage(float amount)
    {
        CurrentHp = Mathf.Max(CurrentHp - amount, 0f);
        OnHealthChanged?.Invoke(CurrentHp, _stats.MaxHp);

        if (CurrentHp <= 0f)
            OnDeath?.Invoke();
    }
}
