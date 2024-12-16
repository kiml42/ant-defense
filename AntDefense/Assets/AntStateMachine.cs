using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AntStateMachine : MonoBehaviour
{
    // TODO improve detection of trails that no longer lead to food (e.g. single berry in the world that has been removed)
    // TODO make ants consume food to make it easier to get more ants when there are fewer ants. This could replace teh simpl,e lifetime mechanism.
    public Smellable _currentTarget;
    private Food _carriedFood;
    public AntState State = AntState.SeekingFood;

    public Transform ViewPoint;

    public readonly List<GameObject> Obstacles = new List<GameObject>();

    public AntTargetPositionProvider PositionProvider;

    public Smellable CurrentTarget
    {
        get
        {
            if(_currentTarget == null || _currentTarget.gameObject == null || _currentTarget.transform == null)
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
    private float? _maxTargetTime;

    /// <summary>
    /// Amount to decrace the max target time by if no better target has been found in <see cref="MaxTimeGoingForTrailPoint"/> seconds.
    /// </summary>
    public float GiveUpPenalty = 0.5f;

    /// <summary>
    /// Extra time to be allowed to go for a given target if the ant collides with an obstacle.
    /// Intended to allow it to keep going for the same target for longer while bouncing round the obstacle.
    /// </summary>
    public float CollisionTargetBonus = 0.5f;

    public float GiveUpRecoveryMultiplier = 2f;

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

    private HashSet<Smellable> _newBetterTargets = new HashSet<Smellable>();

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

        // TODO account for berry being destroyed while being carried.
        if (_currentTarget.IsDestroyed())
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
            // TODO This is working!!!! make it neat.
            if (CurrentTarget == null || potentialTarget.IsActual || potentialTarget.DistanceFromTarget < CurrentTarget.DistanceFromTarget)
            {
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

                if (hasLineOfSight)
                {
                    SetTarget(potentialTarget);
                }
            }
        }
        _newBetterTargets.Clear();

        if(State == AntState.ReportingFood && CurrentTarget?.Smell == Smell.Food)
        {
            throw new Exception($"State is {State} so the currnet target should not be food, but it is {CurrentTarget}");
        }

        _timeSinceTargetAquisition += Time.fixedDeltaTime;
        if(CurrentTarget != null)
        {
            Debug.DrawLine(transform.position, CurrentTarget.TargetPoint.position, Color.cyan);
            if(!CurrentTarget.IsActual && _timeSinceTargetAquisition > MaxTimeGoingForTrailPoint)
            {
                //Debug.Log("Hasn't found a better target in " + _timeSinceTargetAquisition + " forgetting " + CurrentTarget);
                _maxTargetTime = CurrentTarget.DistanceFromTarget - GiveUpPenalty;
                ClearTarget();
            }
            else if(!HasLineOfSight(CurrentTarget))
            {
                Console.WriteLine("Lost sight of target!");
                ClearTarget();
            }
        }
        if (_maxTargetTime.HasValue)
        {
            //Debug.Log($"MaxTargetTime {_maxTargetTime}");
            _maxTargetTime += Time.fixedDeltaTime * GiveUpRecoveryMultiplier;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        PositionProvider.NoLongerTouching(collision.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        var world = other.GetComponent<WorldZone>();
        if(world != null /*&& State == AntState.SeekingFood*/)
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
            if(CurrentTarget == smellable)
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

    public void ProcessSmell(Smellable smellable)
    {
        if (smellable?.IsDestroyed() == true)
            return;
        switch (smellable.Smell)
        {
            case Smell.Food:
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
                        _maxTargetTime = null;
                        ClearTarget();
                        UpdateTarget(smellable);
                        return;
                    case AntState.ReturningToFood:
                        UpdateTarget(smellable);
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
                        UpdateTarget(smellable);

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

    private void UpdateTarget(Smellable smellable)
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

        if(_maxTargetTime.HasValue && smellable.DistanceFromTarget > _maxTargetTime)
        {
            //Debug.Log("Ignoring " + smellable + " because it's more than " + _maxTargetTime + " from the target.");
            return;
        }

        if (CurrentTarget == null || smellable.IsActual || smellable.DistanceFromTarget < CurrentTarget.DistanceFromTarget)
        {
            if(IsScout && !smellable.IsActual && smellable.Smell == Smell.Food)
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

    private void SetTarget(Smellable smellable)
    {
        _currentTarget = smellable;
        PositionProvider.SetTarget(CurrentTarget);
        _timeSinceTargetAquisition = 0;
    }

    private bool HasLineOfSight(Smellable smellable)
    {
        if (smellable == null)
        {
            return false;
        }
        if (smellable.gameObject == null)
        {
            return false;
        }
        var direction = smellable?.TargetPoint?.position - ViewPoint?.position;

        if (!direction.HasValue)
        {
            Debug.Log("No direction between " + smellable + " & " + ViewPoint);
            return false;
        }
        return true;

        // TODO this still doesn't work!!!
        var rayStart = ViewPoint.position;
        var rayTarget = smellable.transform.position;



        //Debug.DrawRay(rayStart, direction.Value, Color.magenta);

        {
            var isHit = Physics.Raycast(rayStart, direction.Value, out var hit, direction.Value.magnitude * 100);
            var hasLineOfSight = !(isHit && hit.transform != smellable.transform);
            if (isHit)
            {
                Debug.Log("forwards ray Hit " + hit.transform);
                if (!hasLineOfSight)
                {
                    Debug.Log("hit " + hit.transform + " when looking for " + smellable);
                    //return false;
                }
            }
        }
        //reverse ray
        {
            var isHit = Physics.Raycast(rayTarget, -direction.Value, out var hit, direction.Value.magnitude* 100);
            var hasLineOfSight = !(isHit && hit.transform != smellable.transform);
            if (isHit)
            {
                Debug.Log("Reverse ray Hit " + hit.transform);
                if (!hasLineOfSight)
                {
                    Debug.Log("hit " + hit.transform + " when looking for " + smellable);
                    //return false;
                }
            }
        }
        
        



        return true;
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
                    FoundNewFood();
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
                        FoundNewFood();
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
                    State = AntState.SeekingFood;
                    ClearTarget();
                    _maxTargetTime = null;
                    _disableTrail = false;
                    return;
                }
                switch (State)
                {
                    case AntState.ReportingFood:
                        _disableTrail = false;
                        _maxTargetTime = null;
                        ClearTarget();
                        State = AntState.ReturningToFood;
                        UpdateTarget(LastTrailPoint);
                        return;
                    case AntState.CarryingFood:
                        _disableTrail = false;
                        State = AntState.ReturningToFood;
                        _maxTargetTime = null;
                        ClearTarget();
                        UpdateTarget(LastTrailPoint);
                        DropOffFood(smellable);
                        return;
                    case AntState.ReturningHome:
                        _disableTrail = false;
                        State = AntState.SeekingFood;
                        _maxTargetTime = null;
                        ClearTarget();
                        return;
                }
                return;
        }
    }

    private void CollectKnownFood(Smellable smellable)
    {
        _maxTargetTime = null;
        ClearTarget();
        UpdateTarget(LastTrailPoint);
        PickUpFood(smellable);
    }

    private void FoundNewFood()
    {
        //Debug.Log($"State Seeking -> Reporting");
        State = AntState.ReportingFood;
        _maxTargetTime = null;
        _disableTrail = false;
        ClearTarget();
        this.UpdateTarget(LastTrailPoint);
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
        if(_carriedFood == null || _carriedFood.gameObject?.IsDestroyed() != false)
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

        // TODO Cancell this as a target for any that are already targetting it.
        //food.transform.position = CarryPoint.position;

        var lifetime = food.GetComponent<LifetimeController>();
        lifetime.Reset();

        // TODO get the joint to work right
        //var rb = food.GetComponent<Rigidbody>();
        //Destroy(rb);
        //var colliders = rb.GetComponentsInChildren<Collider>();
        //foreach (var collider in colliders)
        //{
        //    collider.enabled = false;
        //}
        //food.transform.parent = this.transform;

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
