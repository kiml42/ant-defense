using UnityEngine;

public class AntMoveController : MonoBehaviour
{
    public float TorqueMultiplier = 10;
    public float ForceMultiplier = 0.1f;
    private ITargetPositionProvider _positionProvider;

    private Rigidbody _rigidbody;


    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _positionProvider = GetComponent<ITargetPositionProvider>();
    }

    void FixedUpdate()
    {
        Turn();
        ApplyForce();
    }

    private void Turn()
    {
        Vector3 headingError;
        if (_positionProvider.TurnAround)
        {
            headingError = Vector3.up;
        }
        else
        {
            var direction = _positionProvider.TargetPosition - _rigidbody.position;
            //Debug.DrawRay(transform.position, direction, Color.magenta);

            headingError = Vector3.Cross(transform.forward, direction);
        }

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
