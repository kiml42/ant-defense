using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Rigidbody Projectile;
    public Transform Emitter;
    public TurretTurner Turner;

    public float ReloadTime = 1;
    public float ProjectileSpeed = 10;

    private float _reloadTimer = 0;

    private List<HealthController> _targetsInRange = new List<HealthController>();

    void FixedUpdate()
    {
        this._reloadTimer -= Time.deltaTime;
        if (this._targetsInRange.Any())
        {
            // TODO work out a better way to pick the target.
            var bestTarget = this._targetsInRange.First();

            if(bestTarget == null || bestTarget.transform == null)
            {
                this._targetsInRange.Remove(bestTarget);
                return;
            }
            var direction = bestTarget.transform.position - this.Turner.transform.position;
            Debug.DrawRay(this.Turner.transform.position, direction);

            this.Turner.TurnTo(direction);

            if(this._reloadTimer < 0)
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
        if(healthController != null)
        {
            this._targetsInRange.Add(healthController);
        }
        this.CleanTargets();
    }

    private void CleanTargets()
    {
        this._targetsInRange = this._targetsInRange.Where(t => t != null && t.transform != null).ToList();
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
}
