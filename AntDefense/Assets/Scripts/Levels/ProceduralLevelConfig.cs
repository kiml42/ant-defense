using System;

[Serializable]
public class ProceduralLevelConfig
{
    public int NestCount = 2;
    public int ClusterCount = 6;
    public int ClusterSize = 4;
    public int WallDensity = 3;
    public int Seed = 1;

    public ProceduralLevelConfig() { }

    public ProceduralLevelConfig(int nestCount, int clusterCount, int clusterSize, int wallDensity, int seed)
    {
        NestCount = nestCount;
        ClusterCount = clusterCount;
        ClusterSize = clusterSize;
        WallDensity = wallDensity;
        Seed = seed;
    }

    public ProceduralLevelConfig WithSeed(int seed) =>
        new ProceduralLevelConfig(NestCount, ClusterCount, ClusterSize, WallDensity, seed);

    public ProceduralLevelConfig WithIncrementedSeed() => WithSeed(Seed + 1);
}
