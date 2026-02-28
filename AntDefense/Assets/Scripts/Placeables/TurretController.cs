using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public interface ISelectableObject : IKnowsPosition
{
    public bool IsSelected { get; }
    public bool IsWallToBuildOn { get { return false; } }
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
    public float RangeIndicatorFadeDuration = 3f; // How long the range indicator stays visible after placement

    public TurretTrigger Trigger;

    private bool _enabled = true;
    private Material _rangeRendererMaterial;
    private Color _originalRangeColor;

    private bool TargetRangeRentererVisibility
    {
        get
        {
            // Show the range either
            // When this is currently selected
            // or when this is not enabled (i.e. while it's being placed)
            return this.IsSelected == true || !this._enabled;
        }
    } // either it's selected, or it's a ghost.

    //void Start()
    //{
    //    this.Init();
    //}


    private void Init()
    {
        if(this.instanceNumber != -1)
        {
            // already initialised
            return;
        }
        instanceCount++;
        instanceNumber = instanceCount;

        Debug.Assert(this.Emitter != null, "TurretController requires an Emitter transform to fire projectiles from.");
        Debug.Assert(this.Turner != null, "TurretController requires a TurretTurner to aim the turret.");
        Debug.Assert(this.Projectile != null, "TurretController requires a Projectile to fire.");
        Debug.Assert(this.Trigger != null, "TurretController requires a TurretTrigger to determine its range.");

        this._range = this.Trigger.TriggerCollider.radius * this.Trigger.TriggerCollider.transform.localScale.x;

        // Cache the range renderer material
        if (this.RangeRenderer != null)
        {
            this._rangeRendererMaterial = this.RangeRenderer.material;
            this._originalRangeColor = this._rangeRendererMaterial.color;
        }
    }

    void Update()
    {
        this.Init();
        if (this._rangeRendererMaterial != null)
        {
            Color newColor = this._originalRangeColor;
            if (this.TargetRangeRentererVisibility && this._rangeRendererMaterial.color.a < this._originalRangeColor.a)
            {
                // instantly make it completely visible.
                newColor.a = this._originalRangeColor.a;
                this._rangeRendererMaterial.color = newColor;
            }
            if (!this.TargetRangeRentererVisibility && this._rangeRendererMaterial.color.a > 0)
            {
                // fading out
                // proportion of the fade time times the original alpha.
                var step = Time.deltaTime / this.RangeIndicatorFadeDuration * this._originalRangeColor.a;

                float alpha = Mathf.Max(0f, this._rangeRendererMaterial.color.a - step);

                newColor.a = alpha;
                this._rangeRendererMaterial.color = newColor;
            }
        }
    }

    void FixedUpdate()
    {
        this.Init();
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
        if (healthController != null && !healthController.CompareTag(this.tag))
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

    public override void Ghostify()
    {
        this.Init();
        this._enabled = false;
    }

    public override void UnGhostify()
    {
        this.Init();
        this._enabled = true;
        NoSpawnZone.Register(this); // register this as an interactive point
    }

    static int instanceCount = 0;
    private int instanceNumber = -1;

    public override string ToString()
    {
        return base.ToString() + "i" + this.instanceNumber;
    }
}
