using System;
using System.Diagnostics;

public class FoodSmell : Smellable
{
    public override Smell Smell => Smell.Food;

    public override bool IsActual => true;

    private float _foodValue;

    private void Start()
    {
        var food = this.GetComponent<Food>();
        Debug.Assert(food != null, $"FoodSmell {this} must be attached to a GameObject with a Food component.");
        this._foodValue = food.FoodValue;
    }

    public override string ToString()
    {
        return "Actual Food";
    }

    public override float GetPriority(ITargetPriorityCalculator priorityCalculator)
    {
        return priorityCalculator?.CalculatePriority(0, this._foodValue)
            ?? -this._foodValue; // fall back to prioritising higher valued food.
    }
}
