using UnityEngine;

public class GlobalKeyHandler : MonoBehaviour
{
    // TODO: camera doesn't move when paused.
    // TODO: Bug: walls still look see-through when paused.
    public KeyCode TogglePauseKey = KeyCode.Space;

    public KeyCode FastForwardModeKey = KeyCode.F;

    public float FastForwardTimeScale = 3f;
    public enum TimeScaleMode
    {
        Paused,
        Normal,
        FastForward
    }

    private TimeScaleMode _currentMode = TimeScaleMode.Normal;

    private float CurrentTimeScale
    {
        get
        {
            switch(this._currentMode)
            {
                case TimeScaleMode.Paused:
                    return 0f;
                case TimeScaleMode.Normal:
                    return 1f;
                case TimeScaleMode.FastForward:
                    return this.FastForwardTimeScale;
                default:
                    return 1f;
            }
        }
    }

    //private void FixedUpdate()
    //{
        
    //    Debug.Log($"FixedUpdate: Current time scale mode: {this._currentMode}, time scale: {Time.timeScale}, deltaTime={Time.deltaTime}, fixedDeltaTime={Time.fixedDeltaTime}");
    //}

    // Update is called once per frame
    void Update()
    {
        //Debug.Log($"Update: Current time scale mode: {this._currentMode}, time scale: {Time.timeScale}, deltaTime={Time.deltaTime}, fixedDeltaTime={Time.fixedDeltaTime}");
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
        var anyChange = false;
        if (Input.GetKeyUp(this.TogglePauseKey))
        {
            if (this._currentMode == TimeScaleMode.Paused)
            {
                this._currentMode = TimeScaleMode.Normal;
            }
            else
            {
                this._currentMode = TimeScaleMode.Paused;
            }
            anyChange = true;
        }
        if (Input.GetKeyUp(this.FastForwardModeKey))
        {
            switch(this._currentMode)
            {
                case TimeScaleMode.Paused:
                case TimeScaleMode.Normal:
                    this._currentMode = TimeScaleMode.FastForward;
                    break;
                case TimeScaleMode.FastForward:
                    this._currentMode = TimeScaleMode.Normal;
                    break;
            }
            anyChange = true;
        }

        if (anyChange)
        {
            Time.timeScale = (int)this.CurrentTimeScale;
            AudioListener.pause = this._currentMode == TimeScaleMode.Paused;
        }
    }
}
