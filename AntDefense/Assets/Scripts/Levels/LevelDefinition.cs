using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Ant Defense/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    public string LevelName = "New Level";

    [Header("Player Resources")]
    public float StartingMoney = 100f;
    public float IncomePerSecond = 0.1f;

    [Header("Map Objects")]
    public List<SpawnEntry> MapObjects;

    [Header("Ant Nests")]
    public List<NestConfig> Nests;
}

[Serializable]
public class SpawnEntry
{
    public Object Prefab;
    public Vector3 Position;
    public Vector3 EulerRotation;
}

[Serializable]
public class NestConfig
{
    public Vector3 Position;

    [Header("Spawning")]
    public int MaxAnts = 100;
    public int AntsPerSpawn = 5;
    public float MinRespawnTime = 1f;
    public float MaxRespawnTime = 5f;

    [Header("Food")]
    public float ReserveFood = 20f;
    public float MaxFood = 200f;
    public float StartFood = 50f;
    public float FoodExpenditure = 0.1f;
}
