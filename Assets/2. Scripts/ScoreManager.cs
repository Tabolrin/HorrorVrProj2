using UnityEngine;
using TMPro;

/// <summary>
/// Singleton wrapper around the provided BeatScoreManager (score.cs).
/// Do NOT rename or modify score.cs — this sits on top of it.
/// Pistol and GunMeleeHit call ScoreManager.Instance instead.
/// Place this on the same GameObject as BeatScoreManager (score.cs).
/// </summary>
[RequireComponent(typeof(BeatScoreManager))]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // The original, untouched BeatScoreManager from score.cs
    private BeatScoreManager _bsm;

    // Pass-through properties so the rest of the codebase
    // can read score/combo without knowing about score.cs internals
    public int   TotalScore      => _bsm.TotalScore;
    public int   Combo           => _bsm.Combo;
    public float ComboMultiplier => _bsm.ComboMultiplier;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _bsm = GetComponent<BeatScoreManager>();

        if (_bsm == null)
            Debug.LogError("[ScoreManager] BeatScoreManager (score.cs) not found on this GameObject!");
    }

    /// <summary>Call from Pistol or GunMeleeHit on any successful hit.</summary>
    public int RegisterHit(bool isKill = false)
    {
        if (_bsm == null) return 0;
        return _bsm.RegisterHit(isKill);
    }

    public void RegisterMiss()
    {
        _bsm?.RegisterMiss();
    }
}
