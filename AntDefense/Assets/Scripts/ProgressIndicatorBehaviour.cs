using UnityEngine;

public abstract class ProgressIndicatorBehaviour : MonoBehaviour
{
    public virtual void AdjustProgress(float currentValue, float MaxValue)
    {
        this.AdjustProgress(currentValue / MaxValue);
    }

    public abstract void AdjustProgress(float progress);
}


