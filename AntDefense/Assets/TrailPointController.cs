using System;
using UnityEngine;

public class TrailPointController : Smellable
{
    public MeshRenderer Material;

    private Smell _trailSmell;
    private float _timeFromTarget;

    public Transform Transform => this.transform;

    public override Smell Smell => _trailSmell;

    public override float TimeFromTarget => _timeFromTarget;

    public override bool IsActual => false;

    public LifetimeController _lifetimeController;

    /// <summary>
    /// The initial lifetime of this trail point will be reduced by <see cref="LifetimePenalty"/> * <see cref="TimeFromTarget"/>
    /// </summary>
    public float LifetimePenalty = 1;

    public float OverlapRadius = 0.2f;

    internal void SetSmell(Smell trailSmell, float timeFromTarget)
    {
        _trailSmell = trailSmell;
        _timeFromTarget = timeFromTarget;

        if(_lifetimeController != null)
        {
            var newLifetime = _lifetimeController.RemainingTime - _timeFromTarget * LifetimePenalty;
            //Debug.Log($"Lifetime reduced from {_lifetimeController.RemainingTime} to {newLifetime}");

            _lifetimeController.RemainingTime = newLifetime;
        }

        // TODO this doesn't seem to be adjusting trails correctly, leading to lots of loose ends.
        var IsStillValid = CheckOverlaps();
        if (IsStillValid)
        {
            if(Material != null)
            {
                var a = Material.material.color.a;
                switch (_trailSmell)
                {
                    case Smell.Home:
                        Material.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, a); break;
                    case Smell.Food:
                        Material.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, a); break;
                }
            }
        }
    }

    private bool CheckOverlaps()
    {
        return true;
        //TODO check overlaps before instanciating the trail point instead
        Collider[] overlaps = Physics.OverlapSphere(transform.position, OverlapRadius);

        foreach (Collider overlap in overlaps)
        {
            if (overlap.TryGetComponent<TrailPointController>(out var otherTrailPoint))
            {
                if (otherTrailPoint.Smell != Smell)
                {
                    // different smell, ignore.
                    continue;
                }

                var bestLifetime = Math.Max(otherTrailPoint._lifetimeController.RemainingTime, _lifetimeController.RemainingTime);
                if (otherTrailPoint.TimeFromTarget > TimeFromTarget)
                {
                    // this one is better, use the best lifetime, and destroy the other.
                    _lifetimeController.RemainingTime = bestLifetime;
                    //Debug.Log($"Destroying other {otherTrailPoint} in favor of {this}");
                    Destroy(otherTrailPoint.gameObject);
                }
                else if(otherTrailPoint.TimeFromTarget < TimeFromTarget)
                {
                    // other is better, reset its lifetime and destroy this one
                    otherTrailPoint._lifetimeController.RemainingTime = bestLifetime;
                    //Debug.Log($"Destroying this {this} in favor of {otherTrailPoint}");
                    Destroy(gameObject);
                    return false;
                }
            }
        }
        return true;
    }

    public override string ToString()
    {
        return $"TrailPoint smell={Smell}, lifetime={_lifetimeController.RemainingTime}, TimeFromTarget={TimeFromTarget}";
    }
}
