using UnityEngine;

public class AntController : MonoBehaviour
{
    private static GameObject _defaultTrailParent;
    public GameObject TrailParent;

    public Transform TrailPoint;
    public float TrailPointDistance = 1;

    public float ForceMultiplier = 1;
    public float RandomForceWeighting = 10;
    public float ReturnForceWeighting = 1;

    private Rigidbody _rigidbody;
    private Vector3? _lastTrailPointLocation;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
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
        ApplyForce();

        LeaveTrail();
    }

    private void LeaveTrail()
    {
        if (TrailPoint != null && (!_lastTrailPointLocation.HasValue || (_lastTrailPointLocation.Value - _rigidbody.position).magnitude > TrailPointDistance))
        {
            var newPoint = Instantiate(TrailPoint, _rigidbody.position, Quaternion.identity, TrailParent.transform);
            _lastTrailPointLocation = _rigidbody.position;
        }
    }

    private void ApplyForce()
    {
        var randomForce = Random.onUnitSphere * RandomForceWeighting;
        var returnForce = -_rigidbody.position * ReturnForceWeighting;
        var force = (randomForce + returnForce).normalized * ForceMultiplier;
        force.y = 0;
        _rigidbody.AddForce(force, ForceMode.Impulse);

        if (_rigidbody.velocity.magnitude > 0.1)
        {
            _rigidbody.rotation = Quaternion.Lerp(_rigidbody.rotation, Quaternion.LookRotation(_rigidbody.velocity), 0.1f);
        }
    }
}
