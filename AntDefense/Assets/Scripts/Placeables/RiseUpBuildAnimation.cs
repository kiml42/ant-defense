using Assets.Scripts;
using UnityEngine;

public class RiseUpBuildAnimation : BaseBuildAnimation
{
    public Vector3 StartOffset;
    private Vector3 EndPosition;
    private bool _endPositionCaptured = false;

    protected override void Initilise()
    {
        if (!this._endPositionCaptured)
        {
            //Debug.Log($"Initilise location = {this.transform.localPosition}");
            this.EndPosition = this.transform.localPosition;
            this._endPositionCaptured = true;
        }
    }

    protected override void UpdateAnimation()
    {
        this.transform.localPosition = Vector3.Lerp(this.EndPosition + StartOffset, this.EndPosition, this._progress);
        //Debug.Log($"RiseUpBuildAnimation UpdateAnimation progress: {_progress}, End: {EndPosition}, Current: {this.transform.localPosition}");
    }
}

public abstract class  BaseBuildAnimation : DeathActionBehaviour
{
    /// <summary>
    /// The animation duration in seconds.
    /// </summary>
    public float Duration = 3f;

    /// <summary>
    /// the animation duration in seconds when reversing on death.
    /// </summary>
    public float DeathAnimationDuration = 1f;
    public float StartDelay = 0.5f;
    private float _delayTimer = 0f;
    protected float _progress = 0f;
    private bool _isRunning = false;

    public bool HasStartedAnimating => this._progress > 0f;

    public bool ReverseOnDeath = true;
    private bool _hasDied = false;
    public override void OnDeath()
    {
        if (this.ReverseOnDeath)
        {
            this._hasDied = true;
            this._isRunning = true;
            enabled = true;
        }
    }
    public void StartAnimation()
    {
        this.Initilise();
        this._isRunning = true;
        this._hasDied = false;
        this._progress = 0;
        this._delayTimer = 0;
        this.UpdateAnimation();
        enabled = true;
    }

    /// <summary>
    /// Pushes the animation start back by <paramref name="delay"/> seconds.
    /// Must be called after StartAnimation.
    /// </summary>
    public void AddStartDelay(float delay)
    {
        this._delayTimer -= delay;
    }

    protected abstract void Initilise();
    protected abstract void UpdateAnimation();

    void FixedUpdate()
    {
        if (!this._isRunning) return;

        this._delayTimer += Time.fixedDeltaTime;
        //Debug.Log($"Build animation delayTimer: {_delayTimer}");
        if(this._delayTimer < this.StartDelay) return;

        if(this._hasDied)
        {
            this._progress -= Time.fixedDeltaTime / DeathAnimationDuration;
            if (this._progress < 0f)
            {
                this._progress = 0f;
                enabled = false;
            }
            this.UpdateAnimation();
            return;
        }

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
