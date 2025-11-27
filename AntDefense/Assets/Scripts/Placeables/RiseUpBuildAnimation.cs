using UnityEngine;

public class RiseUpBuildAnimation : BaseBuildAnimation
{
    public Vector3 StartOffset;
    private Vector3 EndPosition;

    void Start()
    {
        var localPosition = this.transform.localPosition;
        this.EndPosition = localPosition;
    }

    protected override void UpdateAnimation()
    {
        this.transform.localPosition = Vector3.Lerp(this.EndPosition + StartOffset, this.EndPosition, this._progress);
    }
}

public abstract class  BaseBuildAnimation : MonoBehaviour
{
    /// <summary>
    /// The animation duration in seconds.
    /// </summary>
    public float Duration = 3f;
    protected float _progress = 0f;
    private bool _isRunning = false;

    public void StartAnimation()
    {
        this._isRunning = true;
    }

    protected abstract void UpdateAnimation();


    void FixedUpdate()
    {
        if(!this._isRunning) return;
        this._progress += Time.fixedDeltaTime / Duration;
        if (this._progress > 1f)
        {
            this._progress = 1f;
            enabled = false;
        }
        this.UpdateAnimation();
    }
}
