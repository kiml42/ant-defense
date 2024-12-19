using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    // TODO implement cost to place objects
    private const int MouseButton = 0;

    public List<PlaceableGhost> QuickBarObjects;

    public float RotateSpeed = 1;
    public float QuickClickTime = 0.5f;

    private Vector3 _spawnLocation;

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

    // Start is called before the first frame update
    void Start()
    {
        _spawnLocation = transform.position;
    }

    bool _isRotating = false;
    private Vector3 _lastMousePosition;

    // Update is called once per frame
    void Update()
    {
        if(_objectBeingPlaced != null)
        {
            if (!_isRotating)
            {
                UpdateSpawnPoint();
            }
            ProcessClick();
        }

        ProcessQuickKeys();
    }

    private void UpdateSpawnPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500, -1, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log(hit.transform.name);
            //Debug.Log($"hit {hit.transform.name} @ {hit.point}");
            _spawnLocation = hit.point;
        }

        _objectBeingPlaced.transform.position = _spawnLocation - _objectBeingPlaced.transform.InverseTransformPoint(_objectBeingPlaced.FloorPoint.position);
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

        _objectBeingPlaced = Instantiate(prefab, _spawnLocation - prefab.FloorPoint.position, Quaternion.identity);
        //_objectBeingPlaced.transform.parent = transform;
        UpdateSpawnPoint();
    }

    private void CancelPlacingObject()
    {
        if (_objectBeingPlaced != null)
        {
            Destroy(_objectBeingPlaced.gameObject);
            _objectBeingPlaced = null;
        }
    }

    float _clickTime;
    private void ProcessClick()
    {
        // TODO allow placing multiple
        if (_objectBeingPlaced.Rotatable)
        {
            if(!_isRotating && Input.GetMouseButtonDown(MouseButton))
            {
                _isRotating = true;
                _lastMousePosition = Input.mousePosition;
                _clickTime = Time.timeSinceLevelLoad;
            }

            if(_isRotating)
            {
                var mousePosition = Input.mousePosition;

                var positionChange = mousePosition - _lastMousePosition;

                var angle = positionChange.x * RotateSpeed;

                _objectBeingPlaced.transform.Rotate(Vector3.up, angle);

                _lastMousePosition = mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if(_isRotating )
            {
                _isRotating = false;
            }
            else
            {
                CancelPlacingObject();
                return;
            }
        }

        if (Input.GetMouseButtonUp(MouseButton))
        {
            var timeSinceClick = Time.timeSinceLevelLoad - _clickTime;
            if (!_objectBeingPlaced.Rotatable || timeSinceClick > QuickClickTime)
            {
                // Only process the mouse up if it's been a reasonable time since it was clicked.
                _isRotating = false;
                _objectBeingPlaced.Place();
                _objectBeingPlaced.transform.parent = null;
                _objectBeingPlaced = null;
            }
        }
    }
}
