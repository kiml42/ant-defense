using Unity.VisualScripting;
using UnityEngine;

public class CarriedObjectHandler : MonoBehaviour
{
    public Transform CarryPoint;

    private Food _carriedFood;
    private Rigidbody _rigidbody;
    private AntStateMachine _asm;

    public bool IsCarryingFood => _carriedFood != null;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _asm = GetComponent<AntStateMachine>();
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
