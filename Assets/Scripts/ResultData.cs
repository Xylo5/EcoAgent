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
    public static int LeafRating;       // 0 = fail, 1–3 = pass tiers
    public static string ResultMessage; // e.g. "Excellent Management"
}
