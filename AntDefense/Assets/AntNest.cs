using UnityEngine;

public class AntNest : MonoBehaviour
{
    public GameObject AntParent;
    public Transform SpawnPoint;
    public Vector3 SpawnVelocity = Vector3.zero;

    public float MinRespawnTime = 1;
    public float MaxRespawnTime = 5;

    public Transform AntPrefab;

    private float _timeUntilSpawn;

    void Start()
    {
        if (AntParent == null)
        {
            AntParent = this.gameObject;
        }
    }

    void FixedUpdate()
    {
        _timeUntilSpawn -= Time.deltaTime;
        if(_timeUntilSpawn < 0)
        {
            var instance = Instantiate(AntPrefab, SpawnPoint?.position ?? this.transform.position, Quaternion.identity, AntParent.transform);
            instance.GetComponent<Rigidbody>().velocity = SpawnVelocity;
            instance.rotation = Random.rotation;
            _timeUntilSpawn = Random.Range(MinRespawnTime,MaxRespawnTime);
        }
    }
}
