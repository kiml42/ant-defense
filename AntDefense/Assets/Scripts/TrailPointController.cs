using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TrailPointController : Smellable
{
    // TODO make the trail points bigger/bolder when zoomed out to keep trails visible.

    private class SmellComponent
    {
        public readonly float DistanceFromTarget;

        /// <summary>
        /// Estimation of how much food is available at the target.
        /// </summary>
        public readonly float? TargetValue;

        public float RemainingTime { get; private set; }
        public SmellComponent(float distanceFromTarget, float remainingTime, float? targetValue)
        {
            DistanceFromTarget = distanceFromTarget;
            RemainingTime = remainingTime;
            TargetValue = targetValue;
        }

        internal void DecrementTime()
        {
            RemainingTime -= Time.fixedDeltaTime;
        }

        public override string ToString()
        {
            return DistanceFromTarget + ":" + RemainingTime;
        }
    }

    public MeshRenderer Material;

    private Smell _trailSmell;
    private readonly List<SmellComponent> _smellComponents = new();

    public Transform Transform => this.transform;

    public override Smell Smell => _trailSmell;

    public override bool IsActual => false;

    /// <summary>
    /// The initial lifetime of this trail point will be reduced by <see cref="LifetimePenalty"/> * distanceFromTarget
    /// </summary>
    public float LifetimePenalty = 1;

    public float OverlapRadius = 0.2f;

    public float ScaleDownTime = 4;
    public float DefaultLifetime = 80;
    public override bool IsPermanentSource => true;

    public override float GetPriority(ITargetPriorityCalculator priorityCalculator)
    {
        // return the component with the best priotity, if priorityCalculator is null, just return the closest.
        return _smellComponents.Any()
            ? _smellComponents.Min(s => priorityCalculator?.CalculatePriority(s.DistanceFromTarget, s.TargetValue) ?? s.DistanceFromTarget)
            : float.MaxValue;
    }

    private void FixedUpdate()
    {
        foreach (var component in _smellComponents.ToArray())
        {
            component.DecrementTime();
            if(component.RemainingTime <= 0)
            {
                _smellComponents.Remove(component);
            }
        }

        if (!_smellComponents.Any())
        {
            //Debug.Log("Destroyin because No remaining smells");
            DestroyThis();
            return;
        }

        var remainingTime = _smellComponents.Max(c => c.RemainingTime);
        if (ScaleDownTime > 0 && remainingTime < ScaleDownTime)
        {
            this.transform.localScale = Vector3.one * remainingTime / ScaleDownTime;
        }
    }

    private void DestroyThis()
    {
        Destroy(this);
        Destroy(this.gameObject);
    }

    internal void SetSmell(Smell trailSmell, float distanceFromTarget, float? targetValue)
    {
        if (this.IsDestroyed())
        {
            Debug.Log("Already destroyed!!!");
        }
        _trailSmell = trailSmell;

        var component = CreateSmellComponent(distanceFromTarget, targetValue);

        if (component.RemainingTime <= 0)
        {
            //Debug.Log("Destroyin because less than 0 lifetime on set smell");
            DestroyThis();
            return;
        }

        AddSmellComponent(component);
        //Debug.Log("Added smell component to self: " + this);

        if (Material != null)
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

    private SmellComponent CreateSmellComponent(float distanceFromTarget, float? targetValue)
    {
        var newLifetime = DefaultLifetime - distanceFromTarget * LifetimePenalty;
        var component = new SmellComponent(distanceFromTarget, newLifetime, targetValue);
        return component;
    }

    public void AddSmellComponent(float distanceFromTarget, float? targetValue)
    {
        AddSmellComponent(CreateSmellComponent(distanceFromTarget, targetValue));
    }

    private void AddSmellComponent(SmellComponent component)
    {
        _smellComponents.Add(component);
        //Debug.Log("Added smell. Smells: " + string.Join(", ", _smellComponents));
    }

    public override string ToString()
    {
        return $"TrailPoint smell={Smell}, components: {string.Join(", ", _smellComponents)}";
    }
}
