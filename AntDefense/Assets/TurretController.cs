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

    private readonly List<HealthController> _targetsInRange = new List<HealthController>();

    void FixedUpdate()
    {
        _reloadTimer -= Time.fixedDeltaTime;
        if (_targetsInRange.Any())
        {
            Debug.Log("Targets: " + _targetsInRange);
            var bestTarget = _targetsInRange.First();

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
        var healthController = collision.gameObject.GetComponentInParent<HealthController>();
        if(healthController != null)
        {
            _targetsInRange.Add(healthController);
        }
    }

    internal void DeregisterTarget(Collider collision)
    {
        var healthController = collision.gameObject.GetComponentInParent<HealthController>();
        if (healthController != null)
        {
            _targetsInRange.Remove(healthController);
        }
    }
}
