using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public interface ISelectableObject : IKnowsPosition
{
    public bool IsSelected { get; }
    ISelectableObject Select();
    void Deselect();
}

public class TurretController : SelectableGhostableMonoBehaviour
{
    public Rigidbody Projectile;
    public Transform Emitter;
    public TurretTurner Turner;

    public float ReloadTime = 1;
    public float ProjectileSpeed = 10;

    private float _reloadTimer = 0;

    private List<HealthController> _targetsInRange = new();

    private float _range;
    public override Vector3 Position
    {
        get
        {
            Debug.Assert(this.transform != null, "TurretController has no transform!");
            Debug.Assert(!this.IsDestroyed(), "TurretController is destroyed!");
            return this.transform.position;
        }
    }

    public MeshRenderer RangeRenderer;

    public TurretTrigger Trigger;

    private bool _enabled = true;

    void Start()
    {
        Debug.Assert(this.Emitter != null, "TurretController requires an Emitter transform to fire projectiles from.");
        Debug.Assert(this.Turner != null, "TurretController requires a TurretTurner to aim the turret.");
        Debug.Assert(this.Projectile != null, "TurretController requires a Projectile to fire.");
        Debug.Assert(this.Trigger != null, "TurretController requires a TurretTrigger to determine its range.");

        NoSpawnZone.Register(this); // register this as an interactive point

        this._range = this.Trigger.TriggerCollider.radius * this.Trigger.TriggerCollider.transform.localScale.x;
        return;
    }

    void FixedUpdate()
    {
        if (!this._enabled)
        {
            return;
        }

        this._reloadTimer -= Time.deltaTime;
        this.CleanTargets();
        if (this._targetsInRange.Any())
        {
            // TODO work out a better way to pick the target.
            var bestTarget = this._targetsInRange.First();

            if (bestTarget == null || bestTarget.transform == null)
            {
                this._targetsInRange.Remove(bestTarget);
                return;
            }
            var direction = bestTarget.transform.position - this.Turner.transform.position;
            //Debug.DrawRay(this.Turner.transform.position, direction);

            this.Turner.TurnTo(direction);

            if (this._reloadTimer <= 0)
            {
                this.Fire();
                this._reloadTimer = this.ReloadTime;
            }
        }
    }

    private void Fire()
    {
        var projectile = Instantiate(this.Projectile, this.Emitter.position, this.Emitter.rotation);
        projectile.linearVelocity = this.Emitter.forward * this.ProjectileSpeed;

        projectile.transform.parent = this.transform;
    }

    internal void RegisterTarget(Collider collision)
    {
        if (collision.isTrigger) { return; }
        var healthController = collision.gameObject.GetComponentInParent<HealthController>();
        if (healthController != null)
        {
            this._targetsInRange.Add(healthController);
        }
        this.CleanTargets();
    }

    private void CleanTargets()
    {
        this._targetsInRange = this._targetsInRange.Where(this.IsValudTarget).ToList();
    }

    private bool IsValudTarget(HealthController t)
    {
        return t != null && t.transform != null && (t.transform.position - this.transform.position).magnitude <= this._range;
    }

    internal void DeregisterTarget(Collider collision)
    {
        if (collision.isTrigger) { return; }
        var healthController = collision.gameObject.GetComponentInParent<HealthController>();
        if (healthController != null)
        {
            this._targetsInRange.Remove(healthController);
        }
        this.CleanTargets();
    }

    protected override void OnSelect()
    {
        if (this.RangeRenderer != null)
            this.RangeRenderer.enabled = true;
    }

    protected override void OnDeselect()
    {
        if (this.RangeRenderer != null)
            this.RangeRenderer.enabled = false;
    }

    public override void Ghostify()
    {
        this._enabled = false;
    }

    public override void UnGhostify()
    {
        this._enabled = true;
    }
}
