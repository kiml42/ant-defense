using UnityEngine;

public class ContinualSpawner : MonoBehaviour
{
    public GameObject ParentForSpawnedObjects;
    public Transform PrefabToSpawn;
    public Transform DefaultSpawnPoint;

    public Vector3 SpawnPositionRandomisation;

    public float FirstSpawnCount = 1;
    public float CountPerSpawn = 1;

    /// <summary>
    /// If negative, no limit on the number of spawns
    /// </summary>
    public float MaxSpawns = -1;
    private float _spawnCount = 0;
    public float MinRespawnTime = 1;
    public float MaxRespawnTime = 5;

    private float _timeUntilSpawn = 0;

    void Start()
    {
        if (this.ParentForSpawnedObjects == null)
        {
            this.ParentForSpawnedObjects = this.gameObject;
        }
        this.SpawnObjects(this.FirstSpawnCount);
    }

    void FixedUpdate()
    {
        if (this.MaxSpawns >= 0 && this._spawnCount >= this.MaxSpawns)
        {
            this.enabled = false;
            return;
        }
        this._timeUntilSpawn -= Time.fixedDeltaTime;
        if (this._timeUntilSpawn < 0)
        {
            this.SpawnObjects(this.CountPerSpawn);
        }
    }

    private void SpawnObjects(float count)
    {
        for (int i = 0; i < count; i++)
        {
            var randomisation = Random.insideUnitSphere;
            randomisation.Scale(this.SpawnPositionRandomisation);

            var position = (this.DefaultSpawnPoint?.position ?? this.transform.position) + new Vector3(randomisation.x, randomisation.y, randomisation.z);

            var orientation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            Instantiate(this.PrefabToSpawn, position, orientation, this.ParentForSpawnedObjects.transform);
        }
        this._timeUntilSpawn = Random.Range(this.MinRespawnTime, this.MaxRespawnTime);
        this._spawnCount++;
    }
}
