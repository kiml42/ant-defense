using UnityEngine;

/// <summary>
/// Reads a LevelDefinition asset at scene start and sets up the map:
/// spawns map objects, instantiates ant nests with configured parameters,
/// and applies player resource settings.
/// </summary>
public class LevelLoader : MonoBehaviour
{
    public LevelDefinition Level;

    [Header("Nest Setup")]
    [Tooltip("Prefab used to instantiate ant nests. Must have AntNest and Digestion components.")]
    public GameObject NestPrefab;

    void Awake()
    {
        if (Level == null)
        {
            Debug.LogWarning("LevelLoader: No LevelDefinition assigned.");
            return;
        }

        ApplyPlayerResources();
        SpawnMapObjects();
        SpawnNests();
    }

    private void ApplyPlayerResources()
    {
        if (MoneyTracker.Instance != null)
        {
            MoneyTracker.Instance.InitialMoney = Level.StartingMoney;
            MoneyTracker.Instance.IncomePerSecond = Level.IncomePerSecond;
        }
    }

    private void SpawnMapObjects()
    {
        foreach (var entry in Level.MapObjects)
        {
            if (entry.Prefab == null)
            {
                Debug.LogWarning("LevelLoader: SpawnEntry has null prefab, skipping.");
                continue;
            }
            var rotation = Quaternion.Euler(entry.EulerRotation);
            Instantiate(entry.Prefab, entry.Position, rotation);
        }
    }

    private void SpawnNests()
    {
        if (NestPrefab == null)
        {
            Debug.LogWarning("LevelLoader: NestPrefab not assigned, cannot spawn nests.");
            return;
        }

        foreach (var config in Level.Nests)
        {
            var nestObj = Instantiate(NestPrefab, config.Position, Quaternion.identity);

            var nest = nestObj.GetComponent<AntNest>();
            if (nest != null)
            {
                nest.MaxAnts = config.MaxAnts;
                nest.AntsPerSpawn = config.AntsPerSpawn;
                nest.MinRespawnTime = config.MinRespawnTime;
                nest.MaxRespawnTime = config.MaxRespawnTime;
                nest.ReserveFood = config.ReserveFood;
            }

            var digestion = nestObj.GetComponent<Digestion>();
            if (digestion != null)
            {
                digestion.MaxFood = config.MaxFood;
                digestion.StartFood = config.StartFood;
                digestion.Expenditure = config.FoodExpenditure;
            }
        }
    }
}
