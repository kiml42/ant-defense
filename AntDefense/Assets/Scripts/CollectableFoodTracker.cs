using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollectableFoodTracker : MonoBehaviour
{
    /// <summary>
    /// If the ant has found a total food value less than or equal to this amount, it will just carry the food home
    /// while reporting it, instead of just reporting it.
    /// </summary>
    public float LimitForReporitingOnly = 50;

    private readonly HashSet<Food> _knownNearbyFood = new HashSet<Food>();
    private AntStateMachine _asm;

    public bool IsSmallQuantityOfFood => _knownNearbyFood.Count == 1 || KnownFoodValue <= LimitForReporitingOnly;
    public float KnownFoodValue => _knownNearbyFood.Sum(f => f != null ? f.FoodValue : 0);

    private void Awake()
    {
        _asm = GetComponent<AntStateMachine>();
    }

    public void RememberNearbyFood(Food food)
    {
        _knownNearbyFood.Add(food);
    }

    public void UpdateTrailValueForKnownFood()
    {
        _asm.TrailTargetValue = KnownFoodValue;
        _knownNearbyFood.Clear();
    }
}
