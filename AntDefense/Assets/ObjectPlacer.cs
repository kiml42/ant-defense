using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    public List<GameObject> QuickBarObjects;

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
        _spawnLocation = transform.position + SpawnOffset;
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
        if (Physics.Raycast(ray, out var hit, 100))
        {
            Debug.Log(hit.transform.name);
            Debug.Log($"hit {hit.transform.name} @ {hit.point}");
            _spawnLocation = hit.point + SpawnOffset;
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
        var newObject = Instantiate(prefab, _spawnLocation, Quaternion.identity);
        
        var foodSmell = newObject.GetComponent<FoodSmell>();
        if(foodSmell != null)
        {
            foodSmell.MarkAsPermanant(false);
        }
    }
}
