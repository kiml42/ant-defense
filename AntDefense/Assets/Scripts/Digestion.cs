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

    private float _requiredFood => this.MaxFood - this.CurrentFood;

    void Start()
    {
        this.CurrentFood = this.StartFood;
    }

    void FixedUpdate()
    {
        var foodUse = this.Expenditure * Time.fixedDeltaTime;
        this.UseFood(foodUse);

        if (this.CurrentFood <= 0)
        {
            // take damage instead
            this.HealthController.Injure(-this.CurrentFood);
            this.CurrentFood = 0;
        }
        else if(this.HealthController.Damage > 0)
        {
            // heal
            var maxHealing = this.HealRate * Time.fixedDeltaTime;
            var requiredHealing = this.HealthController.Damage;
            var actualHealing = Mathf.Min(requiredHealing, maxHealing, this.CurrentFood);

            this.HealthController.Heal(actualHealing);
            this.UseFood(actualHealing);
        }
        this.FoodBar?.AdjustProgress(this.CurrentFood, this.MaxFood);
    }

    internal void EatFoodFrom(AntNest home)
    {
        var foodToEat = MathF.Min(this._requiredFood, home.CurrentFood);
        home.UseFood(foodToEat);
        this.AddFood(foodToEat);
    }

    internal void AddFood(float additionalFood)
    {
        this.CurrentFood += additionalFood;
    }

    internal void UseFood(float cost)
    {
        this.CurrentFood -= cost;
    }
}
