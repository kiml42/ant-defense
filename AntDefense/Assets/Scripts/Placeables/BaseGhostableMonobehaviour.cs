using UnityEngine;

public abstract class BaseGhostableMonobehaviour : MonoBehaviour
{
    public abstract void Ghostify();
    public abstract void UnGhostify();
}

public abstract class SelectableGhostableMonoBehaviour : BaseGhostableMonobehaviour, ISelectableObject
{
    public abstract Vector3 Position { get; }

    public abstract void Deselect();
    public abstract void Select();
}
