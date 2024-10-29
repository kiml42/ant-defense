using UnityEngine;

public class AntNest : Smellable
{
    public GameObject AntParent;
    public Transform SpawnPoint;
    public Vector3 SpawnVelocity = Vector3.zero;

    public float MinRespawnTime = 1;
    public float MaxRespawnTime = 5;

    public Transform AntPrefab;

    public override Smell Smell => Smell.Home;
    public override float TimeFromTarget => 0;
    public override bool IsActual => true;

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
        _timeUntilSpawn -= Time.deltaTime;
        if(_timeUntilSpawn < 0)
        {
            var count = Random.Range(1, MaxAntsPerSpawn + 1);
            for (int i = 0; i < count; i++)
            {
                var position = (SpawnPoint?.position ?? this.transform.position) + Random.insideUnitSphere * SpawnRadius;
                var instance = Instantiate(this.AntPrefab, position, Random.rotation, this.AntParent.transform);
                instance.GetComponent<Rigidbody>().velocity = SpawnVelocity;
            }
            _timeUntilSpawn = Random.Range(MinRespawnTime,MaxRespawnTime);
        }
    }
}
