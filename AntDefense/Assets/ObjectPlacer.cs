using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    public List<PlaceableGhost> QuickBarObjects;

    private Vector3 _spawnLocation;

    public Vector3 SpawnOffset;

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

    // Update is called once per frame
    void Update()
    {
        UpdateSpawnPoint();
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
        
        Instantiate(prefab, _spawnLocation - prefab.FloorPoint.position, Quaternion.identity);
    }
}
