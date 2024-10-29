using System;
using UnityEngine;

public class AntStateMachine : MonoBehaviour
{
    // TODO stop ants getting stuck at the end of a trail if teh food has since gone
 
    private Smellable _currentTarget;

    public AntState State = AntState.SeekingFood;

    public float? TurnAroundDuration = null;

    public LifetimeController LifetimeController;

    public Transform ViewPoint;

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

    private float _timeSinceTargetAquisition;
    private float? _maxTargetTime;

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

        if (TurnAroundDuration.HasValue)
        {
            TurnAroundDuration -= Time.deltaTime;
            if(TurnAroundDuration.Value <= 0)
            {
                TurnAroundDuration = null;
            }
        }
        _timeSinceTargetAquisition += Time.deltaTime;
        if(CurrentTarget != null)
        {
            if(_timeSinceTargetAquisition > 2)
            {
                Debug.Log("Hasn't found a better target in " + _timeSinceTargetAquisition);
                _maxTargetTime = CurrentTarget.TimeFromTarget;
                ClearTarget();
            }
            else if(!HasLineOfSight(CurrentTarget))
            {
                Console.WriteLine("Lost sight of target!");
                ClearTarget();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var @object = collision.gameObject;
        if(@object.TryGetComponent<Smellable>(out var smellable))
        {
            ProcessCollision(smellable);
        }
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
                TurnAroundDuration = null;
                _timeSinceTargetAquisition = 0;
            }
        }
    }

    private bool HasLineOfSight(Smellable smellable)
    {
        var direction = smellable?.transform?.position - ViewPoint?.position;

        if (!direction.HasValue)
        {
            Debug.Log("No direction between " + smellable + " & " + ViewPoint);
            return false;
        }

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

    private void ProcessCollision(Smellable smellable)
    {
        switch (smellable.Smell)
        {
            case Smell.Food:
                switch (State)
                {
                    case AntState.SeekingFood:
                        State = AntState.ReportingFood;
                        _maxTargetTime = null;
                        TurnAroundDuration = 2;
                        ClearTarget();
                        ResetLifetime();
                        return;
                     case AntState.ReturningToFood:
                        State = AntState.ReportingFood; // Temporary tuntil they can pick up the food.
                        _maxTargetTime = null;
                        TurnAroundDuration = 2;
                        ClearTarget();
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
                        TurnAroundDuration = 2;
                        _maxTargetTime = null;
                        ClearTarget();
                        ResetLifetime();
                        return;
                }
                return;
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
