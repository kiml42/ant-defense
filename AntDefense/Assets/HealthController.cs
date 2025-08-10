using UnityEngine;

public class HealthController : MonoBehaviour
{
    public float MaxHealth = 10f;
    private float? _currentHealth = null;

    private float CurrentHealth => _currentHealth ?? MaxHealth;

    public ProgressBar HealthBar;

    public float Damage => MaxHealth - CurrentHealth;

    public void Heal(float additionalHealth)
    {
        _currentHealth = Mathf.Min(MaxHealth, CurrentHealth + additionalHealth);
        UpdateBar();
    }

    public void Injure(float lostHealth)
    { 
        _currentHealth = CurrentHealth - lostHealth;
        if (CurrentHealth <= 0)
        {
            Debug.Log(this.transform + " has died");
            // TODO drop food when killed (and reactivate it's smell
            // TODO create dead ant when killed.
            Destroy(gameObject);
            return;
        }
        UpdateBar();
    }

    private void UpdateBar()
    {
        HealthBar?.AdjustProgress(CurrentHealth, MaxHealth);
    }
}
