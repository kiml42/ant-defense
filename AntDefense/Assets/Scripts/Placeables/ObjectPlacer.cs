using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    public static ObjectPlacer Instance { get; private set; }
    public static List<PlaceableObjectOrGhost> StaticQuickBarObjects;
    // TODO implement cost to place objects

    public TranslateHandle Handle;

    public List<PlaceableObjectOrGhost> QuickBarObjects;

    private PlaceableObjectOrGhost _objectBeingPlaced;

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

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            throw new Exception("There should not be multiple Object Placers!");
        }
        Instance = this;
        StaticQuickBarObjects = this.QuickBarObjects;
    }

    void Update()
    {
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

        this._objectBeingPlaced = Instantiate(prefab, this.Handle.transform.position - prefab.FloorPoint.position, this.Handle.transform.rotation);
        this._objectBeingPlaced.transform.parent = this.Handle.transform;
        this._objectBeingPlaced.StartPlacing();
    }

    public void CancelPlacingObject()
    {
        if (this._objectBeingPlaced != null)
        {
            Destroy(this._objectBeingPlaced.gameObject);
            this._objectBeingPlaced = null;
            //Debug.Log("Clearing last wall node because placement is being cancelled.");
            this._lastWallNode = null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="keepPlacing">if <see langword="true"/> then this should keep placing more of this object regardless of weathr that's the defualt behaviour of the object being placed.</param>
    public void PlaceObject(bool keepPlacing)
    {
        if (this._objectBeingPlaced != null && this.PositionIsValid(this._objectBeingPlaced.transform.position))
        {
            var newObject = Instantiate(this._objectBeingPlaced, this._objectBeingPlaced.transform.position, this._objectBeingPlaced.transform.rotation);

            var wallNode = newObject.GetComponent<WallNode>();
            if(wallNode != null)
            {
                wallNode.ConnectTo(this._lastWallNode);
                this._lastWallNode = wallNode;
                this._objectBeingPlaced.GetComponent<WallNode>().ConnectTo(this._lastWallNode); // make the ghost on the handle connect so that it knows where to connect its ghost wall to.
                keepPlacing = true; // always keep placing walls, they should form a chain until the user cancels.
            }
            else
            {
                //Debug.Log("Clearing last wall node because there's no wall node component.");
                this._lastWallNode = null;
            }
            newObject.Place();
            if(!keepPlacing)
            {
                this.CancelPlacingObject();
            }
        }
    }

    private WallNode _lastWallNode = null;

    public bool? CanRotateCurrentObject()
    {
        return this._objectBeingPlaced == null ? null : this._objectBeingPlaced.Rotatable;
    }

    internal bool PositionIsValid(Vector3 position)
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
        for (var i = 0; i < QuickBarObjects.Count; i++)
        {
            var prefab = this.QuickBarObjects[i];
            if (prefab.GetComponent<WallNode>() != null)
            {
                this.StartPlacingGhost(prefab);
                this.WallNodeBeingPlaced.ConnectTo(wallNode);
            }
        }
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
}
