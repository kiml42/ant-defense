using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AntNest : MonoBehaviour
{
    private static GameObject antParent;
    public Transform SpawnPoint;
    public Vector3 SpawnVelocity = Vector3.zero;

    public float MinRespawnTime = 1;
    public float MaxRespawnTime = 5;

    public List<FoodCost> AntPrefabs;
    public List<float> TargetProportions;

    public int AntsPerSpawn = 5;
    public float SpawnRadius = 1;

    public float CurrentFood { get { return this.Digestion.CurrentFood; } }

    // TODO consider multiple ants
    private float CostEachSpawn => this.AntPrefabs.First().Cost * this.AntsPerSpawn;

    public Digestion Digestion;

    /// <summary>
    /// Amount of food to hold on to so the nest doesn't starve.
    /// </summary>
    public float ReserveFood = 20f;

    /// <summary>
    /// The maximum number of ants allowed to exist at one time.
    /// </summary>
    public int MaxAnts = 100;

    void Start()
    {
        if (antParent == null)
        {
            antParent = new GameObject("Ants");
        }
        this.spawnsByPrefabIndex = new int[this.AntPrefabs.Count];
    }

    private int[] spawnsByPrefabIndex;

    void FixedUpdate()
    {
        var availableFood = this.CurrentFood - this.ReserveFood;
        if(availableFood >= this.CostEachSpawn)
        {
            for (int i = 0; i < this.AntsPerSpawn; i++)
            {
                if (antParent.transform.childCount >= this.MaxAnts)
                {
                    break;
                }
                var position = this.SpawnPoint.position + (Random.insideUnitSphere * this.SpawnRadius);
                var randomLookTarget = Random.insideUnitCircle;
                var rotation = Quaternion.LookRotation(new Vector3(randomLookTarget.x, 0, randomLookTarget.y), Vector3.up);
                var prefab = this.PickPrefab();
                var instance = Instantiate(prefab.transform, position, rotation, antParent.transform);
                instance.tag = this.tag;
                this.Digestion.UseFood(prefab.Cost);

                instance.GetComponent<Rigidbody>().linearVelocity = this.SpawnVelocity;
            }
        }
    }

    private FoodCost PickPrefab()
    {
        var totalSpawned = this.spawnsByPrefabIndex.Sum();
        var bestIndex = 0;

        
        if (totalSpawned != 0)
        {
            var worstDefecit = float.MinValue;
            for (var i = 0; i < this.AntPrefabs.Count; i++)
            {
                var actualProportion = (float)this.spawnsByPrefabIndex[i] / totalSpawned;
                var defecit = this.TargetProportions[i] - actualProportion;
                //Debug.Log($"AntNest: Prefab {i} actual proportion {actualProportion}, target {this.TargetProportions[i]}, defecit {defecit}");
                if (defecit > worstDefecit)
                {
                    worstDefecit = defecit;
                    bestIndex = i;
                }
            }
        }

        //Debug.Log($"AntNest: Picking prefab index {bestIndex}");
        this.spawnsByPrefabIndex[bestIndex]++;
        return this.AntPrefabs[bestIndex];
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
