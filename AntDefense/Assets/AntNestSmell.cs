public class AntNestSmell : Smellable
{
    public override Smell Smell => Smell.Home;
    public override float DistanceFromTarget => 0;
    public override bool IsActual => true;
}
