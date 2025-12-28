using UnityEngine;

public class ScoreTracker : NumberTracker<ScoreTracker>
{
    private const char ScoreSymbol = '*';
    public override string FormattedValue => $"Score: {CurrentValue}{ScoreSymbol}";

    public BubblingText BubblingTextPrefab;
    /// <summary>
    /// The world space offset to apply to the bubbling text when instantiated.
    /// </summary>
    public Vector3 BubblingTextOffset;

    public Color ScoreTextColor = Color.purple;

    private void Start()
    {
        this.Text.color = this.ScoreTextColor;
    }

    public void AddScore(float score, Vector3? location)
    {
        CurrentValue += score;
        if (location.HasValue && this.BubblingTextPrefab != null)
        {
            var text = Instantiate(this.BubblingTextPrefab);
            text.transform.position = location.Value + this.BubblingTextOffset;

            text.Initialise($"+{score}{ScoreSymbol}", this.ScoreTextColor);
        }
    }
}
