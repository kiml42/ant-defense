using Assets.Scripts.Helpers;
using UnityEngine;

public class ButtonShooter : MonoBehaviour
{
    public KeyCode ShootKey = KeyCode.Z;
    public Rigidbody ProjectilePrefab;
    public float ShootSpeed = 3f;
    public float ReloadTime = 0.1f;
    public float StartOffset = 0.1f;
    private float _reloadTimer = 0;

    void Update()
    {
        if (Input.GetKey(this.ShootKey) && this._reloadTimer <= 0 && MouseHelper.RaycastToFloor(out var hit))
        {
            var vectorToHit = hit.point - this.transform.position;
            var projectile = Instantiate(this.ProjectilePrefab, this.transform.position + (vectorToHit.normalized * this.StartOffset), this.transform.rotation);
            projectile.linearVelocity = vectorToHit * this.ShootSpeed;
            this._reloadTimer = this.ReloadTime;
        }
        this._reloadTimer -= Time.deltaTime;
    }
}
