using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class TranslateHandle : MonoBehaviour
{
    public int PlaceMouseButton = 0;
    public int CancelMouseButton = 1;
    public float MinRotateMouseDistance = 1f;
    private int _uiLayermask;
    private int _groundLayermask;
    private bool lastPositionIsGood = false;

    // TODO: handle this with a visually disaleable script on every rendered object.
    private IEnumerable<Material> _materials;
    public Color DisabledColour;
    private Color _originalColour;

    private void Start()
    {
        _uiLayermask = LayerMask.GetMask("UI");
        _groundLayermask = LayerMask.GetMask("Ground");
        _materials = this.GetComponentsInChildren<Renderer>().SelectMany(r => r.materials);
        _originalColour = _materials.First().color;
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

        HandleMousePosition();

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

    private void HandleMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        var previousPosition = transform.position;
        if (Physics.Raycast(ray, out var hit, 500, _groundLayermask, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("Pointing at: " + hit.transform + " @ " + hit.point);
            if (Input.GetMouseButton(this.PlaceMouseButton) && (ObjectPlacer.Instance.CanRotateCurrentObject() == true))
            {
                // mous button is down, and the object is rotatable, so rotate it to face the mouse.
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
                // not in rotate mode, so just move to the hit point.
                transform.position = hit.point;

                // I don't remember what this code was for. Delte it if it's not needed.
                //var rotatedAgle = transform.rotation;
                //transform.position = hit.point - new Vector3(rotatedAgle.x, 0, rotatedAgle.z);
            }

            if (_lastMousePosition.HasValue)
            {
                // tracked to support cancelling the placement
                // TODO: check why.
                _distanceSinceClick += Vector3.Distance(_lastMousePosition.Value, hit.point);
                _lastMousePosition = hit.point;
            }
        }

        var changedPosition = NoSpawnZone.GetBestEdgePosition(transform.position, previousPosition);
        if (changedPosition.HasValue)
        {
            //Debug.Log("Snapping to edge of no spawn zone @ " + changedPosition);
            this.transform.position = changedPosition.Value;
        }
        var isGood = !NoSpawnZone.IsInAnyNoSpawnZone(transform.position);
        if(isGood != lastPositionIsGood)
        {
            // Position state changed.
            lastPositionIsGood = isGood;
            foreach (var material in _materials)
            {
                material.color = isGood
                    ? _originalColour
                    : DisabledColour;
            }
        }
    }

    private void HandleMainMouseUp()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500, _uiLayermask, QueryTriggerInteraction.Collide))
        {
            var quickBarButton = hit.transform.GetComponentInParent<QuickBarButton>();
            if (quickBarButton != null)
            {
                ObjectPlacer.Instance.StartPlacingGhost(quickBarButton.Ghost);
                return;
            }
        }

        if(NoSpawnZone.IsInAnyNoSpawnZone(transform.position))
        {
            // Can't place here.
            return;
        }

        ObjectPlacer.Instance.PlaceObject(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
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
