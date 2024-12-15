using UnityEngine;

public abstract class Smellable : MonoBehaviour
{
    public abstract Smell Smell { get; }

    public abstract float DistanceFromTarget { get; }

    public abstract bool IsActual {  get; }

    public Transform TargetPoint;
}

public enum Smell
{
    Food, Home
}