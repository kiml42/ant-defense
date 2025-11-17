using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class WallNode : PlaceableSelectableGhostableMonoBehaviour, IPlaceablePositionValidator, ISelectableObject
{
    public WallNode ConnectedNode;
    public Transform Wall;
    public Transform Node;
    public float MaxLength;

    public float CostPerMeter = 1f;

    private ISelectableObject[] _selectionDelegates;    // TODO replace with similar property in parent class.

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

    public override Vector3 Position => this.transform.position;

    public override void OnPlace()
    {
        //Debug.Log("WallNode placed, connected to " + this.ConnectedNode);
        this.UpdateWall();
        this.enabled = false;   //disable to prevent updating the wall every frame
        NoSpawnZone.Register(this); // register this as a selection point
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

    protected override void OnSelect()
    {
        Debug.Log("WallNode selected: " + this);
        // TODO have a wall placing mode for placing walls, rather than just relying on selecting wall nodes.

        if (ObjectPlacer.Instance.CanBuildOnWall && this._selectionDelegates == null)
        {
            // The object can be placed on a wall, and this wall can have an object placed on top of it.
            // Place the object on this wall, and set this wall as the parent.
            var newObject = ObjectPlacer.Instance.PlaceObject(this);
            if(newObject != null)   // new object will be null if it can't be placed (e.g. too expensive)
            {
                this.ConnectedSelectable = newObject.GetComponent<SelectableGhostableMonoBehaviour>();
                this.ConnectedSelectable.ConnectedSelectable = this;

                newObject.WallParent = this;
            }
            return;
        }

        if (ObjectPlacer.Instance.WallNodeBeingPlaced != null && ObjectPlacer.Instance.WallNodeBeingPlaced.ConnectedNode != null)
        {
            // is currently placing a wall node
            // the wall node that is being placed is already connected to another node
            // So place the new wall node at this location, for the connected wall to be placed correctly, but then hide the new node because it overlaps this node.
            var placedObject = ObjectPlacer.Instance.PlaceObject();
            placedObject.GetComponent<WallNode>().RemoveNode();
            return;
        }

        // not currently placing a wall node, or the wall node being placed is not yet connected to another node, so start placing a new wall node connected to this one.
        ObjectPlacer.Instance.StartPlacingWallConnectedTo(this);
    }

    protected override void OnDeselect()
    {
        // Do nothing
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

    public override void Ghostify()
    {
        // Do nothing
    }

    public override void UnGhostify()
    {
        // Do nothing
    }
}

public abstract class PlaceableSelectableGhostableMonoBehaviour : SelectableGhostableMonoBehaviour
{
    public virtual float AdditionalCost => 0f;

    /// <summary>
    /// Called when the object is placed to start whatever spawn behaviour it has defined.
    /// </summary>
    public abstract void OnPlace();
}
