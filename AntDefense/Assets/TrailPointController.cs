using UnityEngine;

public class TrailPointController : MonoBehaviour, ISmellable
{
    public Smell Smell { get; set; }

    public override string ToString()
    {
        return "TrailPoint " + Smell;
    }
}
