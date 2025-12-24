using Assets.Scripts;
using System;
using UnityEngine;

public class WallNode : PlaceableSelectableGhostableMonoBehaviour, IPlaceablePositionValidator, ISelectableObject
{
    public WallNode ConnectedNode;
    public Transform Wall;
    public Transform Node;
    public float MaxLength;

    public float CostPerMeter = 1f;
    private SelectableGhostableMonoBehaviour _child;

    public bool IsWallToBuildOn => this._child == null;

    /// <summary>
    /// Calls on death action for the turret connected to this wall node, if any.
    /// </summary>
    public void OnDeath()
    {
        if(this._child != null)
        {
            var actions = this._child.GetComponentsInChildren<DeathActionBehaviour>();
            foreach(var action in actions)
            {
                action.OnDeath();
            }
        }
    }

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
        this.UpdateWall();
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
        //TODO - move the health bar to over the middle of the wall.
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
        //Debug.Log("WallNode selected: " + this);
        // TODO have a wall placing mode for placing walls, rather than just relying on selecting wall nodes.

        if (ObjectPlacer.Instance.CanBuildOnWall && this._child == null)
        {
            // The object can be placed on a wall, and this wall can have an object placed on top of it.
            // Place the object on this wall, and set this wall as the parent.
            var newObject = ObjectPlacer.Instance.PlaceObject(this);
            if(newObject != null)   // new object will be null if it can't be placed (e.g. too expensive)
            {
                this._child = newObject.GetComponent<SelectableGhostableMonoBehaviour>();

                newObject.WallParent = this;

                this.Deselect();
            }
            return;
        }

        if (
            ObjectPlacer.Instance.WallNodeBeingPlaced != null                   // A wall node is being placed
            && ObjectPlacer.Instance.WallNodeBeingPlaced.ConnectedNode != null  // It is connected to something
            && ObjectPlacer.Instance.WallNodeBeingPlaced.ConnectedNode != this  // It's not connected to this
            )
        {
            // is currently placing a wall node
            // the wall node that is being placed is already connected to another node
            // So place the new wall node at this location, for the connected wall to be placed correctly, but then hide the new node because it overlaps this node.
            var placedObject = ObjectPlacer.Instance.PlaceObject();
            if (placedObject != null)   // may not be able to place the object (e.g. too expensive), so do nothing.
            {
                placedObject.GetComponent<WallNode>().RemoveNode();
            }
            return;
        }

        if(!ObjectPlacer.Instance.IsPlacingObject || (ObjectPlacer.Instance.WallNodeBeingPlaced != null && ObjectPlacer.Instance.WallNodeBeingPlaced.ConnectedNode != null))
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
