using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    public static ObjectPlacer Instance { get; private set; }
    public static List<PlaceableGhost> StaticQuickBarObjects;
    // TODO implement cost to place objects

    public TranslateHandle Handle;

    public List<PlaceableGhost> QuickBarObjects;

    private PlaceableGhost _objectBeingPlaced;

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
            throw new System.Exception("There should not be multiple Object Placers!");
        }
        Instance = this;
        StaticQuickBarObjects = this.QuickBarObjects;
    }

    void Update()
    {
        ProcessQuickKeys();
    }

    private void ProcessQuickKeys()
    {
        for (int i = 0; i < _quickBarKeys.Length; i++)
        {
            if (Input.GetKeyUp(_quickBarKeys[i]))
            {
                SpawnQuickObject(i);
            }
        }
    }

    private void SpawnQuickObject(int i)
    {
        if (QuickBarObjects.Count <= i) return;
        var prefab = QuickBarObjects[i];
        StartPlacingGhost(prefab);
    }

    public void StartPlacingGhost(PlaceableGhost prefab)
    {
        CancelPlacingObject();

        _objectBeingPlaced = Instantiate(prefab, Handle.transform.position - prefab.FloorPoint.position, Handle.transform.rotation);
        _objectBeingPlaced.transform.parent = Handle.transform;
    }

    public void CancelPlacingObject()
    {
        if (_objectBeingPlaced != null)
        {
            Destroy(_objectBeingPlaced.gameObject);
            _objectBeingPlaced = null;
            Debug.Log("Clearing last wall node because placement is being cancelled.");
            _lastWallNode = null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="keepPlacing">if <see langword="true"/> then this should keep placing more of this object regardless of weathr that's the defualt behaviour of the object being placed.</param>
    public void PlaceObject(bool keepPlacing)
    {
        if (_objectBeingPlaced != null)
        {
            var newObject = Instantiate(_objectBeingPlaced, _objectBeingPlaced.transform.position, _objectBeingPlaced.transform.rotation);
            newObject.Place();

            var wallNode = newObject.GetComponent<WallNode>();
            if(wallNode != null)
            {
                Debug.Log($"Placing wall node {wallNode}. Connecting to last node: " + _lastWallNode);
                wallNode.ConnectTo(_lastWallNode);
                _lastWallNode = wallNode;
                wallNode.OnPlaceAsGhost();
                Debug.Log("New last wall node: " + _lastWallNode);
                _objectBeingPlaced.GetComponent<WallNode>().ConnectTo(_lastWallNode); // make the ghost on the handle connect so that it knows where to connect its ghost wall to.
                keepPlacing = true; // always keep placing walls, they should form a chain until the user cancels.
            }
            else
            {
                Debug.Log("Clearing last wall node because there's no wall node component.");
                _lastWallNode = null;
            }
        }
        if(!keepPlacing)
        {
            this.CancelPlacingObject();
        }
    }

    private WallNode _lastWallNode = null;

    public bool? CanRotateCurrentObject()
    {
        return this._objectBeingPlaced == null ? null : this._objectBeingPlaced.Rotatable;
    }

    internal void NotifyBuiltWall(WallNode wallNode, PlaceableGhost ghost)
    {
        if(this._lastWallNode == ghost.GetComponent<WallNode>())
        {
            Debug.Log("Updating last wall node from the ghost to the real one.");
            _lastWallNode = wallNode;
        }
        if (_objectBeingPlaced != null)
        {
            var placingOjectWallNode = _objectBeingPlaced.GetComponent<WallNode>();
            if (placingOjectWallNode != null)
            {
                placingOjectWallNode.ConnectTo(_lastWallNode);
            }
        }
    }
}
