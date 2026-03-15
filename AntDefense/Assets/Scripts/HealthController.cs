using Assets.Scripts;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    public float MaxHealth = 10f;
    private float? _currentHealth = null;

    private float CurrentHealth => this._currentHealth ?? this.MaxHealth;

    public ProgressBar[] HealthIndicators;

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
        {
            action.OnDeath();
        }
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
