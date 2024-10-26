using UnityEngine;

public class HomeSmell : Smellable
{
    public override Smell Smell => Smell.Home;

    public override float TimeFromTarget => 0;

    public override bool IsActual => true;

    public override string ToString()
    {
        return "Actual Home";
    }
}
