using UnityEngine;

/// <summary>
/// Defines one playable level entry shown in the level select screen.
/// Create via Assets > Levels > Level Data.
/// </summary>
[CreateAssetMenu(menuName = "Levels/Level Data", fileName = "NewLevelData")]
public class LevelDataSO : ScriptableObject
{
    [Header("Display")]
    [Tooltip("Name shown on the level select button.")]
    public string DisplayName = "Level 1";

    [Tooltip("Optional thumbnail shown on the level card. Can be left empty.")]
    public Sprite PreviewSprite;

    [Header("FMOD")]
    [Tooltip("FMOD event path for this level's music. e.g. event:/Music/Level1")]
    public string FmodEventPath = "event:/Music/Level1";

    [Header("Scene")]
    [Tooltip("Exact name of the gameplay scene to load for this level.")]
    public string GameplaySceneName = "Gameplay";
}
