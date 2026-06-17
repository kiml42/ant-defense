using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AntTrailController : MonoBehaviour
{
    private static GameObject TrailParent;

    private const float TrailPointSpawnDistance = 3f;

    /// <summary>
    /// Distance around the proposed new location for a trail point to check for existing trail points
    /// </summary>
    public const float OverlapRadius = 2f;

    private AntStateMachine AntStateMachine;

    public TrailPointController TrailPoint;

    private Smell? _lastSmell = null;

    /// <summary>
    /// the previous trail point, or the original object.
    /// </summary>
    private Smellable LastTrailPointSmellable = null;

    // TODO this is somewhat duplicative with _lastTrailPoint, consider merging.
    public TrailPointController LastTrailPointController { get; private set; }
    private float _distanceSinceTarget = 0;
    private float? _targetValue = null;

    private Rigidbody _rigidbody;

    private bool trailDisabled;


    public Smell? TrailSmell
    {
        get
        {
            if (this.trailDisabled) return null;
            switch (this.AntStateMachine.State)
            {
                case AntState.SeekingFood:
                case AntState.ReturningToFood:
                    this.AntStateMachine.TrailTargetValue = null;
                    return Smell.Home;
                case AntState.ReportingFood:
                case AntState.CarryingFood:
                    return Smell.Food;
                case AntState.ReturningHome:
                    return null;
                default:
                    throw new Exception("Unknown state " + this.AntStateMachine.State);
            }
        }
    }

    void Start()
    {
        this.AntStateMachine = this.GetComponentInChildren<AntStateMachine>();

        if (TrailParent == null)
        {
            TrailParent = new GameObject();
            TrailParent.name = "TrailParent";
        }

        this._rigidbody = this.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // TODO might be best to have an explicit method instead of just spying on the ASM.
        if (this._lastSmell != this.TrailSmell)
        {
            // The smell has changed, so reset the trail.
            this._lastSmell = this.TrailSmell;
            this.LastTrailPointSmellable = null;    // TODO this should probably be set to the new real object a trail is being left to.
            this._distanceSinceTarget = 0;
            this._targetValue = this.AntStateMachine.TrailTargetValue;
        }
        var updateDistance = this._rigidbody.linearVelocity.magnitude * Time.deltaTime;
        this._distanceSinceTarget += updateDistance;

        this.LeaveTrail();
    }

    public void DisableTrail()
    {
        this.trailDisabled = true;
    }

    public void EnableTrail()
    {
        this.trailDisabled = false;
    }

    private void LeaveTrail()
    {
        if (!this.TrailSmell.HasValue || this._targetValue <= 0) return;
        var hasPreviousTrailPoint = this.LastTrailPointSmellable != null && !this.LastTrailPointSmellable.IsDestroyed();
        var distanceToLastPoint = hasPreviousTrailPoint
            ? (this.LastTrailPointSmellable.transform.position - this.transform.position).magnitude
            : 0;
        if (!hasPreviousTrailPoint || distanceToLastPoint > TrailPointSpawnDistance)
        {
            // TODO consider if there is a lighter method for this just seeing the location of the center Possibly by keeping an octree index for the locations of all trail points
            Collider[] overlaps = Physics.OverlapSphere(this.transform.position, OverlapRadius);

            TrailPointController closest = null;
            float closestDist = float.MaxValue;
            for (int i = 0; i < overlaps.Length; i++)
            {
                var overlap = overlaps[i];
                if (overlap.IsDestroyed()) continue;
                var tpc = overlap.GetComponent<TrailPointController>();
                if (tpc == null || tpc.IsDestroyed() || tpc.Smell != this.TrailSmell) continue;
                float dist = (tpc.transform.position - this.transform.position).sqrMagnitude;
                if (dist < closestDist) { closestDist = dist; closest = tpc; }
            }

            if (closest != null)
            {
                // there's one close enough, use that.
                closest.AddSmellComponent(this._distanceSinceTarget, this._targetValue, this.LastTrailPointSmellable);
                //Debug.Log("Added smell component to other: " + closest + ". distance = " + Mathf.Sqrt(closestDist));

                this.LastTrailPointController = closest;
            }
            else
            {
                // none are close enough, so create a new one.
                var newPoint = Instantiate(this.TrailPoint, this.transform.position, Quaternion.identity, TrailParent.transform);
                newPoint.SetSmell(this.TrailSmell.Value, this._distanceSinceTarget, this._targetValue, this.LastTrailPointSmellable);
                newPoint.gameObject.layer = 2;
                //Debug.Log("Leaving trail with smell: " + newPoint.GetComponent<TrailPointController>().Smell);

                TrailPointManager.Register(newPoint);

                this.LastTrailPointController = newPoint;
                this.LastTrailPointSmellable = newPoint;
            }
        }
    }
}