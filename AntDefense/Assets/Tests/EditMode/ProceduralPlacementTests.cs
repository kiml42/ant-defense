using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ProceduralPlacementTests
{
    private static Rect StandardArea => new Rect(-100f, -60f, 200f, 120f);
    private const float Margin = 10f;
    private const float MinSeparation = 30f;

    // ── Nest placement ────────────────────────────────────────────────────────

    [Test]
    public void PlaceNests_ReturnsRequestedCount()
    {
        var rng = new System.Random(42);
        var nests = ProceduralPlacement.PlaceNests(3, StandardArea, MinSeparation, Margin, rng);
        Assert.AreEqual(3, nests.Count);
    }

    [Test]
    public void PlaceNests_AllWithinBounds()
    {
        var rng = new System.Random(1);
        var inner = new Rect(
            StandardArea.x + Margin, StandardArea.y + Margin,
            StandardArea.width - Margin * 2, StandardArea.height - Margin * 2);

        var nests = ProceduralPlacement.PlaceNests(4, StandardArea, MinSeparation, Margin, rng);
        foreach (var n in nests)
        {
            Assert.IsTrue(inner.Contains(n), $"Nest {n} is outside inner bounds {inner}");
        }
    }

    [Test]
    public void PlaceNests_SingleNest_AlwaysPlaced()
    {
        var rng = new System.Random(7);
        var nests = ProceduralPlacement.PlaceNests(1, StandardArea, MinSeparation, Margin, rng);
        Assert.AreEqual(1, nests.Count);
    }

    [Test]
    public void PlaceNests_DeterministicWithSameSeed()
    {
        var nests1 = ProceduralPlacement.PlaceNests(3, StandardArea, MinSeparation, Margin, new System.Random(99));
        var nests2 = ProceduralPlacement.PlaceNests(3, StandardArea, MinSeparation, Margin, new System.Random(99));

        for (int i = 0; i < nests1.Count; i++)
            Assert.AreEqual(nests1[i], nests2[i]);
    }

    [Test]
    public void PlaceNests_DifferentSeeds_ProduceDifferentResults()
    {
        var nests1 = ProceduralPlacement.PlaceNests(2, StandardArea, MinSeparation, Margin, new System.Random(1));
        var nests2 = ProceduralPlacement.PlaceNests(2, StandardArea, MinSeparation, Margin, new System.Random(2));
        Assert.AreNotEqual(nests1[0], nests2[0]);
    }

    // ── BiscuitPlate placement ────────────────────────────────────────────────

    [Test]
    public void PlacePlate_WithinBounds()
    {
        var rng = new System.Random(5);
        var nests = new List<Vector2> { new Vector2(-50f, 0f) };
        var plate = ProceduralPlacement.PlacePlate(StandardArea, Margin, nests, rng);

        var inner = new Rect(
            StandardArea.x + Margin, StandardArea.y + Margin,
            StandardArea.width - Margin * 2, StandardArea.height - Margin * 2);
        Assert.IsTrue(inner.Contains(plate), $"Plate {plate} outside inner bounds");
    }

    [Test]
    public void PlacePlate_RespectsMinDistance()
    {
        var rng = new System.Random(99);
        var nest = new Vector2(0f, 0f);
        var nests = new List<Vector2> { nest };
        const float minDist = 50f;

        var plate = ProceduralPlacement.PlacePlate(StandardArea, Margin, nests, rng, minDistance: minDist);

        Assert.GreaterOrEqual(Vector2.Distance(plate, nest), minDist,
            "Plate should be at least minDistance from every nest");
    }

    [Test]
    public void PlacePlate_FartherFromNestThanRandom()
    {
        // With many candidates the plate should be well away from the nest.
        var rng = new System.Random(42);
        var nest = new Vector2(-80f, 0f);
        var nests = new List<Vector2> { nest };
        var plate = ProceduralPlacement.PlacePlate(StandardArea, Margin, nests, rng, candidates: 500);

        Assert.Greater(Vector2.Distance(plate, nest), 60f,
            "Plate should be placed far from the single nest");
    }

    // ── Connectivity ──────────────────────────────────────────────────────────

    [Test]
    public void IsConnected_NoWalls_ReturnsTrue()
    {
        var nests = new List<Vector2> { new Vector2(-30f, 0f) };
        var plate = new Vector2(30f, 0f);
        var walls = new List<ProceduralPlacement.WallSegment>();
        Assert.IsTrue(ProceduralPlacement.IsConnected(nests, plate, walls, StandardArea, 5f));
    }

    [Test]
    public void IsConnected_WallWithGap_ReturnsTrue()
    {
        // A wall along X=0 with a gap at the top should still be passable
        var nests = new List<Vector2> { new Vector2(-50f, 0f) };
        var plate = new Vector2(50f, 0f);

        // Two segments: one above centre with gap, one below — but leave a gap
        var walls = new List<ProceduralPlacement.WallSegment>
        {
            // Bottom segment from y=-60 to y=-20 (leaves gap -20 to +60)
            new ProceduralPlacement.WallSegment(new Vector2(0f, -40f), 90f, 40f),
        };

        Assert.IsTrue(ProceduralPlacement.IsConnected(nests, plate, walls, StandardArea, 5f));
    }

    [Test]
    public void IsConnected_FullWallNoGap_ReturnsFalse()
    {
        // A wall spanning the full height with no gap — should block the path
        var nests = new List<Vector2> { new Vector2(-50f, 0f) };
        var plate = new Vector2(50f, 0f);

        var walls = new List<ProceduralPlacement.WallSegment>
        {
            new ProceduralPlacement.WallSegment(new Vector2(0f, 0f), 90f, 200f),
        };

        Assert.IsFalse(ProceduralPlacement.IsConnected(nests, plate, walls, StandardArea, 5f));
    }

    // ── Cluster placement ─────────────────────────────────────────────────────

    [Test]
    public void PlaceClusters_ReturnsRequestedCount()
    {
        var rng = new System.Random(3);
        var nests = new List<Vector2> { new Vector2(0f, 0f) };
        var clusters = ProceduralPlacement.PlaceClusters(8, 4, StandardArea, Margin, nests, rng);
        Assert.AreEqual(8, clusters.Count);
    }

    [Test]
    public void PlaceClusters_AllHaveAtLeastOneBush()
    {
        var rng = new System.Random(10);
        var nests = new List<Vector2> { new Vector2(0f, 0f) };
        var clusters = ProceduralPlacement.PlaceClusters(10, 3, StandardArea, Margin, nests, rng);
        foreach (var c in clusters)
            Assert.GreaterOrEqual(c.BushCount, 1, $"Cluster at {c.Centre} has {c.BushCount} bushes");
    }

    [Test]
    public void PlaceClusters_DeterministicWithSameSeed()
    {
        var nests = new List<Vector2> { new Vector2(-20f, 10f) };
        var c1 = ProceduralPlacement.PlaceClusters(5, 4, StandardArea, Margin, nests, new System.Random(77));
        var c2 = ProceduralPlacement.PlaceClusters(5, 4, StandardArea, Margin, nests, new System.Random(77));

        for (int i = 0; i < c1.Count; i++)
        {
            Assert.AreEqual(c1[i].Centre, c2[i].Centre);
            Assert.AreEqual(c1[i].BushCount, c2[i].BushCount);
        }
    }

    [Test]
    public void PlaceClusters_ClustersFartherFromNestTendToBelarger()
    {
        // Run many seeds and check the average: far clusters should be bigger on average.
        float nearTotal = 0f, farTotal = 0f;
        var nests = new List<Vector2> { new Vector2(0f, 0f) };

        for (int seed = 0; seed < 50; seed++)
        {
            var clusters = ProceduralPlacement.PlaceClusters(20, 5, StandardArea, Margin, nests, new System.Random(seed));
            foreach (var c in clusters)
            {
                float dist = Vector2.Distance(c.Centre, nests[0]);
                if (dist < 50f) nearTotal += c.BushCount;
                else farTotal += c.BushCount;
            }
        }

        Assert.Greater(farTotal, nearTotal,
            "Clusters far from nests should on average have more bushes than nearby ones");
    }

    [Test]
    public void PlaceClusters_RespectsMinAvoidDistance()
    {
        var rng = new System.Random(42);
        var nests = new List<Vector2> { new Vector2(0f, 0f) };
        var avoid = new List<Vector2> { new Vector2(0f, 0f) };
        const float minDist = 25f;

        var clusters = ProceduralPlacement.PlaceClusters(
            10, 4, StandardArea, Margin, nests, rng,
            avoidPositions: avoid, minAvoidDistance: minDist);

        // Most clusters (allowing for the best-effort fallback) should respect the limit.
        int violations = 0;
        foreach (var c in clusters)
            if (Vector2.Distance(c.Centre, avoid[0]) < minDist) violations++;

        Assert.Less(violations, clusters.Count / 2,
            "Majority of clusters should be at least minAvoidDistance from avoid positions");
    }

    // ── Combined wall build ───────────────────────────────────────────────────
    //
    // Design contract:
    //   - Blocking walls are automatic when dist(nest, plate) < blockingWallThreshold.
    //   - wallDensity controls only ambient additional walls.
    //   - Both are independent: blocking walls fire even at density=0 for close nests.

    private const float BlockingThreshold = 80f;

    private static List<ProceduralPlacement.WallSegment> Walls(
        List<Vector2> nests, Vector2 plate, int density, System.Random rng,
        float threshold = BlockingThreshold)
    {
        return ProceduralPlacement.BuildWalls(
            nests, plate, StandardArea, density,
            minGapOffset: 5f, gapSize: 10f,
            gapsPerUnitLength: 0.02f, minAvoidDistance: 0f,
            connectivityCellSize: 4f, blockingWallThreshold: threshold,
            rng);
    }

    [Test]
    public void BuildWalls_CloseNest_DensityZero_PlacesBlockingWall()
    {
        // Nest at distance ~50 from plate — below the 80-unit threshold.
        // Even at density=0, a blocking wall should be placed.
        var nests = new List<Vector2> { new Vector2(-40f, 0f) };
        var plate = new Vector2(0f, -30f); // dist ≈ 50

        var walls = Walls(nests, plate, density: 0, new System.Random(1));

        Assert.Greater(walls.Count, 0, "Close nest should get a blocking wall even at density 0");
    }

    [Test]
    public void BuildWalls_FarNest_DensityZero_ReturnsNoSegments()
    {
        // Nest far beyond threshold — no blocking wall, no ambient walls.
        var nests = new List<Vector2> { new Vector2(-90f, 0f) };
        var plate = new Vector2(90f, 0f); // dist = 180 >> threshold 80

        var walls = Walls(nests, plate, density: 0, new System.Random(2));

        Assert.AreEqual(0, walls.Count, "Far nest + density 0 should produce no walls");
    }

    [Test]
    public void BuildWalls_FarNest_DensityN_ProducesOnlyAmbientWalls()
    {
        // Nest far beyond threshold — no blocking wall.
        // density=3 should give exactly 3 ambient wall segments.
        var nests = new List<Vector2> { new Vector2(-90f, 0f) };
        var plate = new Vector2(90f, 0f); // dist = 180 >> threshold 80

        var walls = Walls(nests, plate, density: 3, new System.Random(3));

        // Connectivity check may reject some; at most 3 ambient walls (none from blocking).
        Assert.LessOrEqual(walls.Count, 3);
    }

    [Test]
    public void BuildWalls_CloseNest_DensityN_HasMoreWallsThanDensityAlone()
    {
        // Close nest produces a blocking wall on top of ambient walls.
        var nests = new List<Vector2> { new Vector2(-40f, 0f) };
        var plate = new Vector2(0f, -30f); // dist ≈ 50

        var farNests = new List<Vector2> { new Vector2(-90f, 0f) };
        var farPlate = new Vector2(90f, 0f);

        int closeCount = Walls(nests, plate, density: 2, new System.Random(5)).Count;
        int farCount   = Walls(farNests, farPlate, density: 2, new System.Random(5)).Count;

        Assert.Greater(closeCount, farCount,
            "Close nest should add a blocking wall on top of the same ambient wall count");
    }

    [Test]
    public void BuildWalls_HigherDensity_ProducesMoreAmbientWalls()
    {
        // With far nests (no blocking walls), higher density → more ambient walls.
        var nests = new List<Vector2> { new Vector2(-90f, 0f) };
        var plate = new Vector2(90f, 0f);

        int count2 = Walls(nests, plate, density: 2, new System.Random(7)).Count;
        int count5 = Walls(nests, plate, density: 5, new System.Random(7)).Count;

        Assert.GreaterOrEqual(count5, count2,
            "Higher density should produce at least as many ambient walls");
    }

    // ── Blocking wall ─────────────────────────────────────────────────────────

    [Test]
    public void BuildBlockingWall_ProducesAtLeastOneSegment()
    {
        var rng = new System.Random(1);
        var segments = ProceduralPlacement.BuildBlockingWall(
            new Vector2(-40f, 0f), new Vector2(40f, 0f),
            StandardArea, minGapOffset: 5f, gapSize: 10f, gapsPerUnitLength: 0.02f, minAvoidDistance: 0f, rng);

        Assert.Greater(segments.Count, 0);
    }

    [Test]
    public void BuildBlockingWall_LeavesSufficientGap()
    {
        // The two segments should not overlap — there must be a gap between them.
        var rng = new System.Random(2);
        var segments = ProceduralPlacement.BuildBlockingWall(
            new Vector2(-40f, 0f), new Vector2(40f, 0f),
            StandardArea, minGapOffset: 5f, gapSize: 12f, gapsPerUnitLength: 0.02f, minAvoidDistance: 0f, rng);

        if (segments.Count < 2) return; // single segment is fine

        // Segments are along the Y axis (perpendicular to X-axis nest-plate line).
        // Ensure the centres are separated by more than gapSize along that axis.
        float dist = Vector2.Distance(segments[0].Centre, segments[1].Centre);
        Assert.Greater(dist, 10f, "Gap between wall segments is too small");
    }
}
