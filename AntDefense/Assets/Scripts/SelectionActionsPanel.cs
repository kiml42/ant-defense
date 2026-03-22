using UnityEngine;

/// <summary>
/// Add as a child of a placeable object. Wire up SellButton and RepairButton in the Inspector.
/// The panel shows when the parent object is selected and hides when deselected or ghostified.
/// Requires a CameraFacer component on the same GameObject to face the camera.
/// </summary>
public class SelectionActionsPanel : SelectableGhostableMonoBehaviour
{
    public SellActionButton SellButton;
    public RepairActionButton RepairButton;

    public override Vector3 Position => this.transform.position;

    private void Awake()
    {
        this.gameObject.SetActive(false);
    }

    public override void Ghostify()
    {
        this.gameObject.SetActive(false);
    }

    public override void UnGhostify()
    {
        // Stay hidden until explicitly selected.
    }

    protected override void OnSelect()
    {
        base.OnSelect();
        this.gameObject.SetActive(true);

        var sellable = this.GetComponentInParent<Sellable>();
        if (this.SellButton != null)
        {
            this.SellButton.Initialise(sellable);
            this.SellButton.gameObject.SetActive(sellable != null);
        }

        var repairable = this.GetComponentInParent<Repairable>();
        if (this.RepairButton != null)
        {
            this.RepairButton.Initialise(repairable);
            this.RepairButton.gameObject.SetActive(repairable != null && repairable.NeedsRepair);
        }
    }

    protected override void OnDeselect()
    {
        base.OnDeselect();
        this.gameObject.SetActive(false);
    }
}
