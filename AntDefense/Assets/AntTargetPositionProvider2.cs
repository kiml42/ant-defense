using UnityEngine;

//TODO - Make turn around work by setting the heading direction rather than just generally turning it around.
//the direction can then be allowed to wander back normal with its random movements.
// For obstacles the target position should be set to a position along the tangent of teh collision.
// For turning around to find the way back it should be set behind the ant.
// When the ant has a target it's moving towards,don't just use that location imediately, instead have the target wander back towards it.
public class AntTargetPositionProvider2 : MonoBehaviour, ITargetPositionProvider
{
    public float ForwardsBias = 0.1f;
    public float TargetBias = 1.2f;
    public float RandomBias = 0.2f;

    public Vector3 LocalTargetDirection {  get; private set; }
    public Vector3 WorldTargetDirection => transform.TransformDirection(LocalTargetDirection);

    /// <summary>
    /// Target position in world space
    /// </summary>
    public Vector3 TargetPosition => transform.position + WorldTargetDirection;

    /// <summary>
    /// The non-randomised target position that this ant should be turning towards.
    /// Relative to this ant.
    /// </summary>
    private Vector3 _eventualTargetDirection = Vector3.zero;

    public TurnAround? TurnAround => AntStateMachine.TurnAroundState;

    private AntStateMachine AntStateMachine;

    void Start()
    {
        AntStateMachine = GetComponentInChildren<AntStateMachine>();
        _eventualTargetDirection = Vector3.forward; // default to walking forwards.
        var randomPosition = Random.insideUnitCircle.normalized;
        LocalTargetDirection = new Vector3(randomPosition.x, 0, randomPosition.y);    // Start with the current target position in a random direction.
    }

    void FixedUpdate()
    {
        var targetObject = AntStateMachine.CurrentTarget?.TargetPoint;
        if (targetObject != null)
        {
            _eventualTargetDirection = transform.InverseTransformPoint(targetObject.position);
        }
        else
        {
            _eventualTargetDirection = Vector3.forward * ForwardsBias;
        }

        var randomBias = RandomBias * Time.fixedDeltaTime;
        var forwardsBias = TargetBias * Time.fixedDeltaTime;
        var currentBias = 1f - forwardsBias - randomBias;
        LocalTargetDirection = LocalTargetDirection + (Random.insideUnitSphere - LocalTargetDirection) * randomBias + (_eventualTargetDirection - LocalTargetDirection) * forwardsBias;

        Debug.DrawRay(transform.position, transform.TransformDirection(_eventualTargetDirection), Color.gray);
        Debug.DrawLine(transform.position, TargetPosition, Color.black);
    }
}
