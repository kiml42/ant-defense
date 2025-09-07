using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    public int PlaceMouseButton = 0;
    public int CancelMouseButton = 1;
    public float MinRotateMouseDistance = 1f;
    private int _layerMask;

    private void Start()
    {
        _layerMask = LayerMask.GetMask("UI");
    }

    private Vector3? _lastMousePosition;
    private float _distanceSinceClick;
    public float CancelThreshold = 0.1f;
    // Update is called once per frame
    void Update()
    {
        this.HandleCancelButton();

        if (Input.GetMouseButtonUp(this.PlaceMouseButton))
        {
            HandleMainMouseUp();
        }

        HandleDrag();
        MoveOnTop();

        ScaleForDistanceToCamera();

        if (ObjectPlacer.Instance.CanRotateCurrentObject() == false)
        {
            this.transform.rotation = Quaternion.identity;
        }
    }

    private void HandleCancelButton()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            ObjectPlacer.Instance.CancelPlacingObject();
            _distanceSinceClick = 0;
            _lastMousePosition = null;
            return;
        }
        if (Input.GetMouseButtonDown(this.CancelMouseButton))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500, -1, QueryTriggerInteraction.Ignore))
            {
                _lastMousePosition = hit.point;
            }
        }
        if (Input.GetMouseButtonUp(this.CancelMouseButton))
        {
            if (_distanceSinceClick < CancelThreshold)
            {
                ObjectPlacer.Instance.CancelPlacingObject();
            }
            _distanceSinceClick = 0;
            _lastMousePosition = null;
        }
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
            if (IsStaticObject(hit))
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
            if (Input.GetMouseButton(this.PlaceMouseButton) && (ObjectPlacer.Instance.CanRotateCurrentObject() == true))
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

            if (_lastMousePosition.HasValue)
            {
                _distanceSinceClick += Vector3.Distance(_lastMousePosition.Value, hit.point);
                _lastMousePosition = hit.point;
            }
        }
    }

    private void HandleMainMouseUp()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500, _layerMask, QueryTriggerInteraction.Collide))
        {
            var quickBarButton = hit.transform.GetComponentInParent<QuickBarButton>();
            if (quickBarButton != null)
            {
                ObjectPlacer.Instance.StartPlacingGhost(quickBarButton.Ghost);
                return;
            }
        }

        ObjectPlacer.Instance.PlaceObject();

        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            return;
        }

        ObjectPlacer.Instance.CancelPlacingObject();
    }

    private bool IsStaticObject(RaycastHit hit)
    {
        if (hit.collider.isTrigger)
        {
            return false;
        }
        if (hit.rigidbody != null)
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
