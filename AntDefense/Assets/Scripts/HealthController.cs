using Assets.Scripts;
using UnityEngine;

public class HealthController : MonoBehaviour
{
    public float MaxHealth = 10f;
    private float? _currentHealth = null;

    private float CurrentHealth => this._currentHealth ?? this.MaxHealth;

    public ProgressBar HealthBar;

    public float Damage => this.MaxHealth - this.CurrentHealth;

    public Transform DeadObject;
    public Transform Ouch;

    public void Heal(float additionalHealth)
    {
        this._currentHealth = Mathf.Min(this.MaxHealth, this.CurrentHealth + additionalHealth);
        this.UpdateBar();
    }

    public void Injure(float lostHealth)
    {
        Debug.Log(this.transform + " lost " + lostHealth + " health.");
        if (this.Ouch != null)
        {
            Instantiate(this.Ouch, this.transform.position, Quaternion.identity);
        }
        this._currentHealth = this.CurrentHealth - lostHealth;
        if (this.CurrentHealth <= 0)
        {
            this.Die();
            return;
        }
        this.UpdateBar();
    }

    private void Die()
    {
        //Debug.Log(this.transform + " has died");
        if (this.DeadObject != null)
        {
            var deadObject = Instantiate(this.DeadObject);
            this.DeadObject = null; // prevent duplicate instanciation.
            deadObject.transform.position = this.transform.position;
        }
        var deathActions = this.GetComponentsInChildren<DeathActionBehaviour>();
        foreach (var action in deathActions)
        {
            action.OnDeath();
        }
        Destroy(this.gameObject);
    }

    private void UpdateBar()
    {
        if (this.HealthBar != null)
        {
            this.HealthBar.AdjustProgress(this.CurrentHealth, this.MaxHealth);
        }
    }
}
