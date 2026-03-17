/// <summary>
/// Static data passed from a gameplay level to the Result scene.
/// Set before loading Result, read by ResultUI on Start().
/// </summary>
public static class ResultData
{
    public static int PollutionScore;
    public static bool Won;
    public static bool AllBuildingsPlaced;
    public static int LevelIndex;
}
