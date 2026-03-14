using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AntTargetSelector))]
[RequireComponent(typeof(AntFoodHandler))]
public class AntStateMachine : DeathActionBehaviour
{
    public AntState State = AntState.SeekingFood;

    public AntTargetSelector TargetSelector;
    public AntFoodHandler FoodHandler;
    public AntTargetPositionProvider PositionProvider;
    public AntTrailController TrailController;
    public Transform CarryPoint;
    public AttackController AttackController;
    public Digestion Digestion;

    /// <summary>
    /// If the ant has found a total food value less than or equal to this amount, it will just carry the food home
    /// while reporting it, instead of just reporting it.
    /// </summary>
    public float LimitForReporitingOnly = 50;

    /// <summary>
    /// If the last trail point has this much time or less remaining when it is placed, the ant will give up and return home.
    /// </summary>
    public float GoHomeTime = 2f;

    /// <summary>
    /// Scouts only look for new food and leave trails to show where it is, they never actually carry it themselves.
    /// </summary>
    public bool IsScout = false;

    public bool FullDebugLogs = false;

    public const int GroundLayer = 3;

    public Smell? TrailSmell
    {
        get
        {
            if (_disableTrail) return null;
            switch (State)
            {
                case AntState.SeekingFood:
                case AntState.ReturningToFood:
                    TrailTargetValue = null;
                    return Smell.Home;
                case AntState.ReportingFood:
                case AntState.CarryingFood:
                    return Smell.Food;
                case AntState.ReturningHome:
                    return null;
                default:
                    throw new Exception("Unknown state " + State);
            }
        }
    }

    public float? TrailTargetValue { get; private set; }

    public ITargetPriorityCalculator PriorityCalculator { get; private set; }

