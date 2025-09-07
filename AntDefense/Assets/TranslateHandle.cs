using System;
using System.Linq;
using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    public ClickableButton TickButton;
    public ClickableButton CrossButton;

    /// <summary>
    /// The point on this object that was hit with the mouse down
    /// </summary>
    private Vector3? _localHit = null;
    private bool _rotateMode;
    private int _layerMask;

    private void Start()
    {
        _layerMask = LayerMask.GetMask("UI");
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
        //    HandleMouseDown();
        //}

        if(Input.GetMouseButtonUp(0))
        {
            HandleMouseUp();
        }

        HandleDrag();
        MoveOnTop();

        ScaleForDistanceToCamera();
    }

    public Transform UiObjectsToScale;
    public float DefaultCameraDistance = 30f;

    private void ScaleForDistanceToCamera()
    {
        var distance = Camera.main.transform.position.y;

        var excessDistance = distance - DefaultCameraDistance;

        var scale = ((excessDistance / DefaultCameraDistance)/1.5f) + 1;

        UiObjectsToScale.localScale = Vector3.one * scale;
    }

    private void MoveOnTop()
    {
        var lookDownOffset = Vector3.up * 5;
        Ray ray = new Ray(transform.position + lookDownOffset, -lookDownOffset);
        if (Physics.Raycast(ray, out var hit, lookDownOffset.magnitude * 2, -1, QueryTriggerInteraction.Ignore))
        {
            if(IsStaticObject(hit))
            {
                transform.position = hit.point;
            }
        }
    }

    private void HandleDrag()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500, -1, QueryTriggerInteraction.Ignore))
        {
            if (_rotateMode)
            {
                var vectorToHit = hit.point - transform.position;

                var lookRotation = Quaternion.LookRotation(vectorToHit, Vector3.up);

                transform.rotation = AdjustYUp(lookRotation);
            }
            else
            {
                var rotatedAgle = transform.rotation * _localHit ?? Vector3.zero;
                transform.position = hit.point - new Vector3(rotatedAgle.x, 0, rotatedAgle.z);
            }
        }
    }

    private void HandleMouseUp()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500, _layerMask, QueryTriggerInteraction.Collide))
        {
            var quickBarButton = hit.transform.GetComponentInParent<QuickBarButton>();
            if (quickBarButton != null)
            {
                Debug.Log("Click on quick bar button " + quickBarButton);
                ObjectPlacer.Instance.StartPlacingGhost(quickBarButton.Ghost);
                return;
            }
        }

        if(!_rotateMode && ObjectPlacer.Instance.CanRotateCurrentObject())
        {
            Console.WriteLine("Entering rotate mode");
            _rotateMode = true;
            return;
        }

        Console.WriteLine("Placing object");
        ObjectPlacer.Instance.PlaceObject();
        _rotateMode = false;

        ObjectPlacer.Instance.CancelPlacingObject();
    }

    private bool IsStaticObject(RaycastHit hit)
    {
        if (hit.collider.isTrigger)
        {
            return false;
        }
        if(hit.rigidbody != null)
        {
            return hit.rigidbody.IsSleeping();
        }
        return true;
    }

    private Quaternion AdjustYUp(Quaternion originalRotation)
    {
        // Thanks Chat GPT.
        // Extract the forward direction from the original rotation
        Vector3 forward = originalRotation * Vector3.forward;

        // Calculate the right direction based on the forward direction
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // Recalculate the forward direction to ensure orthogonality
        forward = Vector3.Cross(right, Vector3.up).normalized;

        // Construct a new rotation with Y pointing up and the adjusted forward direction
        return Quaternion.LookRotation(forward, Vector3.up);
    }
}
