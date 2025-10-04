using UnityEngine;

public class AntMoveController : MonoBehaviour
{
    public float TorqueMultiplier = 10;
    public float ForceMultiplier = 0.1f;
    private AntTargetPositionProvider _positionProvider;

    private Rigidbody _rigidbody;


    void Start()
    {
        this._rigidbody = this.GetComponent<Rigidbody>();
        this._positionProvider = this.GetComponent<AntTargetPositionProvider>();
    }

    void FixedUpdate()
    {
        this.Turn();
        this.ApplyForce();
    }

    private void Turn()
    {
        // always apply torque to get upright.
        Vector3 headingError = Vector3.Cross(this.transform.up, Vector3.up) * 10;

        if (this.IsUpright)
        {
            // only turn towards teh target if upright
            var direction = this._positionProvider.DirectionToMove;

            var angle = Vector3.SignedAngle(this.transform.forward, direction, this.transform.up);

            if(angle >= 90)
            {
                headingError += this.transform.up;
            }
            else if(angle <= -90)
            {
                headingError -= this.transform.up;
            }
            else
            {
                //Debug.DrawRay(transform.position, direction, Color.blue);

                headingError = Vector3.Cross(this.transform.forward, direction).normalized;
            }
        }


        if (headingError.magnitude > 1)
        {
            headingError.Normalize();
        }
        //Debug.DrawRay(transform.position, headingError, Color.green);


        //_rigidbody.rotation = targetRotation;
        this._rigidbody.AddTorque(headingError * this.TorqueMultiplier);
    }

    private bool IsUpright => this.transform.up.y > 0.8;

    private void ApplyForce()
    {
        if (!this.IsUpright)
        {
            return;
        }

        var force = this._rigidbody.transform.forward * this.ForceMultiplier;
        //force.y = 0;
        this._rigidbody.AddForce(force, ForceMode.Impulse);
    }
}
