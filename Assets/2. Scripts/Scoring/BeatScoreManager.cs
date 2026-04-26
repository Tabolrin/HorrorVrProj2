using UnityEngine;
using TMPro;

/// <summary>
/// Integrates BeatDetector with the scoring system.
/// Attach to any GameObject alongside or separate from BeatDetector.
/// </summary>
public class BeatScoreManager : MonoBehaviour
{
    [Header("References")]
    public BeatDetector beatDetector;

    [Header("Base Score Values")]
    public int baseScorePerHit  = 100;
    public int baseScorePerKill = 500;

    [Header("UI (optional)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI ratingText;
    public Animator        ratingAnimator;

    public int   TotalScore      { get; private set; }
    public int   Combo           { get; private set; }
    public float ComboMultiplier => 1f + ((float)Combo / 10f);

    private void Awake()
    {
        if (beatDetector == null)
            beatDetector = GetComponent<BeatDetector>();

        if (beatDetector == null)
            Debug.LogError("[BeatScoreManager] BeatDetector reference not assigned and not found on this GameObject.");
    }

    /// <summary>
    /// Registers a hit against the beat window and updates score and combo.
    /// Pass isKill=true when the hit killed the enemy for the kill score bonus.
    /// </summary>
    public int RegisterHit(bool isKill = false)
    {
        if (beatDetector == null) return 0;

        BeatScore beat      = beatDetector.EvaluateShot();
        int baseScore       = isKill ? baseScorePerKill : baseScorePerHit;
        int finalScore      = Mathf.RoundToInt(baseScore * beat.multiplier * ComboMultiplier);

        TotalScore += finalScore;

        if (beat.rating != BeatRating.OffBeat)
            Combo++;
        else
            Combo = 0;

        UpdateUI(beat, finalScore);
        return finalScore;
    }

    /// <summary>Resets combo. Call when the player misses.</summary>
    public void RegisterMiss()
    {
        Combo = 0;
        UpdateUI(null, 0);
    }

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
            if (ratingAnimator != null) ratingAnimator.SetTrigger("Pop");
        }
    }
}