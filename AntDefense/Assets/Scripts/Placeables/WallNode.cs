using System;
using System.Linq;
using UnityEngine;

public class WallNode : PlaceableMonoBehaviour, IPlaceablePositionValidator, IInteractivePosition, ISelectableObject
{
    public WallNode ConnectedNode;
    public Transform Wall;
    public Transform Node;
    public float MaxLength;

    public float CostPerMeter = 1f;

    private ISelectableObject[] _selectionDelegates;

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
        TranslateHandle.Instance.SetSelectedObject(this);

        if (ObjectPlacer.Instance.WallNodeBeingPlaced != null && ObjectPlacer.Instance.WallNodeBeingPlaced.ConnectedNode != null)
        {
            // is currently placing a wall node
            // the wall node that is being placed is already connected to another node
            // So place the new wall node at this location, for the connected wall to be placed correctly, but then hide the new node because it overlaps this node.
            var placedObject = ObjectPlacer.Instance.PlaceObject();
            placedObject.GetComponent<WallNode>().RemoveNode();
        }
        else if (ObjectPlacer.Instance.CanBuildOnWall && this._selectionDelegates == null)
        {
            // The object can be placed on a wall, and this wall can have an object placed on top of it.
            // Place the object on this wall, and set this wall as the parent.
            var newObject = ObjectPlacer.Instance.PlaceObject(this.transform);
            var selectables = newObject.GetComponents(typeof(ISelectableObject)).Cast<ISelectableObject>().ToArray();

            this._selectionDelegates = selectables;
            this.UpdateSelectionStateForDelegates();
        }
        else
        {
            // not currently placing a wall node, or the wall node being placed is not yet connected to another node, so start placing a new wall node connected to this one.
            ObjectPlacer.Instance.StartPlacingWallConnectedTo(this);
        }
    }

    private void UpdateSelectionStateForDelegates()
    {
        if (this._selectionDelegates == null) return;
        foreach (var item in this._selectionDelegates)
        {
            if (this._isSelected)
                item.Select();
            else
                item.Deselect();
        }
    }

    private bool _isSelected = false;

    public void Select()
    {
        this._isSelected = true;
        Debug.Log("WallNode selected: " + this);
        // TODO make selecting the wall node be the trigger for starting to place a wall node connected to this one.
        // TODO have a wall placing mode for placing walls, rather than just relying on selecting wall nodes.
        this.UpdateSelectionStateForDelegates();
    }

    public void Deselect()
    {
        this._isSelected = false;
        this.UpdateSelectionStateForDelegates();
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
