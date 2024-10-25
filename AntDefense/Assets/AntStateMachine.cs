using UnityEngine;

public class AntStateMachine : MonoBehaviour
{
    public AntState State { get; private set; } = AntState.SeekingFood;
    public Smellable CurrentTarget { get; private set; }

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

    private void OnCollisionEnter(Collision collision)
    {
        // TODO this needs to be done by the object that has the collider, so that it can detect teh collisions.
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
        CurrentTarget = null;
    }

    private void UpdateTarget(Smellable smellable)
    {
        if(CurrentTarget == null || smellable.Distance < CurrentTarget.Distance)
        {
            CurrentTarget = smellable;
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
                        return;
                     case AntState.ReturningToFood:
                        Debug.Log("(collision) Has hit actual food " + smellable);
                        State = AntState.CarryingFood;
                        //TODO actually pick up the food!!!!!!
                        return;
                        // otherwise, is trying to get home, so doesn't care about food.
                }
                return;
            case Smell.Home:
                switch (State)
                {
                    case AntState.ReportingFood:
                    case AntState.CarryingFood:
                        Debug.Log("(collision) Returned home " + smellable);
                        State = AntState.ReturningToFood;
                        return;
                        // otherwise doesn't care about home because it's seeking food.
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
