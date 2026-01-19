using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class TrailPointController : Smellable
{
    // TODO make the trail points bigger/bolder when zoomed out to keep trails visible.

    public class SmellComponent
    {
        public readonly float DistanceFromTarget;

        public readonly Smellable PreviousPoint;

        /// <summary>
        /// Estimation of how much food is available at the target.
        /// </summary>
        public readonly float? TargetValue;

        private readonly float _expirationTime;
        public float RemainingTime => this._expirationTime - Time.fixedTime;
        public float ExpirationTime => this._expirationTime;
        public SmellComponent(float distanceFromTarget, float expirationTime, float? targetValue, Smellable previousPoint)
        {
            Debug.Assert(expirationTime >= Time.fixedTime, $"Expiration time must be in the future. expirationTime = {expirationTime}, Now = {Time.fixedTime}");
            this.DistanceFromTarget = distanceFromTarget;
            this._expirationTime = expirationTime;
            this.TargetValue = targetValue;
            this.PreviousPoint = previousPoint;
        }

        public override string ToString()
        {
            return this.DistanceFromTarget + ":" + this.RemainingTime;
        }
    }

    public MeshRenderer Material;

    private Smell _trailSmell;
    private readonly List<SmellComponent> _smellComponents = new();

    public Transform Transform => this.transform;

    public override Smell Smell => this._trailSmell;

    public override bool IsActual => false;

    /// <summary>
    /// The initial lifetime of this trail point will be reduced by <see cref="LifetimePenalty"/> * distanceFromTarget
    /// </summary>
    public float LifetimePenalty = 1;

    public float ScaleDownTime = 4;
    public float DefaultLifetime = 80;

    public float RemainingTime => this._smellComponents.Any() ? this._smellComponents.Max(c => c.RemainingTime) : 0;

    // TODO: Just for debugging right now, remove later.
    public Smellable Previous;

    public override float GetPriority(ITargetPriorityCalculator priorityCalculator)
    {
        // return the component with the best priotity, if priorityCalculator is null, just return the closest.
        return this._smellComponents.Any()
            ? this._smellComponents.Min(s => priorityCalculator?.CalculatePriority(s.DistanceFromTarget, s.TargetValue) ?? s.DistanceFromTarget)
            : float.MaxValue;
    }

    /// <summary>
    /// Gets the best SmellComponent based on priority, used to find the next point in the trail.
    /// </summary>
    public SmellComponent GetBestSmellComponent(ITargetPriorityCalculator priorityCalculator)
    {
        if (!this._smellComponents.Any()) return null;
        
        // Return the component with the best (lowest) priority
        return this._smellComponents.OrderBy(s => priorityCalculator?.CalculatePriority(s.DistanceFromTarget, s.TargetValue) ?? s.DistanceFromTarget).First();
    }

    /// <summary>
    /// Gets the previous point in the trail according to the best priority path through this point.
    /// </summary>
    public Smellable GetBestPrevious(ITargetPriorityCalculator priorityCalculator)
    {
        var bestComponent = this.GetBestSmellComponent(priorityCalculator);
        return bestComponent?.PreviousPoint;
    }

    public void UpdateVisibility()
    {
        if (TrailPointManager.VisibleTrailSmells.Contains(this.Smell))
        {
            this.Material.enabled = true;
        }
        else
        {
            this.Material.enabled = false;
        }
        this.UpdateScale();
    }

    public void UpdateScale()
    {
        var remainingTime = this.RemainingTime;
        if (remainingTime < this.ScaleDownTime)
        {
            this.transform.localScale = Vector3.one * remainingTime / this.ScaleDownTime;
        }
    }

    public void UpdateTrailPoint()
    {
        foreach (var component in this._smellComponents.Where(c => c.ExpirationTime < Time.fixedTime).ToArray())
        {
            this._smellComponents.Remove(component);
        }

        if (!this._smellComponents.Any())
        {
            // Mark as no longer smellable before destruction so ants stop trying to follow it
            this.IsSmellable = false;
            //Debug.Log("Destroyin because No remaining smells");
            this.DestroyThis();
            return;
        }
    }

    private void DestroyThis()
    {
        if(this.IsDestroyed()) { return; }
        Destroy(this.gameObject);
        Destroy(this);
    }

    internal void SetSmell(Smell trailSmell, float distanceFromTarget, float? targetValue, Smellable previous)
    {
        //Debug.Log($"Setting smell on trail point {this} to {trailSmell} dist={distanceFromTarget} targetValue={targetValue}");
        Assert.IsFalse(this.IsDestroyed(), "Setting smell on destroyed trail point");

        this._trailSmell = trailSmell;
        var added = this.AddSmellComponent(distanceFromTarget, targetValue, previous);

        if (!added)
        {
            //Debug.Log("Destroying because less than 0 lifetime on set smell");
            this.DestroyThis();
            return;
        }

        this.Previous = previous;

        //Debug.Log("Added smell component to self: " + this);
        if (this.Material != null)
        {
            var a = this.Material.material.color.a;
            switch (this._trailSmell)
            {
                case Smell.Home:
                    this.Material.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, a); break;
                case Smell.Food:
                    this.Material.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, a); break;
            }
        }
    }

    private SmellComponent CreateSmellComponent(float distanceFromTarget, float? targetValue, Smellable previousPoint)
    {
        var newLifetime = this.DefaultLifetime - (distanceFromTarget * this.LifetimePenalty);
        if(newLifetime < 0)
        {
            return null;    // Point is too far from the target.
        }
        var expirationTime = Time.fixedTime + newLifetime;
        var component = new SmellComponent(distanceFromTarget, expirationTime, targetValue, previousPoint);
        return component;
    }

    public bool AddSmellComponent(float distanceFromTarget, float? targetValue, Smellable previous)
    {
        var component = this.CreateSmellComponent(distanceFromTarget, targetValue, previous);

        if (component == null)
        {
            return false;
        }
        this._smellComponents.Add(component);
        //Debug.Log("Added smell. Smells: " + string.Join(", ", _smellComponents));
        return true;
    }

    public override string ToString()
    {
        return $"TrailPoint smell={this.Smell}, components: {string.Join(", ", this._smellComponents)}";
    }
}