    private Food _carriedFood;
    private bool _disableTrail;
    private Rigidbody _rigidbody;
    private readonly HashSet<Food> _knownNearbyFood = new HashSet<Food>();

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        PriorityCalculator = GetComponent<ITargetPriorityCalculator>();
    }

    private void FixedUpdate()
    {
        if (transform.position.y < -10)
        {
            Destroy(gameObject);
            return;
        }

        if (LastTrailDroppedPoint != null && LastTrailDroppedPoint.RemainingTime < GoHomeTime)
        {
            if (this.FullDebugLogs)
                Debug.Log($"Ant {this} last trail point {LastTrailDroppedPoint} has only {LastTrailDroppedPoint.RemainingTime} time remaining, going home.");
            GiveUpAndReturnHome();
        }

        Debug.Assert(State != AntState.ReportingFood || TargetSelector.CurrentTarget == null || TargetSelector.CurrentTarget.Smell != Smell.Food,
            $"State is {State} so the current target should not be food, but it is {TargetSelector.CurrentTarget}");
    }

    private void OnCollisionExit(Collision collision)
    {
        PositionProvider.NoLongerTouching(collision.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        var world = other.GetComponent<WorldZone>();
        if (world != null)
            GiveUpAndReturnHome();
    }

    private void GiveUpAndReturnHome()
    {
        State = AntState.ReturningHome;
        if (LastTrailDroppedPoint == null || LastTrailDroppedPoint.Smell != Smell.Home)
            return;
        TargetSelector.SetTarget(LastTrailDroppedPoint);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var @object = collision.gameObject;

        if (@object.TryGetComponent<Smellable>(out var smellable))
        {
            ProcessCollisionWithSmell(smellable);
            if (smellable.Smell == Smell.Home)
                return;
            if (State == AntState.SeekingFood || State == AntState.ReturningToFood)
                return;
        }

        if (@object.TryGetComponent<Rigidbody>(out _))
            return;

        if (@object.layer == GroundLayer)
            return;

        TargetSelector.OnObstacleCollision();

        if (AttackController != null)
        {
            if (UnityEngine.Random.Range(0, 100) <= AttackController.AttackChance)
            {
                var damageHandler = @object.GetComponentInParent<ImpactDamageHandler>();
                if (damageHandler != null)
                {
                    if (damageHandler.HealthController.tag != tag)
                        AttackController.AttackObstable(collision, damageHandler);
                }
            }
        }

        PositionProvider.AvoidObstacle(collision);
    }

    public void ProcessSmell(Smellable smellable, string debugString)
    {
        if ((smellable != null && smellable.IsDestroyed() == true) || !smellable.IsSmellable)
            return;

        switch (smellable.Smell)
        {
            case Smell.Food:
                if ((smellable.IsActual && State == AntState.SeekingFood) || State == AntState.ReturningToFood)
                {
                    TargetSelector.ResetMaxPriority();
                    _knownNearbyFood.Add(smellable.GetComponent<Food>());
                }
                switch (State)
                {
                    case AntState.SeekingFood:
                        if (!smellable.IsActual)
                        {
                            if (IsScout)
                                return;
                            State = AntState.ReturningToFood;
                        }
                        TargetSelector.ResetMaxPriority();
                        TargetSelector.ClearTarget();
                        TargetSelector.RegisterPotentialTarget(smellable, debugString);
                        return;
                    case AntState.ReturningToFood:
                        TargetSelector.RegisterPotentialTarget(smellable, debugString);
                        return;
                }
                return;
            case Smell.Home:
                switch (State)
                {
                    case AntState.ReturningHome:
                    case AntState.ReportingFood:
                    case AntState.CarryingFood:
                        TargetSelector.RegisterPotentialTarget(smellable, debugString);
                        return;
                }
                return;
        }
    }

    private void ProcessCollisionWithSmell(Smellable smellable)
    {
        switch (smellable.Smell)
        {
            case Smell.Food:
                if (_carriedFood != null) return;
                if (IsScout)
                {
                    if (State == AntState.ReportingFood) return;
                    ReportFoodWithoutCarryingIt(smellable);
                    return;
                }
                var isSmallQuantityOfFood = _knownNearbyFood.Count == 1 || KnownFoodValue <= LimitForReporitingOnly;
                var canPickUpFoodFromThisState = State == AntState.SeekingFood || State == AntState.ReturningToFood || State == AntState.ReturningHome;
                if (isSmallQuantityOfFood && canPickUpFoodFromThisState)
                {
                    CollectKnownFood(smellable);
                    _disableTrail = true;
                    return;
                }
                switch (State)
                {
                    case AntState.SeekingFood:
                        ReportFoodWithoutCarryingIt(smellable);
                        return;
                    case AntState.ReturningToFood:
                        CollectKnownFood(smellable);
                        _disableTrail = false;
                        return;
                }
                return;
            case Smell.Home:
                EatFoodAtHome(smellable);
                if (IsScout)
                {
                    GoBackToSeekingFood();
                    return;
                }
                switch (State)
                {
                    case AntState.ReportingFood:
                        _disableTrail = false;
                        TargetSelector.ResetMaxPriority();
                        TargetSelector.ClearTarget();
                        State = AntState.ReturningToFood;
                        TargetSelector.RegisterPotentialTarget(LastTrailDroppedPoint, "Collided with smell");
                        return;
                    case AntState.CarryingFood:
                        _disableTrail = false;
                        State = AntState.ReturningToFood;
                        TargetSelector.ResetMaxPriority();
                        TargetSelector.ClearTarget();
                        TargetSelector.RegisterPotentialTarget(LastTrailDroppedPoint, "Collided with smell");
                        DropOffFood(smellable);
                        return;
                    case AntState.ReturningHome:
                        GoBackToSeekingFood();
                        return;
                }
                return;
        }
    }

    private void GoBackToSeekingFood()
    {
        PositionProvider.RandomiseVector();
        _disableTrail = false;
        State = AntState.SeekingFood;
        TargetSelector.ResetMaxPriority();
        TargetSelector.ClearTarget();
    }

    private void CollectKnownFood(Smellable smellable)
    {
        TargetSelector.ResetMaxPriority();
        TargetSelector.ClearTarget();
        TargetSelector.RegisterPotentialTarget(LastTrailDroppedPoint, "Collected smell");
        UpdateTrailValueForKnownFood();
        PickUpFood(smellable);
    }

    private void ReportFoodWithoutCarryingIt(Smellable smellable)
    {
        UpdateTrailValueForKnownFood();
        State = AntState.ReportingFood;
        TargetSelector.ResetMaxPriority();
        _disableTrail = false;
        TargetSelector.ClearTarget();
        TargetSelector.RegisterPotentialTarget(LastTrailDroppedPoint, "ReportFoodWithoutCarryingIt");
    }

    private void UpdateTrailValueForKnownFood()
    {
        TrailTargetValue = KnownFoodValue;
        _knownNearbyFood.Clear();
    }

    private float KnownFoodValue => _knownNearbyFood.Sum(f => f != null ? f.FoodValue : 0);

    private void EatFoodAtHome(Smellable smellable)
    {
        if (Digestion == null) return;
        var home = smellable.GetComponentInParent<AntNest>();
        Digestion.EatFoodFrom(home);
    }

    private void DropOffFood(Smellable smellable)
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

    private void PickUpFood(Smellable smellable)
    {
        Debug.Assert(CarryPoint != null, "Cannot carry food with no carry point");

        var food = smellable.GetComponentInParent<Food>();
        TrailTargetValue -= food.FoodValue;

        if (food.TryGetComponent<LifetimeController>(out var lifetime))
        {
            lifetime.Reset();
            lifetime.IsRunning = false;
        }

        _carriedFood = food;
        food.transform.position = CarryPoint.position;
        food.Attach(_rigidbody);
        State = AntState.CarryingFood;
    }

    public override void OnDeath()
    {
        if (_carriedFood != null)
        {
            _carriedFood.Detach();
            if (_carriedFood.TryGetComponent<LifetimeController>(out var lifetime))
                lifetime.IsRunning = true;
            _carriedFood = null;
        }
    }

    private TrailPointController LastTrailDroppedPoint =>
        TrailController == null || TrailController.gameObject == null
            ? null
            : TrailController.LastTrailPointController;
}

public enum AntState
{
    /// <summary>
    /// Moving away from the nest to look for food.
    /// </summary>
    SeekingFood,

    /// <summary>
    /// Moving back towards the nest leaving a trail to say where the food was.
    /// </summary>
    ReportingFood,

    /// <summary>
    /// Following a trail towards previously found food.
    /// </summary>
    ReturningToFood,

    /// <summary>
    /// Carrying food home to the nest.
    /// </summary>
    CarryingFood,

    /// <summary>
    /// The ant is returning home without food, e.g. after failing to find food, or exiting the world zone.
    /// </summary>
    ReturningHome
}
