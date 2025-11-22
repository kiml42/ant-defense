using UnityEngine;

public class FlipTrap : Triggerable, ISelectableObject
{
    public HingeJoint Hinge;

    public float FireSpeed = -500;
    public float ResetSpeed = 10;
    public bool IsSelected { get; private set; }

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
        NoSpawnZone.Register(this); // register this as an selection point
        this.Deselect();
        this._rearmCountdown = this.ReArmDelay;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this._rearmCountdown -= Time.deltaTime;
        this._resetCountdown -= Time.deltaTime;

        if (this._resetCountdown <= 0)
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
    public Vector3 Position => this.transform.position;

    public MeshRenderer TriggerRenderer;

    public ISelectableObject Select()
    {
        if (this.IsSelected) return this;
        this.TriggerRenderer.enabled = true;
        this.IsSelected = true;
        return this;
    }

    public void Deselect()
    {
        if (!this.IsSelected) return;
        this.TriggerRenderer.enabled = false;
        this.IsSelected = false;
    }
}
