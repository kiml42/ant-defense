using UnityEngine;

public class AntStateMachine : MonoBehaviour
{
    public AntState State = AntState.SeekingFood;

    public Smell SeekingSmell
    {
        get
        {
            switch (State)
            {
                case AntState.SeekingFood:
                case AntState.ReturningToFood:
                    return Smell.Food;
                case AntState.ReportingFood:
                case AntState.CarryingFood:
                    return Smell.Home;
                default:
                    throw new System.Exception("Unknown state " + State);
            }
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

    private void OnCollisionEnter(Collision collision)
    {
        // TODO this needs to be done by the object that has the collider, so that it can detect teh collisions.
        var @object = collision.gameObject;
        if(@object.TryGetComponent<ISmellable>(out var smellable) && smellable.Smell == SeekingSmell)
        {
            ProcessCollision(smellable);
        }
    }

    public void ProcessSmell(ISmellable smellable)
    {
        switch (smellable.Smell)
        {
            case Smell.Food:
                switch (State)
                {
                    case AntState.SeekingFood:
                        State = AntState.ReportingFood;
                        Debug.Log("(Smelled) food: " + smellable + ". Now seeking " + SeekingSmell + ". Now leaving trail " + TrailSmell);
                        return;
                        // in the Returning to food state, maintain the state until the food is actually collided with.
                        // otherwise, is trying to get home, so doesn't care about food.
                }
                return;
            case Smell.Home:
                switch (State)
                {
                    case AntState.ReportingFood:
                    case AntState.CarryingFood:
                        Debug.Log("(Smelled) home " + smellable);
                        State = AntState.ReturningToFood;
                        return;
                        // otherwise doesn't care about home because it's seeking food.
                }
                return;
        }
    }

    private void ProcessCollision(ISmellable smellable)
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
