using UnityEngine;

public abstract class Smellable : MonoBehaviour
{
    public abstract Smell Smell { get; }

    public abstract float Distance { get; }
}

public enum Smell
{
    Food, Home
}