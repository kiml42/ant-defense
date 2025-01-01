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
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseDown();
        }

        else if(Input.GetMouseButtonUp(0))
        {
            HandleMouseUp();
        }

        if (_localHit.HasValue)
        {
            HandleDrag();
        }

        MoveOnTop();
        ScaleForDistanceToCamera();
    }

    public Transform UiObjectsToScale;
    public float DefaultCameraDistance = 30f;

    private void ScaleForDistanceToCamera()
    {
        var distance = Mathf.Abs((Camera.main.transform.position - this.transform.position).y);

        var excessDistance = distance - DefaultCameraDistance;

        var scale = ((excessDistance / DefaultCameraDistance)/1.3f) + 1;

        UiObjectsToScale.localScale = Vector3.one * scale;
    }

    private void MoveOnTop()
    {
        var lookDownOffset = Vector3.up * 5;
        Ray ray = new Ray(transform.position + lookDownOffset, -lookDownOffset);
        if (Physics.Raycast(ray, out var hit, lookDownOffset.magnitude * 2, -1, QueryTriggerInteraction.Ignore))
        {
            if(hit.rigidbody == null || hit.rigidbody.IsSleeping())
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
                var angle = Vector3.SignedAngle(_localHit.Value, Vector3.forward, Vector3.up);

                var offsetRotation = Quaternion.AngleAxis(angle, Vector3.up);
                var lookRotation = Quaternion.LookRotation(vectorToHit, Vector3.up);

                transform.rotation = AdjustYUp(lookRotation * offsetRotation);
            }
            else
            {
                var rotatedAgle = transform.rotation * _localHit.Value;
                transform.position = hit.point - new Vector3(rotatedAgle.x, 0, rotatedAgle.z);
            }
        }
    }

    private void HandleMouseUp()
    {
        _localHit = null;
        _rotateMode = false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500, _layerMask, QueryTriggerInteraction.Collide))
        {
            var translateHandle = hit.transform.GetComponentInParent<TranslateHandle>();
            if (translateHandle == this)
            {
                // It's part of this object in some way.
                var button = hit.transform.GetComponentInParent<ClickableButton>();
                if (button != null)
                {
                    if (button == this.TickButton)
                    {
                        ObjectPlacer.Instance.PlaceObject();
                        return;
                    }
                    else if (button == this.CrossButton)
                    {
                        ObjectPlacer.Instance.CancelPlacingObject();
                        return;
                    }
                    return;
                }
            }

            var quickBarButton = hit.transform.GetComponentInParent<QuickBarButton>();
            if (quickBarButton != null)
            {
                Debug.Log("Click on quick bar button " + quickBarButton);
                ObjectPlacer.Instance.StartPlacingGhost(quickBarButton.Ghost);
                return;
            }
        }
    }

    private void HandleMouseDown()
    {
        // TODO propogate ray through to the ground to get the actual ground point the mouse is pointing at regardless of the height of the handle.
        // This would let us have any collider geometry and it'll work pretty well.
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 500, _layerMask, QueryTriggerInteraction.Collide);

        var hasHitThisHandle = hits.Any(h => h.transform.GetComponentInParent<TranslateHandle>() == this);
        var hasHitAButton = hits.Any(h => h.transform.GetComponentInParent<ClickableButton>() != null);
        if (!hasHitThisHandle || hasHitAButton)
        {
            _localHit = null;
            return;
        }

        var bestHit = hits.First();

        _rotateMode = hits.Any(h => h.transform.GetComponentInParent<RotateHandle>());
        if (_rotateMode)
        {
            bestHit = hits.First(h => h.transform.GetComponentInParent<RotateHandle>());
        }
        else if(hits.Any(h => h.transform.GetComponentInParent<TranslateHandleCollider>()))
        {
            bestHit = hits.First(h => h.transform.GetComponentInParent<TranslateHandleCollider>());
        }
        _localHit = transform.InverseTransformPoint(bestHit.point);
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
