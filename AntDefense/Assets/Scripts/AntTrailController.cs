using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AntTrailController : MonoBehaviour
{
    private static GameObject _defaultTrailParent;
    public GameObject TrailParent;
    private AntStateMachine AntStateMachine;

    public TrailPointController TrailPoint;
    public float TrailPointSpawnDistance = 3f;

    private Smell? _lastSmell = null;
    private Vector3? _lastTrailPointLocation;
    public TrailPointController LastTrailPoint { get; private set; }
    private float _distanceSinceTarget = 0;
    private float? _targetValue = null;

    /// <summary>
    /// Distance around the proposed new location for a trail point to check for existing trail points
    /// </summary>
    public float OverlapRadius = 0.5f;
    private Rigidbody _rigidbody;

    void Start()
    {
        this.AntStateMachine = this.GetComponentInChildren<AntStateMachine>();
        if (this.TrailParent == null)
        {
            if (_defaultTrailParent == null)
            {
                _defaultTrailParent = new GameObject();
                _defaultTrailParent.name = "TrailParent";
            }
            this.TrailParent = _defaultTrailParent;
        }
        this._rigidbody = this.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (this._lastSmell != this.AntStateMachine.TrailSmell)
        {
            // The smell has changed, so reset the trail.
            this._lastSmell = this.AntStateMachine.TrailSmell;
            this._lastTrailPointLocation = null;
            this._distanceSinceTarget = 0;
            this._targetValue = this.AntStateMachine.TrailTargetValue;
        }
        var updateDistance = this._rigidbody.linearVelocity.magnitude * Time.fixedDeltaTime;
        this._distanceSinceTarget += updateDistance;

        this.LeaveTrail();
    }

    private void LeaveTrail()
    {
        if (!this.AntStateMachine.TrailSmell.HasValue) return;
        var distanceToLastPoint = this._lastTrailPointLocation.HasValue
            ? (this._lastTrailPointLocation.Value - this.transform.position).magnitude
            : 0;
        if (!this._lastTrailPointLocation.HasValue || distanceToLastPoint > this.TrailPointSpawnDistance)
        {
            // TODO consider if there is a lighter method for this just seeing the location of the center Possibly by keeping an octree index for the locations of all trail points
            Collider[] overlaps = Physics.OverlapSphere(this.transform.position, this.OverlapRadius);

            var relevantOverlaps = overlaps
                .Where(overlap => !overlap.IsDestroyed())
                .Select(overlap => overlap.GetComponent<TrailPointController>())
                .Where(otherTrailPoint => otherTrailPoint != null && !otherTrailPoint.IsDestroyed() && otherTrailPoint.Smell == this.AntStateMachine.TrailSmell);

            if (relevantOverlaps.Any())
            {
                // there's one close enough, use that.
                var best = relevantOverlaps.OrderBy(o => (o.transform.position - this.transform.position).magnitude).First();

                best.AddSmellComponent(this._distanceSinceTarget, this._targetValue);
                //Debug.Log("Added smell component to other: " + best + ". distance = " + (best.transform.position - transform.position).magnitude);

                this.LastTrailPoint = best;
            }
            else
            {
                // none are close enough, so create a new one.
                var newPoint = Instantiate(this.TrailPoint, this.transform.position, Quaternion.identity, this.TrailParent.transform);
                newPoint.SetSmell(this.AntStateMachine.TrailSmell.Value, this._distanceSinceTarget, this._targetValue);
                newPoint.gameObject.layer = 2;
                //Debug.Log("Leaving trail with smell: " + newPoint.GetComponent<TrailPointController>().Smell);

                this.LastTrailPoint = newPoint;
            }
            this._lastTrailPointLocation = this.transform.position;
        }
    }
}