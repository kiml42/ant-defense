using UnityEngine;

public interface ITargetPriorityCalculator
{
    /// <summary>
    /// Returns the priority of the smellable for this ant.
    /// Lower values are more important.
    /// </summary>
    /// <param name="smellable"></param>
    /// <returns></returns>
    float CalculatePriority(float distanceFromTarget, float? targetValue);
}

public class TargetPriorityCalculator : MonoBehaviour, ITargetPriorityCalculator
{
    /// <summary>
    /// Between 0 and 1, where 0 means only distance matters, and 1 means only value matters.
    /// </summary>
    public float ValueWeighting = 0.5f;

    /// <summary>
    /// Maximum randomisation to apply to the value weighting, to make ants behave less uniformly.
    /// </summary>
    public float ValueWeightingRandomisation = 0.1f;

    private float _actualValueWeighting;

    private void Start()
    {
        _actualValueWeighting = ValueWeighting + Random.Range(-ValueWeightingRandomisation, ValueWeightingRandomisation);
    }

    /// <summary>
    /// Returns the priority of the smellable for this ant.
    /// Lower values are more important.
    /// </summary>
    /// <param name="smellable"></param>
    /// <returns></returns>
    public float CalculatePriority(float distanceFromTarget, float? targetValue)
    {
        return ((1-_actualValueWeighting) * distanceFromTarget) - (_actualValueWeighting * targetValue ?? 0);
    }
}
