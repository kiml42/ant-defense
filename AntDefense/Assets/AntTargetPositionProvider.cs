using UnityEngine;

public class AntTargetPositionProvider : MonoBehaviour, ITargetPositionProvider
{
    public float ForwardsBias = 0.1f;
    public float RandomBias = 0.2f;

    /// <summary>
    /// Target position relative to this ant
    /// </summary>
    private Vector3 _targetDirection = Vector3.forward;

    /// <summary>
    /// Target position in world space
    /// </summary>
    public Vector3 TargetPosition => TargetObject?.position ?? transform.position + transform.TransformDirection(_targetDirection);

    public Transform TargetObject => AntStateMachine.CurrentTarget?.TargetPoint;

    public TurnAround? TurnAround => AntStateMachine.TurnAroundState;

    public AntStateMachine AntStateMachine;

    void FixedUpdate()
    {
        if(TargetObject == null)
        {
            _targetDirection += Random.insideUnitSphere * RandomBias + transform.forward * ForwardsBias;
            _targetDirection.y *= 0.1f;
            _targetDirection.Normalize();
        }
        else
        {
            _targetDirection = Vector3.zero;
        }

        Debug.DrawRay(transform.position, transform.TransformDirection(_targetDirection), Color.gray);
        Debug.DrawLine(transform.position, TargetPosition, Color.black);
    }
}

public interface ITargetPositionProvider
{
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
