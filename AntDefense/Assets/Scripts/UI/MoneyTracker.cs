using TMPro;
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

public abstract class NumberTracker<TSelf> : ValueTracker<float, TSelf> where TSelf : NumberTracker<TSelf>
{ }

public abstract class ValueTracker<TValue, TSelf> : SingletonMonoBehaviour<TSelf> where TSelf : ValueTracker<TValue, TSelf>
{
    public TextMeshProUGUI Text;
    public static TValue CurrentValue { get; protected set; }

    public abstract string FormattedValue { get; }

    // Update is called once per frame
    void Update()
    {
        if (this.Text != null)
        {
            this.Text.text = this.FormattedValue;
        }
    }
}


public abstract class SingletonMonoBehaviour<TSelf> : MonoBehaviour where TSelf : SingletonMonoBehaviour<TSelf>
{
    public static TSelf Instance { get; private set; }

    private void Awake()
    {
        Debug.Assert(Instance == null || Instance == this, $"There should not be multiple {typeof(TSelf).Name}!");
        Debug.Assert(this is TSelf, $"{typeof(TSelf).Name} should only be attached to {typeof(TSelf).Name} instances!");
        Instance = (TSelf)this;
        Instance.OnAwake();
    }

    protected virtual void OnAwake()
    {
        // Do nothing by default.
    }
}
