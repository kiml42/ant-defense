using UnityEngine;

public class ColliderGhostable : BaseGhostableMonobehaviour
{
    private Collider[] _collidersToDisable;
    private Collider[] CollidersToDisable
    {
        get
        {
            return this._collidersToDisable ??= this.GetComponentsInChildren<Collider>();
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
