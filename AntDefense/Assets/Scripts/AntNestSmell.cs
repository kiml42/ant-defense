using System;

public class AntNestSmell : Smellable
{
    public override Smell Smell => Smell.Home;

    public override bool IsActual => true;

    public override float GetPriority(ITargetPriorityCalculator _)
    {
        return 0;   // Ant nest doesn't need prioritization, It'll always win by being Actual.
    }
}
