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

    public float CurrentFood { get { return this.Digestion.CurrentFood; } }

    // TODO consider multiple ants
    private float costEachSpawn => this.AntPrefabs.First().Cost * this.AntsPerSpawn;

    public Digestion Digestion;

    /// <summary>
    /// Amount of food to hold on to so the nest doesn't starve.
    /// </summary>
    public float ReserveFood = 20f;

    void Start()
    {
        if (this.AntParent == null)
        {
            this.AntParent = this.gameObject;
        }
    }

    void FixedUpdate()
    {
        var availableFood = this.CurrentFood - this.ReserveFood;
        if(availableFood >= this.costEachSpawn)
        {
            for (int i = 0; i < this.AntsPerSpawn; i++)
            {
                var position = (this.SpawnPoint?.position ?? this.transform.position) + (Random.insideUnitSphere * this.SpawnRadius);
                var randomLookTarget = Random.insideUnitCircle;
                var rotation = Quaternion.LookRotation(new Vector3(randomLookTarget.x, 0, randomLookTarget.y), Vector3.up);

                var prefab = this.AntPrefabs[i % this.AntPrefabs.Count];
                var instance = Instantiate(prefab.transform, position, rotation, this.AntParent.transform);
                this.Digestion.UseFood(prefab.Cost);
                
                instance.GetComponent<Rigidbody>().linearVelocity = this.SpawnVelocity;
            }
        }
    }

    internal void UseFood(float foodToEat)
    {
        this.Digestion.UseFood(foodToEat);
    }

    internal void AddFood(float foodValue)
    {
        this.Digestion.AddFood(foodValue);
    }
}
