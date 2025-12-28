using Assets.Scripts;
using UnityEngine;

public class SpawnObjectOnDeath : DeathActionBehaviour
{
    public Transform DeadObject;

    public override void OnDeath()
    {
        if (this.DeadObject == null) return;
        var deadObject = Instantiate(this.DeadObject);
        this.DeadObject = null; // prevent duplicate instanciation.
        deadObject.transform.position = this.transform.position;
    }
}
