using UnityEngine;

public class BuildWallActionButton : SelectionActionButton
{
    public WallNode WallNode;

    public override void Execute()
    {
        ObjectPlacer.Instance.StartPlacingWallConnectedTo(this.WallNode);
    }
}
