using UnityEngine;

//TODO - Make turn around work by setting the heading direction rather than just generally turning it around.
//the direction can then be allowed to wander back normal with its random movements.
// For obstacles the target position should be set to a position along the tangent of teh collision.
// For turning around to find the way back it should be set behind the ant.
// When the ant has a target it's moving towards,don't just use that location imediately, instead have the target wander back towards it.
public class AntTargetPositionProvider2 : MonoBehaviour, ITargetPositionProvider
{
    /// <summary>
    /// Rate at which the position the ant is currently turning towards moves towards the target.
    /// If there is no currnet target, the target is taken as being straight ahead.
    /// </summary>
    public float ForwardsBias =1f;

    /// <summary>
    /// Multiplier for <see cref="ForwardsBias"/> applied when there is a target, to get the ant to turn towards the target faster.
    /// </summary>
    public float TargetMultiplier = 1.2f;

    /// <summary>
    /// Rate at which teh ant's turning direction moves randomly.
    /// </summary>
    public float RandomBias = 1f;

    /// <summary>
    /// World space target direction
    /// </summary>
    public Vector3 TargetDirection {  get; private set; }

    /// <summary>
    /// Target position in world space
    /// </summary>
    public Vector3 TargetPosition => transform.position + TargetDirection;

    /// <summary>
    /// The non-randomised target position that this ant should be turning towards.
    /// In world space
    /// </summary>
    private Vector3 _eventualTargetDirection = Vector3.zero;

    public TurnAround? TurnAround => AntStateMachine.TurnAroundState;

    private AntStateMachine AntStateMachine;

    void Start()
    {
        AntStateMachine = GetComponentInChildren<AntStateMachine>();
        _eventualTargetDirection = transform.forward; // default to walking forwards.
        var randomPosition = Random.insideUnitCircle.normalized;
        TargetDirection = new Vector3(randomPosition.x, 0, randomPosition.y);    // Start with the current target position in a random direction.
    }

    void FixedUpdate()
    {
        var targetObject = AntStateMachine.CurrentTarget?.TargetPoint;
        var forwardsBias = ForwardsBias * Time.fixedDeltaTime;

        if (targetObject != null)
        {
            forwardsBias *= TargetMultiplier;
            _eventualTargetDirection = targetObject.position - transform.position;
            this.TargetDirection = _eventualTargetDirection;
            // TODO don't set it to the target immediately after a recent collision, let it move back gradually for some time,
            // only return to jumping straight to the target after some time without a collision.
            return;
        }
        else
        {
            _eventualTargetDirection = transform.forward;
        }

        var randomBias = RandomBias * Time.fixedDeltaTime;
        var randomComponent = (Random.insideUnitSphere - TargetDirection) * randomBias;
        var forwardsComponent = (_eventualTargetDirection - TargetDirection) * forwardsBias;
        Debug.DrawRay(transform.position, TargetDirection, Color.red);
        Debug.Log(forwardsComponent);
        Debug.DrawRay(TargetPosition, forwardsComponent, Color.white);
        Debug.DrawRay(TargetPosition + forwardsComponent, randomComponent, Color.gray);
        this.TargetDirection += forwardsComponent + randomComponent;

        Debug.DrawRay(transform.position, _eventualTargetDirection, Color.black);
        Debug.DrawRay(transform.position, TargetDirection, Color.green);
    }
}
