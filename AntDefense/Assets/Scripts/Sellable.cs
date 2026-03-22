using UnityEngine;

public class Sellable : MonoBehaviour
{
    public float SellValue;

    public void Sell()
    {
        TranslateHandle.Instance.DeselectObjects();
        MoneyTracker.Earn(this.SellValue);

        // Sellable may be on a child — find the root of the whole placeable unit.
        var placer = this.GetComponentInParent<PlaceableObjectOrGhost>();
        var rootObject = placer != null ? placer.gameObject : this.gameObject;

        // Remove placement interaction immediately — object may linger for the animation duration.
        foreach (var noSpawnZone in rootObject.GetComponentsInChildren<NoSpawnZone>())
            noSpawnZone.Ghostify();
        foreach (var selectable in rootObject.GetComponentsInChildren<SelectableGhostableMonoBehaviour>())
            NoSpawnZone.Unregister(selectable);

        float destroyDelay = 0f;
        foreach (var anim in rootObject.GetComponentsInChildren<BaseBuildAnimation>())
        {
            anim.OnDeath();
            destroyDelay = Mathf.Max(destroyDelay, anim.DeathAnimationDuration);
        }

        Destroy(rootObject, destroyDelay);
    }
}
