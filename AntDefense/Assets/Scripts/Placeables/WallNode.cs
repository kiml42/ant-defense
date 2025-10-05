using UnityEngine;


public interface IPlaceablePositionValidator
{
    bool PositionIsValid(Vector3 position);
}

public interface IInteractivePosition
{
    Vector3 Position { get; }

    void Interact();
}

public class WallNode : PlaceableMonoBehaviour, IPlaceablePositionValidator, IInteractivePosition
{
    public WallNode ConnectedNode;
    public Transform Wall;
    public float MaxLength;

    public Vector3 Position => this.transform.position;

    public override void OnPlace()
    {
        //Debug.Log("WallNode placed, connected to " + this.ConnectedNode);
        this.UpdateWall();
        this.enabled = false;   //disable to prevent updating the wall every frame
        NoSpawnZone.Register(this); // register this as an interactive point
    }

    internal void ConnectTo(WallNode other)
    {
        //Debug.Log("Connecting WallNode to " + other);
        this.ConnectedNode = other;
    }

    public bool PositionIsValid(Vector3 position)
    {
        return this.ConnectedNode == null || (position - this.ConnectedNode.transform.position).magnitude <= this.MaxLength + 0.1f;
    }

    private void Update()
    {
        this.UpdateWall();
    }

    private void UpdateWall()
    {
        Debug.Assert(this.Wall != null, "WallNode has no Wall assigned.");
        //Debug.Log("Updating WallNode. Connected to " + ConnectedNode);
        if (this.ConnectedNode != null)
        {
            var direction = this.ConnectedNode.transform.position - this.transform.position;
            if (direction.magnitude > 0.01f)
            {
                var midpoint = this.transform.position + (direction * 0.5f);
                this.Wall.position = midpoint;
                this.Wall.localScale = new Vector3(1, 1, direction.magnitude);
                this.Wall.rotation = Quaternion.LookRotation(direction, Vector3.up);
                return;
            }
        }
        this.Wall.localScale = Vector3.zero;
    }

    public void Interact()
    {
        // TODO: work out why this starts placing the wall correctly, but when it's finalised the node remains but the wall dissapears.
        Debug.Log("Interaction with wall node " + this);
        ObjectPlacer.Instance.StartPlacingWallConnectedTo(this);
    }
}

public abstract class PlaceableMonoBehaviour : MonoBehaviour
{
    /// <summary>
    /// Called when the object is placed to start whatever spawn behaviour it has defined.
    /// </summary>
    public abstract void OnPlace();
}
