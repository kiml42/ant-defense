using UnityEngine;

public abstract class Smellable : MonoBehaviour
{
    public abstract Smell Smell { get; }

    public abstract float TimeFromTarget { get; }

    public abstract bool IsActual {  get; }

    public Transform TargetPoint;
}

public enum Smell
{
    Food, Home
}