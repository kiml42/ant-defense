using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure static helpers for procedural level generation.
/// No MonoBehaviour or scene dependencies — fully testable in EditMode.
/// </summary>
public static class ProceduralPlacement
{
    // ── Nest placement ────────────────────────────────────────────────────────

    /// <summary>
    /// Places <paramref name="count"/> nests inside <paramref name="area"/>, attempting to keep
    /// them at least <paramref name="minSeparation"/> apart and at least <paramref name="margin"/>
    /// from the edges. Best-effort: if the area is too crowded the closest valid candidate is used.
    /// </summary>
    public static List<Vector2> PlaceNests(
        int count, Rect area, float minSeparation, float margin, System.Random rng)
    {
        var inner = Shrink(area, margin);
        var positions = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            var best = Vector2.zero;
            float bestDist = -1f;
            int attempts = 100;

            for (int a = 0; a < attempts; a++)
            {
                var candidate = RandomPoint(inner, rng);
                float minDist = MinDistanceTo(candidate, positions);

                if (minDist < 0f) // first nest, no existing points
                {
                    best = candidate;
                    break;
                }

                if (minDist >= minSeparation)
                {
                    best = candidate;
                    break;
                }

                if (minDist > bestDist)
                {
                    bestDist = minDist;
                    best = candidate;
                }
            }

            positions.Add(best);
        }

