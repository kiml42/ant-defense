using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placed in ProceduralLevel.unity. On Start it pauses the game, shows the config panel,
/// and generates an initial layout. The player adjusts sliders and clicks Regenerate until
/// satisfied, then clicks Confirm to begin the match.
/// </summary>
public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject AntNestPrefab;
    public GameObject BiscuitPlatePrefab;
    public GameObject[] BerryBushPrefabs;
    public GameObject EnvironmentWallPrefab;
    [Tooltip("Material for the cluster ground polygon. Assign a slightly different shade of green.")]
    public Material ClusterGroundMaterial;

    [Header("Scene References")]
    [Tooltip("The ground plane collider that defines the maximum play area.")]
    public Collider GroundCollider;
    public AntCam CameraRig;
    public ProceduralLevelConfigUI ConfigUI;

    [Header("Generation Tuning")]
    public float MinNestSeparation = 35f;
    public float BoundaryMargin = 15f;
    public float PlateMargin = 20f;
    public float MinGapOffset = 8f;
    public float GapSize = 14f;
    public float ClusterRadius = 40f;
    [Tooltip("Fractional variation in cluster radius (0 = all same size, 0.5 = ±50% of ClusterRadius).")]
    public float ClusterRadiusVariation = 0.5f;
    [Tooltip("Gaps per unit of wall length. A 200-unit wall with 0.02 gets ~4 gaps; a 50-unit wall gets ~1.")]
    public float GapsPerUnitLength = 0.02f;
    [Tooltip("Minimum distance walls must keep from nests and the plate. A gap is forced wherever a wall comes closer than this.")]
    public float MinWallAvoidDistance = 20f;
    [Tooltip("Wall segments shorter than this are dropped; their space merges into the surrounding gap.")]
    public float MinWallSegmentLength = 10f;
    [Tooltip("Angular tolerance (degrees) for considering two walls parallel. Pair with MinParallelWallSeparation.")]
    public float MinParallelWallAngle = 25f;
    [Tooltip("Perpendicular distance below which two parallel walls are rejected and a new position is retried.")]
    public float MinParallelWallSeparation = 35f;
    public float WallConnectivityCellSize = 4f;
    [Tooltip("Clusters stay at least this far from nests and the plate.")]
    public float MinClusterAvoidDistance = 20f;
    [Tooltip("Minimum perpendicular distance that bush positions maintain from wall lines.")]
    public float WallSetback = 5f;
    [Tooltip("Minimum distance between individual bush positions within a cluster.")]
    public float MinBushSeparation = 5f;
    [Tooltip("Walls that would isolate a region (no nest or plate) smaller than this many cells are rejected. 0 disables the check.")]
    public int MinIsolatedRegionCells = 30;
    [Tooltip("Plate is never placed closer than this to any nest.")]
    public float MinNestPlateSeparation = 30f;
    [Tooltip("A blocking wall is placed between nest and plate when they are closer than this. Above this distance ants have a long enough route without one.")]
    public float BlockingWallThreshold = 100f;

    [Header("Camera Overview")]
    [Tooltip("Extra scale on the computed overview height so the play area sits comfortably inside the view.")]
    public float OverviewMargin = 1.15f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private ProceduralLevelConfig _config;
    private readonly List<GameObject> _generatedObjects = new List<GameObject>();
    private Rect _playArea;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        // CameraRig lives in BaseScene (loaded before this scene) — find it if not wired.
        if (CameraRig == null)
            CameraRig = FindFirstObjectByType<AntCam>();

        _playArea = ColliderToRect(GroundCollider);

        SetInitialCamera();

        FindFirstObjectByType<GlobalKeyHandler>()?.SetMode(GlobalKeyHandler.TimeScaleMode.Paused);

        _config = new ProceduralLevelConfig();
        ConfigUI.Initialise(this, _config);

        Generate(_config);
    }

    // ── Public API (called by ConfigUI) ───────────────────────────────────────

    /// <summary>Clears the current layout and generates a new one from <paramref name="config"/>.</summary>
    public void Generate(ProceduralLevelConfig config)
    {
        _config = config;
        ClearGenerated();

        var rng = new System.Random(config.Seed);

        var nestPositions = ProceduralPlacement.PlaceNests(
            config.NestCount, _playArea, MinNestSeparation, BoundaryMargin, rng);

        var platePos2D = ProceduralPlacement.PlacePlate(
            _playArea, PlateMargin, nestPositions, rng, minDistance: MinNestPlateSeparation);

        var allWalls = BuildWalls(nestPositions, platePos2D, config.WallDensity, rng);
        var regions = ProceduralPlacement.BuildRegions(allWalls, _playArea, WallConnectivityCellSize, nestPositions, platePos2D);

        var nestsParent  = CreateParent("Nests");
        var plateParent  = CreateParent("Plate");
        var wallsParent  = CreateParent("Walls");
        var clusterParent = CreateParent("Clusters");

        SpawnNests(nestPositions, nestsParent.transform);
        SpawnPlate(platePos2D, plateParent.transform);
        SpawnWalls(allWalls, wallsParent.transform);

        var avoidPositions = new List<Vector2>(nestPositions) { platePos2D };

        // Derive two independent sub-seeds from the main stream so that cluster centres
        // (driven by ClusterCount) and bush positions (driven by ClusterSize / ClusterRadius)
        // use separate RNG streams. Changing one slider no longer shifts the other.
        var centreRng = new System.Random(rng.Next());
        var bushRng   = new System.Random(rng.Next());

        var centres = ProceduralPlacement.PlaceClusterCentres(
            regions, config.ClusterCount, MinClusterAvoidDistance, avoidPositions, centreRng);
        var clusters = ProceduralPlacement.PlaceClusterBushes(
            centres, allWalls, ClusterRadius, ClusterRadiusVariation, WallSetback, config.ClusterDensity,
            MinBushSeparation, MinClusterAvoidDistance, avoidPositions, bushRng);
        SpawnClusters(clusters, rng, clusterParent.transform);
    }

    /// <summary>Increments the seed and regenerates.</summary>
    public void Regenerate(ProceduralLevelConfig config)
    {
        _config = config.WithIncrementedSeed();
        Generate(_config);
    }

    /// <summary>Returns the current config (may have a different seed after Regenerate calls).</summary>
    public ProceduralLevelConfig CurrentConfig => _config;

    /// <summary>Hides the config panel and resumes play.</summary>
    public void Confirm()
    {
        ConfigUI.Hide();
        FindFirstObjectByType<GlobalKeyHandler>()?.SetMode(GlobalKeyHandler.TimeScaleMode.Normal);
    }

    // ── Wall generation ───────────────────────────────────────────────────────

    private List<ProceduralPlacement.WallSegment> BuildWalls(
        List<Vector2> nestPositions, Vector2 platePos2D, int wallDensity, System.Random rng)
    {
        float wallWidth = EnvironmentWallPrefab != null
            ? EnvironmentWallPrefab.transform.localScale.z : 0f;

        return ProceduralPlacement.BuildWalls(
            nestPositions, platePos2D, _playArea, wallDensity,
            MinGapOffset, GapSize,
            GapsPerUnitLength, MinWallAvoidDistance, MinWallSegmentLength,
            MinParallelWallAngle, MinParallelWallSeparation,
            WallConnectivityCellSize, BlockingWallThreshold, rng,
            wallWidth, MinIsolatedRegionCells);
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private void SpawnNests(List<Vector2> positions, Transform parent)
    {
        foreach (var pos in positions)
            Track(Instantiate(AntNestPrefab, Xz(pos), Quaternion.identity, parent));
    }

    private void SpawnPlate(Vector2 pos, Transform parent)
    {
        Track(Instantiate(BiscuitPlatePrefab, Xz(pos), Quaternion.identity, parent));
    }

    private void SpawnWalls(List<ProceduralPlacement.WallSegment> walls, Transform parent)
    {
        foreach (var wall in walls)
        {
            var go = Instantiate(
                EnvironmentWallPrefab,
                Xz(wall.Centre),
                Quaternion.Euler(0f, -wall.AngleDegrees, 0f),
                parent);

            var s = go.transform.localScale;
            go.transform.localScale = new Vector3(wall.Length, s.y, s.z);
            Track(go);
        }
    }

    private void SpawnClusters(List<ProceduralPlacement.BushCluster> clusters, System.Random rng, Transform parent)
    {
        if (BerryBushPrefabs == null || BerryBushPrefabs.Length == 0) return;

        for (int i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            var clusterGO = CreateParent($"Cluster_{i}", parent);
            clusterGO.transform.position = Xz(cluster.Centre);

            SpawnClusterGround(cluster, clusterGO.transform);

            bool mixed = rng.NextDouble() > 0.5;
            var prefabA = BerryBushPrefabs[rng.Next(BerryBushPrefabs.Length)];
            var prefabB = mixed ? BerryBushPrefabs[rng.Next(BerryBushPrefabs.Length)] : prefabA;

            for (int j = 0; j < cluster.BushPositions.Count; j++)
            {
                var prefab = (j % 2 == 0 || !mixed) ? prefabA : prefabB;
                float yRot = (float)(rng.NextDouble() * 360.0);
                Track(Instantiate(prefab, Xz(cluster.BushPositions[j]), Quaternion.Euler(0f, yRot, 0f), clusterGO.transform));
            }
        }
    }

    private void SpawnClusterGround(ProceduralPlacement.BushCluster cluster, Transform parent)
    {
        if (ClusterGroundMaterial == null) return;
        var poly = cluster.BoundaryPolygon;
        if (poly == null || poly.Count < 3) return;

        var go = new GameObject("ClusterGround");
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;

        var localPoly = cluster.BoundaryPolygon.ConvertAll(p => p - cluster.Centre);
        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = BuildPolygonMesh(localPoly);

        var mr = go.AddComponent<MeshRenderer>();
        mr.material = ClusterGroundMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
    }

    private static Mesh BuildPolygonMesh(List<Vector2> poly)
    {
        var vertices = new Vector3[poly.Count];
        for (int i = 0; i < poly.Count; i++)
            vertices[i] = new Vector3(poly[i].x, 0.01f, poly[i].y);

        // Fan triangulation from vertex 0 — valid for convex polygons.
        var triangles = new int[(poly.Count - 2) * 3];
        for (int i = 0; i < poly.Count - 2; i++)
        {
            triangles[i * 3]     = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        var mesh = new Mesh();
        mesh.vertices  = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ClearGenerated()
    {
        foreach (var go in _generatedObjects)
            if (go != null) Destroy(go);
        _generatedObjects.Clear();
    }

    private GameObject CreateParent(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        Track(go);
        return go;
    }

    private void Track(GameObject go) => _generatedObjects.Add(go);

    private static Vector3 Xz(Vector2 pos) => new Vector3(pos.x, 0f, pos.y);

    private static Rect ColliderToRect(Collider col)
    {
        var b = col.bounds;
        return new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void SetInitialCamera()
    {
        if (CameraRig == null) return;

        var cam = CameraRig.Camera;
        float tanHalfFov = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float h = Mathf.Max(_playArea.height, _playArea.width / cam.aspect) / (2f * tanHalfFov) * OverviewMargin;

        var centre = _playArea.center;
        CameraRig.transform.position = new Vector3(centre.x, 0f, centre.y);
        cam.transform.localPosition = new Vector3(0f, h, 0f);
        cam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
