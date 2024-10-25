using UnityEngine;

public class ScentDetector : MonoBehaviour
{
    public AntStateMachine AntStateMachine;

    void OnTriggerEnter(Collider collider)
    {
        ProcessSmell(collider?.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessSmell(collision.gameObject);
    }

    private void ProcessSmell(GameObject @object)
    {
        if (@object.TryGetComponent<Smellable>(out var smellable))
        {
            AntStateMachine.ProcessSmell(smellable);
        }
    }
}
