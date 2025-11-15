using UnityEngine;

public class RigidbodyGhostable : BaseGhostable
{
    private Rigidbody[] _rbsToDisable;
    private Rigidbody[] RbsToDisable
    {
        get
        {
            return this._rbsToDisable ??= this.GetComponentsInChildren<Rigidbody>();
        }
    }

    public override void Ghostify()
    {
        foreach(var rb in this.RbsToDisable)
        {
            rb.isKinematic = true;
        }
    }

    public override void UnGhostify()
    {
        foreach (var rb in this.RbsToDisable)
        {
            rb.isKinematic = false;
        }
    }
}
