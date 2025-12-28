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
        var direction = this._positionProvider.DirectionToMove;
        var signedAngle = Vector3.SignedAngle(this.transform.forward, direction, this.transform.up);

        this.Turn(signedAngle);
        this.ApplyForce(signedAngle);
    }

    private void Turn(float signedAngle)
    {
        // always apply torque to get upright.
        Vector3 headingError = Vector3.Cross(this.transform.up, Vector3.up) * 10;

        if (this.IsUpright)
        {
            // only turn towards the target if upright
            if(signedAngle >= 90)
            {
                // over 90 degrees off, full torque to turn towards forwards
                headingError += this.transform.up;
            }
            else if(signedAngle <= -90)
            {
                // over 90 degrees off, full torque to turn towards forwards
                headingError -= this.transform.up;
            }
            else
            {
                // less than 90 degrees off, torque should be correlated with the angle.
                //Debug.DrawRay(transform.position, direction, Color.blue);

                headingError += Vector3.Cross(this.transform.forward, this._positionProvider.DirectionToMove).normalized;
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

    /// <summary>
    /// Additional force multiplier to apply when the desired direction is directly sideways.
    /// </summary>
    public float SidewaysForceMultiplier = 0.1f;

    /// <summary>
    /// Additional force multiplier to apply when the desired direction is backwards.
    /// </summary>
    public float BackwardForceMultiplier = 0.8f;

    public float MaxSpeed = 20f;

    private void ApplyForce(float signedAngle)
    {
        if (!this.IsUpright)
        {
            return;
        }

        var unsignedAngle = Mathf.Abs(signedAngle);

        // use half the angle to make the minimm at 0 when going sideways, and maximum at 0 and 180.
        var forceDirectionMultiplier = GetShiftedScaledCosine(unsignedAngle / 2, this.SidewaysForceMultiplier);

        // further reduce the force when going backwards.
        var forwardsOrBackwardsMultiplier = GetShiftedScaledCosine(unsignedAngle, this.BackwardForceMultiplier);

        var velocityAngle = Vector3.Angle(this._rigidbody.linearVelocity, this._positionProvider.DirectionToMove); // angle between the the current velocity and desired direction.
        var proportionOfMaxSpeed = Mathf.Clamp01(this._rigidbody.linearVelocity.magnitude / this.MaxSpeed);
        var velocityAngleMultiplier = 1 - (GetShiftedScaledCosine(velocityAngle, 0.5f) * proportionOfMaxSpeed); // reduce force when the ant is already moving in the desired direction.

        var force = this._positionProvider.DirectionToMove.normalized * forceDirectionMultiplier * this.ForceMultiplier * forwardsOrBackwardsMultiplier * velocityAngleMultiplier;

        //force.y = 0;
        this._rigidbody.AddForce(force, ForceMode.Impulse);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="angle">in gegrees</param>
    /// <param name="shift">Between 0 and 1</param>
    /// <returns></returns>
    private static float GetShiftedScaledCosine(float angle, float shift)
    {
        Debug.Assert(shift >= 0 && shift <= 1, "Shift must be between 0 and 1");

        var cosine = Mathf.Cos(angle * Mathf.Deg2Rad);  // cosine of half the angle between forward and desired direction, so that it's 1 when aligned, or 180 degrees off, and 0 when 90 degrees off.

        var cos0to1 = (cosine + 1) / 2; // Shift and scale the cosine to between 0 and 1.

        var forceDirectionMultiplier = shift + ((1 - shift) * cos0to1);
        return forceDirectionMultiplier;
    }
}
