/// <summary>
/// Plain data container. Populated by GameStateManager at the end of a run
/// and read by WinLoseUI in the results scene.
/// Not a MonoBehaviour - held by AppSceneManager.
/// </summary>
[System.Serializable]
public class RunResultData
{
    public bool   IsWin;
    public int    Score;
    public int    Stars;
    public int    Combo;

    /// <summary>The level that was just played. Used by Play Again.</summary>
    public LevelDataSO Level;
}
