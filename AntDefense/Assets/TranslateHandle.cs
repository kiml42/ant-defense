using UnityEngine;

public class TranslateHandle : MonoBehaviour
{
    /// <summary>
    /// The point on this object that was hit with the mouse down
    /// </summary>
    private Vector3? _localHit = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //TODO get teh layer mask right to just interact with UI(5) elements
            if (Physics.Raycast(ray, out var hit, 500, -1, QueryTriggerInteraction.Collide))
            {
                //Debug.Log(hit.transform.name);
                //Debug.Log($"hit {hit.transform.name} @ {hit.point}");
                var handle = hit.transform.GetComponentInParent<TranslateHandle>();
                if (handle == this)
                {
                    Debug.Log("Clicked on this handle!");
                    _localHit = transform.InverseTransformPoint(hit.point);
                }
                //_spawnLocation = hit.point;
            }
            else
            {
                Debug.Log("Miss");
            }
        }

        if(Input.GetMouseButtonUp(0))
        {
            Debug.Log("Mouse up");
            _localHit = null;
        }

        if (_localHit.HasValue)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500, -1, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("Mouse is down, pointing at " + hit.point);
                transform.position = hit.point - new Vector3(_localHit.Value.x, 0, _localHit.Value.z);
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
