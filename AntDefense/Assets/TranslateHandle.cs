using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    /// <summary>
    /// The point on this object that was hit with the mouse down
    /// </summary>
    private Vector3? _localHit = null;
    private bool _rotateMode;

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
                //Debug.Log(hit.transform.name);
                //Debug.Log($"hit {hit.transform.name} @ {hit.point}");
                var translateHandle = hit.transform.GetComponentInParent<TranslateHandle>();
                if (translateHandle == this)
                {
                    var rotateHandle = hit.transform.GetComponentInParent<RotateHandle>();
                    if (rotateHandle != null)
                    {
                        Debug.Log("Clicked on rotate handle!");
                        _localHit = transform.InverseTransformPoint(hit.point);
                        _rotateMode = true;
                    }
                    else
                    {
                        Debug.Log("Clicked on translate handle!");
                        _localHit = transform.InverseTransformPoint(hit.point);
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
                Debug.Log((_rotateMode ? "Rotating " : "Translating ") + hit.point);
                if (_rotateMode)
                {
                    transform.rotation.SetLookRotation(hit.point);
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
