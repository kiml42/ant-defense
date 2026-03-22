using UnityEngine;

public class Sellable : MonoBehaviour
{
    public float SellValue;

    public void Sell()
    {
        MoneyTracker.Earn(this.SellValue);
        Destroy(this.gameObject);
    }
}
