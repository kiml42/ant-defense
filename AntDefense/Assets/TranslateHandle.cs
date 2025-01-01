using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    public Transform Indicator;
    public ClickableButton TickButton;
    public ClickableButton CrossButton;
    public ObjectPlacer Placer;

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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500, _layerMask, QueryTriggerInteraction.Collide))
            {
                var translateHandle = hit.transform.GetComponentInParent<TranslateHandle>();
                if (translateHandle == this)
                {
                    _localHit = transform.InverseTransformPoint(hit.point);

                    // It's part of this object in some way.
                    var button = hit.transform.GetComponentInParent<ClickableButton>();
                    if (button != null)
                    {
                        _localHit = null;
                    }
                    else
                    {
                        var rotateHandle = hit.transform.GetComponentInParent<RotateHandle>();
                        if (rotateHandle != null)
                        {
                            _rotateMode = true;
                        }
                        else
                        {
                            _rotateMode = false;
                        }
                    }
                }
            }
        }

        if(Input.GetMouseButtonUp(0))
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
                            Placer.PlaceObject();
                            return;
                        }
                        else if (button == this.CrossButton)
                        {
                            Placer.CancelPlacingObject();
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

        if (_localHit.HasValue)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500, -1, QueryTriggerInteraction.Ignore))
            {
                Indicator.position = hit.point;
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
