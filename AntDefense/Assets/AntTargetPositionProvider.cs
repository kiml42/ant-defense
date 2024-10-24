using UnityEngine;

public class AntTargetPositionProvider : MonoBehaviour, ITargetPositionProvider
{
    private Vector3 _targetPosition = new Vector3(10, 0, 20);

    public Vector3 TargetPosition => _targetPosition;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _targetPosition += Random.insideUnitSphere * Time.deltaTime;
    }
}

public interface ITargetPositionProvider
{
    Vector3 TargetPosition { get; }
}
