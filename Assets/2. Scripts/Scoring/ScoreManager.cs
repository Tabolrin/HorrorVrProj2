using UnityEngine;

/// <summary>
/// Singleton wrapper around BeatScoreManager (score.cs / BeatScoreManager.cs).
/// Do not modify BeatScoreManager - this sits on top of it as a singleton adapter.
/// All game code calls ScoreManager.Instance rather than BeatScoreManager directly.
/// Must be on the same GameObject as BeatScoreManager.
/// </summary>
[RequireComponent(typeof(BeatScoreManager))]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private BeatScoreManager _bsm;

    public int   TotalScore      => _bsm.TotalScore;
    public int   Combo           => _bsm.Combo;
    public float ComboMultiplier => _bsm.ComboMultiplier;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _bsm = GetComponent<BeatScoreManager>();
        if (_bsm == null)
            Debug.LogError("[ScoreManager] BeatScoreManager not found on this GameObject.");
    }

    /// <summary>Registers a hit against the beat window. Pass isKill=true for kill bonus.</summary>
    public int RegisterHit(bool isKill = false)
    {
        if (_bsm == null) return 0;
        return _bsm.RegisterHit(isKill);
    }

    public void RegisterMiss() => _bsm?.RegisterMiss();
}