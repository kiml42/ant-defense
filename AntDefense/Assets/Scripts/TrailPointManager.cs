using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TrailPointManager : MonoBehaviour
{
    public static TrailPointManager Instance { get; private set; }
    private static readonly List<TrailPointController> _trailPoints = new();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            throw new System.Exception("There should not be multiple TrailPointManagers!");
        }
        Instance = this;
    }

    private void Update()
    {
        foreach (var trailPoint in _trailPoints.ToArray())
        {
            trailPoint.UpdateVisuals();
        }
    }

    void FixedUpdate()
    {
        //Debug.Log("Updating trail points. Count: " + _trailPoints.Count);
        foreach (var trailPoint in _trailPoints.ToArray())
        {
            trailPoint.UpdateTrailPoint();
            if (trailPoint.IsDestroyed())
            {
                _trailPoints.Remove(trailPoint);
            }
        }
    }

    internal static void Register(TrailPointController newPoint)
    {
        _trailPoints.Add(newPoint);
    }
}
