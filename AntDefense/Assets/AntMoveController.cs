using UnityEngine;

public class AntMoveController : MonoBehaviour
{
    public float TorqueMultiplier = 10;
    public float ForceMultiplier = 0.1f;
    private AntTargetPositionProvider _positionProvider;

    private Rigidbody _rigidbody;


    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _positionProvider = GetComponent<AntTargetPositionProvider>();
    }

    void FixedUpdate()
    {
        Turn();
        ApplyForce();
    }

    private void Turn()
    {
        // always apply torque to get upright.
        Vector3 headingError = Vector3.Cross(transform.up, Vector3.up) * 10;

        if (IsUpright)
        {
            // only turn towards teh target if upright
            var direction = _positionProvider.TargetPosition - _rigidbody.position;

            var angle = Vector3.Angle(transform.forward, direction);

            if(angle >= 90)
            {
                headingError += transform.up;
            }
            else if(angle <= -90)
            {
            // TODO check this works
                headingError -= transform.up;
            }
            else
            {
                //Debug.DrawRay(transform.position, direction, Color.blue);

                headingError = Vector3.Cross(transform.forward, direction);
            }
        }


        if (headingError.magnitude > 1)
        {
            headingError.Normalize();
        }
        //Debug.DrawRay(transform.position, headingError, Color.green);


        //_rigidbody.rotation = targetRotation;
        _rigidbody.AddTorque(headingError * TorqueMultiplier);
    }

    private bool IsUpright => transform.up.y > 0.8;

    private void ApplyForce()
    {
        if (!IsUpright)
        {
            return;
        }

        var force = _rigidbody.transform.forward * ForceMultiplier;
        //force.y = 0;
        _rigidbody.AddForce(force, ForceMode.Impulse);
    }
}
