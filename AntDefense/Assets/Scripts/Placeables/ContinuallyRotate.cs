using UnityEngine;

public class ContinuallyRotate : MonoBehaviour
{
    public float RotationSpeedDegreesPerSecond = 45f;

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(Vector3.up, this.RotationSpeedDegreesPerSecond * Time.deltaTime, Space.World);
    }
}
