using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    /// <summary>
    /// The point on this object that was hit with the mouse down
    /// </summary>
    private Vector3? _localHit = null;
    private bool _rotateMode;
    public Transform Indicator;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int layerMask = LayerMask.GetMask("UI");
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //TODO get teh layer mask right to just interact with UI(5) elements
            if (Physics.Raycast(ray, out var hit, 500, layerMask, QueryTriggerInteraction.Collide))
            {
                _localHit = transform.InverseTransformPoint(hit.point);
                //Debug.Log(hit.transform.name);
                //Debug.Log($"hit {hit.transform.name} @ {hit.point}");
                //Ray ray2 = new Ray(hit.point, Vector3.down);
                //if (Physics.Raycast(ray2, out var hit2, 5, -1, QueryTriggerInteraction.Ignore))
                //{
                //    // Cast a ray down to the floor.
                //    Debug.Log("Ray 2 hit " + hit.transform);
                //    _localHit = hit2.point;
                //}
                var translateHandle = hit.transform.GetComponentInParent<TranslateHandle>();
                if (translateHandle == this)
                {
                    var rotateHandle = hit.transform.GetComponentInParent<RotateHandle>();
                    if (rotateHandle != null)
                    {
                        Debug.Log("Clicked on rotate handle!");
                        _rotateMode = true;
                    }
                    else
                    {
                        Debug.Log("Clicked on translate handle!");
                        _rotateMode = false;
                    }
                }
            }
            else
            {
                //Debug.Log("Miss");
            }
        }

        if(Input.GetMouseButtonUp(0))
        {
            //Debug.Log("Mouse up");
            _localHit = null;
            _rotateMode = false;
        }

        if (_localHit.HasValue)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500, -1, QueryTriggerInteraction.Ignore))
            {
                Indicator.position = hit.point;
                Debug.Log((_rotateMode ? "Rotating " : "Translating ") + hit.point);
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

                    transform.position = hit.point - new Vector3(_localHit.Value.x, 0, _localHit.Value.z);
                }
            }
            else
            {
                Debug.Log("Miss");
            }
        }
    }

    void OnMouseDrag()
    {
        Debug.Log("Drag");
    }
}
