using UnityEngine;

public class NoSpawnZone : MonoBehaviour
{
    public float Radius = 3;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var collider = this.GetComponent<SphereCollider>();
        if (collider != null)
        {
            this.Radius = collider.radius;
        }
    }
}
