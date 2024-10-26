using UnityEngine;

public abstract class Smellable : MonoBehaviour
{
    public abstract Smell Smell { get; }

    public abstract float TimeFromTarget { get; }

    public abstract bool IsActual {  get; }
}

public enum Smell
{
    Food, Home
}