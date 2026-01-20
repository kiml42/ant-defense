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

    public float MaxValueWeighting = 0.95f;

    /// <summary>
    /// Made public just for debugging
    /// </summary>
    public float _actualValueWeighting;

    private void Start()
    {
        this._actualValueWeighting = this.ValueWeighting + Random.Range(-this.ValueWeightingRandomisation, this.ValueWeightingRandomisation);
        this._actualValueWeighting = Mathf.Clamp(this._actualValueWeighting, 0 , this.MaxValueWeighting);   // ensure it's still between 0 and 1, below 0 will actually prefer lower value targets, and above 1 will prefer more distant targets.
    }

    /// <summary>
    /// Returns the priority of the smellable for this ant.
    /// Lower values are more important.
    /// </summary>
    /// <param name="smellable"></param>
    /// <returns></returns>
    public float CalculatePriority(float distanceFromTarget, float? targetValue)
    {
        return ((1- this._actualValueWeighting) * distanceFromTarget) - (this._actualValueWeighting * targetValue ?? 0);
    }
}
