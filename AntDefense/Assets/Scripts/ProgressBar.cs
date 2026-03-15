using UnityEngine;

public abstract class ProgressIndicatorBehaviour : MonoBehaviour
{
    public abstract void AdjustProgress(float currentValue, float MaxValue);

    public abstract void AdjustProgress(float progress);
}

public class ProgressBar : ProgressIndicatorBehaviour
{
    public override void AdjustProgress(float currentValue, float MaxValue)
    {
        this.AdjustProgress(currentValue / MaxValue);
    }

    public override void AdjustProgress(float progress)
    {
        this.transform.localScale = progress > 0
            ? new Vector3(this.transform.localScale.x, this.transform.localScale.y, progress)   // has some volume
            : Vector3.zero; // zero volume so hide it completely.
    }
}


