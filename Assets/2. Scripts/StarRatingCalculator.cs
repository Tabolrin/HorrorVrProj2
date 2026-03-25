using UnityEngine;

/// <summary>
/// Pure static helper. Thresholds set via StarRatingConfig SO assigned to
/// GameStateManager or directly referenced where needed.
/// </summary>
public static class StarRatingCalculator
{
    // Fallback thresholds if no SO is available
    private static readonly int[] DefaultThresholds = { 500, 2000, 5000, 10000, 20000 };

    public static StarRatingConfig Config;

    /// <summary>Returns 1–5 stars based on score.</summary>
    public static int Calculate(int totalScore)
    {
        int[] t = (Config != null) ? Config.thresholds : DefaultThresholds;

        int stars = 0;
        foreach (int threshold in t)
            if (totalScore >= threshold) stars++;

        return Mathf.Clamp(stars, 0, 5);
    }
}

// ── Config SO ─────────────────────────────────────────────────────────────────
[CreateAssetMenu(menuName = "Game/Star Rating Config", fileName = "StarRatingConfig")]
public class StarRatingConfig : ScriptableObject
{
    [Tooltip("Five ascending score thresholds for 1–5 stars.")]
    public int[] thresholds = { 500, 2000, 5000, 10000, 20000 };

    private void OnEnable() => StarRatingCalculator.Config = this;
}
