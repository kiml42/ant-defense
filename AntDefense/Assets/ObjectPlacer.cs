using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    private const int MouseButton = 0;

    public List<PlaceableGhost> QuickBarObjects;

    public float RotateSpeed = 1;

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
        if (Physics.Raycast(ray, out var hit, 100, -1, QueryTriggerInteraction.Ignore))
        {
            Debug.Log(hit.transform.name);
            Debug.Log($"hit {hit.transform.name} @ {hit.point}");
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
        if(_objectBeingPlaced != null)
        {
            Destroy(_objectBeingPlaced.gameObject);
            _objectBeingPlaced = null;
        }

        if (QuickBarObjects.Count <= i) return;
        var prefab = QuickBarObjects[i];
        
        _objectBeingPlaced = Instantiate(prefab, _spawnLocation - prefab.FloorPoint.position, Quaternion.identity);
        //_objectBeingPlaced.transform.parent = transform;
        UpdateSpawnPoint();
    }

    private void ProcessClick()
    {
        // TODO allow placing multiple
        if(Input.GetMouseButtonDown(MouseButton))
        {
            _isRotating = true;
            _lastMousePosition = Input.mousePosition;
        }

        if(_isRotating)
        {
            var mousePosition = Input.mousePosition;

            var positionChange = mousePosition - _lastMousePosition;

            var angle = positionChange.x * RotateSpeed;

            _objectBeingPlaced.transform.Rotate(Vector3.up, angle);

            _lastMousePosition = mousePosition;

        }

        if (Input.GetMouseButtonUp(MouseButton))
        {
            _isRotating = false;
            _objectBeingPlaced.Place();
            _objectBeingPlaced.transform.parent = null;
            _objectBeingPlaced = null;
        }
    }
}
