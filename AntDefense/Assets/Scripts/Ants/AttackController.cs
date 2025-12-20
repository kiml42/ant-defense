using UnityEngine;

public class AttackController : MonoBehaviour
{
    /// <summary>
    /// The time interval, in seconds, to wait before being able to attack again.
    /// </summary>
    public float ResetTime = 1f;
    private float _lastAttackTime = 0f;

    public float AttackDamage = 10f;

    /// <summary>
    /// percentage chance that this ant attacks something it collides with (that can be attacked)
    /// </summary>
    public int AttackChance = 50;

    internal bool AttackObstable(Collision collision, ImpactDamageHandler damageHandler)
    {
        if (this._lastAttackTime + this.ResetTime >= Time.fixedTime)
        {
            // can't attack again yet.
            return false;

        }
        damageHandler.DealDamageAtCollisionPoint(collision, this.AttackDamage);
        this._lastAttackTime = Time.fixedTime;
        return true;
    }

}
