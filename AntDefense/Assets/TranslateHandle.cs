using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    /// <summary>
    /// The point on this object that was hit with the mouse down
    /// </summary>
    private Vector3? _localHit = null;
    private bool _rotateMode;
    public Transform Indicator;

    // Update is called once per frame
    void Update()
    {
        int layerMask = LayerMask.GetMask("UI");
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500, layerMask, QueryTriggerInteraction.Collide))
            {
                _localHit = transform.InverseTransformPoint(hit.point);
                var translateHandle = hit.transform.GetComponentInParent<TranslateHandle>();
                if (translateHandle == this)
                {
                    // It's part of this object in some way.

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

        if(Input.GetMouseButtonUp(0))
        {
            _localHit = null;
            _rotateMode = false;
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
