using UnityEngine;

//TODO - Make turn around work by setting the heading direction rather than just generally turning it around.
//the direction can then be allowed to wander back normal with its random movements.
// For obstacles the target position should be set to a position along the tangent of teh collision.
// For turning around to find the way back it should be set behind the ant.
// When the ant has a target it's moving towards,don't just use that location imediately, instead have the target wander back towards it.
public class AntTargetPositionProvider : MonoBehaviour, ITargetPositionProvider
{
    public float ForwardsBias = 0.1f;
    public float RandomBias = 0.2f;

    /// <summary>
    /// Target position relative to this ant
    /// </summary>
    private Vector3 _targetPosition = Vector3.zero;

    public Vector3 TargetPosition => TargetObject?.position ?? transform.position + _targetPosition;

    public Transform TargetObject => AntStateMachine.CurrentTarget?.TargetPoint;

    public TurnAround? TurnAround => AntStateMachine.TurnAroundState;

    private AntStateMachine AntStateMachine;

    void Start()
    {
        AntStateMachine = GetComponentInChildren<AntStateMachine>();
    }

    void FixedUpdate()
    {
        if(TargetObject == null)
        {
            _targetPosition += Random.insideUnitSphere * RandomBias + transform.forward * ForwardsBias;
            _targetPosition.y *= 0.1f;
            _targetPosition.Normalize();
        }
        else
        {
            _targetPosition = TargetObject.position;
        }
    }
}

public interface ITargetPositionProvider
{
    Transform TargetObject { get; }
    Vector3 TargetPosition { get; }

    TurnAround? TurnAround { get; }
}

public readonly struct TurnAround
{
    public readonly TurnAroundMode Mode;
    public readonly bool Clockwise;
    public bool Move => Mode != TurnAroundMode.LookAround;

    private TurnAround(TurnAroundMode mode, bool clockwise)
    {
        Mode = mode;
        Clockwise = clockwise;
    }

    public static TurnAround AvoidObstacle(bool clockwise) => new TurnAround (TurnAroundMode.AvoidObstacle, clockwise);
    public static TurnAround LookAround(bool clockwise) => new TurnAround (TurnAroundMode.LookAround, clockwise);
}

public enum TurnAroundMode
{
    LookAround,
    AvoidObstacle
}