        return positions;
    }

    // ── BiscuitPlate placement ────────────────────────────────────────────────

    /// <summary>
    /// Returns the position (within <paramref name="area"/> minus <paramref name="margin"/>)
    /// that maximises the minimum Euclidean distance to any nest. Samples
    /// <paramref name="candidates"/> random points and picks the best.
    /// </summary>
    public static Vector2 PlacePlate(
        Rect area, float margin, IReadOnlyList<Vector2> nestPositions, System.Random rng,
        int candidates = 200, float minDistance = 0f)
    {
        var inner = Shrink(area, margin);
        var best = RandomPoint(inner, rng);
        float bestDist = MinDistanceTo(best, nestPositions);

        for (int i = 1; i < candidates; i++)
        {
            var candidate = RandomPoint(inner, rng);
            float d = MinDistanceTo(candidate, nestPositions);
            if (minDistance > 0f && d < minDistance) continue;
            if (d > bestDist)
            {
                bestDist = d;
                best = candidate;
            }
        }

        return best;
    }

    // ── Wall segment generation ───────────────────────────────────────────────

    // A gap in a wall line: centre along the wall axis and the half-width of the opening.
    private readonly struct GapSpec
    {
        public readonly float Centre;
        public readonly float HalfSize;
        public GapSpec(float centre, float halfSize) { Centre = centre; HalfSize = halfSize; }
    }

    public readonly struct WallSegment
    {
        public readonly Vector2 Centre;
        public readonly float AngleDegrees;
        public readonly float Length;

        public WallSegment(Vector2 centre, float angleDegrees, float length)
        {
            Centre = centre;
            AngleDegrees = angleDegrees;
            Length = length;
        }
    }

    public readonly struct Region
    {
        public readonly List<Vector2> SamplePoints;
        public readonly bool ContainsNest;
        public readonly bool ContainsPlate;
        public int CellCount => SamplePoints.Count;

        public Region(List<Vector2> samplePoints, bool containsNest, bool containsPlate)
        {
            SamplePoints = samplePoints;
            ContainsNest = containsNest;
            ContainsPlate = containsPlate;
        }
    }

    /// <summary>
    /// Produces a wall spanning edge-to-edge (or to an existing wall) perpendicular to the
    /// nest→plate line. One strategic gap is placed offset from the midpoint; forced gaps are
    /// added wherever the wall passes near an avoid point; additional random gaps fill out the
    /// density set by <paramref name="gapsPerUnitLength"/>.
    /// </summary>
    public static List<WallSegment> BuildBlockingWall(
        Vector2 nestPos, Vector2 platePos,
        Rect area, float minGapOffset, float gapSize,
        float gapsPerUnitLength, float minAvoidDistance, float minSegmentLength,
        System.Random rng,
        IReadOnlyList<WallSegment> existingWalls = null,
        IReadOnlyList<Vector2> avoidPositions = null)
    {
        var diff = platePos - nestPos;
        var wallDir = new Vector2(-diff.y, diff.x).normalized;
        var midpoint = (nestPos + platePos) * 0.5f;

        var (posExtent, negExtent) = WallExtents(midpoint, wallDir, area);
        ClipToExistingWalls(ref posExtent, ref negExtent, midpoint, wallDir, existingWalls);

        float wallStart  = -negExtent;
        float wallEnd    =  posExtent;
        float wallLength =  posExtent + negExtent;

        // Strategic gap: offset from midpoint so it isn't directly on the nest-plate line.
        bool posValid = posExtent - gapSize * 0.5f >= minGapOffset;
        bool negValid = negExtent - gapSize * 0.5f >= minGapOffset;

        float strategic;
        if (posValid && negValid)
            strategic = rng.NextDouble() > 0.5
                ?  RangeRng(rng, minGapOffset, posExtent - gapSize * 0.5f)
                : -RangeRng(rng, minGapOffset, negExtent - gapSize * 0.5f);
        else if (posValid)
            strategic = RangeRng(rng, minGapOffset, posExtent - gapSize * 0.5f);
        else if (negValid)
            strategic = -RangeRng(rng, minGapOffset, negExtent - gapSize * 0.5f);
        else
            strategic = 0f;

        int totalGaps = Mathf.Max(1, Mathf.RoundToInt(wallLength * gapsPerUnitLength));
        var gaps = new List<GapSpec> { new GapSpec(strategic, gapSize * 0.5f) };
        AddForcedGaps(gaps, midpoint, wallDir, wallStart, wallEnd, gapSize, minAvoidDistance, avoidPositions);
        AddRandomGaps(gaps, Mathf.Max(0, totalGaps - gaps.Count), wallStart, wallEnd, gapSize, rng);

        float wallAngle = Mathf.Atan2(wallDir.y, wallDir.x) * Mathf.Rad2Deg;
        return SegmentsFromGaps(midpoint, wallDir, wallAngle, wallStart, wallEnd, gaps, minSegmentLength);
    }

    /// <summary>
    /// Generates <paramref name="wallDensity"/> ambient walls without existing-wall context.
    /// Used primarily for testing; in-game use goes through BuildWalls.
    /// </summary>
    public static List<WallSegment> BuildAdditionalWalls(
        int wallDensity, Rect area, float gapSize, float gapsPerUnitLength, System.Random rng,
        float minAvoidDistance = 0f, float minSegmentLength = 8f,
        float minParallelAngle = 0f, float minParallelSeparation = 0f,
        IReadOnlyList<Vector2> avoidPositions = null)
    {
        var result = new List<WallSegment>(wallDensity * 4);
        for (int i = 0; i < wallDensity; i++)
            result.AddRange(BuildAmbientWall(area, gapSize, gapsPerUnitLength,
                minAvoidDistance, minSegmentLength, minParallelAngle, minParallelSeparation,
                rng, null, avoidPositions));
        return result;
    }

    // Builds one full-width wall (clipped to existing walls) at a random angle.
    // Forced gaps clear nests/plate; random gaps fill out the density target.
    private static List<WallSegment> BuildAmbientWall(
        Rect area, float gapSize, float gapsPerUnitLength,
        float minAvoidDistance, float minSegmentLength,
        float minParallelAngle, float minParallelSeparation,
        System.Random rng,
        IReadOnlyList<WallSegment> existingWalls,
        IReadOnlyList<Vector2> avoidPositions)
    {
        // Try to find an angle + position that isn't too close and parallel to an existing wall.
        float angleDeg = 0f;
        Vector2 dir = Vector2.right, centre = Vector2.zero;
        var inner = Shrink(area, 10f);
        for (int attempt = 0; attempt < 12; attempt++)
        {
            angleDeg = (float)(rng.NextDouble() * 180.0);
            float rad0 = angleDeg * Mathf.Deg2Rad;
            dir    = new Vector2(Mathf.Cos(rad0), Mathf.Sin(rad0));
            centre = RandomPoint(inner, rng);
            if (!IsTooCloseAndParallel(centre, dir, angleDeg, existingWalls, minParallelAngle, minParallelSeparation))
                break;
        }

        var (posExtent, negExtent) = WallExtents(centre, dir, area);
        ClipToExistingWalls(ref posExtent, ref negExtent, centre, dir, existingWalls);

        float wallStart  = -negExtent;
        float wallEnd    =  posExtent;
        float wallLength =  posExtent + negExtent;

        int targetGaps = Mathf.Max(1, Mathf.RoundToInt(wallLength * gapsPerUnitLength));
        var gaps = new List<GapSpec>(targetGaps);
        AddForcedGaps(gaps, centre, dir, wallStart, wallEnd, gapSize, minAvoidDistance, avoidPositions);
        AddRandomGaps(gaps, Mathf.Max(0, targetGaps - gaps.Count), wallStart, wallEnd, gapSize, rng);

        float wallAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return SegmentsFromGaps(centre, dir, wallAngle, wallStart, wallEnd, gaps, minSegmentLength);
    }

    // Returns true if a wall at (centre, dir, angleDeg) is too close to any existing wall
    // with a similar angle. Uses perpendicular distance between the two infinite lines.
    private static bool IsTooCloseAndParallel(
        Vector2 centre, Vector2 dir, float angleDeg,
        IReadOnlyList<WallSegment> existingWalls,
        float minAngleDiff, float minPerpDist)
    {
        if (existingWalls == null || minAngleDiff <= 0f || minPerpDist <= 0f) return false;

        foreach (var seg in existingWalls)
        {
            float diff = Mathf.Abs(angleDeg - seg.AngleDegrees) % 180f;
            if (diff > 90f) diff = 180f - diff;
            if (diff > minAngleDiff) continue;

            // Perpendicular distance from seg.Centre to the new wall's infinite line.
            var delta = seg.Centre - centre;
            float perpDist = Mathf.Abs(delta.x * dir.y - delta.y * dir.x);
            if (perpDist < minPerpDist) return true;
        }
        return false;
    }

    // Clips posExtent / negExtent so the wall stops where it first hits an existing wall.
    // A small overlap is added so the walls visually join rather than just touching.
    private static void ClipToExistingWalls(
        ref float posExtent, ref float negExtent,
        Vector2 centre, Vector2 dir,
        IReadOnlyList<WallSegment> existingWalls)
    {
        if (existingWalls == null || existingWalls.Count == 0) return;
        const float overlap = 1f;

        foreach (var seg in existingWalls)
        {
            float segRad = seg.AngleDegrees * Mathf.Deg2Rad;
            var segDir = new Vector2(Mathf.Cos(segRad), Mathf.Sin(segRad));
            var delta = seg.Centre - centre;

            float cross = dir.x * segDir.y - dir.y * segDir.x;
            if (Mathf.Abs(cross) < 1e-6f) continue; // parallel — skip

            // t = distance along dir to intersection; s = position along seg
            float t = (delta.x * segDir.y - delta.y * segDir.x) / cross;
            float s = (dir.y  * delta.x   - dir.x  * delta.y)   / cross;

            if (Mathf.Abs(s) > seg.Length * 0.5f) continue; // outside segment bounds

            if (t > overlap)
                posExtent = Mathf.Min(posExtent, t + overlap);
            else if (t < -overlap)
                negExtent = Mathf.Min(negExtent, -t + overlap);
        }
    }

    // Adds a forced gap wherever the wall passes within minAvoidDistance of an avoid point.
    // The gap half-size is the exact geometric clearance needed to keep the wall edge at
    // minAvoidDistance from the point.
    private static void AddForcedGaps(
        List<GapSpec> gaps,
        Vector2 origin, Vector2 dir,
        float wallStart, float wallEnd,
        float gapSize, float minAvoidDistance,
        IReadOnlyList<Vector2> avoidPositions)
    {
        if (avoidPositions == null || minAvoidDistance <= 0f) return;
        float halfGap = gapSize * 0.5f;

        foreach (var pt in avoidPositions)
        {
            float t = Mathf.Clamp(Vector2.Dot(pt - origin, dir), wallStart, wallEnd);
            float dist = Vector2.Distance(pt, origin + dir * t);
            if (dist >= minAvoidDistance) continue;

            float halfRequired = Mathf.Sqrt(Mathf.Max(0f, minAvoidDistance * minAvoidDistance - dist * dist));
            float halfSize = Mathf.Max(halfGap, halfRequired);

            // Skip if an existing gap already covers this point.
            bool covered = false;
            foreach (var g in gaps)
                if (t >= g.Centre - g.HalfSize && t <= g.Centre + g.HalfSize) { covered = true; break; }

            if (!covered)
                gaps.Add(new GapSpec(t, halfSize));
        }
    }

    // Appends up to `count` non-overlapping random gaps; respects existing gap extents.
    private static void AddRandomGaps(List<GapSpec> gaps, int count, float wallStart, float wallEnd, float gapSize, System.Random rng)
    {
        float halfGap   = gapSize * 0.5f;
        float safeStart = wallStart + gapSize;
        float safeEnd   = wallEnd   - gapSize;
        if (safeEnd <= safeStart) return;

        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float t = RangeRng(rng, safeStart, safeEnd);
                bool overlaps = false;
                foreach (var g in gaps)
                    if (t < g.Centre + g.HalfSize + halfGap && t > g.Centre - g.HalfSize - halfGap)
                    { overlaps = true; break; }
                if (!overlaps) { gaps.Add(new GapSpec(t, halfGap)); break; }
            }
        }
    }

    // Converts a list of GapSpecs into solid WallSegments in the spaces between them.
    // Segments shorter than minSegmentLength are dropped; their space becomes part of the gap.
    private static List<WallSegment> SegmentsFromGaps(
        Vector2 origin, Vector2 dir, float angleDeg,
        float wallStart, float wallEnd, List<GapSpec> gaps,
        float minSegmentLength = 8f)
    {
        gaps.Sort((a, b) => a.Centre.CompareTo(b.Centre));

        var bounds = new List<float> { wallStart };
        foreach (var g in gaps)
        {
            bounds.Add(g.Centre - g.HalfSize);
            bounds.Add(g.Centre + g.HalfSize);
        }
        bounds.Add(wallEnd);

        var result = new List<WallSegment>(bounds.Count / 2);
        for (int i = 0; i < bounds.Count - 1; i += 2)
        {
            float start = bounds[i];
            float end   = bounds[i + 1];
            float len   = end - start;
            if (len >= minSegmentLength)
                result.Add(new WallSegment(origin + dir * ((start + end) * 0.5f), angleDeg, len));
        }
        return result;
    }

    // ── Edge walls ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns four WallSegments forming a rectangular perimeter around <paramref name="area"/>.
    /// Adding these to the wall list before placing internal walls lets the parallel-separation
    /// and clip-to-wall rules treat the boundary the same as any other wall.
    /// </summary>
    public static List<WallSegment> BuildEdgeWalls(Rect area, float wallWidth = 0f)
    {
        float cx = area.center.x, cy = area.center.y;
        float hw = wallWidth * 0.5f;
        // Each wall shifts its centre by halfWidth in the clockwise direction around the
        // perimeter so adjacent walls butt against each other without corner gaps or overlaps.
        return new List<WallSegment>
        {
            new WallSegment(new Vector2(cx - hw,        area.yMin), 0f,  area.width),  // bottom → left
            new WallSegment(new Vector2(cx + hw,        area.yMax), 0f,  area.width),  // top    → right
            new WallSegment(new Vector2(area.xMin, cy + hw),        90f, area.height), // left   → up
            new WallSegment(new Vector2(area.xMax, cy - hw),        90f, area.height), // right  → down
        };
    }

    // ── Combined wall build ───────────────────────────────────────────────────

    /// <summary>
    /// Builds all walls for a level.
    /// Blocking walls fire automatically when dist(nest, plate) &lt; blockingWallThreshold.
    /// wallDensity controls ambient wall count. Both use gapsPerUnitLength for gap density.
    /// Each new wall is clipped against already-placed walls so it can end at an existing wall.
    /// When <paramref name="minIsolatedRegionCells"/> &gt; 0, walls that would create a small
    /// isolated region (not containing a nest or plate) are rejected.
    /// </summary>
    public static List<WallSegment> BuildWalls(
        List<Vector2> nestPositions, Vector2 platePos2D,
        Rect area, int wallDensity,
        float minGapOffset, float gapSize,
        float gapsPerUnitLength, float minAvoidDistance, float minSegmentLength,
        float minParallelAngle, float minParallelSeparation,
        float connectivityCellSize, float blockingWallThreshold,
        System.Random rng,
        float wallWidth = 0f,
        int minIsolatedRegionCells = 0)
    {
        var walls = new List<WallSegment>(BuildEdgeWalls(area, wallWidth));
        var avoidPositions = new List<Vector2>(nestPositions) { platePos2D };

        // Blocking walls: automatic when a nest is close enough to the plate.
        foreach (var nest in nestPositions)
        {
            if (Vector2.Distance(nest, platePos2D) >= blockingWallThreshold) continue;

            List<WallSegment> candidate = null;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                candidate = BuildBlockingWall(
                    nest, platePos2D, area, minGapOffset, gapSize,
                    gapsPerUnitLength, minAvoidDistance, minSegmentLength, rng, walls, avoidPositions);

                var combined = new List<WallSegment>(walls);
                combined.AddRange(candidate);

                bool connected = IsConnected(nestPositions, platePos2D, combined, area, connectivityCellSize);
                bool noSmallRegion = minIsolatedRegionCells <= 0 ||
                    !HasSmallIsolatedRegion(
                        BuildRegions(combined, area, connectivityCellSize, nestPositions, platePos2D),
                        minIsolatedRegionCells);

                if (connected && noSmallRegion)
                    break;

                candidate = null;
            }

            if (candidate != null)
                walls.AddRange(candidate);
        }

        // Ambient walls: built one at a time so each sees previously placed walls.
        for (int i = 0; i < wallDensity; i++)
        {
            var segs = BuildAmbientWall(area, gapSize, gapsPerUnitLength,
                minAvoidDistance, minSegmentLength, minParallelAngle, minParallelSeparation,
                rng, walls, avoidPositions);
            var testList = new List<WallSegment>(walls);
            testList.AddRange(segs);

            bool connected = IsConnected(nestPositions, platePos2D, testList, area, connectivityCellSize);
            bool noSmallRegion = minIsolatedRegionCells <= 0 ||
                !HasSmallIsolatedRegion(
                    BuildRegions(testList, area, connectivityCellSize, nestPositions, platePos2D),
                    minIsolatedRegionCells);

            if (connected && noSmallRegion)
                walls.AddRange(segs);
        }

        return walls;
    }

    // ── Cluster placement ─────────────────────────────────────────────────────

    public struct BushCluster
    {
        public readonly Vector2 Centre;
        public readonly List<Vector2> BushPositions;
        public readonly List<Vector2> BoundaryPolygon;
        public BushCluster(Vector2 centre, List<Vector2> bushPositions, List<Vector2> boundaryPolygon)
        {
            Centre = centre;
            BushPositions = bushPositions;
            BoundaryPolygon = boundaryPolygon;
        }
        public int BushCount => BushPositions?.Count ?? 0;
    }

    /// <summary>
    /// Returns the polygon formed by clipping a circle of <paramref name="radius"/> centred at
    /// <paramref name="centre"/> against each wall's infinite half-plane. The result is a convex
    /// polygon in 2D (XZ world space), suitable for mesh generation.
    /// </summary>
    public static List<Vector2> BuildClusterPolygon(
        Vector2 centre, float radius, IReadOnlyList<WallSegment> walls, int segments = 48)
    {
        var polygon = new List<Vector2>(segments);
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            polygon.Add(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        if (walls == null) return polygon;

        foreach (var wall in walls)
        {
            if (polygon.Count == 0) break;

            float wallRad = wall.AngleDegrees * Mathf.Deg2Rad;
            var wallNormal = new Vector2(-Mathf.Sin(wallRad), Mathf.Cos(wallRad));

            if (!WallSegmentReachesCircle(wall, centre, radius)) continue;

            float centreSignedDist = Vector2.Dot(centre - wall.Centre, wallNormal);
            if (Mathf.Abs(centreSignedDist) < 0.01f) continue; // centre on the wall

            polygon = ClipPolygonByHalfPlane(polygon, wall.Centre, wallNormal,
                Mathf.Sign(centreSignedDist));
        }

        return polygon;
    }

    // True if the finite wall segment's closest point to centre is within radius.
    // Prevents infinite-line extensions of short walls from clipping distant regions.
    private static bool WallSegmentReachesCircle(WallSegment wall, Vector2 centre, float radius)
    {
        float wallRad = wall.AngleDegrees * Mathf.Deg2Rad;
        var wallDir   = new Vector2(Mathf.Cos(wallRad), Mathf.Sin(wallRad));
        float t       = Mathf.Clamp(Vector2.Dot(centre - wall.Centre, wallDir), -wall.Length * 0.5f, wall.Length * 0.5f);
        return Vector2.Distance(centre, wall.Centre + wallDir * t) < radius;
    }

    private static List<Vector2> ClipPolygonByHalfPlane(
        List<Vector2> polygon, Vector2 linePoint, Vector2 lineNormal, float keepSide)
    {
        var output = new List<Vector2>(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var next    = polygon[(i + 1) % polygon.Count];

            float dCurrent = Vector2.Dot(current - linePoint, lineNormal) * keepSide;
            float dNext    = Vector2.Dot(next    - linePoint, lineNormal) * keepSide;

            if (dCurrent >= 0f) output.Add(current);

            // Add intersection point when edge crosses the clip line.
            if ((dCurrent >= 0f) != (dNext >= 0f))
            {
                float t = dCurrent / (dCurrent - dNext);
                output.Add(current + t * (next - current));
            }
        }
        return output;
    }

    /// <summary>
    /// Distributes cluster centres across <paramref name="regions"/> proportionally by area,
    /// sampling a radius for each centre from the same RNG stream. Same-region clusters are
    /// rejected when their circles overlap by more than <paramref name="maxOverlap"/>.
    /// Clusters in different regions are not checked — walls clip them naturally.
    /// </summary>
    public static (List<Vector2> Centres, float[] Radii) PlaceClusterCentres(
        IReadOnlyList<Region> regions,
        int totalClusterCount,
        float minAvoidDistance,
        IReadOnlyList<Vector2> avoidPositions,
        float baseRadius,
        float radiusVariation,
        float maxOverlap,
        System.Random rng)
    {
        int totalCells = 0;
        foreach (var r in regions) totalCells += r.CellCount;
        if (totalCells == 0) return (new List<Vector2>(), new float[0]);

        var centres = new List<Vector2>();
        var radii   = new List<float>();

        foreach (var region in regions)
        {
            float expected = (float)totalClusterCount * region.CellCount / totalCells;
            int count = Mathf.FloorToInt(expected);
            if (rng.NextDouble() < (expected - count)) count++;

            var regionCentres = new List<Vector2>();
            var regionRadii   = new List<float>();

            for (int i = 0; i < count; i++)
            {
                float radius = baseRadius * (1f + (float)(rng.NextDouble() * 2.0 - 1.0) * radiusVariation);

                int maxAttempts = Mathf.Min(region.SamplePoints.Count, 50);
                for (int a = 0; a < maxAttempts; a++)
                {
                    var candidate = region.SamplePoints[rng.Next(region.SamplePoints.Count)];

                    float dist = MinDistanceTo(candidate, avoidPositions);
                    if (minAvoidDistance > 0f && dist >= 0f && dist < minAvoidDistance) continue;

                    bool overlaps = false;
                    for (int j = 0; j < regionCentres.Count; j++)
                    {
                        if (Vector2.Distance(candidate, regionCentres[j]) < regionRadii[j] + radius - maxOverlap)
                        { overlaps = true; break; }
                    }
                    if (overlaps) continue;

                    regionCentres.Add(candidate);
                    regionRadii.Add(radius);
                    centres.Add(candidate);
                    radii.Add(radius);
                    break;
                }
            }
        }

        return (centres, radii.ToArray());
    }

    /// <summary>
    /// For each pre-determined cluster centre, places bushes inside the cluster polygon.
    /// Count is derived from <paramref name="densityPerBaseArea"/> scaled by the polygon's actual
    /// area relative to the base circle, so clipped clusters get proportionally fewer bushes.
    /// Lloyd relaxation spreads positions evenly after initial random sampling.
    /// </summary>
    public static List<BushCluster> PlaceClusterBushes(
        IReadOnlyList<Vector2> centres,
        IReadOnlyList<float> clusterRadii,
        IReadOnlyList<WallSegment> walls,
        float wallSetback,
        int densityPerBaseArea,
        float baseRadius,
        float minBushSeparation,
        float minAvoidDistance,
        IReadOnlyList<Vector2> avoidPositions,
        System.Random rng)
    {
        var result = new List<BushCluster>();
        float baseArea = Mathf.PI * baseRadius * baseRadius;

        for (int ci = 0; ci < centres.Count; ci++)
        {
            var centre = centres[ci];
            float radius = clusterRadii[ci];

            var polygon = BuildClusterPolygon(centre, radius, walls);
            if (polygon.Count < 3) continue;

            // Derive bush count from actual polygon area so clipped clusters stay proportionally dense.
            int bushCount = Mathf.Max(1, Mathf.RoundToInt(densityPerBaseArea * PolygonArea(polygon) / baseArea));

            var aabb = PolygonAABB(polygon);
            var bushPositions = new List<Vector2>(bushCount);

            for (int a = 0; a < bushCount * 20 && bushPositions.Count < bushCount; a++)
            {
                var candidate = new Vector2(
                    aabb.xMin + (float)rng.NextDouble() * aabb.width,
                    aabb.yMin + (float)rng.NextDouble() * aabb.height);

                if (!IsInsideConvexPolygon(candidate, polygon)) continue;

                if (wallSetback > 0f && walls != null)
                {
                    bool tooClose = false;
                    foreach (var wall in walls)
                    {
                        if (!WallSegmentReachesCircle(wall, centre, radius + wallSetback)) continue;
                        float wallRad = wall.AngleDegrees * Mathf.Deg2Rad;
                        var wallNormal = new Vector2(-Mathf.Sin(wallRad), Mathf.Cos(wallRad));
                        if (Mathf.Abs(Vector2.Dot(candidate - wall.Centre, wallNormal)) < wallSetback)
                        { tooClose = true; break; }
                    }
                    if (tooClose) continue;
                }

                if (minAvoidDistance > 0f && avoidPositions != null)
                {
                    bool tooClose = false;
                    foreach (var avoid in avoidPositions)
                        if (Vector2.Distance(candidate, avoid) < minAvoidDistance)
                        { tooClose = true; break; }
                    if (tooClose) continue;
                }

                bushPositions.Add(candidate);
            }

            // Guarantee at least one bush even when constraints leave no valid spots.
            if (bushPositions.Count == 0)
            {
                for (int a = 0; a < 100 && bushPositions.Count == 0; a++)
                {
                    var candidate = new Vector2(
                        aabb.xMin + (float)rng.NextDouble() * aabb.width,
                        aabb.yMin + (float)rng.NextDouble() * aabb.height);
                    if (IsInsideConvexPolygon(candidate, polygon))
                        bushPositions.Add(candidate);
                }
            }

            if (bushPositions.Count == 0) continue;

            RelaxBushPositions(bushPositions, polygon, minBushSeparation, avoidPositions, minAvoidDistance);

            result.Add(new BushCluster(centre, bushPositions, polygon));
        }

        return result;
    }

    // Spreads bush positions evenly using Lloyd-style force relaxation, clamped to the polygon.
    private static void RelaxBushPositions(
        List<Vector2> positions, IReadOnlyList<Vector2> polygon,
        float minSeparation, IReadOnlyList<Vector2> avoidPositions, float avoidDistance,
        int iterations = 15)
    {
        float influence = minSeparation * 3f;
        float step      = minSeparation * 0.4f;

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                var push = Vector2.zero;

                for (int j = 0; j < positions.Count; j++)
                {
                    if (i == j) continue;
                    var diff = positions[i] - positions[j];
                    float dist = Mathf.Max(diff.magnitude, 0.01f);
                    if (dist < influence)
                        push += diff / dist * (influence - dist);
                }

                if (avoidPositions != null && avoidDistance > 0f)
                {
                    foreach (var avoid in avoidPositions)
                    {
                        var diff = positions[i] - avoid;
                        float dist = Mathf.Max(diff.magnitude, 0.01f);
                        if (dist < avoidDistance)
                            push += diff / dist * (avoidDistance - dist);
                    }
                }

                if (push.sqrMagnitude < 0.0001f) continue;
                positions[i] = ClampToConvexPolygon(positions[i] + push.normalized * step, polygon);
            }
        }
    }

    private static bool IsInsideConvexPolygon(Vector2 point, IReadOnlyList<Vector2> poly)
    {
        for (int i = 0; i < poly.Count; i++)
        {
            var edge = poly[(i + 1) % poly.Count] - poly[i];
            var toPoint = point - poly[i];
            if (edge.x * toPoint.y - edge.y * toPoint.x < 0f) return false;
        }
        return true;
    }

    private static Vector2 ClampToConvexPolygon(Vector2 point, IReadOnlyList<Vector2> poly)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < poly.Count; i++)
            {
                var a    = poly[i];
                var edge = poly[(i + 1) % poly.Count] - a;
                var toPoint = point - a;
                if (edge.x * toPoint.y - edge.y * toPoint.x < 0f)
                {
                    float edgeLenSq = edge.sqrMagnitude;
                    float t = edgeLenSq > 0f ? Mathf.Clamp01(Vector2.Dot(toPoint, edge) / edgeLenSq) : 0f;
                    point = a + edge * t;
                }
            }
        }
        return point;
    }

    private static float PolygonArea(IReadOnlyList<Vector2> poly)
    {
        float area = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            var a = poly[i]; var b = poly[(i + 1) % poly.Count];
            area += a.x * b.y - b.x * a.y;
        }
        return Mathf.Abs(area) * 0.5f;
    }

    private static Rect PolygonAABB(IReadOnlyList<Vector2> poly)
    {
        float minX = poly[0].x, maxX = poly[0].x, minY = poly[0].y, maxY = poly[0].y;
        for (int i = 1; i < poly.Count; i++)
        {
            if (poly[i].x < minX) minX = poly[i].x; else if (poly[i].x > maxX) maxX = poly[i].x;
            if (poly[i].y < minY) minY = poly[i].y; else if (poly[i].y > maxY) maxY = poly[i].y;
        }
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    // ── Region decomposition ──────────────────────────────────────────────────

    /// <summary>
    /// BFS flood-fill decomposition of the play area into connected regions bounded by walls.
    /// Each region records whether it contains a nest or the plate position.
    /// <paramref name="cellSize"/> is the grid resolution used for both blocking and BFS.
    /// </summary>
    public static List<Region> BuildRegions(
        IReadOnlyList<WallSegment> walls, Rect area, float cellSize,
        IReadOnlyList<Vector2> nestPositions, Vector2 platePos)
    {
        int cols = Mathf.CeilToInt(area.width  / cellSize);
        int rows = Mathf.CeilToInt(area.height / cellSize);
        bool[,] blocked = BuildBlockedGrid(walls, area, cellSize, cols, rows);

        // Pre-compute nest cells for fast lookup.
        var nestCells = new HashSet<(int, int)>();
        if (nestPositions != null)
            foreach (var nest in nestPositions)
                nestCells.Add(WorldToCell(nest, area, cellSize, cols, rows));

        var plateCell = WorldToCell(platePos, area, cellSize, cols, rows);

        bool[,] visited = new bool[cols, rows];
        var regions = new List<Region>();

        int[] dx = { 1, -1, 0, 0, 1, -1, 1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, 1, -1, -1 };

        for (int sx = 0; sx < cols; sx++)
        {
            for (int sy = 0; sy < rows; sy++)
            {
                if (visited[sx, sy] || blocked[sx, sy]) continue;

                var samplePoints  = new List<Vector2>();
                bool containsNest  = false;
                bool containsPlate = false;
                var queue = new Queue<(int, int)>();
                queue.Enqueue((sx, sy));
                visited[sx, sy] = true;

                while (queue.Count > 0)
                {
                    var (cx, cy) = queue.Dequeue();
                    samplePoints.Add(CellToWorld(cx, cy, area, cellSize));

                    if (nestCells.Contains((cx, cy))) containsNest  = true;
                    if (cx == plateCell.x && cy == plateCell.y) containsPlate = true;

                    for (int d = 0; d < 8; d++)
                    {
                        int nx = cx + dx[d], ny = cy + dy[d];
                        if (nx < 0 || nx >= cols || ny < 0 || ny >= rows) continue;
                        if (visited[nx, ny] || blocked[nx, ny]) continue;
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }

                regions.Add(new Region(samplePoints, containsNest, containsPlate));
            }
        }

        return regions;
    }

    private static bool HasSmallIsolatedRegion(IReadOnlyList<Region> regions, int minCells)
    {
        foreach (var r in regions)
            if (!r.ContainsNest && !r.ContainsPlate && r.CellCount < minCells)
                return true;
        return false;
    }

    // ── Connectivity check ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if there is a traversable path (on a 2D grid) from every nest position
    /// to the plate position, given the wall segments that block passage.
    /// <paramref name="cellSize"/> is the grid resolution in world units.
    /// </summary>
    public static bool IsConnected(
        IReadOnlyList<Vector2> nestPositions, Vector2 platePosition,
        IReadOnlyList<WallSegment> walls, Rect area, float cellSize)
    {
        int cols = Mathf.CeilToInt(area.width  / cellSize);
        int rows = Mathf.CeilToInt(area.height / cellSize);
        bool[,] blocked = BuildBlockedGrid(walls, area, cellSize, cols, rows);

        var plateCell = WorldToCell(platePosition, area, cellSize, cols, rows);

        foreach (var nest in nestPositions)
        {
            var nestCell = WorldToCell(nest, area, cellSize, cols, rows);
            if (!BfsConnected(nestCell, plateCell, blocked, cols, rows))
                return false;
        }

        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Rect Shrink(Rect r, float margin) =>
        new Rect(r.x + margin, r.y + margin, r.width - margin * 2, r.height - margin * 2);

    private static Vector2 RandomPoint(Rect r, System.Random rng) =>
        new Vector2(
            r.x + (float)(rng.NextDouble() * r.width),
            r.y + (float)(rng.NextDouble() * r.height));

    private static float MinDistanceTo(Vector2 point, IReadOnlyList<Vector2> others)
    {
        if (others == null || others.Count == 0) return -1f;
        float min = float.MaxValue;
        foreach (var o in others)
            min = Mathf.Min(min, Vector2.Distance(point, o));
        return min;
    }

    private static float RangeRng(System.Random rng, float min, float max) =>
        (float)(rng.NextDouble() * (max - min) + min);

    // Extents from centre along +wallDir and -wallDir to the nearest area boundary.
    private static (float pos, float neg) WallExtents(Vector2 centre, Vector2 wallDir, Rect area)
    {
        float posExtent = float.MaxValue, negExtent = float.MaxValue;

        void Consider(float t)
        {
            if (t > 1e-4f) posExtent = Mathf.Min(posExtent, t);
            else if (t < -1e-4f) negExtent = Mathf.Min(negExtent, -t);
        }

        if (Mathf.Abs(wallDir.x) > 1e-6f)
        {
            Consider((area.xMin - centre.x) / wallDir.x);
            Consider((area.xMax - centre.x) / wallDir.x);
        }
        if (Mathf.Abs(wallDir.y) > 1e-6f)
        {
            Consider((area.yMin - centre.y) / wallDir.y);
            Consider((area.yMax - centre.y) / wallDir.y);
        }

        float pos = posExtent == float.MaxValue ? 50f : posExtent;
        float neg = negExtent == float.MaxValue ? 50f : negExtent;
        return (Mathf.Max(pos, 5f), Mathf.Max(neg, 5f));
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    // ── BFS helpers ───────────────────────────────────────────────────────────

    private static bool[,] BuildBlockedGrid(
        IReadOnlyList<WallSegment> walls, Rect area, float cellSize, int cols, int rows)
    {
        bool[,] blocked = new bool[cols, rows];
        foreach (var wall in walls)
            MarkWall(wall, area, cellSize, cols, rows, blocked);
        return blocked;
    }

    private static (int x, int y) WorldToCell(Vector2 world, Rect area, float cellSize, int cols, int rows)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((world.x - area.xMin) / cellSize), 0, cols - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((world.y - area.yMin) / cellSize), 0, rows - 1);
        return (x, y);
    }

    private static Vector2 CellToWorld(int cx, int cy, Rect area, float cellSize) =>
        new Vector2(area.xMin + (cx + 0.5f) * cellSize, area.yMin + (cy + 0.5f) * cellSize);

    private static void MarkWall(WallSegment wall, Rect area, float cellSize, int cols, int rows, bool[,] blocked)
    {
        float rad = wall.AngleDegrees * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        float halfLen = wall.Length * 0.5f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(wall.Length / (cellSize * 0.5f)));

        for (int s = 0; s <= steps; s++)
        {
            float t = -halfLen + s * (wall.Length / steps);
            var pt = wall.Centre + dir * t;
            var cell = WorldToCell(pt, area, cellSize, cols, rows);
            if (cell.x >= 0 && cell.x < cols && cell.y >= 0 && cell.y < rows)
                blocked[cell.x, cell.y] = true;
        }
    }

    private static bool BfsConnected((int x, int y) from, (int x, int y) to, bool[,] blocked, int cols, int rows)
    {
        if (blocked[from.x, from.y] || blocked[to.x, to.y]) return false;
        if (from == to) return true;

        var visited = new bool[cols, rows];
        var queue = new Queue<(int, int)>();
        queue.Enqueue(from);
        visited[from.x, from.y] = true;

        int[] dx = { 1, -1, 0, 0, 1, -1, 1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, 1, -1, -1 };

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            if (cx == to.x && cy == to.y) return true;

            for (int d = 0; d < 8; d++)
            {
                int nx = cx + dx[d], ny = cy + dy[d];
                if (nx < 0 || nx >= cols || ny < 0 || ny >= rows) continue;
                if (visited[nx, ny] || blocked[nx, ny]) continue;
                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }

        return false;
    }
}
