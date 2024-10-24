using UnityEngine;

public class HomeSmell : MonoBehaviour, ISmellable
{
    public Smell Smell => Smell.Home;

    public override string ToString()
    {
        return "Actual Home";
    }
}
