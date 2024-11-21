using System;
using System.Collections.Generic;
using UnityEngine;

public class AntStateMachine : MonoBehaviour
{
    private Smellable _currentTarget;

    public AntState State = AntState.SeekingFood;

    public LifetimeController LifetimeController;

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

    public Smell TrailSmell
    {
        get
        {
            switch (State)
            {
                case AntState.SeekingFood:
                case AntState.ReturningToFood:
                    return Smell.Home;
                case AntState.ReportingFood:
                case AntState.CarryingFood:
                    return Smell.Food;
                default:
                    throw new Exception("Unknown state " + State);
            }
        }
    }

    private void Start()
    {
        ViewPoint = ViewPoint ?? transform;
    }

    private void FixedUpdate()
    {
        if (transform.position.y < -10)
        {
            Destroy(this.gameObject);
            return;
        }

        _timeSinceTargetAquisition += Time.fixedDeltaTime;
        if(CurrentTarget != null)
        {
            Debug.DrawLine(transform.position, CurrentTarget.TargetPoint.position, Color.cyan);
            if(!CurrentTarget.IsActual && _timeSinceTargetAquisition > MaxTimeGoingForTrailPoint)
            {
                //Debug.Log("Hasn't found a better target in " + _timeSinceTargetAquisition + " forgetting " + CurrentTarget);
                _maxTargetTime = CurrentTarget.TimeFromTarget - GiveUpPenalty;
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
        // allow more time going for the same target as the obstacle avoidence will hopefully be helping the ant work towards this target.
        _timeSinceTargetAquisition -= CollisionTargetBonus;

        // TODO consider only detecting collisions at the front of the ant for this so it doesn't try to avoid things hitting it from behind.
        PositionProvider.AvoidObstacle(collision);
    }

    public void ProcessSmell(Smellable smellable)
    {
        switch (smellable.Smell)
        {
            case Smell.Food:
                switch (State)
                {
                    case AntState.SeekingFood:
                        // has smelled a food or a food trail, follow the trail, or move towards the food!
                        State = AntState.ReturningToFood;
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
        _currentTarget = null;
        PositionProvider.SetTarget(CurrentTarget);
    }

    private void UpdateTarget(Smellable smellable)
    {
        if (CurrentTarget?.IsActual == true)
        {
            // Always stick with an actual smell
            return;
        }

        if(_maxTargetTime.HasValue && smellable.TimeFromTarget > _maxTargetTime)
        {
            //Debug.Log("Ignoring " + smellable + " because it's more than " + _maxTargetTime + " from the target.");
            return;
        }

        if (CurrentTarget == null || smellable.IsActual || smellable.TimeFromTarget < CurrentTarget.TimeFromTarget)
        {
            var hasLineOfSight = HasLineOfSight(smellable);
            if (hasLineOfSight)
            {
                // either there is no hit (no rigidbody int he way) or the hit is the thing we're trying to move towards.
                _currentTarget = smellable;
                PositionProvider.SetTarget(CurrentTarget);
                _timeSinceTargetAquisition = 0;
            }
        }
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
        Debug.DrawRay(ViewPoint.position, direction.Value, Color.magenta);

        var isHit = Physics.Raycast(ViewPoint.position, direction.Value, out var hit, direction.Value.magnitude, 0);
        var hasLineOfSight = !(isHit && hit.transform != smellable.transform);
        if (isHit)
        {
            Debug.Log("Hit " + hit.transform);
            if (!hasLineOfSight)
            {
                Debug.Log("hit " + hit.transform + " when looking for " + smellable);
            }
        }

        return hasLineOfSight;
    }

    private void ProcessCollisionWithSmell(Smellable smellable)
    {
        switch (smellable.Smell)
        {
            case Smell.Food:
                switch (State)
                {
                    case AntState.SeekingFood:
                        State = AntState.ReportingFood;
                        _maxTargetTime = null;
                        ClearTarget();
                        this.UpdateTarget(LastTrailPoint);
                        ResetLifetime();
                        return;
                    case AntState.ReturningToFood:
                        State = AntState.ReportingFood; // Temporary tuntil they can pick up the food.
                        _maxTargetTime = null;
                        ClearTarget();
                        UpdateTarget(LastTrailPoint);
                        ResetLifetime();
                        return;

                        //TODO actually pick up the food!!!!!!
                        State = AntState.CarryingFood;
                        return;
                }
                return;
            case Smell.Home:
                switch (State)
                {
                    case AntState.ReportingFood:
                    case AntState.CarryingFood:
                        State = AntState.ReturningToFood;
                        _maxTargetTime = null;
                        ClearTarget();
                        UpdateTarget(LastTrailPoint);
                        ResetLifetime();
                        return;
                }
                return;
        }
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

    private void ResetLifetime()
    {
        if(LifetimeController != null)
        {
            LifetimeController.Reset();
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
    CarryingFood
}
