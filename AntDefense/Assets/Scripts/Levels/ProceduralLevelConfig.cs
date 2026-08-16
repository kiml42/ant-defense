using System;

[Serializable]
public class ProceduralLevelConfig
{
    public int NestCount = 2;
    public int ClusterCount = 6;
    public int ClusterDensity = 4;
    public int WallDensity = 3;
    public int Seed = 1;

    public ProceduralLevelConfig() { }

    public ProceduralLevelConfig(int nestCount, int clusterCount, int clusterDensity, int wallDensity, int seed)
    {
        NestCount = nestCount;
        ClusterCount = clusterCount;
        ClusterDensity = clusterDensity;
        WallDensity = wallDensity;
        Seed = seed;
    }

    public ProceduralLevelConfig WithSeed(int seed) =>
        new ProceduralLevelConfig(NestCount, ClusterCount, ClusterDensity, WallDensity, seed);

    public ProceduralLevelConfig WithIncrementedSeed() => WithSeed(Seed + 1);
}
