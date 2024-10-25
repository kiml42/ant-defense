using UnityEngine;

public class AntStateMachine : MonoBehaviour
{
    // TODO stop ants getting stuck at the end of a trail if teh food has since gone
    // TODO make ants look for their own trail after finding food
    // 
    public float RemainingTime = 120;
    private Smellable _currentTarget;

    public AntState State = AntState.SeekingFood;
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
        RemainingTime -= Time.deltaTime;
        if (RemainingTime <= 0 || transform.position.y < -10)
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

                        if (smellable.Distance == 0)
                        {
                            // Has smelled the actual food, so go back and report it
                            State = AntState.ReportingFood;
                            ClearTarget();
                        }
                        else
                        {
                            // has smelled a food trail, so go and get some food!
                            State = AntState.ReturningToFood;
                            ClearTarget();
                            UpdateTarget(smellable);
                        }

                        Debug.Log("(Smelled) food: " + smellable + ". Now leaving trail " + TrailSmell);
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
                        if(smellable.Distance == 0)
                        {
                            // Has smelled the actual home, so the trail it's currently leaving is close enough
                            State = AntState.ReturningToFood;
                            ClearTarget();
                        }
                        else
                        {
                            UpdateTarget(smellable);
                        }
                        return;
                    case AntState.CarryingFood:
                        // wait until colliding with home to change state.
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

        if (CurrentTarget == null || smellable.IsActual || smellable.Distance < CurrentTarget.Distance)
        {
            _currentTarget = smellable;
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
                        ClearTarget();
                        return;
                     case AntState.ReturningToFood:
                        Debug.Log("(collision) Has hit actual food " + smellable);
                        State = AntState.ReportingFood; // Temporary tuntil they can pick up the food.
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
