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

    /// <summary>
    /// Nullable so that either a select or deselect will always trigger the first time.
    /// </summary>
    private bool? _isSelected = null;
    public bool IsSelected => this._isSelected ?? false;

    public void Select()
    {
        this.OnSelect();
        TranslateHandle.Instance.SetSelectedObject(this, false);
        if (this._isSelected == true) return;
        this._isSelected = true; // Make sure this is set before calling Select on the connected selectable to avoid infinite recursion.

        // TODO: make sure the turret and the wall each have the other registered as their connected selectable.
        if (this.ConnectedSelectable != null)
        {
            this.ConnectedSelectable.Select();
        }
    }

    public void Deselect()
    {
        this.OnDeselect();
        if (this.IsSelected == false) return;
        this._isSelected = false;    // Make sure this is unset before calling Deselect on the connected selectable to avoid infinite recursion.

        if (this.ConnectedSelectable != null)
        {
            this.ConnectedSelectable.Deselect();
        }
    }

    protected abstract void OnSelect();
    protected abstract void OnDeselect();
}
