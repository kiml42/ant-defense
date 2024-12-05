using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AntNest : MonoBehaviour
{
    public GameObject AntParent;
    public Transform SpawnPoint;
    public Vector3 SpawnVelocity = Vector3.zero;

    public float MinRespawnTime = 1;
    public float MaxRespawnTime = 5;

    public List<FoodCost> AntPrefabs;

    public int AntsPerSpawn = 5;
    public float SpawnRadius = 1;

    public float CurrentFood { get { return Digestion.CurrentFood; } }

    // TODO consider multiple ants
    private float costEachSpawn => AntPrefabs.First().Cost * AntsPerSpawn;

    public Digestion Digestion;

    /// <summary>
    /// Amount of food to hold on to so the nest doesn't starve.
    /// </summary>
    public float ReserveFood = 20f;

    void Start()
    {
        if (AntParent == null)
        {
            AntParent = this.gameObject;
        }
    }

    void FixedUpdate()
    {
        var availableFood = CurrentFood - ReserveFood;
        if(availableFood >= costEachSpawn)
        {
            for (int i = 0; i < AntsPerSpawn; i++)
            {
                var position = (SpawnPoint?.position ?? this.transform.position) + Random.insideUnitSphere * SpawnRadius;
                var randomLookTarget = Random.insideUnitCircle;
                var rotation = Quaternion.LookRotation(new Vector3(randomLookTarget.x, 0, randomLookTarget.y), Vector3.up);

                var prefab = AntPrefabs[i % AntPrefabs.Count];
                var instance = Instantiate(prefab.transform, position, rotation, this.AntParent.transform);
                Digestion.UseFood(prefab.Cost);
                
                instance.GetComponent<Rigidbody>().velocity = SpawnVelocity;
            }
        }
    }

    internal void UseFood(float foodToEat)
    {
        Digestion.UseFood(foodToEat);
    }

    internal void AddFood(float foodValue)
    {
        Digestion.AddFood(foodValue);
    }
}
