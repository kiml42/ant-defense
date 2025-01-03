using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipTrap : MonoBehaviour
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
        _rearmCountdown = ReArmDelay;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _rearmCountdown -= Time.fixedDeltaTime;
        _resetCountdown -= Time.fixedDeltaTime;

        if(_resetCountdown <= 0)
        {
            var motor = Hinge.motor;
            motor.targetVelocity = ResetSpeed;
            Hinge.motor = motor;
        }

        if(_rearmCountdown <= 0)
        {
            _rearmCountdown = ReArmDelay;
            _resetCountdown = ResetDelay;

            var motor = Hinge.motor;
            motor.targetVelocity = FireSpeed;
            Hinge.motor = motor;
        }
    }

    enum State
    {
        Rearming,
        Firing,
    }
}
