using UnityEngine;

public class ScoreTracker : ValueTracker<int>
{
    private const char ScoreSymbol = '*';
    public override string FormattedValue => $"Score: {CurrentValue}{ScoreSymbol}";

    public BubblingText BubblingTextPrefab;
    /// <summary>
    /// The world space offset to apply to the bubbling text when instantiated.
    /// </summary>
    public Vector3 BubblingTextOffset;

    public static ScoreTracker Instance { get; private set; }

    // TODO: add base class for singleton monobehaviours
    private void Awake()
    {
        Debug.Assert(Instance == null || Instance == this, "There should not be multiple score trackers!");
        Instance = this;
    }

    public void AddScore(int score, Vector3? location)
    {
        CurrentValue += score;
        if (location.HasValue && this.BubblingTextPrefab != null)
        {
            var text = Instantiate(this.BubblingTextPrefab);
            text.transform.position = location.Value + this.BubblingTextOffset;

            // TODO: work out how to read and writhe the colour from TextMeshPro component
            text.Initialise($"+{score}{ScoreSymbol}", this.Text.material.color);
        }
    }
}
