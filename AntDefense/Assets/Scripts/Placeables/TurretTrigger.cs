using UnityEngine;

public class TurretTrigger : MonoBehaviour
{
    public TurretController TurretController;
    public SphereCollider TriggerCollider;

    private void Start()
    {
        Debug.Assert(this.TurretController != null, "TurretTrigger requires a TurretController to register targets with.");
        Debug.Assert(this.TriggerCollider != null, "TurretTrigger requires a SphereCollider to define its trigger area.");
    }

    private void OnTriggerEnter(Collider other)
    {
        this.TurretController.RegisterTarget(other);
    }

    private void OnTriggerStay(Collider other)
    {
        this.TurretController.RegisterTarget(other);
    }

    private void OnTriggerExit(Collider other)
    {
        this.TurretController.DeregisterTarget(other);
    }
}
