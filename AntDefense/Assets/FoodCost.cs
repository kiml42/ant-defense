using UnityEngine;

public class FoodCost : MonoBehaviour
{
    public float OverheadCost = 10;
    public HealthController HealthController;
    public Digestion Digestion;

    public float Cost => OverheadCost + ((HealthController?.MaxHealth + Digestion.MaxFood) ?? 0);
}
