using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// TODO split this up into multiple classes, it's getting a bit too big and complicated.
public class AntStateMachine : DeathActionBehaviour
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
            if (this._currentTarget == null || this._currentTarget.gameObject == null || this._currentTarget.transform == null)
            {
                this._currentTarget = null;
            }
            return this._currentTarget;
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

    /// <summary>
    /// If the ant has found a total food value less than or equal to this amount, it will just carry the food home while reporting it, instead of just reporting it.
    /// The idea is to prioritise summoning more ants if there's a larger amount of food, but to just get the food home now if it's a smaller amount.
    /// </summary>
    public float LimitForReporitingOnly = 50;
    public AntTrailController TrailController;

    public Transform CarryPoint;

    public Smell? TrailSmell
    {
        get
        {
            if (this._disableTrail) return null;
            switch (this.State)
            {
                case AntState.SeekingFood:
                case AntState.ReturningToFood:
                    this.TrailTargetValue = null; // no target value for trails to home.
                    return Smell.Home;
                case AntState.ReportingFood:
                case AntState.CarryingFood:
                    return Smell.Food;
                case AntState.ReturningHome:
                    return null;
                default:
                    throw new Exception("Unknown state " + this.State);
            }
        }
    }

    public float? TrailTargetValue { get; private set; }

    private readonly HashSet<Smellable> _newBetterTargets = new();

    private Rigidbody _rigidbody;
    private ITargetPriorityCalculator _priorityCalculator;

    /// <summary>
    /// If the last trail point has this much time or less remaining when it is placed, the ant will give up and return home.
    /// </summary>
    public float GoHomeTime = 2f;

    private void Start()
    {
        this.ViewPoint = this.ViewPoint != null ? this.ViewPoint : this.transform;
        this._rigidbody = this.GetComponent<Rigidbody>();
        this._priorityCalculator = this.GetComponent<ITargetPriorityCalculator>();
    }

    private void FixedUpdate()
    {
        if (this.transform.position.y < -10)
        {
            Destroy(this.gameObject);
            return;
        }

        if (this._currentTarget.IsDestroyed() || (this.CurrentTarget != null && this.CurrentTarget.IsSmellable == false))
        {
            this.ClearTarget();
        }

        if (this.LastTrailPoint != null && this.LastTrailPoint.RemainingTime < this.GoHomeTime)
        {
            //Debug.Log($"Ant {this} last trail point {this.LastTrailPoint} has only {this.LastTrailPoint.RemainingTime} time remaining, going home.");
            this.GiveUpAndReturnHome();
        }

        //test ray -This is successfully detecting obstacles between the ant and the current target!
        if (this.CurrentTarget != null && this.ViewPoint != null)
        {
            var end = this.ViewPoint.position;
            var start = this.CurrentTarget.transform.position;

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

        foreach (var potentialTarget in this._newBetterTargets)
        {
            if (this.IsBetterThanCurrent(potentialTarget))
            {
                //if (!potentialTarget.IsActual && CurrentTarget != null && potentialTarget.DistanceFromTarget > CurrentTarget.DistanceFromTarget)
                //{
                //    Debug.Log($"considering {potentialTarget} even though it has a greater distance than {CurrentTarget} because it has a higher priority.");
                //}
                var hasLineOfSight = this.CheckLineOfSight(potentialTarget);

                if (hasLineOfSight)
                {
                    this.SetTarget(potentialTarget);
                }
            }
        }
        this._newBetterTargets.Clear();

        Debug.Assert(this.State != AntState.ReportingFood || this.CurrentTarget == null || this.CurrentTarget.Smell != Smell.Food, $"State is {this.State} so the currnet target should not be food, but it is {this.CurrentTarget}");

        this._timeSinceTargetAquisition += Time.deltaTime;
        if (this.CurrentTarget != null)
        {
            Debug.DrawLine(this.transform.position, this.CurrentTarget.TargetPoint.position, Color.cyan);
            if (!this.CurrentTarget.IsActual && this._timeSinceTargetAquisition > this.MaxTimeGoingForTrailPoint)
            {
                this._maxTargetPriority = this.CurrentTarget.Smell == Smell.Home
                    ? null  // Continue to accept any home smell after forgetting this one.
                    : this.CurrentTarget.GetPriority(this._priorityCalculator) - this.GiveUpPenalty; // Only accept better food smells after forgetting this one.
                //Debug.Log("Hasn't found a better target in " + _timeSinceTargetAquisition + " forgetting " + CurrentTarget + ". MaxTargetTime = " + _maxTargetTime);
                this.ClearTarget();
            }
            else if (!this.CheckLineOfSight(this.CurrentTarget))
            {
                //Debug.Log("Lost sight of current target!");
                this.ClearTarget();
            }
        }
        if (this._maxTargetPriority.HasValue)
        {
            //Debug.Log($"MaxTargetTime {_maxTargetTime}");
            this._maxTargetPriority += Time.deltaTime * this.GiveUpRecoveryMultiplier;
        }
    }

    private bool CheckLineOfSight(Smellable potentialTarget)
    {
        // TODO This is working!!!! make it neat.
        bool hasLineOfSight;
        if (potentialTarget != null && this.ViewPoint != null)
        {
            Vector3 start = potentialTarget.transform.position;
            Vector3 end = this.ViewPoint.position;
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            // Offset to avoid self-collision
            Vector3 startOffset = start - (direction * 0.1f);

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
            //Debug.Log("Not checking for barriers. Missing target or viewpoint.");
            hasLineOfSight = false;
        }

        return hasLineOfSight;
    }

    private void OnCollisionExit(Collision collision)
    {
        this.PositionProvider.NoLongerTouching(collision.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        var world = other.GetComponent<WorldZone>();
        if (world != null /*&& State == AntState.SeekingFood*/)
        {
            // TODO work out how well this works from all states. (particularly when not leaving a trail from home)
            //Debug.Log($"State {State} -> ReturningHome");
            this.GiveUpAndReturnHome();
        }
    }

    private void GiveUpAndReturnHome()
    {
        //Debug.Log($"Ant {this} giving up and returning home from state {this.State}");
        this.State = AntState.ReturningHome;
        if (this.LastTrailPoint == null || this.LastTrailPoint.Smell != Smell.Home)
        {
            // not leaving a trail towards home, so can't use it to go home.
            return;
        }
        this.SetTarget(this.LastTrailPoint);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var @object = collision.gameObject;

        if (@object.TryGetComponent<Smellable>(out var smellable))
        {
            //Debug.Log($"Collided With {@object} smellable: {smellable}");
            this.ProcessCollisionWithSmell(smellable);
            if (smellable.Smell == Smell.Home)
            {
                return; // never consider home smells as obstacles.
            }
            if (this.State == AntState.SeekingFood || this.State == AntState.ReturningToFood)
            {
                return; // while seeking or returning to food, ignore collisions with smellables that aren't the target.
            }
        }
        if (@object.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            // TODO consider a differnt evasion approach for other ants.
            return; // ignore collisions with rigidbodies (other ants, moving objects, etc)
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
        this._timeSinceTargetAquisition -= this.CollisionTargetBonus;

        // TODO: keep trying to attack while still colliding with the wall.
        var didAttack = false;
        if (this.AttackController != null)
        {
            if (UnityEngine.Random.Range(0, 100) <= this.AttackController.AttackChance)
            {
                var damageHandler = @object.GetComponentInParent<ImpactDamageHandler>();
                if (damageHandler != null)
                {
                    if (damageHandler.HealthController.tag != this.tag)
                    {
                        didAttack = this.AttackController.AttackObstable(collision, damageHandler);
                    }
                }
            }
        }


        if (!didAttack)
        {
            this.PositionProvider.AvoidObstacle(collision);
        }
    }

    public AttackController AttackController;

    /// <summary>
    /// Scouts only look for new food and leave trails to show where it is, they never actually carry it themselves.
    /// </summary>
    public bool IsScout = false;

    private readonly HashSet<Food> _knownNearbyFood = new HashSet<Food>();

    public void ProcessSmell(Smellable smellable)
    {
        if ((smellable != null && smellable.IsDestroyed() == true) || !smellable.IsSmellable)
            return;

        switch (smellable.Smell)
        {
            case Smell.Food:
                if ((smellable.IsActual && this.State == AntState.SeekingFood) || this.State == AntState.ReturningToFood)
                {
                    this._maxTargetPriority = null;
                    // When seeking or returning to food, rember smelled foods to know how much is in the area when setting the trail back home.
                    this._knownNearbyFood.Add(smellable.GetComponent<Food>());
                }
                switch (this.State)
                {
                    case AntState.SeekingFood:
                        // has smelled a food or a food trail, follow the trail, or move towards the food!
                        if (!smellable.IsActual)
                        {
                            if (this.IsScout)
                            {
                                // Scouts always ignore food smells except actual food.
                                return;
                            }
                            // has found an existing trail, so retrn to the food and pick it up.
                            this.State = AntState.ReturningToFood;
                        }
                        this._maxTargetPriority = null;
                        this.ClearTarget();
                        this.RegisterPotentialTarget(smellable);
                        return;
                    case AntState.ReturningToFood:
                        this.RegisterPotentialTarget(smellable);
                        // in the Returning to food state, maintain the state until the food is actually collided with.
                        return;
                }
                return;
            case Smell.Home:
                switch (this.State)
                {
                    case AntState.ReturningHome:
                    case AntState.ReportingFood:
                    case AntState.CarryingFood:
                        this.RegisterPotentialTarget(smellable);

                        return;
                }
                return;
        }
    }

    private void ClearTarget()
    {
        this._newBetterTargets.Clear();
        this._currentTarget = null;
        this.PositionProvider.SetTarget(this.CurrentTarget);
    }

    private void RegisterPotentialTarget(Smellable smellable)
    {
        Debug.Assert(this.State != AntState.ReportingFood || smellable.Smell != Smell.Food, $"State is {this.State} so {smellable} should not be being considered as a possible target.");

        if (this.CurrentTarget != null && this.CurrentTarget.IsActual == true)
        {
            // Always stick with an actual smell
            return;
        }

        if (this._maxTargetPriority.HasValue && smellable.GetPriority(this._priorityCalculator) > this._maxTargetPriority)
        {
            //Debug.Log("Ignoring " + smellable + " because it's more than " + _maxTargetTime + " from the target.");
            return;
        }

        if (this.IsBetterThanCurrent(smellable))
        {
            Debug.Assert(!this.IsScout || smellable.IsActual || smellable.Smell != Smell.Food);
            this._newBetterTargets.Add(smellable);

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
        return this.CurrentTarget == null    // there is no current target
            || (smellable.IsActual && !this.CurrentTarget.IsActual)  // the new one is actual and the current one isn't
            || smellable.GetPriority(this._priorityCalculator) < this.CurrentTarget.GetPriority(this._priorityCalculator); // the new one has a better priority than the current one
    }

    private void SetTarget(Smellable smellable)
    {
        this._currentTarget = smellable;
        this.PositionProvider.SetTarget(this.CurrentTarget);
        this._timeSinceTargetAquisition = 0;
    }

    private void ProcessCollisionWithSmell(Smellable smellable)
    {
        switch (smellable.Smell)
        {
            case Smell.Food:

                if (this._carriedFood != null) return;   // ignore all other food if already carrying
                if (this.IsScout)
                {
                    if (this.State == AntState.ReportingFood) return;
                    this.ReportFoodWithoutCarryingIt(smellable);
                    return;
                }
                var isSmallQuantityOfFood = this._knownNearbyFood.Count == 1 || this.KnownFoodValue <= LimitForReporitingOnly;
                var CanPickUpFoodFromThisState = this.State == AntState.SeekingFood || this.State == AntState.ReturningToFood || this.State == AntState.ReturningHome;
                if (isSmallQuantityOfFood && CanPickUpFoodFromThisState)
                {
                    // it's a one-off, so just take it home.
                    this.CollectKnownFood(smellable);
                    this._disableTrail = true;
                    return;
                }
                switch (this.State)
                {
                    case AntState.SeekingFood:
                        this.ReportFoodWithoutCarryingIt(smellable);
                        return;
                    case AntState.ReturningToFood:
                        this.CollectKnownFood(smellable);
                        this._disableTrail = false;
                        return;
                }
                return;
            case Smell.Home:
                this.EatFoodAtHome(smellable);
                if (this.IsScout)
                {
                    this.GoBackToSeekingFood();
                    return;
                }
                switch (this.State)
                {
                    case AntState.ReportingFood:
                        this._disableTrail = false;
                        this._maxTargetPriority = null;
                        this.ClearTarget();
                        this.State = AntState.ReturningToFood;
                        this.RegisterPotentialTarget(this.LastTrailPoint);
                        return;
                    case AntState.CarryingFood:
                        this._disableTrail = false;
                        this.State = AntState.ReturningToFood;
                        this._maxTargetPriority = null;
                        this.ClearTarget();
                        this.RegisterPotentialTarget(this.LastTrailPoint);
                        this.DropOffFood(smellable);
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
        this._disableTrail = false;
        this.State = AntState.SeekingFood;
        this._maxTargetPriority = null;
        this.ClearTarget();
    }

    private void CollectKnownFood(Smellable smellable)
    {
        this._maxTargetPriority = null;
        this.ClearTarget();
        this.RegisterPotentialTarget(this.LastTrailPoint);
        this.UpdateTrailValueForKnownFood();
        this.PickUpFood(smellable);
    }

    private void ReportFoodWithoutCarryingIt(Smellable smellable)
    {
        //var food = smellable.GetComponentInParent<Food>();

        this.UpdateTrailValueForKnownFood();

        //Debug.Log($"State Seeking -> Reporting");
        this.State = AntState.ReportingFood;
        this._maxTargetPriority = null;
        this._disableTrail = false;
        this.ClearTarget();
        this.RegisterPotentialTarget(this.LastTrailPoint);
    }

    private void UpdateTrailValueForKnownFood()
    {
        // Leave a trail indicating how much food has been found at this location.
        var remainingFoodValue = this.KnownFoodValue;
        this.TrailTargetValue = remainingFoodValue;
        this._knownNearbyFood.Clear();   // Forget about all the food now it knows what value to use for the trail.
    }

    private float KnownFoodValue => this._knownNearbyFood.Sum(f => f != null ? f.FoodValue : 0);

    public Digestion Digestion;
    private bool _disableTrail;

    private void EatFoodAtHome(Smellable smellable)
    {
        if (this.Digestion == null) return;
        var home = smellable.GetComponentInParent<AntNest>();

        this.Digestion.EatFoodFrom(home);
    }

    private void DropOffFood(Smellable smellable)
    {
        if (this._carriedFood == null || this._carriedFood.gameObject == null || this._carriedFood.gameObject.IsDestroyed())
        {
            this._carriedFood = null;
            return;
        }
        var home = smellable.GetComponentInParent<AntNest>();

        home.AddFood(this._carriedFood.FoodValue);

        this._carriedFood.Destroy();
        this._carriedFood = null;
    }

    private void PickUpFood(Smellable smellable)
    {
        Debug.Assert(this.CarryPoint != null, "Cannot carry food with no carry point");

        var food = smellable.GetComponentInParent<Food>();

        // adjust the trail target value to account for this food being removed.
        this.TrailTargetValue -= food.FoodValue;

        if (food.TryGetComponent<LifetimeController>(out var lifetime))
        {
            lifetime.Reset();
            lifetime.IsRunning = false;
        }

        this._carriedFood = food;
        food.transform.position = this.CarryPoint.position;
        food.Attach(this._rigidbody);

        this.State = AntState.CarryingFood;
    }

    public override void OnDeath()
    {
        if (this._carriedFood != null)
        {
            this._carriedFood.Detach();
            if (this._carriedFood.TryGetComponent<LifetimeController>(out var lifetime))
            {
                lifetime.IsRunning = true;
            }
            this._carriedFood = null;
        }
    }

    private TrailPointController LastTrailPoint
    {
        get
        {
            return this.TrailController == null || this.TrailController.gameObject == null
                ? null
                : this.TrailController.LastTrailPoint;
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

    /// <summary>
    /// The ant is returning home without food, e.g. after failing to find food, or exiting the world zone.
    /// </summary>
    ReturningHome
}
