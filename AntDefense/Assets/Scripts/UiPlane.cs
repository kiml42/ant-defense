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
        Debug.Assert(Instance == null || Instance == this, "There should not be multiple UI planes!");
        Instance = this;
        this.InitialiseQuickBar();
    }

    private void Update()
    {
        if (ProtectMes.Any(p => p.UiObject == null))
        {
            this.InitialiseProtectMes();
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
        var leftOffset = -this.ProtectMesSpacing * (ProtectMes.Count - 1) / 2;

        // foreach with index

        var i = 0;
        foreach (var p in ProtectMes)
        {
            var offset = leftOffset + (i * this.ProtectMesSpacing);
            if (p.UiObject == null)
            {
                p.UiObject = Instantiate(p.ProtectMe.transform, this.ProtectMesCenter.position + new Vector3(offset, 0, 0), Quaternion.Euler(this.ProtectMeRotation));
                p.UiObject.parent = this.transform;
                p.UiObject.localScale *= this.ProtectMeScale;
                Dummyise(p.UiObject);
            }
            i++;
        }
    }

    private void InitialiseQuickBar()
    {
        if (this._buttons != null) return;

        Camera cam = Camera.main;
        var distance = (cam.transform.position - this.transform.position).magnitude;
        this._height = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance * 2f;
        this._width = this._height * cam.aspect;
        var min = Mathf.Min(this._height, this._width);
        this.transform.position = cam.transform.position + (cam.transform.forward * distance);

        this.transform.localScale = new Vector3(min, min, min);

        this._buttons = new List<QuickBarButton>();
        var quickBarObjects = ObjectPlacer.Instance.QuickBarObjects;

        var leftOffset = -this.QuickBarSpacing * (quickBarObjects.Count - 1) / 2;

        for (int i = 0; i < quickBarObjects.Count; i++)
        {
            var offset = leftOffset + (i * this.QuickBarSpacing);
            var ghost = quickBarObjects[i];
            var newButton = Instantiate(this.QuickBarButton, this.QuickBarCenter.transform.position + new Vector3(offset, 0, 0), this.QuickBarCenter.transform.rotation);
            newButton.transform.parent = this.transform;
            newButton.Ghost = ghost;

            newButton.CostText.text = $"£{ghost.BaseCost:F2}";
            CreateDummy(ghost, newButton);

            this._buttons.Add(newButton);
        }
    }

    private static void CreateDummy(PlaceableObjectOrGhost ghost, QuickBarButton newButton)
    {
        var objectToUse = ghost.ActualIcon;
        var dummy = Instantiate(objectToUse, newButton.transform.position + ghost.OffsetForButton, newButton.transform.rotation * ghost.RotationForButton);
        dummy.localScale = dummy.localScale.normalized * newButton.transform.localScale.magnitude * ghost.ScaleForButton;
        dummy.parent = newButton.transform;

        Dummyise(dummy);
    }

    private static void Dummyise(Transform dummy)
    {
        var componentsToDestroy = dummy.GetComponentsInChildren<MonoBehaviour>().Cast<UnityEngine.Object>().ToList();
        componentsToDestroy.AddRange(dummy.GetComponentsInChildren<HingeJoint>());
        componentsToDestroy.AddRange(dummy.GetComponentsInChildren<Rigidbody>());
        componentsToDestroy.AddRange(dummy.GetComponentsInChildren<Collider>());
        componentsToDestroy.AddRange(dummy.GetComponentsInChildren<TurretTrigger>().Select(t => t.gameObject));

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
            this.ProtectMe = protectMe;
        }
    }
}
