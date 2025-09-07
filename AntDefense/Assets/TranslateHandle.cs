using System;
using UnityEngine;

// TODO add a way to cancel
// TODO rotate while holding mouse down
public class TranslateHandle : MonoBehaviour
{
    public int MouseButton = 0;
    public float MinRotateMouseDistance = 0.1f;
    private int _layerMask;

    private void Start()
    {
        _layerMask = LayerMask.GetMask("UI");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(this.MouseButton))
        {

        }
        if(Input.GetMouseButtonUp(this.MouseButton))
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
        var scale = this.GetDistanceToCameraScaleFactor();

        UiObjectsToScale.localScale = Vector3.one * scale;
    }

    private float GetDistanceToCameraScaleFactor()
    {
        var distance = Camera.main.transform.position.y;

        var excessDistance = distance - DefaultCameraDistance;

        var scale = ((excessDistance / DefaultCameraDistance) / 1.5f) + 1;
        return scale;
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
            if (Input.GetMouseButton(this.MouseButton) && ObjectPlacer.Instance.CanRotateCurrentObject())
            {
                var vectorToHit = hit.point - transform.position;

                if (vectorToHit.magnitude < MinRotateMouseDistance * GetDistanceToCameraScaleFactor())
                {
                    return;
                }

                var lookRotation = Quaternion.LookRotation(vectorToHit, Vector3.up);

                transform.rotation = AdjustYUp(lookRotation);
            }
            else
            {
                var rotatedAgle = transform.rotation;
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

        //if(!_rotateMode && ObjectPlacer.Instance.CanRotateCurrentObject())
        //{
        //    Console.WriteLine("Entering rotate mode");
        //    //_rotateMode = true;
        //    return;
        //}

        Console.WriteLine("Placing object");
        ObjectPlacer.Instance.PlaceObject();

        //_rotateMode = false;
        if(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            Console.WriteLine("Starting new object");
            return;
        }
        this.transform.rotation = Quaternion.identity;

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
