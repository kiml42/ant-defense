using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    public float MaxHealth = 10f;
    private float? _currentHealth = null;

    private float CurrentHealth => this._currentHealth ?? this.MaxHealth;

    public ProgressIndicatorBehaviour[] HealthIndicators;

    /// <summary>
    /// Death actions registered at runtime (e.g. by stumps attached to this node).
    /// Called alongside the normal GetComponentsInChildren search when this controller dies.
    /// </summary>
    public List<DeathActionBehaviour> AdditionalDeathActions = new List<DeathActionBehaviour>();

    public float Damage => this.MaxHealth - this.CurrentHealth;

    public void Heal(float additionalHealth)
    {
        this._currentHealth = Mathf.Min(this.MaxHealth, this.CurrentHealth + additionalHealth);
        this.ShowDamage();
    }

    public void Injure(float lostHealth)
    {
        this._currentHealth = this.CurrentHealth - lostHealth;
        if (this.CurrentHealth <= 0)
        {
            this.Die();
            return;
        }
        this.ShowDamage();
    }

    private void Die()
    {
        //Debug.Log(this.transform + " has died");
        var deathActions = this.GetComponentsInChildren<DeathActionBehaviour>();
        foreach (var action in deathActions)
            action.OnDeath();
        foreach (var action in this.AdditionalDeathActions)
            action.OnDeath();
    }

    private void ShowDamage()
    {
        if (this.HealthIndicators != null)
        {
            foreach (var indicator in this.HealthIndicators)
            {
                indicator.AdjustProgress(this.CurrentHealth, this.MaxHealth);
            }
        }
    }
}
