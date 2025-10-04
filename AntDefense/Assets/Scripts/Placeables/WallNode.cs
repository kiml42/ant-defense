using UnityEngine;

public class WallNode : PlaceableMonoBehaviour
{
    public WallNode ConnectedNode;
    public Transform Wall;
    public float MaxLength;

    public override void OnPlace()
    {
        //Debug.Log("WallNode placed, connected to " + this.ConnectedNode);
        this.UpdateWall();
        this.enabled = false;   //disable to prevent updating the wall every frame
    }

    internal void ConnectTo(WallNode other)
    {
        //Debug.Log("Connecting WallNode to " + other);
        this.ConnectedNode = other;
    }

    // TODO make a position validator interface, or consider fixing the position instead of just disallowing placement.
    internal bool PositionIsValid(Vector3 position)
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
                var midpoint = this.transform.position + direction * 0.5f;
                this.Wall.position = midpoint;
                this.Wall.localScale = new Vector3(1, 1, direction.magnitude);
                this.Wall.rotation = Quaternion.LookRotation(direction, Vector3.up);
                return;
            }
        }
        this.Wall.localScale = Vector3.zero;
    }
}

public abstract class PlaceableMonoBehaviour : MonoBehaviour
{
    /// <summary>
    /// Called when the object is placed to start whatever spawn behaviour it has defined.
    /// </summary>
    public abstract void OnPlace();
}
