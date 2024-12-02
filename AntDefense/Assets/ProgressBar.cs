using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    public void AdjustProgress(float currentValue, float MaxValue)
    {
        AdjustProgress(currentValue / MaxValue);
    }
    public void AdjustProgress(float progress)
    {
        this.transform.localScale = new Vector3(this.transform.localScale.x, this.transform.localScale.y, progress);
        Debug.Log("Bar scale = " + this.transform.localScale);
    }
}
