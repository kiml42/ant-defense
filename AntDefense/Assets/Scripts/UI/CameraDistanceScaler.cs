using UnityEngine;

public class CameraDistanceScaler : MonoBehaviour
{
    /// <summary>
    /// If true, use the actual distance to the camera for scaling calculations.
    /// If false, use only the camera's Y position.
    /// </summary>
    public bool UseActualDistance = false;
    public float ScaleWithDistanceFactor = 1f;
    public float MinScale = 0.1f;
    public float MaxScale = 10f;
    public float DefaultCameraDistance = 30f;

    void LateUpdate()
    {
        if(ScaleWithDistanceFactor != 0f)
        {
            scale = MathfGetDistanceToCameraScaleFactor;
            this.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    private float GetDistanceToCameraScaleFactor()
    {
        var distance = this.UseActualDistance
            ? Vector3.Distance(this.transform.position, Camera.main.transform.position)
            : Camera.main.transform.position.y;

        var excessDistance = distance - this.DefaultCameraDistance;

        var scale = (excessDistance / this.DefaultCameraDistance / 1.5f) + 1;
        return Mathf.Clamp(scale, this.MinScale, this.MaxScale);
    }
}
