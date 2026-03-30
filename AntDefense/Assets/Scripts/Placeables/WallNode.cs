using Assets.Scripts;
using System;
using UnityEngine;

public class WallNode : PlaceableSelectableGhostableMonoBehaviour, IPlaceablePositionValidator, ISelectableObject
{
    public WallNode ConnectedNode;
    public Transform WallGhost;
    public Transform Node;
    public float MaxLength;

    public float CostPerMeter = 1f;
    private SelectableGhostableMonoBehaviour _child;

    public GameObject SectionPrefab;
    public GameObject StumpPrefab;
    /// <summary>
    /// Length of each wall section in world units. The total number of sections is floor(distance / SectionLength).
    /// Any remaining distance is covered by a stump at each node end.
    /// </summary>
    public float SectionLength = 1f;

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

    public override void OnBuildStart()
    {
        this.SpawnSections();
        this.enabled = false;   // disable to prevent UpdateWallGhost() asserting on the destroyed ghost
    }

    public override void OnPlace()
    {
        NoSpawnZone.Register(this);
    }

    private void SpawnSections()
    {
        if (this.ConnectedNode == null || this.SectionPrefab == null) return;

        Vector3 start = this.transform.position;
        Vector3 end = this.ConnectedNode.transform.position;
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance < 0.01f) return;

        int sectionCount = Mathf.FloorToInt(distance / this.SectionLength);
        if (sectionCount == 0) return;

        Vector3 dirNorm = direction.normalized;
        Quaternion rotation = Quaternion.LookRotation(dirNorm, Vector3.up);

        float halfGap = (distance - sectionCount * this.SectionLength) / 2f;

        for (int i = 0; i < sectionCount; i++)
        {
            Vector3 pos = start + (halfGap + (i + 0.5f) * this.SectionLength) * dirNorm;
            Instantiate(this.SectionPrefab, pos, rotation, this.transform);
        }

        if (halfGap > 0.001f && this.StumpPrefab != null)
        {
            var stump = Instantiate(this.StumpPrefab, start + (halfGap / 2f) * dirNorm, rotation, this.transform);
            stump.transform.localScale = new Vector3(1, 1, halfGap);
        }

        Debug.Log("Destroying the wall ghost");
        Destroy(this.WallGhost.gameObject);
    }

    internal void ConnectTo(WallNode other)
    {
        //Debug.Log("Connecting WallNode to " + other);
        this.ConnectedNode = other;
        this.UpdateWallGhost();
    }

    public bool PositionIsValid(Vector3 position)
    {
        return this.ConnectedNode == null || (position - this.ConnectedNode.transform.position).magnitude <= this.MaxLength + 0.1f;
    }

    private void Update()
    {
        this.UpdateWallGhost();
    }

    private void UpdateWallGhost()
    {
        //TODO - move the health bar to over the middle of the wall.
        Debug.Assert(this.WallGhost != null, "WallNode has no Wall Ghost assigned.");
        //Debug.Log("Updating WallNode. Connected to " + ConnectedNode);
        if (this.ConnectedNode != null)
        {
            var direction = this.ConnectedNode.transform.position - this.transform.position;
            if (direction.magnitude > 0.01f)
            {
                var midpoint = this.transform.position + (direction * 0.5f);
                this.WallGhost.position = midpoint;
                this.WallGhost.localScale = new Vector3(1, 1, direction.magnitude);
                this.WallGhost.rotation = Quaternion.LookRotation(direction, Vector3.up);
                return;
            }
        }
        this.WallGhost.localScale = Vector3.zero;
    }

    protected override void OnSelect()
    {
        base.OnSelect();

        //Debug.Log("WallNode selected: " + this);
        // TODO have a wall placing mode for placing walls, rather than just relying on selecting wall nodes.

        if (ObjectPlacer.Instance.CanBuildOnWall && this._child == null)
        {
            // Calculate wall duration before placing the child, so the turret isn't a child yet.
            float wallDuration = 0f;
            foreach (var anim in this.GetComponentsInChildren<BaseBuildAnimation>())
                wallDuration = Mathf.Max(wallDuration, anim.Duration);

            // The object can be placed on a wall, and this wall can have an object placed on top of it.
            // Place the object on this wall, and set this wall as the parent.
            var newObject = ObjectPlacer.Instance.PlaceObject(this);
            if(newObject != null)   // new object will be null if it can't be placed (e.g. too expensive)
            {
                this._child = newObject.GetComponent<SelectableGhostableMonoBehaviour>();
                newObject.WallParent = this;

                if (wallDuration > 0f)
                {
                    foreach (var anim in newObject.GetComponentsInChildren<BaseBuildAnimation>())
                        anim.AddStartDelay(wallDuration);

                    newObject.gameObject.AddComponent<GhostUntilBuildAnimationStart>();
                    foreach (var ghostable in newObject.GetComponentsInChildren<BaseGhostableMonobehaviour>(includeInactive: true))
                        ghostable.Ghostify();
                }

                this.Deselect();
            }
            return;
        }

        if (ObjectPlacer.Instance.WallNodeBeingPlaced != null
            && ObjectPlacer.Instance.WallNodeBeingPlaced.ConnectedNode == null)  // placing a disconnected wall node
        {
            ObjectPlacer.Instance.StartPlacingWallConnectedTo(this);
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
                ObjectPlacer.Instance.StartPlacingWallConnectedTo(this);
            }
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
    /// Called at the moment the object is placed (when the build animation begins).
    /// </summary>
    public virtual void OnBuildStart() { }

    /// <summary>
    /// Called when the object finishes being built (after the build animation completes).
    /// </summary>
    public abstract void OnPlace();
}
