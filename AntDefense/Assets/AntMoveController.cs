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
        //transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);

        Vector3 headingError;
        if (_positionProvider.TurnAround.HasValue)
        {
            headingError = (_positionProvider.TurnAround.Value.Clockwise ? Vector3.up : Vector3.down);
        }
        else
        {
            var direction = _positionProvider.TargetPosition - _rigidbody.position;
            Debug.DrawRay(transform.position, direction, Color.blue);

            headingError = Vector3.Cross(transform.forward, direction);
        }
        if(headingError.magnitude > 1)
        {
            headingError.Normalize();
        }
        Debug.DrawRay(transform.position, headingError, Color.green);


        //_rigidbody.rotation = targetRotation;
        _rigidbody.AddTorque(headingError * TorqueMultiplier);
    }

    private void ApplyForce()
    {
        if (_positionProvider.TurnAround?.Move == false)
        {
            return;
        }

        var force = _rigidbody.transform.forward * ForceMultiplier;
        //force.y = 0;
        _rigidbody.AddForce(force, ForceMode.Impulse);
    }
}
