using UnityEngine;

//TODO - Make turn around work by setting the heading direction rather than just generally turning it around.
//the direction can then be allowed to wander back normal with its random movements.
// For obstacles the target position should be set to a position along the tangent of teh collision.
// For turning around to find the way back it should be set behind the ant.
// When the ant has a target it's moving towards,don't just use that location imediately, instead have the target wander back towards it.
public class AntTargetPositionProvider : MonoBehaviour
{
    /// <summary>
    /// Rate at which the position the ant is currently turning towards moves towards the target.
    /// If there is no currnet target, the target is taken as being straight ahead.
    /// </summary>
    public float ForwardsWeightingWithoutTarget =1f;

    /// <summary>
    /// Rate at which teh ant's turning direction moves randomly.
    /// </summary>
    public float RandomDirectionChangePerSecond = 1f;

    /// <summary>
    /// Weighting of the random component when there is no target.
    /// </summary>
    public float MaxRandomMagnitude = 2f;

    /// <summary>
    /// direction this ant wants to move in
    /// </summary>
    public Vector3 DirectionToMove {  get; private set; }

    /// <summary>
    /// The random direction this ant wants to move in.
    /// </summary>
    private Vector3 _randomDirection = Vector3.zero;

    /// <summary>
    /// Current target this ant should move towards
    /// </summary>
    private Smellable _target;

    void Start()
    {
        var randomPosition = Random.insideUnitCircle.normalized;
        DirectionToMove = new Vector3(randomPosition.x, 0, randomPosition.y);    // Start with the current target position in a random direction.
    }

    void FixedUpdate()
    {
        var targetObject = _target?.TargetPoint;

        if (targetObject != null)
        {
            this.DirectionToMove = targetObject.position - transform.position;
            // TODO don't set it to the target immediately after a recent collision, let it move back gradually for some time,
            // only return to jumping straight to the target after some time without a collision.
            return;
        }

        var randomChangeMagnitude = RandomDirectionChangePerSecond * Time.fixedDeltaTime;
        var randomComponent = Random.insideUnitSphere * randomChangeMagnitude;
        _randomDirection += randomComponent;
        if(_randomDirection.magnitude > MaxRandomMagnitude)
        {
            _randomDirection = _randomDirection.normalized * MaxRandomMagnitude;
        }

        var forwardsComponent = transform.forward * ForwardsWeightingWithoutTarget;
        Debug.DrawRay(transform.position, DirectionToMove, Color.red);

        this.DirectionToMove = _randomDirection + forwardsComponent;

        Debug.DrawRay(transform.position, _randomDirection, Color.grey);
        Debug.DrawRay(transform.position + _randomDirection, forwardsComponent, Color.grey);
        Debug.DrawRay(transform.position, DirectionToMove, Color.green);
    }

    public void SetTarget(Smellable target)
    {
        _target = target;
    }

    internal void AvoidObstacle(Collision collision)
    {
        var contact = collision.GetContact(0);
        var contactPointInAntSpace = transform.InverseTransformPoint(contact.point);
        // TODO set the target location to somewhere that avoids the obstacle
        // possible chose randomly with a strong chance of tangential to the obstacle if it's a wall, may need differet behaviour if hitting an ant.
        //throw new System.NotImplementedException();
    }
}
