using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TrailPointController : Smellable
{
    // TODO make the trail points bigger/bolder when zoomed out to keep trails visible.
    public static float CalculatePriority(float distanceFromTarget, float? targetValue)
    {
        // TODO work out what function works best for this.
        // this seems to be working pretty well as is, but it may not be optimal.
        return distanceFromTarget - (targetValue ?? 0);
    }

    private class SmellComponent
    {
        public readonly float Priority;
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
            Priority = CalculatePriority(DistanceFromTarget, TargetValue);
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
    private List<SmellComponent> _smellComponents = new List<SmellComponent>();

    public Transform Transform => this.transform;

    public override Smell Smell => _trailSmell;

    public override float Priority => _smellComponents.Any() ? _smellComponents.Min(s => s.Priority) : float.MaxValue;
    public override float DistanceFromTarget => _smellComponents.Any() ? _smellComponents.Min(s => s.DistanceFromTarget) : float.MaxValue;

    public override bool IsActual => false;

    /// <summary>
    /// The initial lifetime of this trail point will be reduced by <see cref="LifetimePenalty"/> * <see cref="DistanceFromTarget"/>
    /// </summary>
    public float LifetimePenalty = 1;

    public float OverlapRadius = 0.2f;

    public float ScaleDownTime = 4;
    public float DefaultLifetime = 80;
    public override bool IsPermanentSource => true;

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
