using UnityEngine;

public class Repairable : MonoBehaviour
{
    public float RepairCostPerHealth = 1f;

    private HealthController _health;
    private HealthController Health => this._health ??= this.GetComponent<HealthController>();

    public bool NeedsRepair => this.Health != null && this.Health.Damage > 0;
    public float RepairCost => (this.Health?.Damage ?? 0) * this.RepairCostPerHealth;

    public void Repair()
    {
        if (this.Health == null) return;
        var cost = this.RepairCost;
        if (!MoneyTracker.CanAfford(cost)) return;
        MoneyTracker.Spend(cost);
        this.Health.Heal(this.Health.Damage);
    }
}
