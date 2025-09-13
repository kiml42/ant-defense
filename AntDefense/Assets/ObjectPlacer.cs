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
        }
    }

    public void PlaceObject()
    {
        if(_objectBeingPlaced != null)
        {
            var newObject = Instantiate(_objectBeingPlaced, _objectBeingPlaced.transform.position, _objectBeingPlaced.transform.rotation);
            newObject.Place();
        }
    }

    public bool? CanRotateCurrentObject()
    {
        return this._objectBeingPlaced == null ? null : this._objectBeingPlaced.Rotatable;
    }
}
