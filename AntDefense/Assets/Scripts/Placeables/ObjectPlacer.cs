using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : SingletonMonoBehaviour<ObjectPlacer>
{
    public static List<PlaceableObjectOrGhost> StaticQuickBarObjects;

    public TranslateHandle Handle;

    public List<PlaceableObjectOrGhost> QuickBarObjects;

    /// <summary>
    /// This is tracked so that when the new object is created and finalised,
    /// it is a new copy and doesn't retain any of the changes made to the <see cref="_objectBeingPlaced"/> instance.
    /// </summary>
    PlaceableObjectOrGhost _prefabBeingPlaced;
    private PlaceableObjectOrGhost _objectBeingPlaced;
    private WallNode _additionalWallGhost;

    private readonly KeyCode[] _quickBarKeys = {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9,
        KeyCode.Alpha0,
    };
    public bool IsPlacingObject => this._objectBeingPlaced != null;

    protected override void OnAwake()
    {
        StaticQuickBarObjects = this.QuickBarObjects;
    }

    void Update()
    {
        this._costCache = null; // reset cost cache each frame.
        this.ProcessQuickKeys();
    }

    private void ProcessQuickKeys()
    {
        for (int i = 0; i < this._quickBarKeys.Length; i++)
        {
            if (Input.GetKeyUp(this._quickBarKeys[i]))
            {
                this.SpawnQuickObject(i);
            }
        }
    }

    private void SpawnQuickObject(int i)
    {
        if (this.QuickBarObjects.Count <= i) return;
        var prefab = this.QuickBarObjects[i];
        this.StartPlacingGhost(prefab);
    }

    public void StartPlacingGhost(PlaceableObjectOrGhost prefab)
    {
        this.CancelPlacingObject();

        this._prefabBeingPlaced = prefab;
        this._objectBeingPlaced = Instantiate(prefab, this.Handle.transform.position - prefab.FloorPoint.position, this.Handle.transform.rotation);
        this._objectBeingPlaced.transform.parent = this.Handle.transform;
        this._objectBeingPlaced.StartPlacing();

        if (this._objectBeingPlaced.WallToBuildOn != null)
        {
            this._additionalWallGhost = Instantiate(this._objectBeingPlaced.WallToBuildOn, this.Handle.transform.position, this.Handle.transform.rotation);
            this._additionalWallGhost.transform.parent = this.Handle.transform;
            this._additionalWallGhost.GetComponent<PlaceableObjectOrGhost>().StartPlacing();
        }

        var turretController = this._objectBeingPlaced.GetComponentInChildren<TurretController>();
        if (turretController != null)
        {
            Debug.Log("Starting placing turret " + turretController);
        }
    }

    public void CancelPlacingObject()
    {
        if (this.IsPlacingObject)
        {
            Destroy(this._objectBeingPlaced.gameObject);
            this._objectBeingPlaced = null;
            //Debug.Log("Clearing last wall node because placement is being cancelled.");
            this._lastWallNode = null;
            this._prefabBeingPlaced = null;
        }
        if (this._additionalWallGhost != null)
        {
            Destroy(this._additionalWallGhost.gameObject);
            this._additionalWallGhost = null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>The object that was placed</returns>
    public PlaceableObjectOrGhost PlaceObject(WallNode parent = null)
    {
        if (TranslateHandle.IsMouseOverQuickBarButton)
        {
            Debug.LogWarning("Cannot place object while mouse is over a quick bar button.");
            return null;
        }
        if (this._objectBeingPlaced == null)
        {
            Debug.LogWarning("No object is being placed, so cannot place anything.");
            return null;
        }
        if (!this.CanPlaceAt(this._objectBeingPlaced.transform.position))
        {
            Debug.LogWarning("Cannot place the object being placed at its current position.");
            return null;
        }
        if (this._prefabBeingPlaced != null && this._prefabBeingPlaced.WallToBuildOn != null && parent == null)
        {
            // There is a prefab being placed, and it needs a wall, but a wall hasn't been provided.
            var wall = Instantiate(this._prefabBeingPlaced.WallToBuildOn, this._objectBeingPlaced.transform.position, this._objectBeingPlaced.transform.rotation);
            var wallPlaceable = wall.GetComponentInChildren<PlaceableObjectOrGhost>();
            wallPlaceable.Place();
            MoneyTracker.Spend(wallPlaceable.TotalCost);
            TranslateHandle.Instance.SetSelectedObject(wall);
            return wallPlaceable;
        }

        //Debug.Log($"Spending {this._objectBeingPlaced.TotalCost} for {this._objectBeingPlaced}");
        MoneyTracker.Spend(this._objectBeingPlaced.TotalCost);
        var newObject = Instantiate(this._prefabBeingPlaced, this._objectBeingPlaced.transform.position, this._objectBeingPlaced.transform.rotation);

        var wallNode = newObject.GetComponentInChildren<WallNode>();
        if (wallNode != null)
        {
            // TODO fix chaninging wall nodes when placing multiple walls in a row.
            wallNode.ConnectTo(this._lastWallNode);
            this._lastWallNode = wallNode;
            this._objectBeingPlaced.GetComponent<WallNode>().ConnectTo(this._lastWallNode); // make the ghost on the handle connect so that it knows where to connect its ghost wall to.
            TranslateHandle.Instance.SetSelectedObject(wallNode);
        }
        else
        {
            //Debug.Log("Clearing last wall node because there's no wall node component.");
            this._lastWallNode = null;
        }

        newObject.Place();

        if (parent != null)
        {
            // it's being built with a parent already.
            newObject.transform.parent = parent.transform;
        }

        return newObject;
    }

    private WallNode _lastWallNode = null;

    public bool? CanRotateCurrentObject()
    {
        return this._objectBeingPlaced == null ? null : this._objectBeingPlaced.Rotatable;
    }

    private float? _costCache;

    public float? CostForCurrentObject
    {
        get
        {
            if (!this._costCache.HasValue)
            {
                this._costCache = this._objectBeingPlaced == null ? null : (float?)this._objectBeingPlaced.TotalCost;
                if (this.CanBuildOnWall && !TranslateHandle.IsOnBuildableWall)
                {
                    // need to add the cost of the wall too.
                    var placeableWallPrefab = this._objectBeingPlaced.WallToBuildOn.GetComponent<PlaceableObjectOrGhost>();
                    this._costCache += placeableWallPrefab.BaseCost;
                }
            }

            return this._costCache;
        }
    }
    public bool CanAffordCurrentObject
    {
        get
        {
            var cost = this.CostForCurrentObject;
            return !cost.HasValue || MoneyTracker.CanAfford(cost.Value);
        }
    }

    internal bool CanPlaceAt(Vector3 position)
    {
        return this.CanAffordCurrentObject && this.PositionIsValid(position);
    }

    private bool PositionIsValid(Vector3 position)
    {
        if (this._objectBeingPlaced != null)
        {
            var positionValidators = this._objectBeingPlaced.GetComponentsInChildren<IPlaceablePositionValidator>();
            if (positionValidators != null && positionValidators.Length != 0)
            {
                foreach (var validator in positionValidators)
                {
                    if (!validator.PositionIsValid(position))
                    {
                        return false;
                    }
                }
            }
        }

        return true; // no object being placed, so position is valid.
    }

    internal void StartPlacingWallConnectedTo(WallNode wallNode)
    {
        for (var i = 0; i < this.QuickBarObjects.Count; i++)
        {
            var prefab = this.QuickBarObjects[i];
            if (prefab.GetComponent<WallNode>() != null)
            {
                this.StartPlacingGhost(prefab);
                this.WallNodeBeingPlaced.ConnectTo(wallNode);
            }
        }
        this._lastWallNode = wallNode;  // this must come after StartPlacingGhost because StartPlacingGhost cancelss teh current placing, and clears the last wall node.
    }

    /// <summary>
    /// Returns the WallNode component of the object being placed, or null if there is no object being placed or if the object being placed does not have a WallNode component.
    /// </summary>
    public WallNode WallNodeBeingPlaced
    {
        get
        {
            return this._objectBeingPlaced == null
                ? null
                : this._objectBeingPlaced.GetComponent<WallNode>();
        }
    }

    public bool CanBuildOnWall { get { return this._prefabBeingPlaced != null && this._prefabBeingPlaced.CanBuildOnWall; } }
}
