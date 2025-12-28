using UnityEngine;

public class ScoreGenerator : MonoBehaviour
{
    public float ScoreIncrement = 10;
    public float IncrementTime = 10;
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
            ScoreTracker.Instance.AddScore(this.ScoreIncrement, this.transform.position);
            this.currentDelay = this.IncrementTime;
        }
    }
}
