using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UiPlane : MonoBehaviour
{
    public QuickBarButton QuickBarButton;
    public Transform QuickBarCenter;
    public float QuickBarSpacing = 0.1f;
    private List<QuickBarButton> _buttons = null;

    float _height;
    float _width;

    private void Start()
    {
        Initialise();
    }

    private void Initialise()
    {
        if (_buttons != null) return;

        Camera cam = Camera.main;
        var distance = (cam.transform.position - transform.position).magnitude;
        _height = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance * 2f;
        _width = _height * cam.aspect;
        var min = Mathf.Min(_height, _width);
        transform.position = cam.transform.position + cam.transform.forward * distance;

        transform.localScale = new Vector3(min, min, min);

        _buttons = new List<QuickBarButton>();
        var quickBarObjects = ObjectPlacer.Instance.QuickBarObjects;

        var leftOffset = -QuickBarSpacing * (quickBarObjects.Count - 1) / 2;

        for (int i = 0; i < quickBarObjects.Count; i++)
        {
            var offset = leftOffset + i * QuickBarSpacing;
            var ghost = quickBarObjects[i];
            var newButton = Instantiate(QuickBarButton, QuickBarCenter.transform.position + new Vector3(offset, 0, 0), QuickBarCenter.transform.rotation);
            newButton.transform.parent = this.transform;
            newButton.Ghost = ghost;
            CreateDummy(ghost, newButton);

            _buttons.Add(newButton);
        }
    }

    private static void CreateDummy(PlaceableGhost ghost, QuickBarButton newButton)
    {
        var dummy = Instantiate(ghost.RealObject, newButton.transform.position, newButton.transform.rotation);
        dummy.localScale = dummy.localScale.normalized * newButton.transform.localScale.magnitude;
        dummy.parent = newButton.transform;

        var componentsToDestroy = dummy.GetComponentsInChildren<Rigidbody>().Cast<Component>().ToList();
        componentsToDestroy.AddRange(dummy.GetComponentsInChildren<Collider>());

        foreach (var component in componentsToDestroy)
        {
            Destroy(component);
        }

        var renderers = dummy.GetComponentsInChildren<MeshRenderer>();

        foreach (var renderer in renderers)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
