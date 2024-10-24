using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntNest : MonoBehaviour
{
    private static GameObject _defaultParent;
    public GameObject AntParent;

    public Transform AntPrefab;

    private float _timeUntilSpawn;

    // Start is called before the first frame update
    void Start()
    {
        if (AntParent == null)
        {
            if (_defaultParent == null)
            {
                _defaultParent = new GameObject();
                _defaultParent.name = "AntParent";
            }
            AntParent = _defaultParent;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _timeUntilSpawn -= Time.deltaTime;
        if(_timeUntilSpawn < 0)
        {
            Instantiate(AntPrefab, this.transform.position, Quaternion.identity, AntParent.transform);
            _timeUntilSpawn = Random.Range(1f,5f);
        }
    }
}
