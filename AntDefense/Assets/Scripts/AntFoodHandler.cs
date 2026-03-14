using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AntFoodHandler : MonoBehaviour
{
    public Transform CarryPoint;

    /// <summary>
    /// If the ant has found a total food value less than or equal to this amount, it will just carry the food home
    /// while reporting it, instead of just reporting it.
    /// </summary>
    public float LimitForReporitingOnly = 50;

    private Food _carriedFood;
    private readonly HashSet<Food> _knownNearbyFood = new HashSet<Food>();
    private Rigidbody _rigidbody;
    private AntStateMachine _asm;

    public bool IsCarryingFood => _carriedFood != null;
    public bool IsSmallQuantityOfFood => _knownNearbyFood.Count == 1 || KnownFoodValue <= LimitForReporitingOnly;
    public float KnownFoodValue => _knownNearbyFood.Sum(f => f != null ? f.FoodValue : 0);

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
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

    public void PickUp(Smellable smellable)
    {
        Debug.Assert(CarryPoint != null, "Cannot carry food with no carry point");

        var food = smellable.GetComponentInParent<Food>();
        _asm.TrailTargetValue -= food.FoodValue;

        if (food.TryGetComponent<LifetimeController>(out var lifetime))
        {
            lifetime.Reset();
            lifetime.IsRunning = false;
        }

        _carriedFood = food;
        food.transform.position = CarryPoint.position;
        food.Attach(_rigidbody);
        _asm.State = AntState.CarryingFood;
    }

    public void DropOff(Smellable smellable)
    {
        if (_carriedFood == null || _carriedFood.gameObject == null || _carriedFood.gameObject.IsDestroyed())
        {
            _carriedFood = null;
            return;
        }
        var home = smellable.GetComponentInParent<AntNest>();
        home.AddFood(_carriedFood.FoodValue);
        _carriedFood.Destroy();
        _carriedFood = null;
    }

    public void ReleaseCarriedFood()
    {
        if (_carriedFood == null) return;
        _carriedFood.Detach();
        if (_carriedFood.TryGetComponent<LifetimeController>(out var lifetime))
            lifetime.IsRunning = true;
        _carriedFood = null;
    }
}
