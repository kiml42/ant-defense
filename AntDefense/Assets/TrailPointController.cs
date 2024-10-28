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

    internal void SetSmell(Smell trailSmell, float timeFromTarget)
    {
        _trailSmell = trailSmell;
        _timeFromTarget = timeFromTarget;

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

        if(_lifetimeController != null)
        {

            var newLifetime = _lifetimeController.RemainingTime - _timeFromTarget * LifetimePenalty;
            //Debug.Log($"Lifetime reduced from {_lifetimeController.RemainingTime} to {newLifetime}");

            _lifetimeController.RemainingTime = newLifetime;
        }
    }
    public override string ToString()
    {
        return "TrailPoint " + Smell;
    }
}
