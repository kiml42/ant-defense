using System;
using UnityEngine;

[Obsolete("Use PlaceableRealObject instead - only used for flip traps")]
public class PlaceableGhost : PlaceableObjectOrGhost
{
    public Transform RealObject;

    override protected Transform FallbackIcon { get { return this.RealObject; } }

    protected override void Finalise()
    {
        Instantiate(this.RealObject, this.transform.position + this.SpawnOffset, this.transform.rotation);
        Destroy(this.gameObject);
    }
}

