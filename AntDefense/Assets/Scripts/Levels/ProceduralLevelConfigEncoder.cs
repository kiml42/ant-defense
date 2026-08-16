using System.Collections.Generic;

public static class ProceduralLevelConfigEncoder
{
    private const string CurrentVersion = "v2";

    public static string Encode(ProceduralLevelConfig config) =>
        $"{CurrentVersion}:nests={config.NestCount},clusters={config.ClusterCount},density={config.ClusterDensity},walls={config.WallDensity},seed={config.Seed}";

    public static bool TryDecode(string s, out ProceduralLevelConfig config)
    {
        config = null;
        if (string.IsNullOrEmpty(s)) return false;

        var colonIdx = s.IndexOf(':');
        if (colonIdx < 0) return false;

        var version = s.Substring(0, colonIdx);
        if (version != CurrentVersion) return false;

        var body = s.Substring(colonIdx + 1);
        var pairs = body.Split(',');
        var dict = new Dictionary<string, string>();
        foreach (var pair in pairs)
        {
            var eqIdx = pair.IndexOf('=');
            if (eqIdx < 0) return false;
            dict[pair.Substring(0, eqIdx).Trim()] = pair.Substring(eqIdx + 1).Trim();
        }

        if (!TryGet(dict, "nests", out int nests)) return false;
        if (!TryGet(dict, "clusters", out int clusters)) return false;
        if (!TryGet(dict, "density", out int density)) return false;
        if (!TryGet(dict, "walls", out int walls)) return false;
        if (!TryGet(dict, "seed", out int seed)) return false;

        config = new ProceduralLevelConfig(nests, clusters, density, walls, seed);
        return true;
    }

    private static bool TryGet(Dictionary<string, string> dict, string key, out int value)
    {
        value = 0;
        return dict.TryGetValue(key, out var raw) && int.TryParse(raw, out value);
    }
}
