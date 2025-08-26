using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UiPlane : MonoBehaviour
{
    public QuickBarButton QuickBarButton;
    public Transform QuickBarCenter;
    public float QuickBarSpacing = 0.1f;
    public float ProtectMesSpacing = 0.1f;
    private List<QuickBarButton> _buttons = null;

    public Transform ProtectMesCenter;
    public float ProtectMeScale = 0.1f;
    public Vector3 ProtectMeRotation;

    float _height;
    float _width;

    private static readonly List<ProtectMeBarObject> ProtectMes = new List<ProtectMeBarObject>();

    public static UiPlane Instance { get; private set; }

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            throw new Exception("There should not be multiple UI planes!");
        }
        Instance = this;
        InitialiseQuickBar();
    }

    private void Update()
    {
        if (ProtectMes.Any(p => p.UiObject == null))
        {
            InitialiseProtectMes();
        }

        foreach (var p in ProtectMes.Where(p => p.ProtectMe == null && p.UiObject != null).ToArray())
        {
            Destroy(p.UiObject.gameObject);
            ProtectMes.Remove(p);
        }
        if(ProtectMes.Count == 0)
        { 
            Debug.Log("All protectMes are gone!");

            Debug.Log("GAME OVER");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    private void InitialiseProtectMes()
    {
        // TODO : calculate spacing based on available space.
        // TODO : improve positioning & rotation of the objects
        var leftOffset = -ProtectMesSpacing * (ProtectMes.Count - 1) / 2;

        // foreach with index

        var i = 0;
        foreach (var p in ProtectMes)
        {
            var offset = leftOffset + i * ProtectMesSpacing;
            if (p.UiObject == null)
            {
                p.UiObject = Instantiate(p.ProtectMe.transform, ProtectMesCenter.position + new Vector3(offset, 0, 0), Quaternion.Euler(this.ProtectMeRotation));
                p.UiObject.parent = this.transform;
                p.UiObject.localScale *= this.ProtectMeScale;
                Dummyise(p.UiObject);
            }
            i++;
        }
    }

    private void InitialiseQuickBar()
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
        var dummy = Instantiate(ghost.RealObject, newButton.transform.position + ghost.OffsetForButton, newButton.transform.rotation * ghost.RotationForButton);
        dummy.localScale = dummy.localScale.normalized * newButton.transform.localScale.magnitude * ghost.ScaleForButton;
        dummy.parent = newButton.transform;

        Dummyise(dummy);
    }

    private static void Dummyise(Transform dummy)
    {
        var componentsToDestroy = dummy.GetComponentsInChildren<MonoBehaviour>().Cast<Component>().ToList();
        componentsToDestroy.AddRange(dummy.GetComponentsInChildren<HingeJoint>().Cast<Component>());
        componentsToDestroy.AddRange(dummy.GetComponentsInChildren<Rigidbody>().Cast<Component>());
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

    internal static void RegisterProtectMe(ProtectMe protectMe)
    {
        if (!ProtectMes.Any(p => p.ProtectMe == protectMe))
        {
            ProtectMes.Add(new ProtectMeBarObject(protectMe));
        }
    }

    private class ProtectMeBarObject
    {
        public Transform UiObject;
        public ProtectMe ProtectMe;
        public ProtectMeBarObject(ProtectMe protectMe)
        {
            ProtectMe = protectMe;
        }
    }
}
