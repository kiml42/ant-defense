using UnityEngine;

public class WallNode : PlaceableMonoBehaviour
{
    public WallNode ConnectedNode;
    public Transform Wall;

    public override void OnPlace()
    {
        //Debug.Log("WallNode placed, connected to " + this.ConnectedNode);
        this.UpdateWall();
        this.enabled = false;   //disable to prevent updating the wall every frame
    }

    internal void ConnectTo(WallNode other)
    {
        //Debug.Log("Connecting WallNode to " + other);
        ConnectedNode = other;
    }

    private void Update()
    {
        this.UpdateWall();
    }

    private void UpdateWall()
    {
        Debug.Assert(Wall != null, "WallNode has no Wall assigned.");
        //Debug.Log("Updating WallNode. Connected to " + ConnectedNode);
        if (ConnectedNode != null)
        {
            var direction = ConnectedNode.transform.position - this.transform.position;
            if (direction.magnitude > 0.01f)
            {
                var midpoint = this.transform.position + direction * 0.5f;
                Wall.position = midpoint;
                Wall.localScale = new Vector3(1, 1, direction.magnitude);
                Wall.rotation = Quaternion.LookRotation(direction, Vector3.up);
                return;
            }
        }
        Wall.localScale = Vector3.zero;
    }
}

public abstract class PlaceableMonoBehaviour : MonoBehaviour
{
    /// <summary>
    /// Called when the object is placed to start whatever spawn behaviour it has defined.
    /// </summary>
    public abstract void OnPlace();
}
