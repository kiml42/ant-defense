using System;
using UnityEngine;

public class ImpactDamageHandler : MonoBehaviour
{
    /// <summary>
    /// Impulse below which this takes no damage.
    /// </summary>
    public float ResistanceImpulse = 0;

    public float DamagePerUnitImpulse = 1;

    public HealthController HealthController;



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
        if (damage > 0)
        {
            //Debug.Log("Collider = " + collision.collider.gameObject + ", Impulse = " + impulse + ", Damage = " + damage);
            this.HealthController.Injure(damage);
        }
    }
}
