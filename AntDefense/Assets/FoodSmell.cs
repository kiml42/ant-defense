using System;

public class FoodSmell : Smellable
{
    private bool _isPermanentSource = true;

    public override Smell Smell => Smell.Food;

    public override bool IsActual => true;

    public override bool IsPermanentSource => _isPermanentSource;

    private float _foodValue;

    private void Start()
    {
        var food = GetComponent<Food>();
        if (food == null) throw new System.Exception($"FoodSmell {this} must be attached to a GameObject with a Food component.");
        _foodValue = food.FoodValue;
    }

    public override string ToString()
    {
        return "Actual Food";
    }

    public void MarkAsPermanant(bool isPermanentSource)
    {
        _isPermanentSource = isPermanentSource;
    }

    public override float GetPriority(Func<float, float?, float> priorityCalculator)
    {
        return priorityCalculator(0, _foodValue);
    }
}
