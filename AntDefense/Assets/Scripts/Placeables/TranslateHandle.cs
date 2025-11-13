using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    public static TranslateHandle Instance { get; private set; }

    public int PlaceMouseButton = 0;
    public int CancelMouseButton = 1;
    public float MinRotateMouseDistance = 1f;
    private int _uiLayermask;
    private int _groundLayermask;
    private bool _lastPositionIsGood = false;
    private NoSpawnZone.AdjustedPoint _lastActivateablePoint;

    // TODO: handle this with a visually disaleable script on every rendered object.
    private IEnumerable<Material> _materials;
    public Color DisabledColour;
    private Color _originalColour;

    public TextMeshPro CostText;

    public Transform SelectedObjectHighlight;
    private Transform _slelectedObjectHighlightInstance;

    private void Start()
    {
        Debug.Assert(Instance == null || Instance == this, "Multiple TranslateHandle instances detected!");
        Instance = this;
        this._uiLayermask = LayerMask.GetMask("UI");
        this._groundLayermask = LayerMask.GetMask("Ground");
        this._materials = this.GetComponentsInChildren<Renderer>().SelectMany(r => r.materials);
        this._originalColour = this._materials.First().color;
    }

    private Vector3? _lastMousePosition;
    private float _distanceSinceClick;
    public float CancelThreshold = 0.1f;
    // Update is called once per frame
    void Update()
    {
        this.HandleCancelButton();

        if (Input.GetMouseButtonDown(this.PlaceMouseButton))
        {
            this.TryActivateQuickBarButton();
        }

        if (Input.GetMouseButtonUp(this.PlaceMouseButton))
        {
            this.DeselectObject();
            this.ActivatePoint();
        }

        this.HandleMousePosition();

        this.ScaleForDistanceToCamera();

        if (ObjectPlacer.Instance.CanRotateCurrentObject() == false)
        {
            this.transform.rotation = Quaternion.identity;
        }

        if(this.CostText != null)
        {
            var cost = ObjectPlacer.Instance.CostForCurrentObject;
            this.CostText.gameObject.SetActive(cost.HasValue);
            if (cost.HasValue)
            {
                this.CostText.text = $"£{cost:F2}";
            }
        }
    }

    private void HandleCancelButton()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            this.DeselectObject();
            ObjectPlacer.Instance.CancelPlacingObject();
            this._distanceSinceClick = 0;
            this._lastMousePosition = null;
            return;
        }
        if (Input.GetMouseButtonDown(this.CancelMouseButton))
        {
            if (this.RaycastToFloor(out var hit))
            {
                this._lastMousePosition = hit.point;
            }
        }
        if (Input.GetMouseButtonUp(this.CancelMouseButton))
        {
            //Debug.Log("Cancel mouse up after moving " + _distanceSinceClick);
            if (this._distanceSinceClick < this.CancelThreshold)
            {
                this.DeselectObject();
                ObjectPlacer.Instance.CancelPlacingObject();
            }
            this._distanceSinceClick = 0;
            this._lastMousePosition = null;
        }
    }

    public Transform UiObjectsToScale;
    public float DefaultCameraDistance = 30f;

    private void ScaleForDistanceToCamera()
    {
        var scale = this.GetDistanceToCameraScaleFactor();

        this.UiObjectsToScale.localScale = Vector3.one * scale;
    }

    private float GetDistanceToCameraScaleFactor()
    {
        var distance = Camera.main.transform.position.y;

        var excessDistance = distance - this.DefaultCameraDistance;

        var scale = (excessDistance / this.DefaultCameraDistance / 1.5f) + 1;
        return scale;
    }

    private void HandleMousePosition()
    {
        var previousPosition = this.transform.position;
        if (this.RaycastToFloor(out var hit))
        {
            //Debug.Log("Pointing at: " + hit.transform + " @ " + hit.point);
            if (Input.GetMouseButton(this.PlaceMouseButton) && (ObjectPlacer.Instance.CanRotateCurrentObject() == true))
            {
                // mous button is down, and the object is rotatable, so rotate it to face the mouse.
                var vectorToHit = hit.point - this.transform.position;

                if (vectorToHit.magnitude < this.MinRotateMouseDistance * this.GetDistanceToCameraScaleFactor())
                {
                    return;
                }

                var lookRotation = Quaternion.LookRotation(vectorToHit, Vector3.up);

                this.transform.rotation = this.AdjustYUp(lookRotation);
            }
            else
            {
                // not in rotate mode, so just move to the hit point.
                this.transform.position = hit.point;

                // I don't remember what this code was for. Delte it if it's not needed.
                //var rotatedAgle = transform.rotation;
                //transform.position = hit.point - new Vector3(rotatedAgle.x, 0, rotatedAgle.z);
            }

            if (this._lastMousePosition.HasValue)
            {
                // tracked to support cancelling the placement
                // TODO: check why.
                this._distanceSinceClick += Vector3.Distance(this._lastMousePosition.Value, hit.point);
                this._lastMousePosition = hit.point;
            }
        }

        bool isGood = true;
        var changedPosition = NoSpawnZone.GetBestEdgePosition(this.transform.position, previousPosition);
        this._lastActivateablePoint = changedPosition;
        switch (changedPosition.Type)
        {
            case NoSpawnZone.PointType.Original:
                //Debug.Log($"Original is fine {changedPosition.Point}");
                // no adjustment needed
                break;
            case NoSpawnZone.PointType.Corrected:
                //Debug.Log($"Corrected position {changedPosition.Point}");
                this.transform.position = changedPosition.Point;
                break;
            case NoSpawnZone.PointType.InteractionPoint:
                //Debug.Log($"InteractionPoint position {changedPosition.Point}");
                this.transform.position = changedPosition.Point;
                // TODO remember that this is an interactive point.
                break;
            case NoSpawnZone.PointType.Invalid:
                //Debug.Log($"Invalid position {changedPosition.Point}");
                isGood = false;
                break;
        }

        isGood &= ObjectPlacer.Instance == null || ObjectPlacer.Instance.CanPlaceAt(this.transform.position);
        if (isGood != this._lastPositionIsGood)
        {
            // Position state changed.
            this._lastPositionIsGood = isGood;
            foreach (var material in this._materials)
            {
                material.color = isGood
                    ? this._originalColour
                    : this.DisabledColour;
            }
        }
    }

    private bool RaycastToFloor(out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        return Physics.Raycast(ray, out hit, 500, this._groundLayermask, QueryTriggerInteraction.Ignore);
    }

    private void ActivatePoint()
    {
        if (this._lastActivateablePoint == null) return;

        //Debug.Log("Activating " +  this._lastCorrectedPoint);
        this._lastActivateablePoint.Activate();
    }

    private bool TryActivateQuickBarButton()
    {
        var activated = false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500, this._uiLayermask, QueryTriggerInteraction.Collide))
        {
            var quickBarButton = hit.transform.GetComponentInParent<QuickBarButton>();
            if (quickBarButton != null)
            {
                ObjectPlacer.Instance.StartPlacingGhost(quickBarButton.Ghost);
                activated = true;
            }
        }

        return activated;
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

    private ISelectableObject _selectedObject;
    internal void SetSelectedObject(ISelectableObject activeObject)
    {
        this.DeselectObject();
        this._selectedObject = activeObject;
        this._selectedObject?.Select();
        if (this.SelectedObjectHighlight != null)
        {
            Debug.Log("Creating selected object highlight instance");
            this._slelectedObjectHighlightInstance = Instantiate(this.SelectedObjectHighlight, this._selectedObject.Position, Quaternion.identity);
        }
    }

    private void DeselectObject()
    {
        if (this._slelectedObjectHighlightInstance != null)
        {
            Destroy(this._slelectedObjectHighlightInstance.gameObject);
        }
        this._selectedObject?.Deselect();   // deselect any selected object when clicking anywhere.
        this._selectedObject = null;
    }
}
