using TMPro;
using UnityEngine;

public class MoneyTracker : NumberTracker
{
    public float InitialMoney = 100f;
    public float IncomePerSecond = 0.1f;

    public override string FormattedValue => $"£{CurrentValue:F2}";

    public static ScoreTracker Instance { get; private set; }

    // TODO: add base class for singleton monobehaviours
    private void Awake()
    {
        Debug.Assert(Instance == null || Instance == this, "There should not be multiple money trackers!");
        Instance = this;
    }

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

public abstract class NumberTracker : ValueTracker<float> { }

public abstract class ValueTracker<T> : MonoBehaviour
{
    public TextMeshProUGUI Text;
    public static T CurrentValue { get; protected set; }

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
