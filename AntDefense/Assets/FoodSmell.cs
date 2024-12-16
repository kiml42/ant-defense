using UnityEngine;

public class FoodSmell : Smellable
{
    private bool _isPermanentSource = true;

    public override Smell Smell => Smell.Food;

    public override float DistanceFromTarget => 0;
    public override bool IsActual => true;

    public override bool IsPermanentSource => _isPermanentSource;

    public override string ToString()
    {
        return "Actual Food";
    }

    public void MarkAsPermanant(bool isPermanentSource)
    {
        _isPermanentSource = isPermanentSource;
    }
}
