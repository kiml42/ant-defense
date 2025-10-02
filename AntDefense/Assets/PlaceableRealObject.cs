using UnityEngine;

public class PlaceableRealObject : PlaceableObjectOrGhost
{
    override protected Transform FallbackIcon { get { return this.transform; } }

    protected override void Finalise()
    {
        var foodSmells = this.GetComponentsInChildren<FoodSmell>();
        foreach (var foodSmell in foodSmells)
        {
            foodSmell.MarkAsPermanant(false);
        }

        var placeables = this.GetComponents<PlaceableMonoBehaviour>();

        foreach (var placeable in placeables)
        {
            placeable.OnPlace(null);
        }
        this.enabled = false;   // disable to prevent updating every frame
    }
}
