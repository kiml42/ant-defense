using UnityEngine;

public abstract class BaseGhostableMonobehaviour : MonoBehaviour
{
    public abstract void Ghostify();
    public abstract void UnGhostify();
}

public abstract class SelectableGhostableMonoBehaviour : BaseGhostableMonobehaviour, ISelectableObject
{
    public abstract Vector3 Position { get; }

    public SelectableGhostableMonoBehaviour ConnectedSelectable { get; set; }

    public bool IsSelected { get; protected set; }

    public void Select()
    {
        if (this.IsSelected) return;
        this.IsSelected = true; // Make sure this is set before calling Select on the connected selectable to avoid infinite recursion.

        // TODO: make sure the turret and the wall each have the other registered as their connected selectable.
        if (this.ConnectedSelectable != null)
        {
            this.ConnectedSelectable.Select();
        }
        this.OnSelect();
    }

    public void Deselect()
    {
        if (!this.IsSelected) return;
        this.IsSelected = false;    // Make sure this is unset before calling Deselect on the connected selectable to avoid infinite recursion.

        if (this.ConnectedSelectable != null)
        {
            this.ConnectedSelectable.Deselect();
        }
        this.OnDeselect();
    }

    protected abstract void OnSelect();
    protected abstract void OnDeselect();
}
