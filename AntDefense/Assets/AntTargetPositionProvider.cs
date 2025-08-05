using UnityEngine;

//TODO - consider:
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
    private Vector3 _randomDirection;

    /// <summary>
    /// Current target this ant should move towards
    /// </summary>
    private Smellable _target;

    /// <summary>
    /// Direction to move in to avoid an obstacle in world space
    /// </summary>
    private Vector3? _obstacleAvoidenceVector;

    private float _obstacleAvoidenceTime = 0;

    /// <summary>
    /// The time to increase the obstacle avoidence time by each time there is a collision.
    /// </summary>
    public float ObstacleAvoidenceTime = 0.25f;

    /// <summary>
    /// The maximum value to let the obstacle avoidance time get up to after repeated collisions.
    /// </summary>
    public float MaxObstacleAvoidenceTime = 2f;

    private Rigidbody _rigidbody;

    private Transform _currentObstacle;

    /// <summary>
    /// Distance to the left of the trail to go when following one.
    /// </summary>
    public float TrailOffset;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();

        this.RandomiseVector();
    }

    /// <summary>
    /// Resets teh randomisation to a completely random vector.
    /// </summary>
    public void RandomiseVector()
    {
        // Start with the current target position in a random direction.
        var randomPosition = Random.insideUnitCircle.normalized;
        _randomDirection = new Vector3(randomPosition.x, 0, randomPosition.y);
        DirectionToMove = _randomDirection;
    }

    float ObstacleAvoidenceWeight => Mathf.Max(0, _obstacleAvoidenceTime) / ObstacleAvoidenceTime;
    void FixedUpdate()
    {
        if(_obstacleAvoidenceTime <= 0)
        {
            _obstacleAvoidenceVector = null;
            _obstacleAvoidenceTime = 0;
        }

        if (_target?.TargetPoint != null)
        {
            SetDirectionToMoveWithTarget();
        }
        else
        {
            TetDirectionToMoveInWanderingMode();
        }

    }

    private void SetDirectionToMoveWithTarget()
    {
        DirectionToMove = _target.TargetPoint.position - transform.position;
        if(!_target.IsActual)
        {
            DirectionToMove -= (transform.right * TrailOffset);
        }
        if (_obstacleAvoidenceVector.HasValue)
        {
            // Add the weighted obstacle avoidence vector to the current direction to move.
            if(_currentObstacle == null)
            {
                // only decrease the time if this ant is no longer colliding with the obstacle.
                _obstacleAvoidenceTime -= Time.deltaTime;
            }

            var currentTargetDirectionWeight = Mathf.Max(0, 1 - ObstacleAvoidenceWeight);
            var weightedCurrentDirection = DirectionToMove * currentTargetDirectionWeight;
            var weightedAvodanceVector = this._obstacleAvoidenceVector.Value * this.ObstacleAvoidenceWeight;
            this.DirectionToMove = weightedCurrentDirection + weightedAvodanceVector;
            //Debug.DrawRay(transform.position, weightedCurrentDirection, Color.blue);
            //Debug.DrawRay(transform.position + weightedCurrentDirection, weightedAvodanceVector, Color.yellow);
            //Debug.DrawRay(transform.position, DirectionToMove, Color.green);
        }
    }

    private void TetDirectionToMoveInWanderingMode()
    {
        if (_obstacleAvoidenceVector.HasValue)
        {
            // set the random direction to the obstacle avoidence direction.
            _randomDirection = _obstacleAvoidenceVector.Value.normalized * MaxRandomMagnitude;
            //Debug.DrawRay(transform.position, _obstacleAvoidenceVector.Value, Color.yellow);
            _obstacleAvoidenceVector = null;
            _obstacleAvoidenceTime = 0;
        }
        else
        {
            var randomChangeMagnitude = RandomDirectionChangePerSecond * Time.fixedDeltaTime;
            var randomComponent = Random.insideUnitSphere * randomChangeMagnitude;
            _randomDirection += randomComponent;
            _randomDirection = new Vector3(_randomDirection.x, _randomDirection.y * 0.2f, _randomDirection.z);
        }
        if (_randomDirection.magnitude > MaxRandomMagnitude)
        {
            _randomDirection = _randomDirection.normalized * MaxRandomMagnitude;
        }

        var forwardsComponent = transform.forward * ForwardsWeightingWithoutTarget;
        //Debug.DrawRay(transform.position, DirectionToMove, Color.red);

        this.DirectionToMove = _randomDirection + forwardsComponent;

        //Debug.DrawRay(transform.position, _randomDirection, Color.blue);
        //Debug.DrawRay(transform.position + _randomDirection, forwardsComponent, Color.yellow);

        //Debug.DrawRay(transform.position, DirectionToMove, Color.green);
    }

    public void SetTarget(Smellable target)
    {
        _target = target;
    }

    internal void NoLongerTouching(Transform @object)
    {
        if (@object == _currentObstacle)
        {
            _currentObstacle = null;
        }
    }

    internal void AvoidObstacle(Collision collision)
    {
        _currentObstacle = collision.transform;
        var relativeObstacleMass = (collision?.rigidbody?.mass ?? 10) / (this._rigidbody?.mass ?? 1);
        var contact = collision.GetContact(0);

        var wallNormal = contact.normal;
        var facingDirection = transform.forward;

        var tangent1 = Vector3.Cross(wallNormal, transform.up).normalized;
        var tangent2 = -tangent1; // Opposite direction

        // Determine which tangent is closest to the facing direction
        var chosenTangent = Vector3.Dot(facingDirection, tangent1) > Vector3.Dot(facingDirection, tangent2)
            ? tangent1
            : tangent2;

        var tangentWeighting = Mathf.Max(0, Random.Range(-0.25f, 1));

        _obstacleAvoidenceVector = chosenTangent + (contact.normal * tangentWeighting);
        var timeIncrament = ObstacleAvoidenceTime * (relativeObstacleMass / 10);
        var unboundTime = this._obstacleAvoidenceTime + timeIncrament;
        this._obstacleAvoidenceTime = Mathf.Min(this.MaxObstacleAvoidenceTime, Mathf.Max(this.ObstacleAvoidenceTime, unboundTime));
    }
}
