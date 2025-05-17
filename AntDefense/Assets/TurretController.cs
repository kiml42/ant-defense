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
        _reloadTimer -= Time.fixedDeltaTime;
        if (_targetsInRange.Any())
        {
            // TODO work out a better way to pick the target.
            var bestTarget = _targetsInRange.First();

            if(bestTarget == null || bestTarget.transform == null)
            {
                _targetsInRange.Remove(bestTarget);
                return;
            }
            var direction = bestTarget.transform.position - Turner.transform.position;
            Debug.DrawRay(Turner.transform.position, direction);

            Turner.TurnTo(direction);

            if(_reloadTimer < 0)
            {
                Fire();
                _reloadTimer = this.ReloadTime;
            }
        }
    }

    private void Fire()
    {
        var projectile = Instantiate(Projectile, Emitter.position, Emitter.rotation);
        projectile.velocity = Emitter.forward * ProjectileSpeed;
    }

    internal void RegisterTarget(Collider collision)
    {
        if (collision.isTrigger) { return; }
        var healthController = collision.gameObject.GetComponentInParent<HealthController>();
        if(healthController != null)
        {
            _targetsInRange.Add(healthController);
        }
        CleanTargets();
    }

    private void CleanTargets()
    {
        _targetsInRange = _targetsInRange.Where(t => t != null && t.transform != null).ToList();
    }

    internal void DeregisterTarget(Collider collision)
    {
        if (collision.isTrigger) { return; }
        var healthController = collision.gameObject.GetComponentInParent<HealthController>();
        if (healthController != null)
        {
            _targetsInRange.Remove(healthController);
        }
        CleanTargets();
    }
}
