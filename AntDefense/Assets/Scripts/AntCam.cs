using System.Linq;
using UnityEngine;

public class AntCam : MonoBehaviour
{
    private const int MouseButton = 1;
    public float CameraZoomSpeed = 150;
    public float CameraPanSpeed = 0.8f;
    public float MinCameraSpeed = 10f;
    public float MaxCameraSpeed = 50f;
    private Vector3 _lastMousePosition;

    private KeyCode[] _upKeyCodes = new[] { KeyCode.W, KeyCode.UpArrow };
    private KeyCode[] _leftKeyCodes = new[] { KeyCode.A, KeyCode.LeftArrow };
    private KeyCode[] _downKeyCodes = new[] { KeyCode.S, KeyCode.DownArrow };
    private KeyCode[] _rightKeyCodes = new[] { KeyCode.D, KeyCode.RightArrow };
    private KeyCode[] _directionKeys
    {
        get
        {
            var all = this._upKeyCodes.ToList();
            all.AddRange(this._leftKeyCodes);
            all.AddRange(this._downKeyCodes);
            all.AddRange(this._rightKeyCodes);
            return all.ToArray();
        }
    }

    public Camera Camera;

    public float KeyScrollSpeed = 1.5f;

    public float KeyZoomSpeed = 0.5f;

    private void Start()
    {
        this._targetXRotation = this.transform.rotation.x;
    }

    // Update is called once per frame
    void Update()
    {
        float newX = this.transform.position.x;
        float newZ = this.transform.position.z;

        this.HandleZoom();

        if (Input.GetMouseButtonDown(MouseButton))
        {
            this._lastMousePosition = Input.mousePosition;
        }
        var zoomProportion = this.GetZoomProportion(this.CurrentY);
        var speed = GetProportionOfRange(zoomProportion, this.MinCameraSpeed, this.MaxCameraSpeed) * Time.deltaTime;
        if (Input.GetMouseButton(MouseButton))
        {
            var change = Input.mousePosition - this._lastMousePosition;
            this._lastMousePosition = Input.mousePosition;
            if (change.magnitude > 0)
            {
                //Debug.Log("Mouse Move " + change);
                //transform.position += change;
                newX -= change.x * speed;
                newZ -= change.y * speed;
            }
        }
        else
        {
            this.ProcessKeys(speed, ref newX, ref newZ);
        }


        this.transform.position = new Vector3(newX, this.transform.position.y, newZ);
    }

    private void ProcessKeys(float speed, ref float newX, ref float newZ)
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        foreach (var key in this._directionKeys)
        {
            if (Input.GetKey(key))
            {
                if (this._upKeyCodes.Contains(key))
                {
                    newZ += this.KeyScrollSpeed * speed;
                }
                if (this._downKeyCodes.Contains(key))
                {
                    newZ -= this.KeyScrollSpeed * speed;
                }
                if (this._rightKeyCodes.Contains(key))
                {
                    newX += this.KeyScrollSpeed * speed;
                }
                if (this._leftKeyCodes.Contains(key))
                {
                    newX -= this.KeyScrollSpeed * speed;
                }
            }
        }
    }

    private float _targetXRotation = 0;
    public float MinY = 10;
    public float MaxY = 200;
    public float MinAngle = -60;
    public float MaxAngleHeight = 100;
    public float MaxAngle = 0;

    private float CurrentY => this.transform.InverseTransformPoint(this.Camera.transform.position).y;
    private float GetZoomProportion(float actualNewY)
    {
        return Mathf.Clamp01((actualNewY - this.MinY) / (this.MaxAngleHeight - this.MinY));
    }

    private void HandleZoom()
    {
        var ctrlPushed = Input.GetKey(KeyCode.LeftControl);
        var shiftPushed = Input.GetKey(KeyCode.LeftShift);
        if (Input.mouseScrollDelta.y != 0 || ctrlPushed || shiftPushed)
        {
            // TODO consider make this work on the proportion first, and get both angle and distance from that.
            float currentY = this.CurrentY;
            var speedMultiplyer = currentY / 100;
            var newY = currentY - Input.mouseScrollDelta.y * this.CameraZoomSpeed * speedMultiplyer;

            if (shiftPushed)
            {
                // Zoom in
                newY -= this.KeyZoomSpeed * this.CameraZoomSpeed * speedMultiplyer;
            }
            if(ctrlPushed)
            {
                // Zoom out
                newY += this.KeyZoomSpeed * this.CameraZoomSpeed * speedMultiplyer;
            }


            var actualNewY = Mathf.Clamp(newY, this.MinY, this.MaxY);
            if (currentY == actualNewY)
            {
                return;
            }

            var zoomProportion = this.GetZoomProportion(actualNewY);

            var newAngle = GetProportionOfRange(zoomProportion, this.MinAngle, this.MaxAngle);

            this._targetXRotation = this._targetXRotation - Input.mouseScrollDelta.y * this.CameraPanSpeed * speedMultiplyer;

            this.transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.right);

            this.Camera.transform.position = this.transform.TransformPoint(new Vector3(0, actualNewY, 0));
        }

    }

    private static float GetProportionOfRange(float proportion, float min, float max)
    {
        return min + ((max - min) * proportion);
    }
}
