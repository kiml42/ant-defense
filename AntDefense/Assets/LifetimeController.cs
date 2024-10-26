using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifetimeController : MonoBehaviour
{
    public float RemainingTime = 30;

    public float ScaleDownTime = 0;

    private void FixedUpdate()
    {
        RemainingTime -= Time.deltaTime;
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
