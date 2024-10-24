using UnityEngine;

public class TrailPointController : MonoBehaviour, ISmellable
{
    public Smell TrailSmell = Smell.Home;
    public Smell Smell => TrailSmell;

    public override string ToString()
    {
        return "TrailPoint " + Smell;
    }
}
