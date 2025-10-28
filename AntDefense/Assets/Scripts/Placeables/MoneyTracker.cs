using TMPro;
using UnityEngine;

public class MoneyTracker : MonoBehaviour
{
    public float InitialMoney = 100f;
    public float IncomePerSecond = 0.1f;

    public static float CurrentMoney { get; private set; }

    public TextMeshProUGUI MoneyText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CurrentMoney = this.InitialMoney;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CurrentMoney += this.IncomePerSecond * Time.fixedDeltaTime;
        //Debug.Log("Current Money: " + CurrentMoney);
    }

    void Update()
    {
        if (this.MoneyText != null)
        {
            this.MoneyText.text = $"Money: £{CurrentMoney:F2}";
        }
    }
}
