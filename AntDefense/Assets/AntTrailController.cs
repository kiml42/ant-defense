using System.ComponentModel;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class AntTrailController : MonoBehaviour
{
    private static GameObject _defaultTrailParent;
    public GameObject TrailParent;
    private AntStateMachine AntStateMachine;

    public Smellable TrailPoint;
    public float TrailPointSpawnDistance = 1.25f;

    private Smell? _lastSmell = null;
    private Vector3? _lastTrailPointLocation;
    public Smellable LastTrailPoint { get; private set; }
    private float _timeSinceTarget = 0;

    /// <summary>
    /// Distance around the proposed new location for a trail point to check for existing trail points
    /// </summary>
    public float OverlapRadius = 0.5f;

    void Start()
    {
        AntStateMachine = GetComponentInChildren<AntStateMachine>();
        if (TrailParent == null)
        {
            if(_defaultTrailParent == null)
            {
                _defaultTrailParent = new GameObject();
                _defaultTrailParent.name = "TrailParent";
            }
            TrailParent = _defaultTrailParent;
        }
    }

    void FixedUpdate()
    {
        if (_lastSmell != AntStateMachine.TrailSmell)
        {
            // The smell has changed, so reset the trail.
            _lastSmell = AntStateMachine.TrailSmell;
            _lastTrailPointLocation = null;
            _timeSinceTarget = 0;
        }
        _timeSinceTarget += Time.fixedDeltaTime;

        LeaveTrail();
    }

    private void LeaveTrail()
    {
        if (!AntStateMachine.TrailSmell.HasValue) return;
        var distanceToLastPoint = this._lastTrailPointLocation.HasValue
            ? (_lastTrailPointLocation.Value - this.transform.position).magnitude
            : 0;
        if (!this._lastTrailPointLocation.HasValue || distanceToLastPoint > this.TrailPointSpawnDistance)
        {
            // TODO consider if there is a lighter method for this just seeing the location of the center Possibly by keeping an octree index for the locations of all trail points
            Collider[] overlaps = Physics.OverlapSphere(transform.position, OverlapRadius);

            var relevantOverlaps = overlaps
                .Where(overlap => !overlap.IsDestroyed())
                .Select(overlap => overlap.GetComponent<TrailPointController>())
                .Where(otherTrailPoint => otherTrailPoint != null && !otherTrailPoint.IsDestroyed() && otherTrailPoint.Smell == AntStateMachine.TrailSmell);

            if (relevantOverlaps.Any())
            {
                // there's one close enough, use that.
                var best = relevantOverlaps.OrderBy(o => (o.transform.position - transform.position).magnitude).First();

                best.AddSmellComponent(_timeSinceTarget);
                //Debug.Log("Added smell component to other: " + best + ". distance = " + (best.transform.position - transform.position).magnitude);
                LastTrailPoint = best;
            }
            else
            {
                // none are close enough, so create a new one.
                var newPoint = Instantiate(TrailPoint, this.transform.position, Quaternion.identity, TrailParent.transform);
                newPoint.GetComponent<TrailPointController>()
                    .SetSmell(AntStateMachine.TrailSmell.Value, _timeSinceTarget);
                newPoint.gameObject.layer = 2;
                //Debug.Log("Leaving trail with smell: " + newPoint.GetComponent<TrailPointController>().Smell);
                LastTrailPoint = newPoint;
            }
            _lastTrailPointLocation = this.transform.position;
        }
    }
}