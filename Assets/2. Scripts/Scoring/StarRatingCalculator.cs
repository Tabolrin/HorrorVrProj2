using UnityEngine;

/// <summary>
/// Static helper that converts a final score into a 0-5 star rating.
/// Thresholds are configured via StarRatingConfig SO, which self-registers on load.
/// </summary>
public static class StarRatingCalculator
{
    private static readonly int[] DefaultThresholds = { 500, 2000, 5000, 10000, 20000 };

    public static StarRatingConfig Config;

    /// <summary>
    /// Returns 0-5 stars. Each threshold in the config that the score meets or
    /// exceeds adds one star.
    /// </summary>
    public static int Calculate(int totalScore)
    {
        int[] t = Config != null ? Config.thresholds : DefaultThresholds;
        int stars = 0;
        foreach (int threshold in t)
            if (totalScore >= threshold) stars++;
        return Mathf.Clamp(stars, 0, 5);
    }
}

/// <summary>
/// ScriptableObject holding the five score thresholds for star rating.
/// Create via Assets - Game - Star Rating Config.
/// Self-registers to StarRatingCalculator.Config on load.
/// </summary>
[CreateAssetMenu(menuName = "Game/Star Rating Config", fileName = "StarRatingConfig")]
public class StarRatingConfig : ScriptableObject
{
    [Tooltip("Five ascending score values. Meeting each one earns one star.")]
    public int[] thresholds = { 500, 2000, 5000, 10000, 20000 };

    private void OnEnable() => StarRatingCalculator.Config = this;
}