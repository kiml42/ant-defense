using UnityEngine;

public class TurretTrigger : MonoBehaviour
{
    public TurretController TurretController;

    private void OnTriggerEnter(Collider other)
    {
        if(this.TurretController != null)
        {
            this.TurretController.RegisterTarget(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (this.TurretController != null)
        {
            this.TurretController.DeregisterTarget(other);
        }
    }
}
