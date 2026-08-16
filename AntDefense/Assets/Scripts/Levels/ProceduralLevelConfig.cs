using System;

[Serializable]
public class ProceduralLevelConfig
{
    public int NestCount = 2;
    public int ClusterCount = 6;
    public int ClusterRadius = 40;
    public int ClusterDensity = 4;
    public int WallDensity = 3;
    public int Seed = 1;

    public ProceduralLevelConfig() { }

    public ProceduralLevelConfig(int nestCount, int clusterCount, int clusterRadius, int clusterDensity, int wallDensity, int seed)
    {
        NestCount = nestCount;
        ClusterCount = clusterCount;
        ClusterRadius = clusterRadius;
        ClusterDensity = clusterDensity;
        WallDensity = wallDensity;
        Seed = seed;
    }

    public ProceduralLevelConfig WithSeed(int seed) =>
        new ProceduralLevelConfig(NestCount, ClusterCount, ClusterRadius, ClusterDensity, WallDensity, seed);

    public ProceduralLevelConfig WithIncrementedSeed() => WithSeed(Seed + 1);
}
