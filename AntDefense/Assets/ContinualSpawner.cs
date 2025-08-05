using UnityEngine;

public class ContinualSpawner : MonoBehaviour
{
    public GameObject ParentForSpawnedObjects;
    public Transform PrefabToSpawn;
    public Transform DefaultSpawnPoint;

    public Vector3 SpawnPositionRandomisation;

    public float CountPerSpawn = 1;

    public float MinRespawnTime = 1;
    public float MaxRespawnTime = 5;

    private float _timeUntilSpawn = 0;

    void Start()
    {
        if (ParentForSpawnedObjects == null)
        {
            ParentForSpawnedObjects = this.gameObject;
        }
    }

    void FixedUpdate()
    {
        _timeUntilSpawn -= Time.fixedDeltaTime;
        if (_timeUntilSpawn < 0)
        {
            for (int i = 0; i < CountPerSpawn; i++)
            {
                var randomisation = Random.insideUnitSphere;
                randomisation.Scale(SpawnPositionRandomisation);

                var position = (DefaultSpawnPoint?.position ?? this.transform.position) + new Vector3(randomisation.x, randomisation.y, randomisation.z);
                Instantiate(PrefabToSpawn, position, Quaternion.identity, ParentForSpawnedObjects.transform);
            }
            _timeUntilSpawn = Random.Range(MinRespawnTime, MaxRespawnTime);
        }
    }
}
