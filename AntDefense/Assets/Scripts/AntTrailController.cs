using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AntTrailController : MonoBehaviour
{
    private static GameObject TrailParent;

    private const float TrailPointSpawnDistance = 3f;

    /// <summary>
    /// Distance around the proposed new location for a trail point to check for existing trail points
    /// </summary>
    public const float OverlapRadius = 2f;

    private AntStateMachine AntStateMachine;

    public TrailPointController TrailPoint;

    private Smell? _lastSmell = null;

    /// <summary>
    /// the previous trail point, or the original object.
    /// </summary>
    private Smellable LastTrailPointSmellable = null;

    // TODO this is somewhat duplicative with _lastTrailPoint, consider merging.
    public TrailPointController LastTrailPointController { get; private set; }
    private float _distanceSinceTarget = 0;
    private float? _targetValue = null;

    private Rigidbody _rigidbody;

    void Start()
    {
        this.AntStateMachine = this.GetComponentInChildren<AntStateMachine>();

        if (TrailParent == null)
        {
            TrailParent = new GameObject();
            TrailParent.name = "TrailParent";
        }

        this._rigidbody = this.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // TODO might be best to have an explicit method instead of just spying on the ASM.
        if (this._lastSmell != this.AntStateMachine.TrailSmell)
        {
            // The smell has changed, so reset the trail.
            this._lastSmell = this.AntStateMachine.TrailSmell;
            this.LastTrailPointSmellable = null;    // TODO this should probably be set to the new real object a trail is being left to.
            this._distanceSinceTarget = 0;
            this._targetValue = this.AntStateMachine.TrailTargetValue;
        }
        var updateDistance = this._rigidbody.linearVelocity.magnitude * Time.deltaTime;
        this._distanceSinceTarget += updateDistance;

        this.LeaveTrail();
    }

    private void LeaveTrail()
    {
        if (!this.AntStateMachine.TrailSmell.HasValue || this._targetValue <= 0) return;
        var hasPreviousTrailPoint = this.LastTrailPointSmellable != null && !this.LastTrailPointSmellable.IsDestroyed();
        var distanceToLastPoint = hasPreviousTrailPoint
            ? (this.LastTrailPointSmellable.transform.position - this.transform.position).magnitude
            : 0;
        if (!hasPreviousTrailPoint || distanceToLastPoint > TrailPointSpawnDistance)
        {
            // TODO consider if there is a lighter method for this just seeing the location of the center Possibly by keeping an octree index for the locations of all trail points
            Collider[] overlaps = Physics.OverlapSphere(this.transform.position, OverlapRadius);

            var relevantOverlaps = overlaps
                .Where(overlap => !overlap.IsDestroyed())
                .Select(overlap => overlap.GetComponent<TrailPointController>())
                .Where(otherTrailPoint => otherTrailPoint != null && !otherTrailPoint.IsDestroyed() && otherTrailPoint.Smell == this.AntStateMachine.TrailSmell);

            if (relevantOverlaps.Any())
            {
                // there's one close enough, use that.
                var best = relevantOverlaps.OrderBy(o => (o.transform.position - this.transform.position).magnitude).First();

                best.AddSmellComponent(this._distanceSinceTarget, this._targetValue, this.LastTrailPointSmellable);
                //Debug.Log("Added smell component to other: " + best + ". distance = " + (best.transform.position - transform.position).magnitude);

                this.LastTrailPointController = best;
            }
            else
            {
                // none are close enough, so create a new one.
                var newPoint = Instantiate(this.TrailPoint, this.transform.position, Quaternion.identity, TrailParent.transform);
                newPoint.SetSmell(this.AntStateMachine.TrailSmell.Value, this._distanceSinceTarget, this._targetValue, this.LastTrailPointSmellable);
                newPoint.gameObject.layer = 2;
                //Debug.Log("Leaving trail with smell: " + newPoint.GetComponent<TrailPointController>().Smell);

                TrailPointManager.Register(newPoint);

                this.LastTrailPointController = newPoint;
                this.LastTrailPointSmellable = newPoint;
            }
        }
    }
}