using UnityEngine;

public class FoodSmell : Smellable
{
    private bool _isPermanentSource = true;
    private float _priority;

    public override Smell Smell => Smell.Food;

    public override float Priority => _priority;
    public override float DistanceFromTarget => 0;
    public override bool IsActual => true;

    public override bool IsPermanentSource => _isPermanentSource;

    private void Start()
    {
        var food = GetComponent<Food>();
        if (food == null) throw new System.Exception($"FoodSmell {this} must be attached to a GameObject with a Food component.");

        _priority = TrailPointController.CalculatePriority(DistanceFromTarget, food.FoodValue);
    }

    public override string ToString()
    {
        return "Actual Food";
    }

    public void MarkAsPermanant(bool isPermanentSource)
    {
        _isPermanentSource = isPermanentSource;
    }
}
