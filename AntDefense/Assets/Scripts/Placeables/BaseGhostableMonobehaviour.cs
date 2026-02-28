using UnityEngine;

public abstract class BaseGhostableMonobehaviour : MonoBehaviour
{
    public abstract void Ghostify();
    public abstract void UnGhostify();
}

public abstract class SelectableGhostableMonoBehaviour : BaseGhostableMonobehaviour, ISelectableObject
{
    public abstract Vector3 Position { get; }

    /// <summary>
    /// Nullable so that either a select or deselect will always trigger the first time.
    /// </summary>
    private bool? _isSelected = null;
    public bool IsSelected => this._isSelected ?? false;

    /// <summary>
    /// Select only deffers up the chin of parents until it gets to the top-level selectable.
    /// Once the top level is found, it calls OnSelect on itself and all children.
    /// Only the top level selectable is registered with the TranslateHandle.
    /// </summary>
    /// <returns>The top-level selected object.</returns>
    public ISelectableObject Select()
    {
        var parents = this.GetComponentsInParent<SelectableGhostableMonoBehaviour>();
        foreach (var parent in parents)
        {
            if (parent != this)
            {
                // Defer selection to parent.
                return parent.Select();
            }
        }

        // this is the top-level selectable
        this.OnSelect();

        var children = this.GetComponentsInChildren<SelectableGhostableMonoBehaviour>();
        foreach (var child in children)
        {
            if (child != this)
            {
                child.OnSelect();
            }
        }
        return this;
    }

    public void Deselect()
    {
        var parents = this.GetComponentsInParent<SelectableGhostableMonoBehaviour>();
        foreach (var parent in parents)
        {
            if (parent != null && parent != this)
            {
                // Defer de-selection to parent
                parent.Deselect();
                return;
            }
        }

        // this is the top-level selectable
        this.OnDeselect();

        var children = this.GetComponentsInChildren<SelectableGhostableMonoBehaviour>();
        foreach (var child in children)
        {
            if (child != this)
            {
                child.OnDeselect();
            }
        }
    }

    /// <summary>
    /// Additional action to perform when this object is selected, or when this is a child object of the selected object.
    /// </summary>
    protected virtual void OnSelect()
    {
        this._isSelected = true;
    }

    /// <summary>
    /// Additional action to perform when this object is deselected, or when this is a child object of the deselected object.
    /// </summary>
    protected virtual void OnDeselect()
    {
        this._isSelected = false;
    }
}
