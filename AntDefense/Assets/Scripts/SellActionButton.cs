public class SellActionButton : SelectionActionButton
{
    private Sellable _sellable;

    public void Initialise(Sellable sellable)
    {
        this._sellable = sellable;
    }

    public override void Execute()
    {
        this._sellable?.Sell();
    }
}
