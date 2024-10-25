using UnityEngine;

public class FoodSmell : Smellable
{
    public override Smell Smell => Smell.Food;

    public override float Distance => 0;
    public override bool IsActual => true;

    public override string ToString()
    {
        return "Actual Food";
    }
}
