using System;
using UnityEngine;

public abstract class Smellable : MonoBehaviour
{
    public abstract Smell Smell { get; }

    /// <summary>
    /// true for objects that are teh actual thing, rather than a trail point leading to the thing.
    /// </summary>
    public abstract bool IsActual { get; }


    public Transform TargetPoint;

    /// <summary>
    /// Allows disabling this from being smelt, without removing the component.
    /// </summary>
    public bool IsSmellable = true;

    public abstract float GetPriority(ITargetPriorityCalculator targetPriorityCalculator);
}

public enum Smell
{
    Food, Home
}