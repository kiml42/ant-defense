using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColliderTrigger : MonoBehaviour
{
    public Triggerable Triggerable;

    public float TriggerDelay = 0;
    private float _timeToTrigger = 0;

    private HashSet<Collider> _currentTargets = new HashSet<Collider>();

    private void FixedUpdate()
    {
        if (this._currentTargets.Any())
        {
            this._timeToTrigger -= Time.deltaTime;
            //Debug.Log("Trigger in " + _timeToTrigger);
            if(this._timeToTrigger <= 0)
            {
                this.Triggerable.Trigger();
                this._currentTargets.Clear();
            }
        }
        else
        {
            this._timeToTrigger = this.TriggerDelay;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        var target = other.GetComponentInParent<Target>();
        if (target != null)
        {
            Debug.Log($"Triggering for " + target);
            this._currentTargets.Add(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        this._currentTargets.Remove(other);
    }
}
