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

    public int MaxAntsPerSpawn = 1; 
    public float SpawnRadius = 1; 

    void Start()
    {
        if (AntParent == null)
        {
            AntParent = this.gameObject;
        }
    }

    void FixedUpdate()
    {
        _timeUntilSpawn -= Time.fixedDeltaTime;
        if(_timeUntilSpawn < 0)
        {
            var count = Random.Range(1, MaxAntsPerSpawn + 1);
            for (int i = 0; i < count; i++)
            {
                var position = (SpawnPoint?.position ?? this.transform.position) + Random.insideUnitSphere * SpawnRadius;
                var randomLookTarget = Random.insideUnitCircle;
                var rotation = Quaternion.LookRotation(new Vector3(randomLookTarget.x, 0, randomLookTarget.y), Vector3.up);
                var instance = Instantiate(this.AntPrefab, position, rotation, this.AntParent.transform);
                instance.GetComponent<Rigidbody>().velocity = SpawnVelocity;
            }
            _timeUntilSpawn = Random.Range(MinRespawnTime,MaxRespawnTime);
        }
    }
}
