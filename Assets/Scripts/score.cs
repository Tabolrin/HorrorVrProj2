using UnityEngine;
using TMPro;

/// <summary>
/// Integrates BeatDetector with your game's scoring system.
/// Attach to any GameObject alongside or separate from BeatDetector.
/// </summary>
public class BeatScoreManager : MonoBehaviour
{
    [Header("References")]
    public BeatDetector beatDetector;

    [Header("Base Score Values")]
    public int baseScorePerHit    = 100;
    public int baseScorePerKill   = 500;

    [Header("UI (optional)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI ratingText;
    public Animator        ratingAnimator; // optional pop animation

    // ── State ────────────────────────────────────────────────────────────────
    public int   TotalScore  { get; private set; }
    public int   Combo       { get; private set; }
    public float ComboMultiplier => 1f + (Combo / 10f); // +10% per 10 combo

    private void Awake()
    {
        if (beatDetector == null)
            beatDetector = GetComponent<BeatDetector>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Call from your shooting/kill logic.
    /// </summary>
    public int RegisterHit(bool isKill = false)
    {
        BeatScore beat = beatDetector.EvaluateShot();

        int baseScore  = isKill ? baseScorePerKill : baseScorePerHit;
        int finalScore = Mathf.RoundToInt(baseScore * beat.multiplier * ComboMultiplier);

        TotalScore += finalScore;

        if (beat.rating != BeatRating.OffBeat)
            Combo++;
        else
            Combo = 0; // break combo on off-beat

        UpdateUI(beat, finalScore);

        Debug.Log($"[ScoreManager] {beat} | Combo×{Combo} | +{finalScore} | Total={TotalScore}");

        return finalScore;
    }

    /// <summary>
    /// Call when the player misses (no hit).
    /// </summary>
    public void RegisterMiss()
    {
        Combo = 0;
        UpdateUI(null, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void UpdateUI(BeatScore? beat, int scoreDelta)
    {
        if (scoreText) scoreText.text = $"{TotalScore:N0}";
        if (comboText) comboText.text = Combo > 1 ? $"x{Combo}" : "";

        if (ratingText && beat.HasValue)
        {
            ratingText.text = beat.Value.rating switch
            {
                BeatRating.Perfect => "PERFECT",
                BeatRating.Good    => "GOOD",
                _                  => ""
            };
            ratingAnimator?.SetTrigger("Pop");
        }
    }
}
