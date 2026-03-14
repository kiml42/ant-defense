using Assets.Scripts;
using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AntTargetSelector))]
[RequireComponent(typeof(AntFoodHandler))]
[RequireComponent(typeof(AntFoodHandler))]
public class AntStateMachine : DeathActionBehaviour
{
    public AntState State = AntState.SeekingFood;

    public AntTargetSelector TargetSelector;
    public AntFoodHandler FoodHandler;
    public CarriedObjectHandler CarriedObjectHandler;
    public AntTargetPositionProvider PositionProvider;
    public AntTrailController TrailController;
    public AttackController AttackController;
    public Digestion Digestion;

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

    public float? TrailTargetValue { get; set; }
    public ITargetPriorityCalculator PriorityCalculator { get; private set; }

    private bool _disableTrail;

    private void Awake()
    {
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
            if (FullDebugLogs)
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
                    FoodHandler.RememberNearbyFood(smellable.GetComponent<Food>());
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
                if (this.CarriedObjectHandler != null && this.CarriedObjectHandler.IsCarryingFood) return;
                if (IsScout)
                {
                    if (State == AntState.ReportingFood) return;
                    ReportFoodWithoutCarryingIt(smellable);
                    return;
                }
                var canPickUpFoodFromThisState = State == AntState.SeekingFood || State == AntState.ReturningToFood || State == AntState.ReturningHome;
                if (FoodHandler.IsSmallQuantityOfFood && canPickUpFoodFromThisState && this.CarriedObjectHandler != null)
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
                EatAtHome(smellable);
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

                        if (this.CarriedObjectHandler != null)
                        {
                            this.CarriedObjectHandler.DropOff(smellable);
                        }

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
        FoodHandler.UpdateTrailValueForKnownFood();

        if (this.CarriedObjectHandler != null)
        {
            this.CarriedObjectHandler.PickUp(smellable);
        }
    }

    private void ReportFoodWithoutCarryingIt(Smellable smellable)
    {
        FoodHandler.UpdateTrailValueForKnownFood();
        State = AntState.ReportingFood;
        TargetSelector.ResetMaxPriority();
        _disableTrail = false;
        TargetSelector.ClearTarget();
        TargetSelector.RegisterPotentialTarget(LastTrailDroppedPoint, "ReportFoodWithoutCarryingIt");
    }

    private void EatAtHome(Smellable smellable)
    {
        if (Digestion == null) return;
        var home = smellable.GetComponentInParent<AntNest>();
        Digestion.EatFoodFrom(home);
    }

    public override void OnDeath()
    {
        if (this.CarriedObjectHandler != null)
        {
            this.CarriedObjectHandler.ReleaseCarriedFood();
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
