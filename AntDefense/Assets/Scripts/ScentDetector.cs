using UnityEngine;

public class ScentDetector : MonoBehaviour
{
    public AntStateMachine AntStateMachine;

    void OnTriggerEnter(Collider collider)
    {
        this.ProcessSmell(collider?.gameObject, $"{this} - Smell sense trigger enter {collider?.gameObject}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        this.ProcessSmell(collision.gameObject, $"{this} - Smell sense collision enter {collision?.gameObject}");
    }

    private void ProcessSmell(GameObject @object, string debugString)
    {
        var smellable = @object.GetComponentInParent<Smellable>();

        if (smellable != null)
        {
            this.AntStateMachine.ProcessSmell(smellable, debugString);
        }

    }
}
