using UnityEngine;

public class ScentDetector : MonoBehaviour
{
    public Smell SeekingSmell => AntStateMachine.SeekingSmell;
    private ISmellable currentSmell;

    public AntStateMachine AntStateMachine;

    void OnTriggerEnter(Collider collider)
    {
        ProcessSmell(collider.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessSmell(collision.gameObject);
    }

    private void ProcessSmell(GameObject @object)
    {
        if (@object.TryGetComponent<ISmellable>(out var smellable))
        {
            if (smellable.Smell == SeekingSmell)
            {
                currentSmell = smellable;
                AntStateMachine.ProcessSmell(smellable);
            }
        }
    }
}
