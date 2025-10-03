using UnityEngine;

public class CameraFacer : MonoBehaviour
{
    void LateUpdate()
    {
        transform.LookAt(Camera.main.transform.position, Camera.main.transform.up);
    }
}
