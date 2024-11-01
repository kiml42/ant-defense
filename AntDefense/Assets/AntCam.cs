using UnityEngine;

public class AntCam : MonoBehaviour
{
    private const int MouseButton = 1;
    public float CameraZoomSpeed = 150;
    public float CameraPanSpeed = 0.8f;
    public float CameraSidewaysSpeed = 0.5f;
    private Vector3 lastMousePosition;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        float newX = transform.position.x;
        float newY = transform.position.y;
        float newZ = transform.position.z;
        if (Input.mouseScrollDelta.y != 0)
        {
            newY -= Input.mouseScrollDelta.y * Time.deltaTime * CameraZoomSpeed;

            var newRotationX = transform.rotation.x - Input.mouseScrollDelta.y * Time.deltaTime * CameraPanSpeed;
            transform.rotation = new Quaternion((float)newRotationX, transform.rotation.y, transform.rotation.z, transform.rotation.w);
            //Debug.Log($"Rotation {transform.rotation}");
        }

        if (Input.GetMouseButtonDown(MouseButton))
        {
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(MouseButton))
        {
            var change = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            if(change.magnitude > 0)
            {
                //Debug.Log("Mouse Move " + change);
                //transform.position += change;
                newX -= change.x * CameraSidewaysSpeed;
                newZ -= change.y * CameraSidewaysSpeed;
            }
        }
        transform.position = new Vector3(newX, newY, newZ);
    }

}
