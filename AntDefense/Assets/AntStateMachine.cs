using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// TODO split this up into multiple classes, it's getting a bit too big and complicated.
public class AntStateMachine : MonoBehaviour
{
    public Smellable _currentTarget;
    private Food _carriedFood;
    public AntState State = AntState.SeekingFood;

    public Transform ViewPoint;

    public readonly List<GameObject> Obstacles = new();

    public AntTargetPositionProvider PositionProvider;

    public Smellable CurrentTarget
    {
        get
        {
            if (_currentTarget == null || _currentTarget.gameObject == null || _currentTarget.transform == null)
            {
                _currentTarget = null;
            }
            return _currentTarget;
        }
    }

    /// <summary>
    /// The maximum time an ant is allowed to try to move towards a single trail point.
    /// If it takes longer than this it'll give up and look for a better trail point.
    /// </summary>
    public float MaxTimeGoingForTrailPoint = 4;
    private float _timeSinceTargetAquisition;
    private float? _maxTargetPriority;

    /// <summary>
    /// Amount to decrace the max target time by if no better target has been found in <see cref="MaxTimeGoingForTrailPoint"/> seconds.
    /// </summary>
    public float GiveUpPenalty = 0.1f;

    /// <summary>
    /// Extra time to be allowed to go for a given target if the ant collides with an obstacle.
    /// Intended to allow it to keep going for the same target for longer while bouncing round the obstacle.
    /// </summary>
    public float CollisionTargetBonus = 0.5f;

    public float GiveUpRecoveryMultiplier = 4f;

    public const int GroundLayer = 3;

    public AntTrailController TrailController;

    public Transform CarryPoint;

