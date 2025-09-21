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
            this.HealthController = GetComponent<HealthController>();
            if (this.HealthController == null)
            {
                throw new Exception("ImpactDamageHandler requires a HealthController component on the same GameObject or a child GameObject.");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var impulse = collision.impulse.magnitude;
        var excessImpule = impulse - ResistanceImpulse;
        var damage = excessImpule * DamagePerUnitImpulse;
        if (damage > 0)
        {
            //Debug.Log("Collider = " + collision.collider.gameObject + ", Impulse = " + impulse + ", Damage = " + damage);
            this.HealthController.Injure(damage);
        }
    }
}
