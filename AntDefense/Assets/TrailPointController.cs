using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TrailPointController : Smellable
{
    private class SmellComponent
    {
        public readonly float TimeFromTarget;
        public float RemainingTime { get; private set; }
        public SmellComponent(float timeFromTarget, float remainingTime)
        {
            TimeFromTarget = timeFromTarget;
            RemainingTime = remainingTime;
        }

        internal void DecrementTime()
        {
            RemainingTime -= Time.fixedDeltaTime;
        }

        public override string ToString()
        {
            return TimeFromTarget + ":" + RemainingTime;
        }
    }

    public MeshRenderer Material;

    private Smell _trailSmell;
    private List<SmellComponent> _smellComponents = new List<SmellComponent>();

    public Transform Transform => this.transform;

    public override Smell Smell => _trailSmell;

    public override float TimeFromTarget => _smellComponents.Min(s => s.TimeFromTarget);

    public override bool IsActual => false;

    /// <summary>
    /// The initial lifetime of this trail point will be reduced by <see cref="LifetimePenalty"/> * <see cref="TimeFromTarget"/>
    /// </summary>
    public float LifetimePenalty = 1;

    public float OverlapRadius = 0.2f;

    public float ScaleDownTime = 4;
    public float DefaultLifetime = 80;

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

    internal void SetSmell(Smell trailSmell, float timeFromTarget)
    {
        if (this.IsDestroyed())
        {
            Debug.Log("Already destroyed!!!");
        }
        _trailSmell = trailSmell;

        var newLifetime = DefaultLifetime - timeFromTarget * LifetimePenalty;
        if (newLifetime <= 0)
        {
            //Debug.Log("Destroyin because less than 0 lifetime on set smell");
            DestroyThis();
            return;
        }
        var component = new SmellComponent(timeFromTarget, newLifetime);

        // TODO this doesn't seem to be adjusting trails correctly, leading to lots of loose ends.
        var IsStillValid = CheckOverlaps(component);
        if (IsStillValid)
        {
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
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="component"></param>
    /// <returns>true if this object should be retained</returns>
    private bool CheckOverlaps(SmellComponent component)
    {
        //TODO check overlaps before instanciating the trail point instead
        Collider[] overlaps = Physics.OverlapSphere(transform.position, OverlapRadius);

        var relevantOverlaps = overlaps
            .Where(overlap => !overlap.IsDestroyed())
            .Select(overlap => overlap.GetComponent<TrailPointController>())
            .Where(otherTrailPoint => otherTrailPoint != null && !otherTrailPoint.IsDestroyed() && otherTrailPoint != this && otherTrailPoint.Smell == Smell);

        if (relevantOverlaps.Any())
        {
            var best = relevantOverlaps.OrderBy(o => (o.transform.position - transform.position).magnitude).First();

            best.AddSmellComponent(component);
            //Debug.Log("Added smell component to other: " + best + ". distance = " + (best.transform.position - transform.position).magnitude);
            return false;
        }

        return true;
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
