using UnityEngine;

public class AntStateMachine : MonoBehaviour
{
    // TODO stop ants getting stuck at the end of a trail if teh food has since gone
    // TODO make ants look for their own trail after finding food
    // 
    private Smellable _currentTarget;

    public AntState State = AntState.SeekingFood;

    public float? TurnAroundDuration = null;

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
                    throw new System.Exception("Unknown state " + State);
            }
        }
    }

    private void FixedUpdate()
    {
        if (TurnAroundDuration.HasValue)
        {
            TurnAroundDuration -= Time.deltaTime;
            if(TurnAroundDuration.Value <= 0)
            {
                TurnAroundDuration = null;
            }
        }
        if (transform.position.y < -10)
        {
            Destroy(this.gameObject);
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
        // TODO raycast to check line of sight
        if (CurrentTarget?.IsActual == true)
        {
            // Always stick with an actual smell
            return;
        }

        if (CurrentTarget == null || smellable.IsActual || smellable.Distance < CurrentTarget.Distance)
        {
            _currentTarget = smellable;
            TurnAroundDuration = null;
        }
    }

    private void ProcessCollision(Smellable smellable)
    {
        switch (smellable.Smell)
        {
            case Smell.Food:
                switch (State)
                {
                    case AntState.SeekingFood:
                        Debug.Log("(collision) Found food " + smellable);
                        State = AntState.ReportingFood;
                        TurnAroundDuration = 2;
                        ClearTarget();
                        return;
                     case AntState.ReturningToFood:
                        Debug.Log("(collision) Has hit actual food " + smellable);
                        State = AntState.ReportingFood; // Temporary tuntil they can pick up the food.
                        TurnAroundDuration = 2;
                        ClearTarget();
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
                        Debug.Log("(collision) Returned home " + smellable);
                        State = AntState.ReturningToFood;
                        TurnAroundDuration = 2;
                        ClearTarget();
                        return;
                }
                return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
