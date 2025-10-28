using UnityEngine;

//TODO - consider When the ant has a target it's moving towards,don't just use that location imediately, instead have the target wander back towards it.
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
        this._rigidbody = this.GetComponent<Rigidbody>();

        this.RandomiseVector();
    }

    /// <summary>
    /// Resets teh randomisation to a completely random vector.
    /// </summary>
    public void RandomiseVector()
    {
        // Start with the current target position in a random direction.
        var randomPosition = Random.insideUnitCircle.normalized;
        this._randomDirection = new Vector3(randomPosition.x, 0, randomPosition.y);
        this.DirectionToMove = this._randomDirection;
    }

    float ObstacleAvoidenceWeight => Mathf.Max(0, this._obstacleAvoidenceTime) / this.ObstacleAvoidenceTime;
    void FixedUpdate()
    {
        if(this._obstacleAvoidenceTime <= 0)
        {
            this._obstacleAvoidenceVector = null;
            this._obstacleAvoidenceTime = 0;
        }

        if (this._target?.TargetPoint != null)
        {
            this.SetDirectionToMoveWithTarget();
        }
        else
        {
            this.SetDirectionToMoveInWanderingMode();
        }

    }

    private void SetDirectionToMoveWithTarget()
    {
        this.DirectionToMove = this._target.TargetPoint.position - this.transform.position;
        if(!this._target.IsActual)
        {
            this.DirectionToMove -= (this.transform.right * this.TrailOffset);
        }
        if (this._obstacleAvoidenceVector.HasValue)
        {
            // Add the weighted obstacle avoidence vector to the current direction to move.
            if(this._currentObstacle == null)
            {
                // only decrease the time if this ant is no longer colliding with the obstacle.
                this._obstacleAvoidenceTime -= Time.deltaTime;
            }

            var currentTargetDirectionWeight = Mathf.Max(0, 1 - this.ObstacleAvoidenceWeight);
            var weightedCurrentDirection = this.DirectionToMove * currentTargetDirectionWeight;
            var weightedAvodanceVector = this._obstacleAvoidenceVector.Value * this.ObstacleAvoidenceWeight;
            this.DirectionToMove = weightedCurrentDirection + weightedAvodanceVector;
            //Debug.DrawRay(transform.position, weightedCurrentDirection, Color.blue);
            //Debug.DrawRay(transform.position + weightedCurrentDirection, weightedAvodanceVector, Color.yellow);
            //Debug.DrawRay(transform.position, DirectionToMove, Color.green);
        }
    }

    private void SetDirectionToMoveInWanderingMode()
    {
        if (this._obstacleAvoidenceVector.HasValue)
        {
            // set the random direction to the obstacle avoidence direction.
            this._randomDirection = this._obstacleAvoidenceVector.Value.normalized * this.MaxRandomMagnitude;
            //Debug.DrawRay(transform.position, _obstacleAvoidenceVector.Value, Color.yellow);
            this._obstacleAvoidenceVector = null;
            this._obstacleAvoidenceTime = 0;
        }
        else
        {
            var randomChangeMagnitude = this.RandomDirectionChangePerSecond * Time.fixedDeltaTime;
            var randomComponent = Random.insideUnitSphere * randomChangeMagnitude;
            this._randomDirection += randomComponent;
            this._randomDirection = new Vector3(this._randomDirection.x, this._randomDirection.y * 0.2f, this._randomDirection.z);
        }
        if (this._randomDirection.magnitude > this.MaxRandomMagnitude)
        {
            this._randomDirection = this._randomDirection.normalized * this.MaxRandomMagnitude;
        }

        var forwardsComponent = this.transform.forward * this.ForwardsWeightingWithoutTarget;
        //Debug.DrawRay(transform.position, DirectionToMove, Color.red);

        this.DirectionToMove = this._randomDirection + forwardsComponent;

        //Debug.DrawRay(transform.position, _randomDirection, Color.blue);
        //Debug.DrawRay(transform.position + _randomDirection, forwardsComponent, Color.yellow);

        //Debug.DrawRay(transform.position, DirectionToMove, Color.green);
    }

    public void SetTarget(Smellable target)
    {
        this._target = target;
    }

    internal void NoLongerTouching(Transform @object)
    {
        if (@object == this._currentObstacle)
        {
            this._currentObstacle = null;
        }
    }

    internal void AvoidObstacle(Collision collision)
    {
        //Debug.Log($"{this.transform} colliding with {collision.transform} - Avoiding Obstacle.");
        this._currentObstacle = collision.transform;
        var relativeObstacleMass = (collision?.rigidbody?.mass ?? 10) / (this._rigidbody?.mass ?? 1);
        var contact = collision.GetContact(0);

        var wallNormal = contact.normal;
        var facingDirection = this.transform.forward;

        var tangent1 = Vector3.Cross(wallNormal, this.transform.up).normalized;
        var tangent2 = -tangent1; // Opposite direction

        // Determine which tangent is closest to the facing direction
        var chosenTangent = Vector3.Dot(facingDirection, tangent1) > Vector3.Dot(facingDirection, tangent2)
            ? tangent1
            : tangent2;

        var tangentWeighting = Mathf.Max(0, Random.Range(-0.25f, 1));

        this._obstacleAvoidenceVector = chosenTangent + (contact.normal * tangentWeighting);
        var timeIncrament = this.ObstacleAvoidenceTime * (relativeObstacleMass / 10);
        var unboundTime = this._obstacleAvoidenceTime + timeIncrament;
        this._obstacleAvoidenceTime = Mathf.Min(this.MaxObstacleAvoidenceTime, Mathf.Max(this.ObstacleAvoidenceTime, unboundTime));
    }
}
