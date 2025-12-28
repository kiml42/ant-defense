using UnityEngine;

public abstract class SingletonMonoBehaviour<TSelf> : MonoBehaviour where TSelf : SingletonMonoBehaviour<TSelf>
{
    public static TSelf Instance { get; private set; }

    private void Awake()
    {
        Debug.Assert(Instance == null || Instance == this, $"There should not be multiple {typeof(TSelf).Name}!");
        Debug.Assert(this is TSelf, $"{typeof(TSelf).Name} should only be attached to {typeof(TSelf).Name} instances!");
        Instance = (TSelf)this;
        Instance.OnAwake();
    }

    protected virtual void OnAwake()
    {
        // Do nothing by default.
    }
}
