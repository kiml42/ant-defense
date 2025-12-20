using System;
using System.Linq;
using UnityEngine;

public class ImpactDamageHandler : MonoBehaviour
{
    /// <summary>
    /// Impulse below which this takes no damage.
    /// </summary>
    public float ResistanceImpulse = 0;

    public float DamagePerUnitImpulse = 1;

    public HealthController HealthController;

    public Transform Ouch;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (this.HealthController == null)
        {
            this.HealthController = this.GetComponent<HealthController>();
            Debug.Assert(this.HealthController != null, "ImpactDamageHandler did not find a HealthController on the same GameObject, trying children.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var impulse = collision.impulse.magnitude;
        var excessImpule = impulse - this.ResistanceImpulse;
        var damage = excessImpule * this.DamagePerUnitImpulse;
        this.DealDamageAtCollisionPoint(collision, damage);
    }

    public void DealDamageAtCollisionPoint(Collision collision, float damage)
    {
        var point = collision.contacts.First().point;
        this.DealDamageAtPoint(damage, point);
    }

    public void DealDamageAtPoint(float damage, Vector3 point)
    {
        if (damage > 0)
        {
            //Debug.Log("Collider = " + collision.collider.gameObject + ", Impulse = " + impulse + ", Damage = " + damage);
            if (this.Ouch != null)
            {
                Instantiate(this.Ouch, point, Quaternion.identity);
            }

            this.HealthController.Injure(damage);
        }
    }
}
