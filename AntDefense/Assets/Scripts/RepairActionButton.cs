public class RepairActionButton : SelectionActionButton
{
    private Repairable _repairable;

    public void Initialise(Repairable repairable)
    {
        this._repairable = repairable;
    }

    protected override void Update()
    {
        base.Update();
        if (this._repairable != null)
            this.gameObject.SetActive(this._repairable.NeedsRepair && MoneyTracker.CanAfford(this._repairable.RepairCost));
    }

    public override void Execute()
    {
        this._repairable?.Repair();
    }
}