    public Smell? TrailSmell
    {
        get
        {
            if (_disableTrail) return null;
            switch (State)
            {
                case AntState.SeekingFood:
                case AntState.ReturningToFood:
                    TrailTargetValue = null; // no target value for trails to home.
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

    private readonly HashSet<Smellable> _newBetterTargets = new();

    private Rigidbody _rigidbody;

    private void Start()
    {
        ViewPoint = ViewPoint ?? transform;
        _rigidbody = this.GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (transform.position.y < -10)
        {
            Destroy(this.gameObject);
            return;
        }

        if (_currentTarget.IsDestroyed() || CurrentTarget?.IsSmellable == false)
        {
            ClearTarget();
        }

        //test ray -This is successfully detecting obstacles between the ant and the current target!
        if (CurrentTarget != null && ViewPoint != null)
        {
            var end = ViewPoint.position;
            var start = CurrentTarget.transform.position;

            //Debug.DrawRay(start, end - start, Color.magenta);
            var isHit = Physics.Raycast(start, end - start, out var hit, (end - start).magnitude);
            if (isHit)
            {
                //Debug.Log("1. Test ray Hit " + hit.transform);
                if (hit.transform != this.transform)
                {
                    //Debug.Log("1. It's an obstacle!");
                }
            }
        }
        //else
        //{
        //    Debug.Log("Not checking for barriers " + (this == null) + " - " + (ViewPoint == null));
        //}

        foreach (var potentialTarget in _newBetterTargets)
        {
            if (this.IsBetterThanCurrent(potentialTarget))
            {
                //if (!potentialTarget.IsActual && CurrentTarget != null && potentialTarget.DistanceFromTarget > CurrentTarget.DistanceFromTarget)
                //{
                //    Console.WriteLine($"considering {potentialTarget} even though it has a greater distance than {CurrentTarget} because it has a higher priority.");
                //}
                var hasLineOfSight = this.CheckLineOfSight(potentialTarget);

                if (hasLineOfSight)
                {
                    SetTarget(potentialTarget);
                }
            }
        }
        _newBetterTargets.Clear();

        if (State == AntState.ReportingFood && CurrentTarget?.Smell == Smell.Food)
        {
            throw new Exception($"State is {State} so the currnet target should not be food, but it is {CurrentTarget}");
        }

        _timeSinceTargetAquisition += Time.fixedDeltaTime;
        if (CurrentTarget != null)
        {
            Debug.DrawLine(transform.position, CurrentTarget.TargetPoint.position, Color.cyan);
            if (!CurrentTarget.IsActual && _timeSinceTargetAquisition > MaxTimeGoingForTrailPoint)
            {
                _maxTargetPriority = CurrentTarget.Smell == Smell.Home
                    ? null  // Continue to accept any home smell after forgetting this one.
                    : CurrentTarget.GetPriority(CalculatePriority) - GiveUpPenalty; // Only accept better food smells after forgetting this one.
                //Debug.Log("Hasn't found a better target in " + _timeSinceTargetAquisition + " forgetting " + CurrentTarget + ". MaxTargetTime = " + _maxTargetTime);
                ClearTarget();
            }
            else if (!CheckLineOfSight(CurrentTarget))
            {
                Console.WriteLine("Lost sight of current target!");
                ClearTarget();
            }
        }
        if (_maxTargetPriority.HasValue)
        {
            //Debug.Log($"MaxTargetTime {_maxTargetTime}");
            _maxTargetPriority += Time.fixedDeltaTime * GiveUpRecoveryMultiplier;
        }
    }

    private bool CheckLineOfSight(Smellable potentialTarget)
    {
        // TODO This is working!!!! make it neat.
        bool hasLineOfSight = false;
        if (potentialTarget != null && ViewPoint != null)
        {
            Vector3 start = potentialTarget.transform.position;
            Vector3 end = ViewPoint.position;
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            // Offset to avoid self-collision
            Vector3 startOffset = start - direction * 0.1f;

            //Debug.DrawRay(startOffset, direction * distance, Color.magenta);
            int layerMask = ~LayerMask.GetMask(LayerMask.LayerToName(2));
            if (Physics.Raycast(startOffset, direction, out RaycastHit hit, distance, layerMask))
            {
                //Debug.Log("Test ray hit: " + hit.transform.name);

                if (hit.transform != this.transform)
                {
                    //Debug.Log("It's an obstacle!");
                    hasLineOfSight = false;
                }
                else
                {
                    hasLineOfSight = true;
                }
            }
            else
            {
                hasLineOfSight = true; // No obstacle in the way
            }
        }
        else
        {
            Debug.Log("Not checking for barriers. Missing target or viewpoint.");
            hasLineOfSight = false;
        }

        return hasLineOfSight;
    }

    private void OnCollisionExit(Collision collision)
    {
        PositionProvider.NoLongerTouching(collision.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        var world = other.GetComponent<WorldZone>();
        if (world != null /*&& State == AntState.SeekingFood*/)
        {
            // TODO work out how well this works from all states. (particularly when not leaving a trail from home)
            //Debug.Log($"State {State} -> ReturningHome");
            State = AntState.ReturningHome;
            SetTarget(LastTrailPoint);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var @object = collision.gameObject;

        if (@object.TryGetComponent<Smellable>(out var smellable))
        {
            //Debug.Log($"Collided With {@object} smellable: {smellable}");
            ProcessCollisionWithSmell(smellable);
            if (CurrentTarget == smellable)
            {
                // this is now the target, and therefore is not an obstacle.
                return;
            }
        }

        if (@object.layer == GroundLayer)
        {
            // collided with the ground
            //Debug.Log($"Collided With ground: {@object}");
            return;
        }

        //Debug.Log($"Collided With obstacle {@object}");

        // TODO test this more.
        // This sort of works, but it often leaves ants stuck on a wall when there are too many of them for them to move freely round it with their obstacle avoidance.
        // Possibly try considering the distance travelled, or the moving average speed, or something like that to detect when the ant is actually stuck and hasn't moved much in the last couple of seconds.
        // Also, make it only work for barriers, not other ants, so you don't get many ants going for a dead trail point and bumping each other forever.

        // allow more time going for the same target as the obstacle avoidence will hopefully be helping the ant work towards this target.
        _timeSinceTargetAquisition -= CollisionTargetBonus;

        // TODO consider only detecting collisions at the front of the ant for this so it doesn't try to avoid things hitting it from behind.
        PositionProvider.AvoidObstacle(collision);
    }

    /// <summary>
    /// Scouts only look for new food and leave trails to show where it is, they never actually carry it themselves.
    /// </summary>
    public bool IsScout = false;

    private readonly HashSet<Food> _knownNearbyFood = new HashSet<Food>();

    public void ProcessSmell(Smellable smellable)
    {
        if (smellable?.IsDestroyed() == true || !smellable.IsSmellable)
            return;

        switch (smellable.Smell)
        {
            case Smell.Food:
                if (smellable.IsActual && State == AntState.SeekingFood || State == AntState.ReturningToFood)
                {
                    _maxTargetPriority = null;
                    // When seeking or returning to food, rember smelled foods to know how much is in the area when setting the trail back home.
                    _knownNearbyFood.Add(smellable.GetComponent<Food>());
                }
                switch (State)
                {
                    case AntState.SeekingFood:
                        // has smelled a food or a food trail, follow the trail, or move towards the food!
                        if (!smellable.IsActual)
                        {
                            if (IsScout)
                            {
                                // Scouts always ignore food smells except actual food.
                                return;
                            }
                            // has found an existing trail, so retrn to the food and pick it up.
                            State = AntState.ReturningToFood;
                        }
                        if (IsScout && !smellable.IsPermanentSource)
                        {
                            // Scouts only care about permanent sources of food.
                            return;
                        }
                        _maxTargetPriority = null;
                        ClearTarget();
                        RegisterPotentialTarget(smellable);
                        return;
                    case AntState.ReturningToFood:
                        RegisterPotentialTarget(smellable);
                        // in the Returning to food state, maintain the state until the food is actually collided with.
                        return;
                }
                return;
            case Smell.Home:
                switch (State)
                {
                    case AntState.ReturningHome:
                    case AntState.ReportingFood:
                    case AntState.CarryingFood:
                        RegisterPotentialTarget(smellable);

                        return;
                }
                return;
        }
    }

    private void ClearTarget()
    {
        _newBetterTargets.Clear();
        _currentTarget = null;
        PositionProvider.SetTarget(CurrentTarget);
    }

    /// <summary>
    /// Returns the priority of the smellable for this ant.
    /// Lower values are more important.
    /// </summary>
    /// <param name="smellable"></param>
    /// <returns></returns>
    private float CalculatePriority(float distanceFromTarget, float? targetValue)
    {
        return distanceFromTarget - (targetValue ?? 0);
    }

    private void RegisterPotentialTarget(Smellable smellable)
    {
        if (State == AntState.ReportingFood && smellable.Smell == Smell.Food)
        {
            throw new Exception($"State is {State} so {smellable} should not be being considered as a possible target.");
        }

        if (CurrentTarget?.IsActual == true)
        {
            // Always stick with an actual smell
            return;
        }

        if (_maxTargetPriority.HasValue && smellable.GetPriority(CalculatePriority) > _maxTargetPriority)
        {
            //Debug.Log("Ignoring " + smellable + " because it's more than " + _maxTargetTime + " from the target.");
            return;
        }

        if (this.IsBetterThanCurrent(smellable))
        {
            if (IsScout && !smellable.IsActual && smellable.Smell == Smell.Food)
            {
                throw new Exception("Scouts should never go for food smells!");
            }
            _newBetterTargets.Add(smellable);
            //// TODO thoroughly test this and refactor it to be neater if it works.
            //bool hasLineOfSight;
            //Debug.Log("Checking for obstacles betwen " + this + " and " + smellable);
            //if (smellable != null && ViewPoint != null)
            //{
            //    var end = ViewPoint.position;
            //    var start = smellable.transform.position;

            //    Debug.DrawRay(start, end - start, Color.magenta);
            //    var isHit = Physics.Raycast(start, end - start, out var hit, (end - start).magnitude);
            //    if (isHit)
            //    {
            //        Debug.Log("Test2 ray Hit " + hit.transform);
            //        if (hit.transform != this.transform)
            //        {
            //            hasLineOfSight = false;
            //            Debug.Log("It's an obstacle!");
            //        }
            //        else
            //        {
            //            hasLineOfSight = true;
            //            Debug.Log("Wasn't an obstacle");
            //        }
            //    }
            //    else
            //    {
            //        hasLineOfSight = true;
            //        Debug.Log("Didn't hit anything");
            //    }
            //}
            //else
            //{
            //    hasLineOfSight = false;
            //    Debug.Log("Not checking for barriers");
            //}


            //var hasLineOfSight = HasLineOfSight(smellable);
            //if (hasLineOfSight)
            //{
            //    // either there is no hit (no rigidbody int he way) or the hit is the thing we're trying to move towards.
            //    SetTarget(smellable);
            //}
        }
    }

    private bool IsBetterThanCurrent(Smellable smellable)
    {
        return CurrentTarget == null    // there is no current target
            || (smellable.IsActual && !CurrentTarget.IsActual)  // the new one is actual and the current one isn't
            || smellable.GetPriority(CalculatePriority) < CurrentTarget.GetPriority(CalculatePriority); // the new one has a better priority than the current one
    }

    private void SetTarget(Smellable smellable)
    {
        _currentTarget = smellable;
        PositionProvider.SetTarget(CurrentTarget);
        _timeSinceTargetAquisition = 0;
    }

    private void ProcessCollisionWithSmell(Smellable smellable)
    {
        switch (smellable.Smell)
        {
            case Smell.Food:

                if (_carriedFood != null) return;   // ignore all other food if already carrying
                if (IsScout)
                {
                    if (!smellable.IsPermanentSource || State == AntState.ReportingFood) return;
                    ReportFoodWithoutCarryingIt(smellable);
                    return;
                }
                if (!smellable.IsPermanentSource && (State == AntState.SeekingFood || State == AntState.ReturningToFood || State == AntState.ReturningHome))
                {
                    // it's a one-off, so just take it home.
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
                    this.GoBackToSeekingFood();
                    return;
                }
                switch (State)
                {
                    case AntState.ReportingFood:
                        _disableTrail = false;
                        _maxTargetPriority = null;
                        ClearTarget();
                        State = AntState.ReturningToFood;
                        RegisterPotentialTarget(LastTrailPoint);
                        return;
                    case AntState.CarryingFood:
                        _disableTrail = false;
                        State = AntState.ReturningToFood;
                        _maxTargetPriority = null;
                        ClearTarget();
                        RegisterPotentialTarget(LastTrailPoint);
                        DropOffFood(smellable);
                        return;
                    case AntState.ReturningHome:
                        this.GoBackToSeekingFood();
                        return;
                }
                return;
        }
    }

    private void GoBackToSeekingFood()
    {
        this.PositionProvider.RandomiseVector();
        _disableTrail = false;
        State = AntState.SeekingFood;
        _maxTargetPriority = null;
        ClearTarget();
    }

    private void CollectKnownFood(Smellable smellable)
    {
        _maxTargetPriority = null;
        ClearTarget();
        RegisterPotentialTarget(LastTrailPoint);
        this.UpdateTrailValueForKnownFood();
        PickUpFood(smellable);
        smellable.IsSmellable = false;  // TODO consider when/if to turn this back on. (e.g. if the ant dies while carrying the food, or drops the food)
    }

    private void ReportFoodWithoutCarryingIt(Smellable smellable)
    {
        var food = smellable.GetComponentInParent<Food>();

        this.UpdateTrailValueForKnownFood();

        //Debug.Log($"State Seeking -> Reporting");
        State = AntState.ReportingFood;
        _maxTargetPriority = null;
        _disableTrail = false;
        ClearTarget();
        this.RegisterPotentialTarget(LastTrailPoint);
    }

    private void UpdateTrailValueForKnownFood()
    {
        // Leave a trail indicating how much food has been found at this location.
        var remainingFoodValue = _knownNearbyFood.Sum(f => f?.FoodValue ?? 0);
        this.TrailTargetValue = remainingFoodValue;
        _knownNearbyFood.Clear();   // Forget about all the food now it knows what value to use for the trail.
    }

    public Digestion Digestion;
    private bool _disableTrail;

    private void EatFoodAtHome(Smellable smellable)
    {
        if (Digestion == null) return;
        var home = smellable.GetComponentInParent<AntNest>();

        Digestion.EatFoodFrom(home);
    }

    private void DropOffFood(Smellable smellable)
    {
        if (_carriedFood == null || _carriedFood.gameObject?.IsDestroyed() != false)
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
        if (CarryPoint == null)
        {
            throw new Exception("Cannot carry food with no carry point");
        }

        var food = smellable.GetComponentInParent<Food>();

        // adjust the rail target value to account for this food being removed.
        this.TrailTargetValue -= food.FoodValue;

        var lifetime = food.GetComponent<LifetimeController>();
        if (lifetime != null)
        {
            lifetime.Reset();
        }

        PickUpFood(food);

        State = AntState.CarryingFood;
    }

    private void PickUpFood(Food food)
    {
        _carriedFood = food;
        food.transform.position = CarryPoint.position;
        food.Attach(_rigidbody);
    }

    private Smellable LastTrailPoint
    {
        get
        {
            if (TrailController?.gameObject == null)
                return null;
            return TrailController.LastTrailPoint;
        }
    }
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
    ReturningHome
}
