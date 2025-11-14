using UnityEngine;

public interface IInteractivePosition : IKnowsPosition
{
    void Interact();
}

public interface  IKnowsPosition
{
    Vector3 Position { get; }
}
