using System.Collections.Generic;
using UnityEngine;

public class UiPlane : MonoBehaviour
{
    public List<Transform> QuickBarButtons;

    float _height;
    float _width;

    private void Start()
    {
        Camera cam = Camera.main;
        var distance = (cam.transform.position - transform.position).magnitude;
        _height = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance * 2f;
        _width = _height * cam.aspect;
        var min = Mathf.Min(_height, _width);
        transform.position = cam.transform.position + cam.transform.forward * distance;

        transform.localScale = new Vector3(min, min, min);
    }

    void Update()
    {
    }
}
