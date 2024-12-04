using UnityEngine;

public class AntCam : MonoBehaviour
{
    private const int MouseButton = 2;
    public float CameraZoomSpeed = 150;
    public float CameraPanSpeed = 0.8f;
    public float MinCameraSpeed = 0.1f;
    public float MaxCameraSpeed = 0.5f;
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
            var zoomProportion = GetZoomProportion(CurrentY);
            var speed = GetProportionOfRange(zoomProportion, MinCameraSpeed, MaxCameraSpeed);
            var change = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            if (change.magnitude > 0)
            {
                //Debug.Log("Mouse Move " + change);
                //transform.position += change;
                newX -= change.x * speed;
                newZ -= change.y * speed;
            }
        }
        transform.position = new Vector3(newX, transform.position.y, newZ);
    }

    private float _targetXRotation = 0;
    public float MinY = 10;
    public float MaxY = 200;
    public float MinAngle = -60;
    public float MaxAngleHeight = 100;
    public float MaxAngle = 0;

    private float CurrentY => transform.InverseTransformPoint(Camera.transform.position).y;
    private float GetZoomProportion(float actualNewY)
    {
        return Mathf.Clamp01((actualNewY - MinY) / (MaxAngleHeight - MinY));
    }

    private void HandleZoom()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            // TODO consider make this work on the proportion first, and get both angle and distance from that.
            float currentY = CurrentY;
            var speedMultiplyer = currentY / 100;
            var newY = currentY - Input.mouseScrollDelta.y * CameraZoomSpeed * speedMultiplyer;

            var actualNewY = Mathf.Clamp(newY, MinY, MaxY);
            if (currentY == actualNewY)
            {
                return;
            }

            var zoomProportion = GetZoomProportion(actualNewY);

            var newAngle = GetProportionOfRange(zoomProportion, MinAngle, MaxAngle);

            _targetXRotation = _targetXRotation - Input.mouseScrollDelta.y * CameraPanSpeed * speedMultiplyer;

            transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.right);

            Camera.transform.position = transform.TransformPoint(new Vector3(0, actualNewY, 0));
        }

    }

    private static float GetProportionOfRange(float proportion, float min, float max)
    {
        return min + ((max - min) * proportion);
    }
}
