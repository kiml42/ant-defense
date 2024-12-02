using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Digestion : MonoBehaviour
{
    public HealthController HealthController;

    /// <summary>
    /// The maximum amount of food this can store
    /// </summary>
    public float MaxFood = 10f;

    public float StartFood = 10f;

    /// <summary>
    /// The maximum amount of food this can turn into health per second
    /// </summary>
    public float HealRate = 0.1f;

    /// <summary>
    /// The amount of food used per second without healing
    /// </summary>
    public float Expenditure = 0.1f;

    public float CurrentFood { get; private set; }

    public ProgressBar FoodBar;

    private float _requiredFood => MaxFood - CurrentFood;

    void Start()
    {
        CurrentFood = StartFood;
    }

    void FixedUpdate()
    {
        var foodUse = Expenditure * Time.fixedDeltaTime;
        UseFood(foodUse);

        if (CurrentFood <= 0)
        {
            // take damage instead
            HealthController.Injure(-CurrentFood);
            CurrentFood = 0;
        }
        else if(HealthController.Damage > 0)
        {
            // heal
            var maxHealing = HealRate * Time.fixedDeltaTime;
            var requiredHealing = HealthController.Damage;
            var actualHealing = Mathf.Min(requiredHealing, maxHealing, CurrentFood);

            HealthController.Heal(actualHealing);
            UseFood(actualHealing);
        }
        FoodBar?.AdjustProgress(CurrentFood, MaxFood);
    }

    internal void EatFoodFrom(AntNest home)
    {
        var foodToEat = MathF.Min(_requiredFood, home.CurrentFood);
        home.UseFood(foodToEat);
        AddFood(foodToEat);
    }

    internal void AddFood(float additionalFood)
    {
        CurrentFood += additionalFood;
    }

    internal void UseFood(float cost)
    {
        CurrentFood -= cost;
    }
}
