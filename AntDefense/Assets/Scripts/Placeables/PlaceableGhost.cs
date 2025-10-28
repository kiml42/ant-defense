using System;
using UnityEngine;

[Obsolete("Use PlaceableRealObject instead")]
public class PlaceableGhost : PlaceableObjectOrGhost
{
    public Transform RealObject;

    override protected Transform FallbackIcon { get { return this.RealObject; } }

    protected override void Finalise()
    {
        var newObject = Instantiate(this.RealObject, this.transform.position + this.SpawnOffset, this.transform.rotation);

        var foodSmells = newObject.GetComponentsInChildren<FoodSmell>();
        foreach (var foodSmell in foodSmells)
        {
            foodSmell.MarkAsPermanant(false);
        }

        Destroy(this.gameObject);
    }
}


public abstract class PlaceableObjectOrGhost : MonoBehaviour
{
    public Transform FloorPoint;

    public bool Rotatable = true;

    public float BaseCost;

    public float TotalCost
    {
        get
        {
            float additionalCost = 0;
            var placeableComponents = this.GetComponentsInChildren<PlaceableMonoBehaviour>();
            //Debug.Log($"Calculating total cost for {this}. Found {placeableComponents.Length} placeable components.");
            foreach (var placeableComponent in placeableComponents)
            {
                //Debug.Log($" - {placeableComponent} has additional cost {placeableComponent.AdditionalCost}");
                additionalCost += placeableComponent.AdditionalCost;
            }
            return this.BaseCost + additionalCost;
        }
    }

    // TODO put this on the UI canvas.
    // TODO just make a button version.
    public Transform Icon;
    public float ScaleForButton = 1;
    public Vector3 OffsetForButton = Vector3.zero;
    public Quaternion RotationForButton = Quaternion.identity;

    protected abstract Transform FallbackIcon { get; }
    public Transform ActualIcon { get { return this.Icon == null ? this.FallbackIcon : this.Icon; } }

    public float TimeOut = 3;

    protected bool _isPlaced = false;

    public Vector3 SpawnOffset = Vector3.zero;

    private NoSpawnZone[] _noSpawnZones;

    private void SetNoSpawnZoneEnabled(bool enabled)
    {
        // TODO check if this is correctly activating the no spawn zone at the right time. and calculating the intersects only once it's placed.
        this._noSpawnZones ??= this.GetComponentsInChildren<NoSpawnZone>();
        foreach (var noSpawnZone in this._noSpawnZones)
        {
            noSpawnZone.enabled = enabled;
        }
    }

    /// <summary>
    /// Called when the object is instanciated attached to the translate handle.
    /// </summary>
    public virtual void StartPlacing()
    {
        this.SetNoSpawnZoneEnabled(false);
    }

    /// <summary>
    /// Called when the object is placed and detached from the translate handle.
    /// </summary>
    public virtual void Place()
    {
        this._isPlaced = true;

        this.SetNoSpawnZoneEnabled(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (this._isPlaced)
        {
            this.TimeOut -= Time.deltaTime;
            if (this.TimeOut < 0)
            {
                this.Finalise();
            }
            this.SetNoSpawnZoneEnabled(true);
        }
        else
        {
            this.SetNoSpawnZoneEnabled(false);
        }
    }

    public bool CanAfford => MoneyTracker.CanAfford(this.TotalCost);

    protected abstract void Finalise();
}

