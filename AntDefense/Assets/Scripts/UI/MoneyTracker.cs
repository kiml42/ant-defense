using UnityEngine;

public class MoneyTracker : NumberTracker<MoneyTracker>
{
    public float InitialMoney = 100f;
    public float IncomePerSecond = 0.1f;

    public override string FormattedValue => $"£{CurrentValue:F2}";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CurrentValue = this.InitialMoney;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CurrentValue += this.IncomePerSecond * Time.deltaTime;
    }

    internal static void Spend(float cost)
    {
        CurrentValue -= cost;
    }

    internal static bool CanAfford(float cost)
    {
        return CurrentValue >= cost;
    }
}
