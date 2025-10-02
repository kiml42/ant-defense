using UnityEngine;

public class PlaceableRealObject : PlaceableObjectOrGhost
{
    private Ghostable[] _ghostables;
    private Ghostable[] Ghostables { get { return this._ghostables ??= this.GetComponentsInChildren<Ghostable>(); } }
    override protected Transform FallbackIcon { get { return this.transform; } }

    public override void StartPlacing()
    {
        //Debug.Log("Starting placing real object " + this);
        base.StartPlacing();
        foreach (var ghostable in this.Ghostables)
        {
            ghostable.Ghostify();
        }
    }

    public override void Place()
    {
        base.Place();
        foreach (var ghostable in this.Ghostables)
        {
            Debug.Log("Unghostifying " + ghostable);
            ghostable.UnGhostify();
        }
    }

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
            placeable.OnPlace();
        }
        this.enabled = false;   // disable to prevent updating every frame
    }
}
