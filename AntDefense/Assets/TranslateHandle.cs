using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    /// <summary>
    /// The point on this object that was hit with the mouse down
    /// </summary>
    private Vector3? _localHit = null;
    private bool _rotateMode;
    public Transform Indicator;
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
                _localHit = transform.InverseTransformPoint(hit.point);
                var translateHandle = hit.transform.GetComponentInParent<TranslateHandle>();
                if (translateHandle == this)
                {
                    // It's part of this object in some way.
                    var tick = hit.transform.GetComponentInParent<TickButton>();
                    if (tick != null)
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
                    var tick = hit.transform.GetComponentInParent<TickButton>();
                    if (tick != null)
                    {
                        Debug.Log("Mouse up on tick");
                        return;
                    }
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
                    var lookRotation = Quaternion.LookRotation(vectorToHit);

                    transform.rotation = lookRotation * offsetRotation;
                }
                else
                {
                    var rotatedAgle = transform.rotation * _localHit.Value;
                    transform.position = hit.point - new Vector3(rotatedAgle.x, 0, rotatedAgle.z);
                }
            }
        }
    }
}
