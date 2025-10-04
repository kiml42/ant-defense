using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    public void AdjustProgress(float currentValue, float MaxValue)
    {
        this.AdjustProgress(currentValue / MaxValue);
    }
    public void AdjustProgress(float progress)
    {
        this.transform.localScale = progress > 0
            ? new Vector3(this.transform.localScale.x, this.transform.localScale.y, progress)   // has some volume
            : Vector3.zero; // zero volume so hide it completely.
    }
}
