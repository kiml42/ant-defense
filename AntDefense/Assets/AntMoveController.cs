using UnityEngine;

public class AntMoveController : MonoBehaviour
{
    public float TorqueMultiplier = 10;
    public float ForceMultiplier = 0.1f;
    public float RandomTorqueWeighting = 100;
    public float ReturnTorqueWeighting = 0.1f;

    private Rigidbody _rigidbody;

    private Vector3 _targetPosition = new Vector3(10, 0, 20);

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Turn();
        ApplyForce();
    }

    private void Turn()
    {
        var direction = _targetPosition - _rigidbody.position;
        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        Debug.DrawRay(transform.position, direction, Color.magenta);
        Debug.DrawRay(transform.position, transform.forward * 15, Color.blue);

        var headingError = Vector3.Cross(transform.forward, direction);

        var angle = Vector3.Angle(direction, transform.forward);

        //_rigidbody.rotation = targetRotation;
        _rigidbody.AddTorque(headingError * TorqueMultiplier);
    }

    private void ApplyForce()
    {
        var force = _rigidbody.transform.forward * ForceMultiplier;
        force.y = 0;
        _rigidbody.AddForce(force, ForceMode.Impulse);
    }
}
