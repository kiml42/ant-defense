using UnityEngine;

public class AntTargetPositionProvider : MonoBehaviour, ITargetPositionProvider
{
    public float ForwardsBias = 0.1f;
    public float RandomBias = 0.2f;

    /// <summary>
    /// Target position relative to this ant
    /// </summary>
    private Vector3 _targetPosition = new Vector3(10, 0, 20);

    public Vector3 TargetPosition => TargetObject?.position ?? transform.position + _targetPosition;

    public Transform TargetObject { get; set; }

    void Start()
    {
    }

    void FixedUpdate()
    {
        if(TargetObject == null)
        {
            _targetPosition += Random.insideUnitSphere * RandomBias + transform.forward * ForwardsBias;
            _targetPosition.y *= 0.1f;
            _targetPosition.Normalize();
        }
        else
        {
            _targetPosition = TargetObject.position;
        }
    }
}

public interface ITargetPositionProvider
{
    Transform TargetObject { get; set; }
    Vector3 TargetPosition { get; }
}
