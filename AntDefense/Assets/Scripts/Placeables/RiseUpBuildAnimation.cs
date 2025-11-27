using UnityEngine;

public class RiseUpBuildAnimation : BaseBuildAnimation
{
    public Vector3 StartOffset;
    private Vector3 EndPosition;

    protected override void Initilise()
    {
        //Debug.Log($"Initilise location = {this.transform.localPosition}");
        var localPosition = this.transform.localPosition;
        this.EndPosition = localPosition;
    }

    protected override void UpdateAnimation()
    {
        this.transform.localPosition = Vector3.Lerp(this.EndPosition + StartOffset, this.EndPosition, this._progress);
        //Debug.Log($"RiseUpBuildAnimation UpdateAnimation progress: {_progress}, End: {EndPosition}, Current: {this.transform.localPosition}");
    }
}

public abstract class  BaseBuildAnimation : MonoBehaviour
{
    /// <summary>
    /// The animation duration in seconds.
    /// </summary>
    public float Duration = 3f;
    public float StartDelay = 0.5f;
    private float _delayTimer = 0f;
    protected float _progress = 0f;
    private bool _isRunning = false;

    public void StartAnimation()
    {
        this.Initilise();
        this._isRunning = true;
        this._progress = 0;
        this.UpdateAnimation();
    }

    protected abstract void Initilise();
    protected abstract void UpdateAnimation();

    void FixedUpdate()
    {
        if (!this._isRunning) return;
        this._delayTimer += Time.fixedDeltaTime;
        //Debug.Log($"Build animation delayTimer: {_delayTimer}");
        if(this._delayTimer < this.StartDelay) return;
        //Debug.Log($"Build animation progress: {_progress}");
        this._progress += Time.fixedDeltaTime / Duration;
        if (this._progress > 1f)
        {
            this._progress = 1f;
            enabled = false;
        }
        this.UpdateAnimation();
    }
}
