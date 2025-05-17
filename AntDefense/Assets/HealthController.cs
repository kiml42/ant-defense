using UnityEngine;

public class HealthController : MonoBehaviour
{
    public float MaxHealth = 10f;
    private float _currentHealth;

    public ProgressBar HealthBar;

    public float Damage => MaxHealth - _currentHealth;

    void Start()
    {
        _currentHealth = MaxHealth;
    }

    public void Heal(float additionalHealth)
    {
        _currentHealth += additionalHealth;
        if (_currentHealth > MaxHealth)
        {
            _currentHealth = MaxHealth;
        }
        UpdateBar();
    }

    public void Injure(float lostHealth)
    { 
        _currentHealth -= lostHealth;
        if (_currentHealth <= 0)
        {
            Debug.Log(this.transform + " has died");
            Destroy(gameObject);
            return;
        }
        UpdateBar();
    }

    private void UpdateBar()
    {
        HealthBar?.AdjustProgress(_currentHealth, MaxHealth);
    }
}
