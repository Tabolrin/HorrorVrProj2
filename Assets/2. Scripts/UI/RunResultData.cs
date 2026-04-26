/// <summary>
/// Plain data container. Populated by GameStateManager at the end of a run
/// and read by WinLoseUI in the results scene.
/// </summary>
[System.Serializable]
public class RunResultData
{
    public bool   IsWin;
    public int    Score;
    public string Grade;
    public int    Combo;
    public LevelDataSO Level;

    public static string CalculateGrade(int score)
    {
        if (score >= 20000) return "Perfect";
        if (score >= 10000) return "Good";
        if (score >= 5000)  return "Ok";
        if (score >= 2000)  return "Bad";
        return "Oh No";
    }
}