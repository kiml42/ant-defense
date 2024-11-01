using UnityEngine;

public class AntCam : MonoBehaviour
{
    public float CameraMoveSpeed = 100;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            float newY = transform.position.y - Input.mouseScrollDelta.y * Time.deltaTime * CameraMoveSpeed;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            var newRotationX = transform.rotation.x - Input.mouseScrollDelta.y * Time.deltaTime * 0.5f;
            transform.rotation = new Quaternion((float)newRotationX, transform.rotation.y, transform.rotation.z, transform.rotation.w);
            Debug.Log($"Rotation {transform.rotation}");
        }
    }
}
