using UnityEngine;

public class AntCam : MonoBehaviour
{
    private const int MouseButton = 2;
    public float CameraZoomSpeed = 150;
    public float CameraPanSpeed = 0.8f;
    public float CameraSidewaysSpeed = 0.5f;
    private Vector3 lastMousePosition;

    public Camera Camera;

    private void Start()
    {
        _targetXRotation = transform.rotation.x;
    }

    // Update is called once per frame
    void Update()
    {
        float newX = transform.position.x;
        float newZ = transform.position.z;

        HandleZoom();

        if (Input.GetMouseButtonDown(MouseButton))
        {
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(MouseButton))
        {
            var change = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            if (change.magnitude > 0)
            {
                //Debug.Log("Mouse Move " + change);
                //transform.position += change;
                newX -= change.x * CameraSidewaysSpeed;
                newZ -= change.y * CameraSidewaysSpeed;
            }
        }
        transform.position = new Vector3(newX, transform.position.y, newZ);
    }

    private float _targetXRotation = 0;
    public float MinY = 10;

    private void HandleZoom()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            float currentY = transform.InverseTransformPoint(Camera.transform.position).y;
            var newY = currentY - Input.mouseScrollDelta.y * Time.deltaTime * CameraZoomSpeed;
            
            var actualNewY = Mathf.Max(currentY, MinY);
            if(currentY <= MinY && newY <= MinY)
            {
                // already as low as it can go.
                return;
            }

            _targetXRotation = _targetXRotation - Input.mouseScrollDelta.y * Time.deltaTime * CameraPanSpeed;
            var newRotationX = Mathf.Min(0, _targetXRotation);
            //Debug.Log($"Target: {_targetXRotation}, actual: {newRotationX}");
            transform.rotation = new Quaternion((float)newRotationX, transform.rotation.y, transform.rotation.z, transform.rotation.w);
            //Debug.Log($"Rotation {transform.rotation}");

            Camera.transform.position = transform.TransformPoint(new Vector3(0, actualNewY, 0));
        }

    }
}
