using UnityEngine;

public class GlobalKeyHandler : MonoBehaviour
{
    public KeyCode TogglePauseKey = KeyCode.Space;

    public KeyCode FastForwardModeKey = KeyCode.F;

    public float FastForwardTimeScale = 3f;

    public float FastForwardTargetFrameRate = 30f;

    private float _scaledFastForwardTimeScale
    {
        get
        {
            var currentTimeScale = Time.timeScale;
            var currentFrameRate = 1f / Time.unscaledDeltaTime;
            var scaleFactor =  currentFrameRate / this.FastForwardTargetFrameRate;
            float newTimeScale = currentTimeScale;
            if (scaleFactor < 0.8)
            {
                // current frame rate is too low so decrease the timescale proportionally.
                newTimeScale = currentTimeScale - 0.1f;
                //Debug.Log($"Slowing down scale factor: {scaleFactor}, adjusted time scale: {newTimeScale}");
            } else if (scaleFactor > 1.2)
            {
                // current frame rate could stand to be lower so increase the timescale proportionally.
                newTimeScale = currentTimeScale + 0.01f;
                //Debug.Log($"Speeding up scale factor: {scaleFactor}, adjusted time scale: {newTimeScale}");
            }
            //Debug.Log($"Fast forward scale factor: {scaleFactor}, adjusted time scale: {newTimeScale}");
            return Mathf.Max(newTimeScale, this.FastForwardTimeScale);
        }
    }
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
                    return this._scaledFastForwardTimeScale;
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

        if (anyChange || this._currentMode == TimeScaleMode.FastForward)
        {
            Time.timeScale = this.CurrentTimeScale;
            //Debug.Log($"Changed time scale mode to {this._currentMode}, time scale: {Time.timeScale}, deltaTime={Time.deltaTime}");
            AudioListener.pause = this._currentMode == TimeScaleMode.Paused;
        }
    }
}
