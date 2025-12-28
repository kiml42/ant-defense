using UnityEngine;

public class ScoreGenerator : MonoBehaviour
{
    public float ScoreIncrement = 10;

    public void IncrementScore()
    {
        ScoreTracker.Instance.AddScore(this.ScoreIncrement, this.transform.position);
    }
}
