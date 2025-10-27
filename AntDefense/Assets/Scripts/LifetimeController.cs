using UnityEngine;

public class LifetimeController : MonoBehaviour
{
    public float RemainingTime = 30;

    public float ScaleDownTime = 0;

    public bool IsRunning = true;

    private float _initialLifetime;
    Vector3 _initialScale;

    public void Reset()
    {
        this.RemainingTime = this._initialLifetime;
    }

    private void Start()
    {
        this._initialLifetime = this.RemainingTime;
        this._initialScale = this.transform.localScale;
    }

    private void FixedUpdate()
    {
        this.RemainingTime -= Time.fixedDeltaTime;
        if (this.RemainingTime <= 0)
        {
            Destroy(this.gameObject);
        }
        if (this.ScaleDownTime > 0 && this.RemainingTime < this.ScaleDownTime)
        {
            this.transform.localScale = this._initialScale * this.RemainingTime / this.ScaleDownTime;
        }
    }
}
