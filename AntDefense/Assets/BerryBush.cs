using UnityEngine;

public class BerryBush : MonoBehaviour
{
    public GameObject BerryParent;
    public Transform Berry;
    public Transform DefaultSpawnPoint;

    public float SpawnRadius;

    public float MinRespawnTime = 1;
    public float MaxRespawnTime = 5;

    private float _timeUntilSpawn;

    void Start()
    {
        if (BerryParent == null)
        {
            BerryParent = this.gameObject;
        }
    }

    void FixedUpdate()
    {
        _timeUntilSpawn -= Time.deltaTime;
        if (_timeUntilSpawn < 0)
        {
            var randomisation = Random.insideUnitCircle * SpawnRadius;
            var position = (DefaultSpawnPoint?.position ?? this.transform.position) + new Vector3(randomisation.x, 0, randomisation.y);
            Instantiate(Berry, position, Quaternion.identity, BerryParent.transform);
            _timeUntilSpawn = Random.Range(MinRespawnTime, MaxRespawnTime);
        }
    }
}
