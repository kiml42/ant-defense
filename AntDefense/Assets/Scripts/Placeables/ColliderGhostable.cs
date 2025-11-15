using UnityEngine;

public class ColliderGhostable : BaseGhostable
{
    private Collider[] _colliderToDisable;
    private Collider[] CollidersToDisable
    {
        get
        {
            return this._colliderToDisable ??= this.GetComponentsInChildren<Collider>();
        }
    }

    public override void Ghostify()
    {
        foreach(Collider collider in this.CollidersToDisable)
        {
            collider.enabled = false;
        }
    }

    public override void UnGhostify()
    {
        foreach (Collider collider in this.CollidersToDisable)
        {
            collider.enabled = true;
        }
    }
}
