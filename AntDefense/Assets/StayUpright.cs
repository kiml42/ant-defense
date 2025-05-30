using UnityEngine;

public class StayUpright : MonoBehaviour
{
    public float TorqueMultiplier = 10;
    private Rigidbody _rigidbody;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // always apply torque to get upright.
        Vector3 headingError = Vector3.Cross(transform.up, Vector3.up) * 10;

        if (headingError.magnitude > 1)
        {
            headingError.Normalize();
        }

        _rigidbody.AddTorque(headingError * TorqueMultiplier);
    }
}
