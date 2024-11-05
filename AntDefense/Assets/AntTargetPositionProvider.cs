﻿using UnityEngine;

//TODO - Make turn around work by setting the heading direction rather than just generally turning it around.
//the direction can then be allowed to wander back normal with its random movements.
// For obstacles the target position should be set to a position along the tangent of teh collision.
// For turning around to find the way back it should be set behind the ant.
// When the ant has a target it's moving towards,don't just use that location imediately, instead have the target wander back towards it.
public class AntTargetPositionProvider : MonoBehaviour
{
    /// <summary>
    /// Rate at which the position the ant is currently turning towards moves towards the target.
    /// If there is no currnet target, the target is taken as being straight ahead.
    /// </summary>
    public float ForwardsBias =1f;

    /// <summary>
    /// Multiplier for <see cref="ForwardsBias"/> applied when there is a target, to get the ant to turn towards the target faster.
    /// </summary>
    public float TargetMultiplier = 1.2f;

    /// <summary>
    /// Rate at which teh ant's turning direction moves randomly.
    /// </summary>
    public float RandomBias = 1f;

    /// <summary>
    /// World space target direction
    /// </summary>
    public Vector3 TargetDirection {  get; private set; }

    /// <summary>
    /// Target position in world space
    /// </summary>
    public Vector3 TargetPosition => transform.position + TargetDirection;

    /// <summary>
    /// The non-randomised target position that this ant should be turning towards.
    /// In world space
    /// </summary>
    private Vector3 _eventualTargetDirection = Vector3.zero;

    private Smellable _target;

    void Start()
    {
        _eventualTargetDirection = transform.forward; // default to walking forwards.
        var randomPosition = Random.insideUnitCircle.normalized;
        TargetDirection = new Vector3(randomPosition.x, 0, randomPosition.y);    // Start with the current target position in a random direction.
    }

    void FixedUpdate()
    {
        var targetObject = _target?.TargetPoint;
        var forwardsBias = ForwardsBias * Time.fixedDeltaTime;

        if (targetObject != null)
        {
            forwardsBias *= TargetMultiplier;
            _eventualTargetDirection = targetObject.position - transform.position;
            this.TargetDirection = _eventualTargetDirection;
            // TODO don't set it to the target immediately after a recent collision, let it move back gradually for some time,
            // only return to jumping straight to the target after some time without a collision.
            return;
        }
        else
        {
            _eventualTargetDirection = transform.forward;
        }

        var randomBias = RandomBias * Time.fixedDeltaTime;
        var randomComponent = (Random.insideUnitSphere - TargetDirection) * randomBias;
        var forwardsComponent = (_eventualTargetDirection - TargetDirection) * forwardsBias;
        Debug.DrawRay(transform.position, TargetDirection, Color.red);
        Debug.DrawRay(TargetPosition, forwardsComponent, Color.white);
        Debug.DrawRay(TargetPosition + forwardsComponent, randomComponent, Color.gray);
        this.TargetDirection += forwardsComponent + randomComponent;

        Debug.DrawRay(transform.position, _eventualTargetDirection, Color.black);
        Debug.DrawRay(transform.position, TargetDirection, Color.green);
    }

    public void SetTarget(Smellable target)
    {
        _target = target;
    }

    public void ClearTurnAround()
    {
        // TODO
        //throw new System.NotImplementedException();
    }

    public void SetTurnAround()
    {
        // TODO set the target to move towards to behind the ant, or, ideally to the location of the last point left before changing state.
        //throw new System.NotImplementedException();
    }

    internal void AvoidObstacle(Collision collision)
    {
        var contact = collision.GetContact(0);
        var contactPointInAntSpace = transform.InverseTransformPoint(contact.point);
        // TODO set the target location to somewhere that avoids the obstacle
        // possible chose randomly with a strong chance of tangential to the obstacle if it's a wall, may need differet behaviour if hitting an ant.
        //throw new System.NotImplementedException();
    }
}
