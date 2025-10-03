using UnityEngine;

public class ColliderGhostable : BaseGhostable
{
    private Collider _colliderToDisable;
    private Collider ColliderToDisable
    {
        get
        {
            return this._colliderToDisable ??= this.GetComponent<Collider>();
        }
    }

    public override void Ghostify()
    {
        this.ColliderToDisable.enabled = false;
    }

    public override void UnGhostify()
    {
       this.ColliderToDisable.enabled = true;
    }
}
