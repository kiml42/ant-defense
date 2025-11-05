using System;
using TMPro;
using Unity.VisualScripting;
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
        CurrentMoney += this.IncomePerSecond * Time.deltaTime;
    }

    void Update()
    {
        if (this.MoneyText != null)
        {
            this.MoneyText.text = $"£{CurrentMoney:F2}";
        }
    }

    internal static void Spend(float cost)
    {
        CurrentMoney -= cost;
    }

    internal static bool CanAfford(float cost)
    {
        return CurrentMoney >= cost;
    }
}
