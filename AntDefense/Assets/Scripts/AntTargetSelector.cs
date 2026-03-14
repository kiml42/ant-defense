using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(AntTargetPositionProvider))]
public class AntTargetSelector : MonoBehaviour
{
    public Smellable _currentTarget;
    public Transform ViewPoint;

    /// <summary>
    /// The maximum time an ant is allowed to try to move towards a single trail point.
    /// If it takes longer than this it'll give up and look for a better trail point.
    /// </summary>
    public float MaxTimeGoingForTrailPoint = 4;
    public float _timeSinceTargetAquisition;

    /// <summary>
    /// Amount to decrease the max target time by if no better target has been found in <see cref="MaxTimeGoingForTrailPoint"/> seconds.
    /// </summary>
    public float GiveUpPenalty = 0.1f;

    /// <summary>
    /// Extra time allowed to go for a given target if the ant collides with an obstacle.
    /// </summary>
    public float CollisionTargetBonus = 0.5f;

    public float GiveUpRecoveryMultiplier = 4f;

    // TODO check if this mechanism is still useful now the LOS check issues are fixed.
    public float AutomaticallyFindPreviousTrailPointDistance = 1f;

    private float? _maxTargetPriority;
    private readonly HashSet<Smellable> _newBetterTargets = new();
    private AntStateMachine _antStateMachine;

    private AntTargetPositionProvider PositionProvider => _antStateMachine.PositionProvider;
    private ITargetPriorityCalculator PriorityCalculator => _antStateMachine.PriorityCalculator;
    private bool FullDebugLogs => _antStateMachine != null && _antStateMachine.FullDebugLogs;

    public Smellable CurrentTarget
    {
        get
        {
            if (_currentTarget == null || _currentTarget.gameObject == null || _currentTarget.transform == null)
                _currentTarget = null;
            return _currentTarget;
        }
    }

    private void Awake()
    {
        _antStateMachine = GetComponent<AntStateMachine>();
        ViewPoint = ViewPoint != null ? ViewPoint : transform;
    }

    private void FixedUpdate()
    {
        if (_currentTarget.IsDestroyed() || (CurrentTarget != null && !CurrentTarget.IsSmellable))
        {
            Log("Current target is no longer valid, clearing target.");
            ClearTarget();
        }

        // Workaround for unreliable smell detection — if the ant gets too close to a trail point,
        // automatically find the next point in the trail chain.
        if (CurrentTarget != null && !CurrentTarget.IsDestroyed() &&
            Vector3.Distance(transform.position, CurrentTarget.transform.position) < AutomaticallyFindPreviousTrailPointDistance)
        {
            var nextPoint = GetNextTrailPoint(CurrentTarget as TrailPointController);
            if (nextPoint != null)
                RegisterPotentialTarget(nextPoint, "automatically finding next trail point in chain.");
            else
                DrawLineToTarget(CurrentTarget, Color.gray);
        }

        Log($"****Current target: {CurrentTarget}, MaxTargetPriority: {_maxTargetPriority}, TimeSinceTargetAquisition: {_timeSinceTargetAquisition}. Checking new targets...");

        foreach (var potentialTarget in _newBetterTargets)
        {
            if (IsBetterThanCurrent(potentialTarget))
            {
                if (CheckLineOfSight(potentialTarget))
                {
                    Log($"Switching to {potentialTarget} because it's better than {CurrentTarget} and has line of sight.");
                    SetTarget(potentialTarget);
                }
                else
                {
                    Log($"Not switching to {potentialTarget} — no line of sight.");
                    DrawLineToTarget(potentialTarget, Color.red);
                }
            }
            else
            {
                Log($"Not switching to {potentialTarget} — not better than current target {CurrentTarget}.");
                DrawLineToTarget(potentialTarget, Color.orange);
            }
        }
        _newBetterTargets.Clear();

        Log($"****Finished checking targets. Current target: {CurrentTarget}, MaxTargetPriority: {_maxTargetPriority}.");

        _timeSinceTargetAquisition += Time.deltaTime;
        if (CurrentTarget != null)
        {
            DrawLineToTarget(CurrentTarget, Color.cyan, true);
            if (!CurrentTarget.IsActual && _timeSinceTargetAquisition > MaxTimeGoingForTrailPoint)
            {
                var nextPoint = GetNextTrailPoint(CurrentTarget as TrailPointController);
                if (nextPoint != null)
                {
                    Log("Switching to next trail point @" + nextPoint.transform.position + " rather than timing out on " + CurrentTarget.transform.position);
                    RegisterPotentialTarget(nextPoint, "Switching to next trail point " + nextPoint + " rather than timing out on " + CurrentTarget);
                }
                else
                {
                    Log("Hasn't found a better target in " + _timeSinceTargetAquisition + ", forgetting " + CurrentTarget + ". MaxTargetPriority = " + _maxTargetPriority);
                    _maxTargetPriority = CurrentTarget.GetPriority(PriorityCalculator) - GiveUpPenalty;
                    ClearTarget();
                }
            }
            else if (!CheckLineOfSight(CurrentTarget))
            {
                Log("Lost sight of current target!");
                ClearTarget();
            }
        }

        if (_maxTargetPriority.HasValue)
        {
            Log($"Adjusting _maxTargetPriority from {_maxTargetPriority} to {_maxTargetPriority + Time.deltaTime * GiveUpRecoveryMultiplier}");
            _maxTargetPriority += Time.deltaTime * GiveUpRecoveryMultiplier;
        }
    }

