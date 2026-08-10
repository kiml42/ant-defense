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
    public float ClusterRadius = 8f;
    public float AdditionalWallMinLength = 20f;
    public float AdditionalWallMaxLength = 50f;
    public float WallConnectivityCellSize = 4f;

    [Header("Camera Overview")]
    public float OverviewCameraHeight = 120f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private ProceduralLevelConfig _config;
    private readonly List<GameObject> _generatedObjects = new List<GameObject>();
    private Rect _playArea;

    // Camera state saved before overview
    private Vector3 _savedRigPosition;
    private Vector3 _savedCamLocalPosition;
    private Quaternion _savedCamLocalRotation;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        // CameraRig lives in BaseScene (loaded before this scene) — find it if not wired.
        if (CameraRig == null)
            CameraRig = FindFirstObjectByType<AntCam>();

        _playArea = ColliderToRect(GroundCollider);

        SaveAndSetOverviewCamera();

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
            _playArea, PlateMargin, nestPositions, rng);

        var allWalls = BuildWalls(nestPositions, platePos2D, config.WallDensity, rng);

        SpawnNests(nestPositions);
        SpawnPlate(platePos2D);
        SpawnWalls(allWalls);

        var clusters = ProceduralPlacement.PlaceClusters(
            config.ClusterCount, config.ClusterSize, _playArea, BoundaryMargin, nestPositions, rng);
        SpawnClusters(clusters, rng);
    }

    /// <summary>Increments the seed and regenerates.</summary>
    public void Regenerate(ProceduralLevelConfig config)
    {
        _config = config.WithIncrementedSeed();
        Generate(_config);
    }

    /// <summary>Returns the current config (may have a different seed after Regenerate calls).</summary>
    public ProceduralLevelConfig CurrentConfig => _config;

    /// <summary>Hides the config panel, restores the camera, and resumes play.</summary>
    public void Confirm()
    {
        ConfigUI.Hide();
        RestoreCamera();
        FindFirstObjectByType<GlobalKeyHandler>()?.SetMode(GlobalKeyHandler.TimeScaleMode.Normal);
    }

    // ── Wall generation ───────────────────────────────────────────────────────

    private List<ProceduralPlacement.WallSegment> BuildWalls(
        List<Vector2> nestPositions, Vector2 platePos2D, int wallDensity, System.Random rng)
    {
        var walls = new List<ProceduralPlacement.WallSegment>();

        // Blocking wall between each nest and the plate
        foreach (var nest in nestPositions)
        {
            List<ProceduralPlacement.WallSegment> candidate = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                candidate = ProceduralPlacement.BuildBlockingWall(
                    nest, platePos2D, _playArea, MinGapOffset, GapSize, rng);

                var combined = new List<ProceduralPlacement.WallSegment>(walls);
                combined.AddRange(candidate);

                if (ProceduralPlacement.IsConnected(
                    nestPositions, platePos2D, combined, _playArea, WallConnectivityCellSize))
                    break;

                // This placement blocked all paths — try a fresh wall with a new rng state
                candidate = null;
            }

            if (candidate != null)
                walls.AddRange(candidate);
        }

        // Extra ambient walls based on density slider
        var extra = ProceduralPlacement.BuildAdditionalWalls(
            wallDensity, _playArea, GapSize, AdditionalWallMinLength, AdditionalWallMaxLength, rng);

        // Only add extra walls that preserve connectivity
        foreach (var seg in extra)
        {
            var testList = new List<ProceduralPlacement.WallSegment>(walls) { seg };
            if (ProceduralPlacement.IsConnected(
                nestPositions, platePos2D, testList, _playArea, WallConnectivityCellSize))
                walls.Add(seg);
        }

        return walls;
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private void SpawnNests(List<Vector2> positions)
    {
        foreach (var pos in positions)
            Track(Instantiate(AntNestPrefab, Xz(pos), Quaternion.identity));
    }

    private void SpawnPlate(Vector2 pos)
    {
        Track(Instantiate(BiscuitPlatePrefab, Xz(pos), Quaternion.identity));
    }

    private void SpawnWalls(List<ProceduralPlacement.WallSegment> walls)
    {
        foreach (var wall in walls)
        {
            var go = Instantiate(
                EnvironmentWallPrefab,
                Xz(wall.Centre),
                Quaternion.Euler(0f, -wall.AngleDegrees, 0f));

            // Scale only the X (length) axis; height and depth are baked into the prefab.
            var s = go.transform.localScale;
            go.transform.localScale = new Vector3(wall.Length, s.y, s.z);
            Track(go);
        }
    }

    private void SpawnClusters(List<ProceduralPlacement.BushCluster> clusters, System.Random rng)
    {
        if (BerryBushPrefabs == null || BerryBushPrefabs.Length == 0) return;

        foreach (var cluster in clusters)
        {
            // Pick one or two prefabs for this cluster (monoculture or mixed)
            bool mixed = rng.NextDouble() > 0.5;
            var prefabA = BerryBushPrefabs[rng.Next(BerryBushPrefabs.Length)];
            var prefabB = mixed ? BerryBushPrefabs[rng.Next(BerryBushPrefabs.Length)] : prefabA;

            for (int i = 0; i < cluster.BushCount; i++)
            {
                var prefab = (i % 2 == 0 || !mixed) ? prefabA : prefabB;
                var offset = new Vector2(
                    (float)(rng.NextDouble() * 2 - 1) * ClusterRadius,
                    (float)(rng.NextDouble() * 2 - 1) * ClusterRadius);
                var pos = cluster.Centre + offset;
                float yRot = (float)(rng.NextDouble() * 360.0);
                Track(Instantiate(prefab, Xz(pos), Quaternion.Euler(0f, yRot, 0f)));
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ClearGenerated()
    {
        foreach (var go in _generatedObjects)
            if (go != null) Destroy(go);
        _generatedObjects.Clear();
    }

    private void Track(GameObject go) => _generatedObjects.Add(go);

    private static Vector3 Xz(Vector2 pos) => new Vector3(pos.x, 0f, pos.y);

    private static Rect ColliderToRect(Collider col)
    {
        var b = col.bounds;
        return new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void SaveAndSetOverviewCamera()
    {
        if (CameraRig == null) return;

        _savedRigPosition = CameraRig.transform.position;
        _savedCamLocalPosition = CameraRig.Camera.transform.localPosition;
        _savedCamLocalRotation = CameraRig.Camera.transform.localRotation;

        // Disable AntCam so player input doesn't move the camera during config
        CameraRig.enabled = false;

        // Centre the rig over the play area and raise the camera straight up
        var centre = _playArea.center;
        CameraRig.transform.position = new Vector3(centre.x, 0f, centre.y);
        CameraRig.Camera.transform.localPosition = new Vector3(0f, OverviewCameraHeight, 0f);
        CameraRig.Camera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void RestoreCamera()
    {
        if (CameraRig == null) return;

        CameraRig.transform.position = _savedRigPosition;
        CameraRig.Camera.transform.localPosition = _savedCamLocalPosition;
        CameraRig.Camera.transform.localRotation = _savedCamLocalRotation;
        CameraRig.enabled = true;
    }
}
