using UnityEngine;

public class FoodCost : MonoBehaviour
{
    public float OverheadCost = 10;
    public HealthController HealthController;
    public Digestion Digestion;

    public float Cost => this.OverheadCost + ((this.HealthController?.MaxHealth + this.Digestion.MaxFood) ?? 0);
}
