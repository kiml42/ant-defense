using UnityEngine;

public class TurretController : MonoBehaviour
{
    public Rigidbody Projectile;
    public Transform Emitter;

    public float ReloadTime = 1;
    public float ProjectileSpeed = 10;

    private float _reloadTimer = 0;

    void FixedUpdate()
    {
        _reloadTimer -= Time.fixedDeltaTime;
        if(_reloadTimer < 0)
        {
            Fire();
            _reloadTimer = ReloadTime;
        }
    }

    private void Fire()
    {
        var projectile = Instantiate(Projectile, Emitter.position, Emitter.rotation);
        projectile.velocity = Emitter.forward * ProjectileSpeed;
    }
}
