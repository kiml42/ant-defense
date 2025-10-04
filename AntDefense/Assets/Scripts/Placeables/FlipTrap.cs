using UnityEngine;

public class FlipTrap : Triggerable
{
    public HingeJoint Hinge;

    public float FireSpeed = -500;
    public float ResetSpeed = 10;

    /// <summary>
    /// Delay before reseeting
    /// </summary>
    public float ResetDelay = 1;

    /// <summary>
    /// Delay after firing to be able to fire again.
    /// </summary>
    public float ReArmDelay = 10;

    /// <summary>
    /// countdown until this is ready to trigger
    /// </summary>
    private float _rearmCountdown;

    /// <summary>
    /// countdown until this should start resetting the flipper
    /// </summary>
    private float _resetCountdown;

    // Start is called before the first frame update
    void Start()
    {
        this._rearmCountdown = this.ReArmDelay;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this._rearmCountdown -= Time.fixedDeltaTime;
        this._resetCountdown -= Time.fixedDeltaTime;

        if(this._resetCountdown <= 0)
        {
            var motor = this.Hinge.motor;
            motor.targetVelocity = this.ResetSpeed;
            this.Hinge.motor = motor;
        }
    }

    public override void Trigger()
    {
        if (this._rearmCountdown <= 0)
        {
            this._rearmCountdown = this.ReArmDelay;
            this._resetCountdown = this.ResetDelay;

            var motor = this.Hinge.motor;
            motor.targetVelocity = this.FireSpeed;
            this.Hinge.motor = motor;
        }
    }
}
