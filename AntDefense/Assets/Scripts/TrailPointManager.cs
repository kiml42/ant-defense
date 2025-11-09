using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TrailPointManager : MonoBehaviour
{
    public static TrailPointManager Instance { get; private set; }
    private static readonly Queue<TrailPointController> _trailPoints = new();
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
        foreach (var trailPoint in _trailPoints.Where(t => !t.IsDestroyed()).ToArray())
        {
            trailPoint.UpdateVisuals();
        }
    }

    public int MaxTrailPointsPerFrame = 100;

    void FixedUpdate()
    {
        //Debug.Log($"Updating trail points. Count: {_trailPoints.Count}, Frames to Cycle = {_trailPoints.Count / this.MaxTrailPointsPerFrame}, Time to Cycle = {Time.fixedDeltaTime * _trailPoints.Count / this.MaxTrailPointsPerFrame}");

        for (int i = 0; i < this.MaxTrailPointsPerFrame && _trailPoints.Count > 0; i++)
        {
            var trailPoint = _trailPoints.Dequeue();
            trailPoint.UpdateTrailPoint();
            if (!trailPoint.IsDestroyed())
            {
                _trailPoints.Enqueue(trailPoint);
            }
        }
    }

    internal static void Register(TrailPointController newPoint)
    {
        _trailPoints.Enqueue(newPoint);
    }
}
