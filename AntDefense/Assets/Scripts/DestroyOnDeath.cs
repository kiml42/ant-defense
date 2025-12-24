using Assets.Scripts;
using UnityEngine;

public class DestroyOnDeath : DeathActionBehaviour
{
    public override void OnDeath()
    {
        Destroy(this.gameObject);
    }
}
