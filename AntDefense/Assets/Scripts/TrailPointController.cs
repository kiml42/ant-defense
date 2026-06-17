using System.Collections.Generic;
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
            return this.DistanceFromTarget.ToString("0") + ":" + this.RemainingTime.ToString("0");
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

    public float RemainingTime
    {
        get
        {
            float max = 0;
            for (int i = 0; i < _smellComponents.Count; i++)
            {
                float t = _smellComponents[i].RemainingTime;
                if (t > max) max = t;
            }
            return max;
        }
    }

    // TODO: Just for debugging right now, remove later.
    public Smellable Previous;

    public override float GetPriority(ITargetPriorityCalculator priorityCalculator)
    {
        if (_smellComponents.Count == 0) return float.MaxValue;
        float min = float.MaxValue;
        for (int i = 0; i < _smellComponents.Count; i++)
        {
            var s = _smellComponents[i];
            float p = priorityCalculator?.CalculatePriority(s.DistanceFromTarget, s.TargetValue) ?? s.DistanceFromTarget;
            if (p < min) min = p;
        }
        return min;
    }

    /// <summary>
    /// Gets the best SmellComponent based on priority, used to find the next point in the trail.
    /// </summary>
    public SmellComponent GetBestSmellComponent(ITargetPriorityCalculator priorityCalculator)
    {
        if (_smellComponents.Count == 0) return null;
        SmellComponent best = null;
        float bestPriority = float.MaxValue;
        for (int i = 0; i < _smellComponents.Count; i++)
        {
            var s = _smellComponents[i];
            float p = priorityCalculator?.CalculatePriority(s.DistanceFromTarget, s.TargetValue) ?? s.DistanceFromTarget;
            if (p < bestPriority) { bestPriority = p; best = s; }
        }
        return best;
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
        var visibleSmells = TrailPointManager.VisibleTrailSmells;
        bool visible = false;
        for (int i = 0; i < visibleSmells.Length; i++)
        {
            if (visibleSmells[i] == this.Smell) { visible = true; break; }
        }
        this.Material.enabled = visible;
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
        for (int i = _smellComponents.Count - 1; i >= 0; i--)
        {
            if (_smellComponents[i].ExpirationTime < Time.fixedTime)
                _smellComponents.RemoveAt(i);
        }

        if (_smellComponents.Count == 0)
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
