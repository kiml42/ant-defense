using UnityEngine;

public class WallSection : PlaceableSelectableGhostableMonoBehaviour, IPlaceablePositionValidator, ISelectableObject
{
    public WallNode[] EndNodes;

    public float Cost = 1f;

    /// <summary>
    /// Length of this wall section in world units. The total number of sections is floor(distance / SectionLength).
    /// Any remaining distance is covered by a stump at each node end.
    /// </summary>
    public float SectionLength = 1f;

    public void OnDeath()
    {
    }

    public override Vector3 Position => this.transform.position;

    public override void OnBuildStart()
    {
    }

    public override void OnPlace()
    {
        NoSpawnZone.Register(this);
    }

    internal void ConnectTo(WallNode[] others)
    {
        this.EndNodes = others;
    }

    public bool PositionIsValid(Vector3 position)
    {
        return true;
        //return this.EndNodes == null || (position - this.EndNodes.transform.position).magnitude <= this.MaxLength + 0.1f;
    }

    public override void Ghostify()
    {
        // Do nothing
    }

    public override void UnGhostify()
    {
        // Do nothing
    }
}
