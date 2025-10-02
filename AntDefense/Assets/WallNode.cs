using UnityEngine;

public class WallNode : PlaceableMonoBehaviour
{
    public WallNode ConnectedNode;
    public Transform ConnectedNodeMarker;
    public Transform Wall;
    private bool hasBeenPlaced = false;

    public override void OnPlace(PlaceableGhost ghost)
    {
        var wallNodeGhost = ghost.GetComponent<WallNode>();
        if (wallNodeGhost != null && wallNodeGhost.ConnectedNode != null)
        {
            //Debug.Log("WallNode placed. Temp node connected to " + wallNodeGhost.ConnectedNode);
            this.ConnectTo(wallNodeGhost.ConnectedNode);
            this.UpdateWall();
        }
        else
        {
            //Debug.Log("WallNode placed. without connecting.");
        }
        this.enabled = false;   //disable to prevent updating the wall every frame
        this.hasBeenPlaced = true;
        ObjectPlacer.Instance.NotifyBuiltWall(this, ghost);
    }

    internal void ConnectTo(WallNode other)
    {
        Debug.Log("Connecting WallNode to " + other);
        ConnectedNode = other;
    }

    private void Update()
    {
        this.UpdateWall();
    }

    private void UpdateWall()
    {
        Debug.Log("Updating WallNode. Connected to " + ConnectedNode + ", hasBeenPlaced = " + this.hasBeenPlaced);
        if (ConnectedNodeMarker != null)
        {
            if (ConnectedNode != null)
            {
                ConnectedNodeMarker.position = ConnectedNode.transform.position;
                ConnectedNodeMarker.localScale = Vector3.one;
            }
            else
            {
                ConnectedNodeMarker.localScale = Vector3.zero;
            }
        }
        if (Wall != null)
        {
            if (ConnectedNode != null)
            {
                var direction = ConnectedNode.transform.position - this.transform.position;
                var midpoint = this.transform.position + direction * 0.5f;
                Wall.position = midpoint;
                Wall.localScale = new Vector3(1, 1, direction.magnitude);
                Wall.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
            else
            {
                Wall.localScale = Vector3.zero;
            }
        }
    }
}

public abstract class PlaceableMonoBehaviour : MonoBehaviour
{
    /// <summary>
    /// called on the real object with the ghost that created it so it can get any data it needs from the ghost before the ghost is destroyed.
    /// </summary>
    /// <param name="ghost"></param>
    public abstract void OnPlace(PlaceableGhost ghost);
}
