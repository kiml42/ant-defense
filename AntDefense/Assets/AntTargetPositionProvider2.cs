﻿using UnityEngine;
//TODO - Make turn around work by setting the heading direction rather than just generally turning it around.
//the direction can then be allowed to wander back normal with its random movements.
// For obstacles the target position should be set to a position along the tangent of teh collision.
// For turning around to find the way back it should be set behind the ant.
// When the ant has a target it's moving towards,don't just use that location imediately, instead have the target wander back towards it.
public class AntTargetPositionProvider2 : MonoBehaviour, ITargetPositionProvider
{
    public float ForwardsBias = 0.1f;
    public float RandomBias = 0.2f;

    /// <summary>
    /// Target position relative to this ant
    /// </summary>
    public Vector3 TargetPosition { get; private set; }

    /// <summary>
    /// The non-randomised target position that this ant should be turning towards.
    /// Relative to this ant.
    /// </summary>
    private Vector3 _eventualTargetPosition = Vector3.zero;

    public TurnAround? TurnAround => AntStateMachine.TurnAroundState;

    private AntStateMachine AntStateMachine;

    void Start()
    {
        AntStateMachine = GetComponentInChildren<AntStateMachine>();
        _eventualTargetPosition = Vector3.forward; // default to walking forwards.
        var randomPosition = Random.insideUnitCircle.normalized;
        TargetPosition = new Vector3(randomPosition.x, 0, randomPosition.y);    // Start with the current target position in a random direction.
    }

    void FixedUpdate()
    {
        var targetObject = AntStateMachine.CurrentTarget?.TargetPoint;
        if (targetObject != null)
        {
            _eventualTargetPosition = targetObject.position;
        }
        else
        {
            _eventualTargetPosition = Vector3.forward;
        }

        var randomBias = RandomBias * Time.fixedDeltaTime;
        var forwardsBias = ForwardsBias * Time.fixedDeltaTime;
        var currentBias = 1f - forwardsBias - randomBias;
        TargetPosition = TargetPosition * currentBias + Random.insideUnitSphere * randomBias + _eventualTargetPosition * forwardsBias;

        Debug.DrawRay(transform.position, transform.TransformDirection(_eventualTargetPosition) * 10, Color.gray);
        Debug.DrawRay(transform.position, transform.TransformDirection(TargetPosition) * 10, Color.black);
    }
}
