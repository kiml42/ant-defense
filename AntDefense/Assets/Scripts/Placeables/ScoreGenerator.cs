using UnityEngine;

public class ScoreGenerator : MonoBehaviour
{
    public int ScoreIncrement = 10;
    public float IncrementTime = 10;
    public int NumberOfOffsets = 10;

    private static int index;

    private float currentDelay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.currentDelay = (this.IncrementTime / this.NumberOfOffsets) * index;
        Debug.Log($"ScoreGenerator starting with delay of {this.currentDelay} seconds.");
        index++;
        index = index % this.NumberOfOffsets;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this.currentDelay -= Time.fixedDeltaTime;
        if (this.currentDelay <= 0)
        {
            Debug.Log($"ScoreGenerator adding {this.ScoreIncrement} points.");
            ScoreTracker.AddScore(this.ScoreIncrement);
            this.currentDelay = this.IncrementTime;
        }
    }
}
