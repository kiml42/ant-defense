using System;
using UnityEngine;

public class WallNode : PlaceableMonoBehaviour, IPlaceablePositionValidator, IInteractivePosition
{
    public WallNode ConnectedNode;
    public Transform Wall;
    public Transform Node;
    public float MaxLength;

    public float CostPerMeter = 1f;

    public override float AdditionalCost
    {
        get
        {
            if (this.ConnectedNode != null)
            {
                var length = (this.ConnectedNode.transform.position - this.transform.position).magnitude;
                return length * this.CostPerMeter;
            }
            return 0f;
        }
    }

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
        Debug.Log("Interaction with wall node " + this);

        if(ObjectPlacer.Instance.WallNodeBeingPlaced != null && ObjectPlacer.Instance.WallNodeBeingPlaced.ConnectedNode != null)
        {
            var placedObject = ObjectPlacer.Instance.PlaceObject();
            placedObject.GetComponent<WallNode>().RemoveNode();
        }
        else
        {
            ObjectPlacer.Instance.StartPlacingWallConnectedTo(this);
        }
    }

    /// <summary>
    /// Remove the node, but leave the wall, used for finishing at an existing node
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void RemoveNode()
    {
        foreach(var r in this.Node.GetComponentsInChildren<MeshRenderer>())
        {
            r.enabled = false;
        }
        foreach(var c in this.Node.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
    }
}

public abstract class PlaceableMonoBehaviour : MonoBehaviour
{
    public virtual float AdditionalCost => 0f;

    /// <summary>
    /// Called when the object is placed to start whatever spawn behaviour it has defined.
    /// </summary>
    public abstract void OnPlace();
}
