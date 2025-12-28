using UnityEngine;

public class CameraFacer : MonoBehaviour
{
    void LateUpdate()
    {
        this.transform.LookAt(Camera.main.transform.position, Camera.main.transform.up);
    }
}
