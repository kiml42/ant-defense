using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    // TODO implement cost to place objects

    // TODO allow placing multiple
    public TranslateHandle Handle;

    public List<PlaceableGhost> QuickBarObjects;

    private PlaceableGhost _objectBeingPlaced;

    private KeyCode[] _quickBarKeys = {
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

    // Update is called once per frame
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
        CancelPlacingObject();

        if (QuickBarObjects.Count <= i) return;
        var prefab = QuickBarObjects[i];

        _objectBeingPlaced = Instantiate(prefab, Handle.transform.position - prefab.FloorPoint.position, Quaternion.identity);
        _objectBeingPlaced.transform.rotation = Handle.transform.rotation;
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
            _objectBeingPlaced.Place();
            _objectBeingPlaced.transform.parent = null;
            _objectBeingPlaced = null;
        }
    }
}
