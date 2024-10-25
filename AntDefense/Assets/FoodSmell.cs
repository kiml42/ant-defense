using UnityEngine;

public class FoodSmell : Smellable
{
    public override Smell Smell => Smell.Food;

    public override float Distance => 0;

    public override string ToString()
    {
        return "Actual Food";
    }
}
