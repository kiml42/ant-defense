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
        if (_currentTargets.Any())
        {
            _timeToTrigger -= Time.fixedDeltaTime;
            Debug.Log("Trigger in " + _timeToTrigger);
            if( _timeToTrigger <= 0)
            {
                Triggerable.Trigger();
                _currentTargets.Clear();
            }
        }
        else
        {
            _timeToTrigger = TriggerDelay;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        var target = other.GetComponentInParent<Target>();
        if (target != null)
        {
            Debug.Log($"Triggering for " + target);
            _currentTargets.Add(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        _currentTargets.Remove(other);
    }
}