    public void RegisterPotentialTarget(Smellable smellable, string debugString)
    {
        if (CurrentTarget != null && CurrentTarget.IsActual)
            return;

        if (_maxTargetPriority.HasValue && smellable.GetPriority(PriorityCalculator) > _maxTargetPriority)
        {
            Log("Ignoring " + smellable + " because priority exceeds max of " + _maxTargetPriority);
            return;
        }

        if (IsBetterThanCurrent(smellable))
        {
            Log("Switched Smell Target - " + debugString);
            _newBetterTargets.Add(smellable);
        }
    }

    public void SetTarget(Smellable smellable)
    {
        _currentTarget = smellable;
        PositionProvider.SetTarget(CurrentTarget);
        _timeSinceTargetAquisition = 0;
    }

    public void ClearTarget()
    {
        _newBetterTargets.Clear();
        _currentTarget = null;
        PositionProvider.SetTarget(CurrentTarget);
    }

    public void ResetMaxPriority()
    {
        _maxTargetPriority = null;
    }

    public void OnObstacleCollision()
    {
        _timeSinceTargetAquisition -= CollisionTargetBonus;
    }

    private bool IsBetterThanCurrent(Smellable smellable)
    {
        return CurrentTarget == null
            || (smellable.IsActual && !CurrentTarget.IsActual)
            || smellable.GetPriority(PriorityCalculator) < CurrentTarget.GetPriority(PriorityCalculator);
    }

    private bool CheckLineOfSight(Smellable potentialTarget)
    {
        if (potentialTarget == null || ViewPoint == null)
            return false;

        Vector3 start = potentialTarget.transform.position;
        Vector3 end = ViewPoint.position;
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        Vector3 startOffset = start - direction * 0.1f;

        int layerMask = ~LayerMask.GetMask(LayerMask.LayerToName(2));
        if (Physics.Raycast(startOffset, direction, out RaycastHit hit, distance, layerMask))
        {
            if (hit.transform != transform && hit.transform != potentialTarget.transform)
            {
                Log("It's an obstacle! " + hit.transform);
                if (FullDebugLogs)
                {
                    Debug.DrawLine(start, hit.point, Color.white);
                    Debug.DrawLine(start, end, Color.gray);
                }
                return false;
            }
        }

        return true;
    }

    private Smellable GetNextTrailPoint(TrailPointController currentTrailPoint)
    {
        if (currentTrailPoint == null)
            return null;

        var nextPoint = currentTrailPoint.GetBestPrevious(PriorityCalculator);
        if (nextPoint != null && !nextPoint.IsDestroyed() && nextPoint.IsSmellable)
            return nextPoint;

        return null;
    }

    private void DrawLineToTarget(Smellable target, Color colour, bool force = false)
    {
        if (FullDebugLogs || force)
            Debug.DrawLine(transform.position, target.TargetPoint.position, colour);
    }

    private void Log(string message)
    {
        if (FullDebugLogs)
            Debug.Log(message);
    }
}
