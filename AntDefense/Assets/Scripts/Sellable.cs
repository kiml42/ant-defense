using UnityEngine;

public class Sellable : MonoBehaviour
{
    public float SellValue;

    public void Sell()
    {
        MoneyTracker.Earn(this.SellValue);

        float destroyDelay = 0f;
        foreach (var anim in this.GetComponentsInChildren<BaseBuildAnimation>())
        {
            anim.OnDeath();
            destroyDelay = Mathf.Max(destroyDelay, anim.DeathAnimationDuration);
        }

        Destroy(this.gameObject, destroyDelay);
    }
}
