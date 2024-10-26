using UnityEngine;

public class AntTrailController : MonoBehaviour
{
    private static GameObject _defaultTrailParent;
    public GameObject TrailParent;
    private AntStateMachine AntStateMachine;

    public Transform TrailPoint;
    public float TrailPointSpawnDistance = 1;

    private Smell? _lastSmell = null;
    private Vector3? _lastTrailPointLocation;
    private float _timeSinceTarget = 0;

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
        _timeSinceTarget += Time.deltaTime;

        LeaveTrail();
    }

    private void LeaveTrail()
    {
        var distanceToLastPoint = this._lastTrailPointLocation.HasValue
            ? (_lastTrailPointLocation.Value - this.transform.position).magnitude
            : 0;
        if (!this._lastTrailPointLocation.HasValue || distanceToLastPoint > this.TrailPointSpawnDistance)
        {
            var newPoint = Instantiate(TrailPoint, this.transform.position, Quaternion.identity, TrailParent.transform);
            newPoint.GetComponent<TrailPointController>().SetSmell(AntStateMachine.TrailSmell, _timeSinceTarget);
            newPoint.gameObject.layer = 2;
            //Debug.Log("Leaving trail with smell: " + newPoint.GetComponent<TrailPointController>().Smell);
            _lastTrailPointLocation = this.transform.position;
        }
    }
}
