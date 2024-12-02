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

    /// <summary>
    /// The maximum amount of food this can turn into health per second
    /// </summary>
    public float HealRate = 0.1f;

    /// <summary>
    /// The amount of food used per second without healing
    /// </summary>
    public float Expenditure = 0.1f;

    private float _currentFood;

    public ProgressBar FoodBar;

    void Start()
    {
        _currentFood = MaxFood;
    }

    void FixedUpdate()
    {
        var foodUse = Expenditure * Time.fixedDeltaTime;
        _currentFood -= foodUse;

        if (_currentFood <= 0)
        {
            // take damage instead
            HealthController.Injure(-_currentFood);
            _currentFood = 0;
        }
        else if(HealthController.Damage > 0)
        {
            // heal
            var maxHealing = HealRate * Time.fixedDeltaTime;
            var requiredHealing = HealthController.Damage;
            var actualHealing = Mathf.Min(requiredHealing, maxHealing, _currentFood);

            HealthController.Heal(actualHealing);
            _currentFood -= actualHealing;
        }
        Debug.Log("CurrentFood = " + _currentFood);
        FoodBar?.AdjustProgress(_currentFood, MaxFood);
    }
}
