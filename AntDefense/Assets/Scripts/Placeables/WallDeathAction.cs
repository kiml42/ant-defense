using Assets.Scripts;
using UnityEngine;

public class WallDeathAction : DeathActionBehaviour
{
    public WallNode WallNode;

    public override void OnDeath()
    {
        this.WallNode.OnDeath();
    }
}
