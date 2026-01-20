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
    private float _rangeFadeTimer = 0f;
    private bool _shouldShowRangeFromFade = false;
    private Material _rangeRendererMaterial;
    private Color _originalRangeColor;

    void Start()
    {
        Debug.Assert(this.Emitter != null, "TurretController requires an Emitter transform to fire projectiles from.");
        Debug.Assert(this.Turner != null, "TurretController requires a TurretTurner to aim the turret.");
        Debug.Assert(this.Projectile != null, "TurretController requires a Projectile to fire.");
        Debug.Assert(this.Trigger != null, "TurretController requires a TurretTrigger to determine its range.");

        NoSpawnZone.Register(this); // register this as an interactive point

        this._range = this.Trigger.TriggerCollider.radius * this.Trigger.TriggerCollider.transform.localScale.x;
        
        // Cache the range renderer material
        if (this.RangeRenderer != null)
        {
            this._rangeRendererMaterial = this.RangeRenderer.material;
            this._originalRangeColor = this._rangeRendererMaterial.color;
        }
        
        return;
    }

    void Update()
    {
        // Handle range indicator fade
        if (this._shouldShowRangeFromFade)
        {
            this._rangeFadeTimer -= Time.deltaTime;
            
            // Calculate alpha (fade from 1 to 0)
            float alpha = Mathf.Max(0f, this._rangeFadeTimer / this.RangeIndicatorFadeDuration);
            
            if (this._rangeRendererMaterial != null)
            {
                Color newColor = this._originalRangeColor;
                newColor.a = alpha;
                this._rangeRendererMaterial.color = newColor;
            }
            
            if (this._rangeFadeTimer <= 0)
            {
                this._shouldShowRangeFromFade = false;
                this.UpdateRangeRendererVisibility();
            }
        }
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

    protected override void OnSelect()
    {
        this.UpdateRangeRendererVisibility();
    }

    protected override void OnDeselect()
    {
        this.UpdateRangeRendererVisibility();
    }

    public void ShowRangeIndicator()
    {
        // Show the range indicator and start fade timer
        this._shouldShowRangeFromFade = true;
        this._rangeFadeTimer = this.RangeIndicatorFadeDuration;
        
        // Restore full alpha immediately
        if (this._rangeRendererMaterial != null)
        {
            Color newColor = this._originalRangeColor;
            newColor.a = this._originalRangeColor.a;
            this._rangeRendererMaterial.color = newColor;
        }
        
        this.UpdateRangeRendererVisibility();
    }

    private void UpdateRangeRendererVisibility()
    {
        if (this.RangeRenderer != null)
        {
            // Show if selected OR if fade timer is active
            bool shouldShow = this.IsSelected || this._shouldShowRangeFromFade;
            this.RangeRenderer.enabled = shouldShow;
            
            // When becoming invisible, restore original alpha
            if (!shouldShow && this._rangeRendererMaterial != null)
            {
                this._rangeRendererMaterial.color = this._originalRangeColor;
            }
        }
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
