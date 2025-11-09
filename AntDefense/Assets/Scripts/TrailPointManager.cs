using System;
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

    public static Smell[] VisibleTrailSmells
    {
        get
        {
            switch (_currentTrailDisplayMode)
            {
                case TrailDisplayMode.All:
                    return new[] { Smell.Home, Smell.Food };
                case TrailDisplayMode.FoodOnly:
                    return new[] { Smell.Food };
                default:
                    return Array.Empty<Smell>();
            }
        }
    }
    private static TrailDisplayMode _currentTrailDisplayMode = TrailDisplayMode.All;
    private enum TrailDisplayMode
    {
        All,
        FoodOnly,
        None
    }

    private void Update()
    {
        var cycleTrailMode = Input.GetKeyUp(KeyCode.T);
        if (cycleTrailMode)
        {
            switch (_currentTrailDisplayMode)
            {
                case TrailDisplayMode.All:
                    _currentTrailDisplayMode = TrailDisplayMode.FoodOnly;
                    break;
                case TrailDisplayMode.FoodOnly:
                    _currentTrailDisplayMode = TrailDisplayMode.None;
                    break;
                case TrailDisplayMode.None:
                    _currentTrailDisplayMode = TrailDisplayMode.All;
                    break;
            }
            // update all.
            foreach (var trailPoint in _trailPoints.Where(t => !t.IsDestroyed()))
            {
                trailPoint.UpdateVisibility();
            }
        }
        else if(_currentTrailDisplayMode != TrailDisplayMode.None)
        {
            var includedPoints = _trailPoints.Where(t => !t.IsDestroyed());
            if(_currentTrailDisplayMode != TrailDisplayMode.All)
            {
                includedPoints = includedPoints.Where(t => VisibleTrailSmells.Contains(t.Smell));
            }
            foreach (var trailPoint in includedPoints.Take(this.MaxTrailPointsPurUiFrame))
            {
                trailPoint.UpdateScale();
            }
        }
    }

    public int MaxTrailPointsPerFrame = 100;
    public int MaxTrailPointsPurUiFrame = 300;

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
