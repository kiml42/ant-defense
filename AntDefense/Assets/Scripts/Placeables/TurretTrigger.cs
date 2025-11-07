using UnityEngine;

public class TurretTrigger : MonoBehaviour
{
    public TurretController TurretController;

    private void OnTriggerEnter(Collider other)
    {
        this.TurretController.RegisterTarget(other);
    }

    private void OnTriggerExit(Collider other)
    {
        this.TurretController.DeregisterTarget(other);
    }
}
