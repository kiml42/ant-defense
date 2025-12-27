public class ScoreTracker : ValueTracker<int>
{
    public override string FormattedValue => $"Score: {CurrentValue}";

    public static void AddScore(int score)
    {
        CurrentValue += score;
    }
}
