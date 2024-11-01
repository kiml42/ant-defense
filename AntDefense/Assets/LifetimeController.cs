using UnityEngine;

public class LifetimeController : MonoBehaviour
{
    public float RemainingTime = 30;

    public float ScaleDownTime = 0;

    private float _initialLifetime;

    public void Reset()
    {
        RemainingTime = _initialLifetime;
    }

    private void Start()
    {
        _initialLifetime = RemainingTime;
    }

    private void FixedUpdate()
    {
        RemainingTime -= Time.fixedDeltaTime;
        if (RemainingTime <= 0)
        {
            Destroy(this.gameObject);
        }
        if (ScaleDownTime > 0 && RemainingTime < ScaleDownTime)
        {
            this.transform.localScale = Vector3.one * RemainingTime / ScaleDownTime;
        }
    }
}
