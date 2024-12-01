using UnityEngine;

public class AntNest : MonoBehaviour
{
    public GameObject AntParent;
    public Transform SpawnPoint;
    public Vector3 SpawnVelocity = Vector3.zero;

    public float MinRespawnTime = 1;
    public float MaxRespawnTime = 5;

    public FoodCost AntPrefab;

    public int AntsPerSpawn = 5;
    public float SpawnRadius = 1;

    public float CurrentFood = 100;
    public float FoodGeneration = 0.1f;

    private float costEachSpawn => AntPrefab.Cost * AntsPerSpawn; 

    void Start()
    {
        if (AntParent == null)
        {
            AntParent = this.gameObject;
        }
    }

    void FixedUpdate()
    {
        CurrentFood += FoodGeneration * Time.fixedDeltaTime;
        if(CurrentFood >= costEachSpawn)
        {
            for (int i = 0; i < AntsPerSpawn; i++)
            {
                var position = (SpawnPoint?.position ?? this.transform.position) + Random.insideUnitSphere * SpawnRadius;
                var randomLookTarget = Random.insideUnitCircle;
                var rotation = Quaternion.LookRotation(new Vector3(randomLookTarget.x, 0, randomLookTarget.y), Vector3.up);
                var instance = Instantiate(this.AntPrefab.transform, position, rotation, this.AntParent.transform);
                CurrentFood -= this.AntPrefab.Cost;
                instance.GetComponent<Rigidbody>().velocity = SpawnVelocity;
            }
        }
    }
}
