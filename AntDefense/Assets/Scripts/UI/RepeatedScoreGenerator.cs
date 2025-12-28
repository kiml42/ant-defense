using UnityEngine;

public class RepeatedScoreGenerator : ScoreGenerator
{
    /// <summary>
    /// Delay between incrementing the score
    /// </summary>
    public float IncrementTime = 10;

    /// <summary>
    /// The total number of time offsets within <see cref="IncrementTime"/> to spread out multiple generators
    /// </summary>
    public int NumberOfOffsets = 10;

    private static int index;

    private float currentDelay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.currentDelay = (this.IncrementTime / this.NumberOfOffsets) * index;
        index++;
        index = index % this.NumberOfOffsets;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this.currentDelay -= Time.fixedDeltaTime;
        if (this.currentDelay <= 0)
        {
            this.IncrementScore();
            this.currentDelay = this.IncrementTime;
        }
    }
}
