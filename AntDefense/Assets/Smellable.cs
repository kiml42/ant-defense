using UnityEngine;

public abstract class Smellable : MonoBehaviour
{
    public abstract Smell Smell { get; }

    public abstract float DistanceFromTarget { get; }

    /// <summary>
    /// true for objects that are teh actual thing, rather than a trail point leading to the thing.
    /// </summary>
    public abstract bool IsActual { get; }

    /// <summary>
    /// true for objects that are spawned at a source of that sort of object, or are permanent, and therefore ants should leave a trail to say it's there.
    /// false indicates that this is a one-off source of this smell, so they shouldn't leave a trail for it.
    /// </summary>
    public abstract bool IsPermanentSource { get; }

    public Transform TargetPoint;

    /// <summary>
    /// Allows disabling this from being smelt, without removing the component.
    /// </summary>
    public bool IsSmellable = true;
}

public enum Smell
{
    Food, Home
}