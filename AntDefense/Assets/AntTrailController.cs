using UnityEngine;

public class AntTrailController : MonoBehaviour
{
    private static GameObject _defaultTrailParent;
    public GameObject TrailParent;
    public Smell TrailType = Smell.Home;

    public Transform TrailPoint;
    public float TrailPointDistance = 1;

    private Vector3? _lastTrailPointLocation;

    void Start()
    {
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
        LeaveTrail();
    }

    private void LeaveTrail()
    {
        if (TrailPoint != null && (!_lastTrailPointLocation.HasValue || (_lastTrailPointLocation.Value - this.transform.position).magnitude > TrailPointDistance))
        {
            var newPoint = Instantiate(TrailPoint, this.transform.position, Quaternion.identity, TrailParent.transform);
            newPoint.GetComponent<TrailPointController>().Smell = TrailType;
            print(newPoint.GetComponent<TrailPointController>().Smell);
            _lastTrailPointLocation = this.transform.position;
        }
    }
}
