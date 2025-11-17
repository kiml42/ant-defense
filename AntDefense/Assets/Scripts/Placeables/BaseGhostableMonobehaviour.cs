using UnityEngine;

public abstract class BaseGhostableMonobehaviour : MonoBehaviour
{
    public abstract void Ghostify();
    public abstract void UnGhostify();
}

public abstract class SelectableGhostableMonoBehaviour : BaseGhostableMonobehaviour, ISelectableObject
{
    public abstract Vector3 Position { get; }

    // TODO use this to link the wall node with the connected placeable, both ways so that selecting or deselecting either affects both.
    public ISelectableObject ConnectedSelectable { get; set; }

    public bool IsSelected { get; protected set; }

    public virtual void Deselect()
    {
        if (!this.IsSelected) return;
        if (this.ConnectedSelectable != null)
        {
            this.ConnectedSelectable.Deselect();
        }
        this.IsSelected = false;
    }

    public virtual void Select()
    {
        if (this.IsSelected) return;
        if (this.ConnectedSelectable != null)
        {
            this.ConnectedSelectable.Select();
        }
        this.IsSelected = true;
    }
}
